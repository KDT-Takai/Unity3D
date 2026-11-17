using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// ホラー版の見た目を組み立てる：夜の呪われた島、枯れ木と墓、魂の灯、亡霊、灯台。
// 何度実行しても同じ結果になる（既存の装飾を消してから作り直す）。
// 装飾にはコライダーを付けない（ゲームの当たり判定は変えない）。
public static class SceneryBuilder
{
    const string MatDir = "Assets/Materials/";

    // 魂の灯の配置（ルート選びが必要になるよう島に散らす）
    static readonly Vector3[] ItemPositions =
    {
        new Vector3(-6f, 0.5f, -4f),
        new Vector3(6f, 0.5f, -2f),
        new Vector3(-3f, 0.5f, 3f),
        new Vector3(7f, 0.5f, 6f),
        new Vector3(-7f, 0.5f, 8f),
    };

    [MenuItem("BallCollect/Build Scenery")]
    public static void Build()
    {
        var m = new Mats();
        var gm = Object.FindAnyObjectByType<GameManager>();

        BuildNight();
        GameObject.Find("Floor").GetComponent<Renderer>().sharedMaterial = m.grass;
        var goalLight = BuildWorld(m);
        BuildRobot(m, gm);
        BuildItems(m);
        var ghost = BuildGhost(m, gm);
        BuildTexts();

        var so = new SerializedObject(gm);
        so.FindProperty("goalLight").objectReferenceValue = goalLight;
        so.FindProperty("ghost").objectReferenceValue = ghost;
        so.FindProperty("caughtText").objectReferenceValue = GameObject.Find("Canvas").transform.Find("CaughtText").gameObject;
        so.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
    }

    class Mats
    {
        public Material grass = Mat("Floor", new Color(0.13f, 0.20f, 0.13f));
        public Material dirt = Mat("Dirt", new Color(0.18f, 0.13f, 0.10f));
        public Material sand = Mat("Sand", new Color(0.30f, 0.28f, 0.24f));
        public Material sea = Mat("Sea", new Color(0.03f, 0.06f, 0.10f), smooth: 0.9f);
        public Material deadWood = Mat("Trunk", new Color(0.16f, 0.12f, 0.10f));
        public Material stone = Mat("Rock", new Color(0.35f, 0.36f, 0.38f));
        public Material white = Mat("LighthouseWhite", new Color(0.55f, 0.55f, 0.55f));
        public Material red = Mat("LighthouseRed", new Color(0.40f, 0.08f, 0.07f));
        public Material lampOff = Mat("LampOff", new Color(0.15f, 0.15f, 0.18f), smooth: 0.9f);
        public Material lampOn = Mat("LampOn", new Color(1f, 0.92f, 0.5f), emission: new Color(3f, 2.5f, 1f));
        public Material eye = Mat("RobotEye", Color.white, smooth: 0.6f);
        public Material pupil = Mat("RobotPupil", new Color(0.05f, 0.05f, 0.08f), smooth: 0.8f);
        public Material metal = Mat("RobotMetal", new Color(0.25f, 0.27f, 0.32f), smooth: 0.6f);
        public Material gaugeOff = Mat("GaugeOff", new Color(0.12f, 0.12f, 0.15f));
        public Material soul = Mat("Item", new Color(0.55f, 0.9f, 1f), emission: new Color(0.5f, 1.4f, 1.8f));
        public Material ghost = Fade(Mat("Ghost", new Color(0.85f, 0.95f, 1f, 0.55f), emission: new Color(0.25f, 0.35f, 0.4f)));
        public Material ghostFace = Mat("GhostFace", Color.black);
    }

    static void BuildNight()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.03f;
        RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.04f);

        var sun = GameObject.Find("Directional Light").GetComponent<Light>();
        sun.color = new Color(0.45f, 0.55f, 0.85f);
        sun.intensity = 0.25f;
        EditorUtility.SetDirty(sun);

        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        EditorUtility.SetDirty(cam);
    }

    static GameObject BuildWorld(Mats m)
    {
        var old = GameObject.Find("Scenery");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Scenery").transform;

        Prim(PrimitiveType.Cube, "IslandCliff", root, new Vector3(0f, -1.52f, 0f), new Vector3(20f, 3f, 20f), m.dirt);
        Prim(PrimitiveType.Plane, "Sea", root, new Vector3(0f, -1f, 0f), new Vector3(60f, 1f, 60f), m.sea);

        var a = Islet(root, m, "Islet_West", new Vector3(-17f, -0.9f, 4f), 4f);
        DeadTree(a, m, new Vector3(-1f, 0.3f, 0.5f), 3f);
        Grave(a, m, new Vector3(1f, 0.3f, -0.5f), 15f);
        Grave(a, m, new Vector3(2f, 0.3f, 1f), -10f);
        Grave(a, m, new Vector3(0.5f, 0.3f, 2f), 25f);

        var b = Islet(root, m, "Islet_East", new Vector3(17f, -0.9f, -2f), 3.5f);
        DeadTree(b, m, new Vector3(0.5f, 0.3f, 0.5f), 2.6f);
        Grave(b, m, new Vector3(-1.2f, 0.3f, -0.8f), -20f);

        var c = Islet(root, m, "Islet_NorthWest", new Vector3(-14f, -0.9f, 19f), 3.5f);
        DeadTree(c, m, new Vector3(0f, 0.3f, 0f), 3.4f);
        DeadTree(c, m, new Vector3(1.6f, 0.3f, 1f), 2.2f);

        var d = Islet(root, m, "Islet_North", new Vector3(2f, -0.9f, 30f), 5f);
        DeadTree(d, m, new Vector3(-2f, 0.3f, 0f), 3.6f);
        DeadTree(d, m, new Vector3(2.5f, 0.3f, -1.5f), 2.8f);
        Grave(d, m, new Vector3(0f, 0.3f, -2f), 5f);
        Grave(d, m, new Vector3(1f, 0.3f, -2.6f), -12f);

        Rock(root, m, new Vector3(-12.5f, -1f, -8f), 1.2f);
        Rock(root, m, new Vector3(-21f, -1f, -12f), 1.6f);

        var e = Islet(root, m, "Islet_Lighthouse", new Vector3(15f, -0.9f, 11f), 3.5f);
        var lh = new GameObject("Lighthouse").transform;
        lh.SetParent(e, false);
        lh.localPosition = new Vector3(0f, 0.3f, 0f);
        Prim(PrimitiveType.Cylinder, "Tower", lh, new Vector3(0f, 3f, 0f), new Vector3(1.6f, 3f, 1.6f), m.white);
        Prim(PrimitiveType.Cylinder, "Band1", lh, new Vector3(0f, 1.6f, 0f), new Vector3(1.65f, 0.4f, 1.65f), m.red);
        Prim(PrimitiveType.Cylinder, "Band2", lh, new Vector3(0f, 4.2f, 0f), new Vector3(1.65f, 0.4f, 1.65f), m.red);
        Prim(PrimitiveType.Cylinder, "Gallery", lh, new Vector3(0f, 6.1f, 0f), new Vector3(2.1f, 0.12f, 2.1f), m.metal);
        Prim(PrimitiveType.Sphere, "Lamp", lh, new Vector3(0f, 6.75f, 0f), Vector3.one * 1.1f, m.lampOff);
        Prim(PrimitiveType.Cylinder, "Roof", lh, new Vector3(0f, 7.45f, 0f), new Vector3(1.6f, 0.15f, 1.6f), m.red);

        // 全回収で灯台が灯り、島全体を照らす
        var goal = new GameObject("GoalLight").transform;
        goal.SetParent(lh, false);
        goal.localPosition = new Vector3(0f, 6.75f, 0f);
        Prim(PrimitiveType.Sphere, "LitLamp", goal, Vector3.zero, Vector3.one * 1.25f, m.lampOn);
        AddLight(goal.gameObject, LightType.Point, new Color(1f, 0.85f, 0.5f), 45f, 6f);
        goal.gameObject.SetActive(false);
        return goal.gameObject;
    }

    static void BuildRobot(Mats m, GameManager gm)
    {
        var player = GameObject.Find("Player");
        var old = player.transform.Find("Visual");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var v = new GameObject("Visual").transform;
        v.SetParent(player.transform, false);

        foreach (float side in new[] { -1f, 1f })
        {
            var eye = Prim(PrimitiveType.Sphere, side < 0 ? "EyeL" : "EyeR", v, new Vector3(0.17f * side, 0.12f, 0.4f), Vector3.one * 0.24f, m.eye);
            Prim(PrimitiveType.Sphere, "Pupil", eye.transform, new Vector3(0f, 0f, 0.32f), Vector3.one * 0.55f, m.pupil);
        }
        Prim(PrimitiveType.Cylinder, "Antenna", v, new Vector3(0f, 0.6f, 0f), new Vector3(0.05f, 0.12f, 0.05f), m.metal);
        var tip = Prim(PrimitiveType.Sphere, "AntennaTip", v, new Vector3(0f, 0.78f, 0f), Vector3.one * 0.16f, m.gaugeOff);

        Prim(PrimitiveType.Cube, "GaugeFrame", v, new Vector3(0f, 0.05f, -0.5f), new Vector3(0.56f, 0.7f, 0.18f), m.metal);
        Prim(PrimitiveType.Cube, "GaugeCap", v, new Vector3(0f, 0.44f, -0.5f), new Vector3(0.2f, 0.08f, 0.1f), m.metal);
        var segs = new Renderer[5];
        for (int i = 0; i < 5; i++)
        {
            var seg = Prim(PrimitiveType.Cube, "Gauge_" + (i + 1), v, new Vector3(0f, -0.2f + i * 0.125f, -0.6f), new Vector3(0.44f, 0.095f, 0.03f), m.gaugeOff);
            segs[i] = seg.GetComponent<Renderer>();
        }

        // 懐中電灯（進む向きを照らす）と、暗闇でも自分の位置が分かる小さな明かり
        var flash = new GameObject("Flashlight");
        flash.transform.SetParent(v, false);
        flash.transform.localPosition = new Vector3(0f, 0.3f, 0.45f);
        flash.transform.localEulerAngles = new Vector3(18f, 0f, 0f);
        var spot = AddLight(flash, LightType.Spot, new Color(1f, 0.95f, 0.8f), 16f, 5f);
        spot.spotAngle = 75f;
        var glow = new GameObject("Glow");
        glow.transform.SetParent(v, false);
        glow.transform.localPosition = new Vector3(0f, 1.2f, -0.6f);
        AddLight(glow, LightType.Point, new Color(0.8f, 0.85f, 1f), 4.5f, 1.2f);

        var gauge = player.GetComponent<EnergyGauge>();
        if (gauge == null) gauge = player.AddComponent<EnergyGauge>();
        gauge.gameManager = gm;
        gauge.segments = segs;
        gauge.antennaTip = tip.GetComponent<Renderer>();
        gauge.offMaterial = m.gaugeOff;
        gauge.onMaterial = m.soul;
        gauge.uiOnColor = new Color(0.55f, 0.9f, 1f);
        gauge.uiCells = BuildEnergyBar();
        EditorUtility.SetDirty(gauge);

        var rb = player.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        EditorUtility.SetDirty(rb);
    }

    static void BuildItems(Mats m)
    {
        for (int i = 0; i < ItemPositions.Length; i++)
        {
            var item = GameObject.Find("Item_" + (i + 1));
            item.transform.position = ItemPositions[i];
            item.GetComponent<Renderer>().sharedMaterial = m.soul;
            foreach (var name in new[] { "Terminal", "SoulLight" })
            {
                var old = item.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var lightGo = new GameObject("SoulLight");
            lightGo.transform.SetParent(item.transform, false);
            AddLight(lightGo, LightType.Point, new Color(0.5f, 0.9f, 1f), 4f, 1.6f);
            if (item.GetComponent<ItemFloat>() == null) item.AddComponent<ItemFloat>();
        }
    }

    static Ghost BuildGhost(Mats m, GameManager gm)
    {
        var old = GameObject.Find("Ghost");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Ghost");
        var ghost = root.AddComponent<Ghost>();
        ghost.gameManager = gm;
        ghost.ResetGhost();

        Prim(PrimitiveType.Sphere, "Body", root.transform, Vector3.zero, new Vector3(1f, 1.2f, 1f), m.ghost);
        Prim(PrimitiveType.Sphere, "Tail", root.transform, new Vector3(0f, -0.6f, -0.15f), new Vector3(0.75f, 0.7f, 0.75f), m.ghost);
        Prim(PrimitiveType.Sphere, "Tail2", root.transform, new Vector3(0f, -0.95f, -0.35f), new Vector3(0.4f, 0.45f, 0.4f), m.ghost);
        foreach (float side in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Sphere, side < 0 ? "EyeL" : "EyeR", root.transform, new Vector3(0.18f * side, 0.18f, 0.44f), new Vector3(0.17f, 0.27f, 0.1f), m.ghostFace);
        }
        Prim(PrimitiveType.Sphere, "Mouth", root.transform, new Vector3(0f, -0.15f, 0.47f), new Vector3(0.18f, 0.28f, 0.08f), m.ghostFace);
        AddLight(root, LightType.Point, new Color(0.6f, 0.85f, 1f), 5f, 1.4f);
        return ghost;
    }

    static void BuildTexts()
    {
        BottomText("GoalText", "亡霊から逃げながら、5つの魂の灯を集めて灯台をともせ…", 32, 64f);
        BottomText("ControlsText", "WASD：移動　　R：やり直し", 26, 20f);

        var canvas = GameObject.Find("Canvas").transform;
        var old = canvas.Find("CaughtText");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var caught = new GameObject("CaughtText", typeof(RectTransform), typeof(Text), typeof(Outline));
        caught.transform.SetParent(canvas, false);
        var rt = caught.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1000f, 240f);
        var t = caught.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = "つかまった…";
        t.fontSize = 110;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.85f, 0.05f, 0.05f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        caught.GetComponent<Outline>().effectColor = Color.black;
        caught.SetActive(false);
    }

    // 画面左上、回収数の右に5マスのバッテリー表示
    static Image[] BuildEnergyBar()
    {
        var canvas = GameObject.Find("Canvas").transform;
        var old = canvas.Find("EnergyBar");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var bar = UIRect("EnergyBar", canvas, new Vector2(0f, 1f), new Vector2(130f, -26f), new Vector2(232f, 48f));
        bar.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.6f, 0.65f);
        var nub = UIRect("Nub", bar, new Vector2(1f, 0.5f), new Vector2(10f, 0f), new Vector2(10f, 20f));
        nub.pivot = new Vector2(1f, 0.5f);
        nub.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.6f, 0.65f);
        var cells = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var cell = UIRect("Cell_" + (i + 1), bar, new Vector2(0f, 0.5f), new Vector2(6f + i * 45f, 0f), new Vector2(40f, 36f));
            cell.pivot = new Vector2(0f, 0.5f);
            cells[i] = cell.gameObject.AddComponent<Image>();
            cells[i].color = new Color(0.2f, 0.2f, 0.22f);
        }
        return cells;
    }

    static RectTransform UIRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void BottomText(string name, string text, int size, float y)
    {
        var canvas = GameObject.Find("Canvas").transform;
        var old = canvas.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(canvas, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(1200f, 50f);
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.85f, 0.85f, 0.9f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
    }

    static Transform Islet(Transform parent, Mats m, string name, Vector3 pos, float radius)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        Prim(PrimitiveType.Cylinder, "Shore", root, Vector3.zero, new Vector3(radius * 2f, 0.3f, radius * 2f), m.sand);
        Prim(PrimitiveType.Cylinder, "Grass", root, new Vector3(0f, 0.02f, 0f), new Vector3(radius * 1.6f, 0.3f, radius * 1.6f), m.grass);
        return root;
    }

    // 葉のない枯れ木
    static void DeadTree(Transform parent, Mats m, Vector3 pos, float height)
    {
        var root = new GameObject("DeadTree").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        Prim(PrimitiveType.Cylinder, "Trunk", root, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.25f, height * 0.5f, 0.25f), m.deadWood, new Vector3(0f, 0f, 4f));
        Prim(PrimitiveType.Cylinder, "Branch1", root, new Vector3(0.45f, height * 0.75f, 0f), new Vector3(0.1f, height * 0.25f, 0.1f), m.deadWood, new Vector3(0f, 0f, -50f));
        Prim(PrimitiveType.Cylinder, "Branch2", root, new Vector3(-0.4f, height * 0.6f, 0.1f), new Vector3(0.1f, height * 0.22f, 0.1f), m.deadWood, new Vector3(10f, 0f, 55f));
        Prim(PrimitiveType.Cylinder, "Branch3", root, new Vector3(0.1f, height * 0.95f, -0.35f), new Vector3(0.07f, height * 0.18f, 0.07f), m.deadWood, new Vector3(-45f, 0f, 10f));
    }

    // 傾いた墓石
    static void Grave(Transform parent, Mats m, Vector3 pos, float tilt)
    {
        var root = new GameObject("Grave").transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        root.localEulerAngles = new Vector3(0f, tilt * 2f, tilt * 0.4f);
        Prim(PrimitiveType.Cube, "Stone", root, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.2f), m.stone);
        Prim(PrimitiveType.Cylinder, "Top", root, new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.1f, 0.6f), m.stone, new Vector3(90f, 0f, 0f));
    }

    static void Rock(Transform parent, Mats m, Vector3 pos, float size)
    {
        Prim(PrimitiveType.Sphere, "Rock", parent, pos, new Vector3(size * 1.3f, size * 0.8f, size), m.stone, new Vector3(0f, pos.x * 20f, 8f));
    }

    static Light AddLight(GameObject go, LightType type, Color color, float range, float intensity)
    {
        var light = go.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        return light;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Material Mat(string name, Color color, Color? emission = null, float smooth = 0.15f)
    {
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        m.SetFloat("_Glossiness", smooth);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // Standardシェーダーを半透明（Fade）にする
    static Material Fade(Material m)
    {
        m.SetFloat("_Mode", 2f);
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }
}
