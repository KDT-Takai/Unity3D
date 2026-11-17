using System.Collections.Generic;
using UnityEngine;

namespace NeonMaze
{
    // 迷路の定義と、マス⇔ワールド座標の変換。マス座標は左下が(0,0)、yが奥(+Z)
    public class MazeGrid
    {
        // # 壁 / . ドット / o パワー / P 自機の開始 / G 追っ手の開始 / 空白 何もない通路
        // 左右の端が開いている行はワープトンネル（端から出ると反対側から入る）
        public static readonly string[] DefaultLayout =
        {
            "###################",
            "#o.......#.......o#",
            "#.##.###.#.###.##.#",
            "#.................#",
            "#.##.#.#####.#.##.#",
            "#....#...#...#....#",
            "####.###.#.###.####",
            " .......GGG....... ",
            "####.#.#####.#.####",
            "#....#.......#....#",
            "#.##.###.#.###.##.#",
            "#o.#.....P.....#.o#",
            "#..#.#.#####.#.#..#",
            "#....#...#...#....#",
            "###################",
        };

        public static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right,
        };

        public readonly int width;
        public readonly int height;
        public Vector2Int playerStart;
        public readonly List<Vector2Int> chaserStarts = new List<Vector2Int>();
        public readonly List<Vector2Int> dots = new List<Vector2Int>();
        public readonly List<Vector2Int> powers = new List<Vector2Int>();

        readonly bool[,] walls;

        public MazeGrid(string[] layout)
        {
            height = layout.Length;
            width = layout[0].Length;
            walls = new bool[width, height];
            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, height - 1 - row);
                    switch (layout[row][x])
                    {
                        case '#': walls[cell.x, cell.y] = true; break;
                        case '.': dots.Add(cell); break;
                        case 'o': powers.Add(cell); break;
                        case 'P': playerStart = cell; break;
                        case 'G': chaserStarts.Add(cell); break;
                    }
                }
            }
        }

        public bool IsWall(Vector2Int cell)
        {
            if (cell.y < 0 || cell.y >= height) return true;
            cell = Wrap(cell);
            return walls[cell.x, cell.y];
        }

        // 左右はつながっている（トンネル）
        public Vector2Int Wrap(Vector2Int cell)
        {
            cell.x = ((cell.x % width) + width) % width;
            return cell;
        }

        public Vector3 ToWorld(Vector2 cell, float y)
        {
            return new Vector3(cell.x - (width - 1) * 0.5f, y, cell.y - (height - 1) * 0.5f);
        }

        // 交差点での向き選び。引き返しは行き止まりのときだけ。random があればランダム、なければ target に近づく向き
        public Vector2Int ChooseDirection(Vector2Int cell, Vector2Int currentDir, Vector2Int target, System.Random random)
        {
            var options = new List<Vector2Int>();
            foreach (var d in Directions)
            {
                if (IsWall(cell + d)) continue;
                if (currentDir != Vector2Int.zero && d == -currentDir) continue;
                options.Add(d);
            }
            if (options.Count == 0)
            {
                return IsWall(cell - currentDir) ? Vector2Int.zero : -currentDir;
            }
            if (random != null) return options[random.Next(options.Count)];

            var best = options[0];
            float bestDistance = float.MaxValue;
            foreach (var d in options)
            {
                float distance = (cell + d - target).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = d;
                }
            }
            return best;
        }
    }
}
