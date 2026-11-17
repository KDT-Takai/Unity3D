using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 回収数・クリア・制限時間・亡霊・落下とRでの初期化を管理する
public class GameManager : MonoBehaviour
{
    public PlayerController player;
    public Item[] items;
    public Text countText;
    public GameObject clearText;
    public Text timerText;
    public GameObject timeUpText;
    public GameObject goalLight;
    public Ghost ghost;
    public GameObject caughtText;
    public float fallY = -3f;
    public float timeLimit = 30f;

    public int collectedCount;
    public bool isCleared;
    public float remainingTime;
    public bool isTimeUp;
    public bool isCaught;

    public bool IsPlaying => !isCleared && !isTimeUp && !isCaught;

    Vector3 playerStart;
    bool rWasPressed;

    void Start()
    {
        playerStart = player.transform.position;
        ResetGame();
    }

    void Update()
    {
        // 押した瞬間を isPressed の変化で検出する（シミュレート入力でも取りこぼさない）
        var kb = Keyboard.current;
        bool rPressed = kb != null && kb.rKey.isPressed;
        bool rDown = rPressed && !rWasPressed;
        rWasPressed = rPressed;
        if (rDown)
        {
            ResetGame();
            return;
        }
        if (player.transform.position.y < fallY)
        {
            ResetGame();
            return;
        }

        // 回収(OnTriggerEnter)は物理ステップでUpdateより先に処理されるため、
        // 同じフレームで5個目と時間切れが重なった場合はクリアが優先される
        TickTimer(Time.deltaTime);
    }

    public void TickTimer(float deltaTime)
    {
        if (!IsPlaying) return;

        remainingTime -= deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isTimeUp = true;
            player.canMove = false;
        }
        UpdateUI();
    }

    public void Collect(Item item)
    {
        if (item.collected || isTimeUp || isCaught) return;
        item.collected = true;
        item.gameObject.SetActive(false);
        collectedCount++;
        if (collectedCount >= items.Length)
        {
            isCleared = true;
        }
        UpdateUI();
    }

    // 亡霊につかまったら移動と時間を止める（Rで再開）
    public void Caught()
    {
        if (!IsPlaying) return;
        isCaught = true;
        player.canMove = false;
        UpdateUI();
    }

    public void ResetGame()
    {
        player.ResetMotion(playerStart);
        player.canMove = true;
        foreach (var item in items)
        {
            item.ResetItem();
        }
        collectedCount = 0;
        isCleared = false;
        remainingTime = timeLimit;
        isTimeUp = false;
        isCaught = false;
        if (ghost != null) ghost.ResetGhost();
        UpdateUI();
    }

    void UpdateUI()
    {
        countText.text = collectedCount + "/" + items.Length;
        clearText.SetActive(isCleared);
        timerText.text = "TIME " + Mathf.CeilToInt(remainingTime);
        timeUpText.SetActive(isTimeUp);
        if (goalLight != null) goalLight.SetActive(isCleared);
        if (caughtText != null) caughtText.SetActive(isCaught);
        // 灯台が灯ると亡霊は消える
        if (ghost != null) ghost.gameObject.SetActive(!isCleared);
    }
}
