using UnityEngine;

// 球を追いかける亡霊。回収数が増えるほど速くなり、近づかれたらつかまる（コライダーは使わず距離で判定）
public class Ghost : MonoBehaviour
{
    public GameManager gameManager;
    public Vector3 startPosition = new Vector3(0f, 1f, 9f);
    public float baseSpeed = 2f;
    public float speedPerItem = 0.5f;
    public float catchDistance = 0.9f;
    public float hoverHeight = 1f;

    public float CurrentSpeed => baseSpeed + speedPerItem * gameManager.collectedCount;

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (!gameManager.IsPlaying) return;

        Vector3 pos = transform.position;
        Vector3 target = gameManager.player.transform.position;
        Vector3 toPlayer = new Vector3(target.x - pos.x, 0f, target.z - pos.z);
        float distance = toPlayer.magnitude;
        if (distance <= catchDistance)
        {
            gameManager.Caught();
            return;
        }

        pos += toPlayer / distance * Mathf.Min(CurrentSpeed * deltaTime, distance);
        pos.y = hoverHeight + Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(toPlayer));
    }

    public void ResetGhost()
    {
        transform.SetPositionAndRotation(startPosition, Quaternion.LookRotation(Vector3.back));
    }
}
