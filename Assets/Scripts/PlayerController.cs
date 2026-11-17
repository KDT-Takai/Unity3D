using UnityEngine;
using UnityEngine.InputSystem;

// WASDで球を動かす。W=+Z, S=-Z, D=+X, A=-X
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public bool canMove = true;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // ロボットの顔と背中のゲージが転がらないよう回転は固定し、進む向きへ向ける
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        Vector3 dir = Vector3.zero;
        var kb = Keyboard.current;
        if (canMove && kb != null)
        {
            if (kb.wKey.isPressed) dir.z += 1f;
            if (kb.sKey.isPressed) dir.z -= 1f;
            if (kb.dKey.isPressed) dir.x += 1f;
            if (kb.aKey.isPressed) dir.x -= 1f;
        }
        dir = dir.normalized * speed;
        if (dir != Vector3.zero) rb.MoveRotation(Quaternion.LookRotation(dir));

        // 水平速度だけ入力で決め、落下(Y)は物理に任せる
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(dir.x, v.y, dir.z);
    }

    public void ResetMotion(Vector3 position)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = Quaternion.identity;
        transform.SetPositionAndRotation(position, Quaternion.identity);
    }
}
