using UnityEngine;

namespace NeonMaze
{
    // マスからマスへ一定速度で進む。マスの中心に着くたびに次の向きを決める
    public class GridMover
    {
        public Vector2Int cell;     // 最後に中心へ着いたマス
        public Vector2Int dir;      // 進行中の向き（zero は停止）
        public float progress;      // cell から cell+dir への進み具合（0〜1）
        public float speed;         // マス/秒

        public Vector2 GridPosition => (Vector2)cell + (Vector2)dir * progress;

        public void Place(Vector2Int start)
        {
            cell = start;
            dir = Vector2Int.zero;
            progress = 0f;
        }

        public void Step(float deltaTime, MazeGrid grid, System.Func<Vector2Int> decide, System.Action<Vector2Int> onArrive)
        {
            float remaining = speed * deltaTime;
            for (int guard = 0; remaining > 0f && guard < 16; guard++)
            {
                if (progress <= 0f)
                {
                    var next = decide();
                    if (next == Vector2Int.zero || grid.IsWall(cell + next))
                    {
                        dir = Vector2Int.zero;
                        return;
                    }
                    dir = next;
                }

                float step = Mathf.Min(remaining, 1f - progress);
                progress += step;
                remaining -= step;
                if (progress >= 1f)
                {
                    cell = grid.Wrap(cell + dir);
                    progress = 0f;
                    onArrive?.Invoke(cell);
                }
            }
        }

        // 移動中にその場で引き返す
        public void Reverse(MazeGrid grid)
        {
            if (dir == Vector2Int.zero || progress <= 0f) return;
            cell = grid.Wrap(cell + dir);
            dir = -dir;
            progress = 1f - progress;
        }
    }
}
