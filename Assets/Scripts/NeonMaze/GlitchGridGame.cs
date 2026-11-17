using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NeonMaze
{
    // GLITCH GRID：ネオン迷路でドットを集め、追っ手から逃げる。
    // 迷路・キャラクター・UIは実行時に組み立てる（外部アセットなし）
    public class GlitchGridGame : MonoBehaviour
    {
        public enum State { Ready, Playing, Dying, Clear, GameOver }

        const string BestKey = "GlitchGrid.Best";

        [Header("速さ（マス/秒）")]
        public float playerSpeed = 5f;
        public float chaserSpeed = 4f;
        public float scrambledSpeed = 2.4f;

        [Header("ルール")]
        public float powerDuration = 6f;
        public float firstRelease = 1.5f;
    public float releaseInterval = 2.5f;
        public float respawnTime = 3f;
        public int startLives = 3;
        public int dotScore = 10;
        public int powerScore = 50;
        public int chaserScore = 250;

        [Header("状態（CLIで読む）")]
        public State state;
        public int score;
        public int best;
        public int lives;
        public int dotsLeft;
        public float powerTimer;

        MazeGrid grid;
        readonly GridMover player = new GridMover();
        Vector2Int desiredDir;
        Transform playerTf;
        readonly List<Chaser> chasers = new List<Chaser>();
        readonly Dictionary<Vector2Int, GameObject> pellets = new Dictionary<Vector2Int, GameObject>();
        readonly HashSet<Vector2Int> powerCells = new HashSet<Vector2Int>();
        readonly System.Random random = new System.Random();
        Transform pelletRoot;
        float stateTimer;
        float playTime;
        bool rWasPressed;
        Text scoreText, bestText, livesText, messageText;

        class Chaser
        {
            public string name;
            public int behavior;
            public Color color;
            public Vector2Int start;
            public Vector2Int corner;
            public readonly GridMover mover = new GridMover();
            public bool released;
            public bool scrambled;
            public float respawnTimer;
            public Transform tf;
            public Renderer renderer;
            public Material material;
            public Light light;
        }

        public Vector2Int PlayerCell => player.cell;

        void Awake()
        {
            grid = new MazeGrid(MazeGrid.DefaultLayout);
            SetupCameraAndLight();
            BuildMaze();
            BuildActors();
            BuildUI();
            best = PlayerPrefs.GetInt(BestKey, 0);
            NewGame();
        }

        void Update()
        {
            var kb = Keyboard.current;
            bool rPressed = kb != null && kb.rKey.isPressed;
            if (rPressed && !rWasPressed) NewGame();
            rWasPressed = rPressed;

            float dt = Time.deltaTime;
            switch (state)
            {
                case State.Ready:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) state = State.Playing;
                    break;
                case State.Playing:
                    TickPlaying(dt);
                    break;
                case State.Dying:
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        if (lives <= 0) state = State.GameOver;
                        else ResetRound();
                    }
                    break;
            }
            UpdateVisuals();
            UpdateUI();
        }

        // ---- 進行 ----

        public void NewGame()
        {
            score = 0;
            lives = startLives;
            RebuildPellets();
            ResetRound();
        }

        void ResetRound()
        {
            player.Place(grid.playerStart);
            player.speed = playerSpeed;
            desiredDir = Vector2Int.zero;
            foreach (var c in chasers)
            {
                c.mover.Place(c.start);
                c.released = false;
                c.scrambled = false;
                c.respawnTimer = 0f;
            }
            powerTimer = 0f;
            playTime = 0f;
            state = State.Ready;
            stateTimer = 1.5f;
        }

        void TickPlaying(float dt)
        {
            playTime += dt;
            ReadInput();
            if (desiredDir == -player.dir && player.dir != Vector2Int.zero) player.Reverse(grid);
            player.speed = playerSpeed;
            player.Step(dt, grid, DecidePlayer, OnPlayerArrive);
            if (state != State.Playing) return;

            if (powerTimer > 0f)
            {
                powerTimer -= dt;
                if (powerTimer <= 0f)
                {
                    powerTimer = 0f;
                    foreach (var c in chasers) c.scrambled = false;
                }
            }

            for (int i = 0; i < chasers.Count; i++) TickChaser(chasers[i], i, dt);
            CheckCollisions();
        }

        void ReadInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.wKey.isPressed) desiredDir = Vector2Int.up;
            if (kb.sKey.isPressed) desiredDir = Vector2Int.down;
            if (kb.aKey.isPressed) desiredDir = Vector2Int.left;
            if (kb.dKey.isPressed) desiredDir = Vector2Int.right;
        }

        Vector2Int DecidePlayer()
        {
            if (desiredDir != Vector2Int.zero && !grid.IsWall(player.cell + desiredDir)) return desiredDir;
            if (player.dir != Vector2Int.zero && !grid.IsWall(player.cell + player.dir)) return player.dir;
            return Vector2Int.zero;
        }

        void OnPlayerArrive(Vector2Int cell)
        {
            if (!pellets.TryGetValue(cell, out var pellet)) return;
            Destroy(pellet);
            pellets.Remove(cell);
            dotsLeft--;
            if (powerCells.Contains(cell))
            {
                score += powerScore;
                StartPower();
            }
            else
            {
                score += dotScore;
            }
            if (dotsLeft <= 0)
            {
                state = State.Clear;
                SaveBest();
            }
        }

        void StartPower()
        {
            powerTimer = powerDuration;
            foreach (var c in chasers)
            {
                if (!c.released || c.respawnTimer > 0f) continue;
                c.scrambled = true;
                c.mover.Reverse(grid);
            }
        }

        void TickChaser(Chaser c, int index, float dt)
        {
            if (!c.released)
            {
                if (playTime < firstRelease + index * releaseInterval) return;
                c.released = true;
            }
            if (c.respawnTimer > 0f)
            {
                c.respawnTimer -= dt;
                if (c.respawnTimer > 0f) return;
            }
            c.mover.speed = c.scrambled ? scrambledSpeed : chaserSpeed;
            var rnd = c.scrambled || c.behavior == 3 ? random : null;
            c.mover.Step(dt, grid, () => grid.ChooseDirection(c.mover.cell, c.mover.dir, TargetFor(c), rnd), null);
        }

        Vector2Int TargetFor(Chaser c)
        {
            switch (c.behavior)
            {
                case 1: // 先回り：自機の進む先3マス
                    var facing = player.dir != Vector2Int.zero ? player.dir : desiredDir;
                    return player.cell + facing * 3;
                case 2: // 見張り：離れている間は自分の隅を巡回し、6マス以内に来たら追う
                    return (c.mover.cell - player.cell).sqrMagnitude > 36 ? c.corner : player.cell;
                default: // 追跡
                    return player.cell;
            }
        }

        void CheckCollisions()
        {
            var p = player.GridPosition;
            foreach (var c in chasers)
            {
                if (!c.released || c.respawnTimer > 0f) continue;
                if (Vector2.Distance(c.mover.GridPosition, p) >= 0.6f) continue;
                if (c.scrambled)
                {
                    score += chaserScore;
                    c.scrambled = false;
                    c.mover.Place(c.start);
                    c.respawnTimer = respawnTime;
                }
                else
                {
                    lives--;
                    state = State.Dying;
                    stateTimer = 1.2f;
                    SaveBest();
                    return;
                }
            }
        }

        void SaveBest()
        {
            if (score <= best) return;
            best = score;
            PlayerPrefs.SetInt(BestKey, best);
        }

        // ---- 見た目 ----

        void UpdateVisuals()
        {
            float dying = state == State.Dying ? Mathf.Clamp01(stateTimer / 1.2f) : 1f;
            playerTf.position = grid.ToWorld(player.GridPosition, 0.5f);
            playerTf.localScale = Vector3.one * 0.7f * dying;
            if (player.dir != Vector2Int.zero)
            {
                playerTf.rotation = Quaternion.LookRotation(new Vector3(player.dir.x, 0f, player.dir.y));
            }

            bool flash = powerTimer > 0f && powerTimer < 2f && Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
            foreach (var c in chasers)
            {
                bool visible = c.respawnTimer <= 0f && state != State.Clear;
                c.renderer.enabled = visible;
                c.light.enabled = visible;
                float bob = c.released ? 0f : Mathf.Sin(Time.time * 4f + c.behavior) * 0.1f;
                c.tf.position = grid.ToWorld(c.mover.GridPosition, 0.5f + bob);
                c.tf.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
                Color color = c.scrambled ? (flash ? Color.white : new Color(0.45f, 0.45f, 0.5f)) : c.color;
                c.material.color = color * 0.35f;
                c.material.SetColor("_EmissionColor", color * (c.scrambled ? 0.7f : 1.05f));
                c.light.color = color;
            }

            float pulse = 0.42f + Mathf.Sin(Time.time * 6f) * 0.08f;
            foreach (var cell in powerCells)
            {
                if (pellets.TryGetValue(cell, out var go)) go.transform.localScale = Vector3.one * pulse;
            }
        }

        void UpdateUI()
        {
            scoreText.text = "SCORE " + score;
            bestText.text = "BEST " + best;
            livesText.text = new string('♥', Mathf.Max(lives, 0));
            switch (state)
            {
                case State.Ready: messageText.text = "GLITCH GRID\nREADY"; break;
                case State.Clear: messageText.text = "STAGE CLEAR!\nRでもう一度"; break;
                case State.GameOver: messageText.text = "GAME OVER\nRでリスタート"; break;
                default: messageText.text = ""; break;
            }
        }

        // ---- 組み立て ----

        void SetupCameraAndLight()
        {
            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 9.2f;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 20f, -0.3f), Quaternion.Euler(90f, 0f, 0f));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0f, 0.03f);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.02f, 0.05f);
            // 空の映り込みで床や壁が灰色に見えないようにする
            RenderSettings.reflectionIntensity = 0f;
            var sun = FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional) sun.intensity = 0.08f;
        }

        void BuildMaze()
        {
            var root = new GameObject("Maze").transform;
            var floorMat = Neon(new Color(0.02f, 0.01f, 0.05f), 0f);
            var fillMat = Neon(new Color(0.07f, 0.02f, 0.12f), 0.15f);
            var edgeMat = Neon(new Color(0.8f, 0.25f, 1f), 1.8f);

            Prim(PrimitiveType.Cube, "Floor", root, grid.ToWorld(new Vector2((grid.width - 1) * 0.5f, (grid.height - 1) * 0.5f), -0.05f),
                new Vector3(grid.width, 0.1f, grid.height), floorMat);

            // 壁は暗い塊にし、通路に面した辺だけネオンの線で縁取る
            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!grid.IsWall(cell)) continue;
                    var center = grid.ToWorld(cell, 0.2f);
                    Prim(PrimitiveType.Cube, "Wall", root, center, new Vector3(1f, 0.4f, 1f), fillMat);
                    foreach (var d in MazeGrid.Directions)
                    {
                        var n = cell + d;
                        if (grid.IsWall(n)) continue;
                        var pos = center + new Vector3(d.x * 0.46f, 0.22f, d.y * 0.46f);
                        var scale = d.x != 0 ? new Vector3(0.08f, 0.05f, 1f) : new Vector3(1f, 0.05f, 0.08f);
                        Prim(PrimitiveType.Cube, "Edge", root, pos, scale, edgeMat);
                    }
                }
            }
        }

        void RebuildPellets()
        {
            if (pelletRoot != null) Destroy(pelletRoot.gameObject);
            pelletRoot = new GameObject("Pellets").transform;
            pellets.Clear();
            powerCells.Clear();
            var dotMat = Neon(new Color(1f, 0.85f, 0.6f), 1.4f);
            var powerMat = Neon(new Color(1f, 0.95f, 0.35f), 2.2f);
            foreach (var cell in grid.dots)
            {
                pellets[cell] = Prim(PrimitiveType.Sphere, "Dot", pelletRoot, grid.ToWorld(cell, 0.35f), Vector3.one * 0.18f, dotMat);
            }
            foreach (var cell in grid.powers)
            {
                pellets[cell] = Prim(PrimitiveType.Sphere, "Power", pelletRoot, grid.ToWorld(cell, 0.4f), Vector3.one * 0.42f, powerMat);
                powerCells.Add(cell);
            }
            dotsLeft = pellets.Count;
        }

        void BuildActors()
        {
            var playerColor = new Color(0.2f, 1f, 0.95f);
            playerTf = Prim(PrimitiveType.Sphere, "Player", null, Vector3.zero, Vector3.one * 0.7f, Neon(playerColor, 1.5f)).transform;
            Prim(PrimitiveType.Cube, "Visor", playerTf, new Vector3(0f, 0.15f, 0.42f), new Vector3(0.5f, 0.15f, 0.2f), Neon(Color.white, 1.2f))
                .transform.localPosition = new Vector3(0f, 0.15f, 0.42f);
            AddLight(playerTf.gameObject, playerColor, 3.5f, 2f);

            var specs = new[]
            {
                ("Hunter", new Color(1f, 0.1f, 0.55f), new Vector2Int(grid.width - 2, grid.height - 2)),
                ("Ambusher", new Color(1f, 0.55f, 0.05f), new Vector2Int(1, grid.height - 2)),
                ("Watcher", new Color(0.55f, 1f, 0.1f), new Vector2Int(1, 1)),
                ("Drifter", new Color(0.3f, 0.7f, 1f), new Vector2Int(grid.width - 2, 1)),
            };
            for (int i = 0; i < specs.Length; i++)
            {
                var (name, color, corner) = specs[i];
                var mat = Neon(color, 1.05f);
                var go = Prim(PrimitiveType.Cube, "Chaser_" + name, null, Vector3.zero, Vector3.one * 0.6f, mat);
                go.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
                chasers.Add(new Chaser
                {
                    name = name,
                    behavior = i,
                    color = color,
                    start = grid.chaserStarts[i % grid.chaserStarts.Count],
                    corner = corner,
                    tf = go.transform,
                    renderer = go.GetComponent<Renderer>(),
                    material = mat,
                    light = AddLight(go, color, 3f, 1.5f),
                });
            }
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            var t = canvasGo.transform;
            var cyan = new Color(0.4f, 1f, 0.95f);
            scoreText = Label(t, "Score", new Vector2(0f, 1f), new Vector2(24f, -16f), 34, TextAnchor.UpperLeft, cyan);
            bestText = Label(t, "Best", new Vector2(0.5f, 1f), new Vector2(0f, -16f), 34, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.4f));
            livesText = Label(t, "Lives", new Vector2(1f, 1f), new Vector2(-24f, -16f), 34, TextAnchor.UpperRight, new Color(1f, 0.3f, 0.6f));
            messageText = Label(t, "Message", new Vector2(0.5f, 0.5f), Vector2.zero, 64, TextAnchor.MiddleCenter, Color.white);
            Label(t, "Controls", new Vector2(0.5f, 0f), new Vector2(0f, 10f), 24, TextAnchor.LowerCenter, new Color(0.8f, 0.7f, 1f))
                .text = "WASD：移動　　R：リスタート";
        }

        Text Label(Transform parent, string name, Vector2 anchor, Vector2 pos, int size, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(700f, 200f);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            return text;
        }

        static Material Neon(Color color, float glow)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = glow > 0f ? color * 0.35f : color;
            m.SetFloat("_Glossiness", 0.4f);
            if (glow > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * glow);
            }
            return m;
        }

        static Light AddLight(GameObject go, Color color, float range, float intensity)
        {
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
