using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// 実際の GameManager / Item / PlayerController を組み立ててロジックを確認する
public class GameManagerTests
{
    readonly List<GameObject> created = new List<GameObject>();
    GameManager gm;
    Ghost ghost;

    GameObject Make(string name)
    {
        var go = new GameObject(name);
        created.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        var player = Make("Player");
        player.AddComponent<Rigidbody>();
        var controller = player.AddComponent<PlayerController>();

        gm = Make("GameManager").AddComponent<GameManager>();
        gm.player = controller;
        gm.countText = Make("CountText").AddComponent<Text>();
        gm.clearText = Make("ClearText");
        gm.timerText = Make("TimerText").AddComponent<Text>();
        gm.timeUpText = Make("TimeUpText");
        gm.timeLimit = 30f;
        gm.caughtText = Make("CaughtText");
        ghost = Make("Ghost").AddComponent<Ghost>();
        ghost.gameManager = gm;
        gm.ghost = ghost;

        gm.items = new Item[5];
        for (int i = 0; i < 5; i++)
        {
            var item = Make("Item_" + (i + 1)).AddComponent<Item>();
            item.gameManager = gm;
            gm.items[i] = item;
        }
        gm.ResetGame();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in created) Object.DestroyImmediate(go);
        created.Clear();
    }

    void CollectAll()
    {
        foreach (var item in gm.items) gm.Collect(item);
    }

    [Test]
    public void SameItemTwice_CountsOnce()
    {
        gm.Collect(gm.items[0]);
        gm.Collect(gm.items[0]);

        Assert.AreEqual(1, gm.collectedCount);
        Assert.AreEqual("1/5", gm.countText.text);
        Assert.IsFalse(gm.items[0].gameObject.activeSelf);
    }

    [Test]
    public void FourItems_NotCleared_FiveItems_Cleared()
    {
        for (int i = 0; i < 4; i++) gm.Collect(gm.items[i]);
        Assert.IsFalse(gm.isCleared);
        Assert.IsFalse(gm.clearText.activeSelf);

        gm.Collect(gm.items[4]);
        Assert.IsTrue(gm.isCleared);
        Assert.IsTrue(gm.clearText.activeSelf);
        Assert.AreEqual("5/5", gm.countText.text);
    }

    [Test]
    public void ResetGame_AfterClear_RestoresInitialState()
    {
        CollectAll();
        gm.ResetGame();

        Assert.AreEqual(0, gm.collectedCount);
        Assert.IsFalse(gm.isCleared);
        Assert.IsFalse(gm.clearText.activeSelf);
        Assert.AreEqual("0/5", gm.countText.text);
        foreach (var item in gm.items)
        {
            Assert.IsTrue(item.gameObject.activeSelf);
            Assert.IsFalse(item.collected);
        }
    }

    [Test]
    public void Timer_RunsOut_TimeUpAndStopsMovement()
    {
        gm.TickTimer(10f);
        Assert.AreEqual(20f, gm.remainingTime, 1e-4);
        Assert.AreEqual("TIME 20", gm.timerText.text);

        gm.TickTimer(25f);
        Assert.IsTrue(gm.isTimeUp);
        Assert.AreEqual(0f, gm.remainingTime);
        Assert.IsFalse(gm.player.canMove);
        Assert.IsTrue(gm.timeUpText.activeSelf);
    }

    [Test]
    public void AfterTimeUp_CollectIsIgnored()
    {
        gm.TickTimer(31f);
        gm.Collect(gm.items[0]);

        Assert.AreEqual(0, gm.collectedCount);
        Assert.IsTrue(gm.items[0].gameObject.activeSelf);
    }

    [Test]
    public void LastItemAndTimeoutSameFrame_ClearWins()
    {
        gm.TickTimer(29.99f);
        // 同じフレーム: 物理(回収)が先、その後 Update で時間が切れる
        CollectAll();
        gm.TickTimer(1f);

        Assert.IsTrue(gm.isCleared);
        Assert.IsFalse(gm.isTimeUp);
        Assert.IsFalse(gm.timeUpText.activeSelf);
    }

    [Test]
    public void AfterClear_TimerStopsAndNeverTimesUp()
    {
        gm.TickTimer(5f);
        CollectAll();
        gm.TickTimer(100f);

        Assert.AreEqual(25f, gm.remainingTime, 1e-4);
        Assert.IsFalse(gm.isTimeUp);
    }

    [Test]
    public void ResetGame_AfterTimeUp_RestoresTimeAndMovement()
    {
        gm.Collect(gm.items[0]);
        gm.TickTimer(31f);
        gm.ResetGame();

        Assert.AreEqual(30f, gm.remainingTime);
        Assert.IsFalse(gm.isTimeUp);
        Assert.IsFalse(gm.timeUpText.activeSelf);
        Assert.IsTrue(gm.player.canMove);
        Assert.AreEqual(0, gm.collectedCount);
        Assert.AreEqual("TIME 30", gm.timerText.text);
    }

    [Test]
    public void Ghost_ChasesPlayer_AndCatches()
    {
        // 亡霊は (0,1,9)、球は原点。距離9を2m/sで追う
        ghost.Tick(1f);
        Assert.AreEqual(7f, ghost.transform.position.z, 1e-3);
        Assert.IsFalse(gm.isCaught);

        for (int i = 0; i < 10; i++) ghost.Tick(1f);
        Assert.IsTrue(gm.isCaught);
        Assert.IsFalse(gm.player.canMove);
        Assert.IsTrue(gm.caughtText.activeSelf);
    }

    [Test]
    public void Ghost_GetsFasterWithEachItem()
    {
        Assert.AreEqual(2f, ghost.CurrentSpeed, 1e-4);
        gm.Collect(gm.items[0]);
        gm.Collect(gm.items[1]);
        Assert.AreEqual(3f, ghost.CurrentSpeed, 1e-4);
    }

    [Test]
    public void AfterCaught_TimerAndCollectStop()
    {
        gm.Caught();
        gm.TickTimer(5f);
        gm.Collect(gm.items[0]);

        Assert.AreEqual(30f, gm.remainingTime, 1e-4);
        Assert.AreEqual(0, gm.collectedCount);
    }

    [Test]
    public void ResetGame_AfterCaught_RestoresStateAndGhostPosition()
    {
        ghost.Tick(3f);
        gm.Caught();
        gm.ResetGame();

        Assert.IsFalse(gm.isCaught);
        Assert.IsFalse(gm.caughtText.activeSelf);
        Assert.IsTrue(gm.player.canMove);
        Assert.AreEqual(ghost.startPosition, ghost.transform.position);
    }

    [Test]
    public void Clear_HidesGhost_AndGhostCannotCatch()
    {
        CollectAll();
        Assert.IsFalse(ghost.gameObject.activeSelf);

        gm.Caught();
        Assert.IsFalse(gm.isCaught);

        gm.ResetGame();
        Assert.IsTrue(ghost.gameObject.activeSelf);
    }
}
