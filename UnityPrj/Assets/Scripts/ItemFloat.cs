using UnityEngine;

// エネルギーキューブを浮かせて回転させる（見た目のみ）
public class ItemFloat : MonoBehaviour
{
    public float spinSpeed = 90f;
    public float bobHeight = 0.12f;
    public float bobSpeed = 2.5f;

    Vector3 basePosition;

    void Awake()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed + basePosition.z) * bobHeight;
        transform.position = basePosition + Vector3.up * bob;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }
}
