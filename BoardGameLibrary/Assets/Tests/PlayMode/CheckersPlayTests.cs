using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode test: drives the live <see cref="CheckersGame"/> as if a person were
/// playing it — the MonoBehaviour's Start/Update lifecycle runs, the human "clicks"
/// to make a move, and the computer's reply (which runs in a coroutine over real
/// frames) is awaited. This kind of frame/coroutine-dependent scenario is exactly
/// what requires a PlayMode test rather than an EditMode one.
/// </summary>
public class CheckersPlayTests
{
    const int N = 8;
    GameObject host;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (host != null) Object.Destroy(host);
        yield return null;
    }

    static int CountSide(CheckersGame g, int side)
    {
        int n = 0;
        for (int c = 0; c < N; c++)
            for (int r = 0; r < N; r++)
            {
                int v = g.CellValue(c, r);
                if ((side > 0 && v > 0) || (side < 0 && v < 0)) n++;
            }
        return n;
    }

    [UnityTest]
    public IEnumerator HumanMoves_ThenComputerRepliesAndReturnsTheTurn()
    {
        host = new GameObject("CheckersTestHost");
        var game = host.AddComponent<CheckersGame>();
        yield return null; // let Start() run: build the view and deal a new game

        // --- Opening position sanity ---
        Assert.IsTrue(game.IsHumanTurn, "Human (Red) moves first.");
        Assert.AreEqual(12, CountSide(game, 1), "Human starts with 12 pieces.");
        Assert.AreEqual(12, CountSide(game, -1), "Computer starts with 12 pieces.");
        Assert.AreEqual(1, game.CellValue(2, 2), "Expected a Red man at (2,2).");
        Assert.AreEqual(0, game.CellValue(1, 3), "Target (1,3) should start empty.");

        // --- Simulate the human's two clicks: select the piece, then its destination ---
        game.ClickCell(2, 2); // select the Red man
        game.ClickCell(1, 3); // move it diagonally forward (a legal opening step)

        // The human's move is applied immediately and the turn passes to the computer.
        Assert.AreEqual(0, game.CellValue(2, 2), "The man should have left (2,2).");
        Assert.AreEqual(1, game.CellValue(1, 3), "The man should now be at (1,3).");
        Assert.IsFalse(game.IsHumanTurn, "It should now be the computer's turn.");

        // Input is ignored while it is not the human's turn.
        game.ClickCell(4, 2);
        Assert.AreEqual(1, game.CellValue(4, 2), "Out-of-turn clicks must be ignored.");

        // --- Wait (across real frames) for the computer's coroutine reply ---
        float t = 0f;
        while (!game.IsHumanTurn && !game.IsGameOver && t < 5f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        Assert.IsTrue(game.IsHumanTurn, "The computer should have moved and handed the turn back.");
        // No captures are possible in the opening exchange, so both sides keep all 12.
        Assert.AreEqual(12, CountSide(game, 1), "Red should still have 12 pieces.");
        Assert.AreEqual(12, CountSide(game, -1), "Blue should still have 12 pieces.");
    }
}
