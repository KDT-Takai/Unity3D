using System.Collections.Generic;
using NeonMaze;
using NUnit.Framework;
using UnityEngine;

public class NeonMazeTests
{
    MazeGrid grid;

    [SetUp]
    public void SetUp()
    {
        grid = new MazeGrid(MazeGrid.DefaultLayout);
    }

    [Test]
    public void Layout_IsRectangular_AndHasStartsAndPellets()
    {
        foreach (var row in MazeGrid.DefaultLayout) Assert.AreEqual(grid.width, row.Length);
        Assert.AreEqual(19, grid.width);
        Assert.AreEqual(15, grid.height);
        Assert.IsFalse(grid.IsWall(grid.playerStart));
        Assert.AreEqual(3, grid.chaserStarts.Count);
        Assert.AreEqual(4, grid.powers.Count);
        Assert.Greater(grid.dots.Count, 100);
    }

    [Test]
    public void EveryOpenCell_IsReachableFromPlayerStart()
    {
        var seen = new HashSet<Vector2Int> { grid.playerStart };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(grid.playerStart);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var d in MazeGrid.Directions)
            {
                var n = grid.Wrap(cell + d);
                if (!grid.IsWall(n) && seen.Add(n)) queue.Enqueue(n);
            }
        }
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!grid.IsWall(cell)) Assert.IsTrue(seen.Contains(cell), "unreachable " + cell);
            }
    }

    [Test]
    public void Mover_StopsAtWall_AndMovesInOpenDirection()
    {
        var mover = new GridMover { speed = 5f };
        mover.Place(grid.playerStart);
        Assert.IsTrue(grid.IsWall(grid.playerStart + Vector2Int.up));

        mover.Step(0.1f, grid, () => Vector2Int.up, null);
        Assert.AreEqual(Vector2Int.zero, mover.dir);
        Assert.AreEqual(grid.playerStart, mover.cell);

        var arrived = new List<Vector2Int>();
        mover.Step(0.2f, grid, () => Vector2Int.right, arrived.Add);
        Assert.AreEqual(grid.playerStart + Vector2Int.right, mover.cell);
        Assert.AreEqual(1, arrived.Count);
    }

    [Test]
    public void Mover_Reverse_GoesBackToPreviousCell()
    {
        var mover = new GridMover { speed = 5f };
        mover.Place(grid.playerStart);
        mover.Step(0.1f, grid, () => Vector2Int.right, null);
        Assert.AreEqual(0.5f, mover.progress, 1e-4);

        mover.Reverse(grid);
        Assert.AreEqual(Vector2Int.left, mover.dir);
        Assert.AreEqual(new Vector2(grid.playerStart.x + 0.5f, grid.playerStart.y), mover.GridPosition);
    }

    [Test]
    public void Tunnel_LeftEdge_WarpsToRightEdge()
    {
        var left = new Vector2Int(0, 7);
        Assert.IsFalse(grid.IsWall(left));
        Assert.IsFalse(grid.IsWall(left + Vector2Int.left));

        var mover = new GridMover { speed = 5f };
        mover.Place(left);
        mover.Step(0.2f, grid, () => Vector2Int.left, null);
        Assert.AreEqual(new Vector2Int(grid.width - 1, 7), mover.cell);

        mover.Step(0.2f, grid, () => Vector2Int.right, null);
        Assert.AreEqual(left, mover.cell);
    }

    [Test]
    public void ChooseDirection_MovesTowardTarget_AndNeverReversesAtJunction()
    {
        var start = grid.playerStart;
        var target = start + new Vector2Int(5, 0);
        Assert.AreEqual(Vector2Int.right, grid.ChooseDirection(start, Vector2Int.zero, target, null));

        // 右へ進んでいる最中は、目標が左でも引き返さない
        var dir = grid.ChooseDirection(start, Vector2Int.right, start + new Vector2Int(-5, 0), null);
        Assert.AreNotEqual(Vector2Int.left, dir);
        Assert.IsFalse(grid.IsWall(start + dir));
    }
}
