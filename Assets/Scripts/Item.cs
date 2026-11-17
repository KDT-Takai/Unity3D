using UnityEngine;

// 回収アイテム。Playerが触れたら一度だけGameManagerへ通知する
public class Item : MonoBehaviour
{
    public GameManager gameManager;
    public bool collected;

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.GetComponent<PlayerController>() == null) return;
        gameManager.Collect(this);
    }

    public void ResetItem()
    {
        collected = false;
        gameObject.SetActive(true);
    }
}
