using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for <see cref="CheckersAI"/> (the computer opponent, side -1).
/// These exercise the AI's decision making against hand-built positions, and in
/// doing so also cover the <see cref="CheckersRules"/> move generation it relies on.
///
/// Note: the AI shuffles equally-rated moves with UnityEngine.Random, so these tests
/// assert only properties that hold regardless of tie-breaking (legality, mandatory
/// captures, material outcome) rather than a single exact move.
/// </summary>
public class CheckersAITests
{
    const int N = CheckersRules.N;

    static int[,] EmptyBoard() => new int[N, N];

    static int CountSide(int[,] b, int side)
    {
        int n = 0;
        for (int c = 0; c < N; c++)
            for (int r = 0; r < N; r++)
                if (CheckersRules.Sign(b[c, r]) == side) n++;
        return n;
    }

    static bool SameMove(Move a, Move b)
    {
        if (a.from != b.from) return false;
        if (a.path.Count != b.path.Count) return false;
        for (int i = 0; i < a.path.Count; i++) if (a.path[i] != b.path[i]) return false;
        if (a.caps.Count != b.caps.Count) return false;
        for (int i = 0; i < a.caps.Count; i++) if (a.caps[i] != b.caps[i]) return false;
        return true;
    }

    [Test]
    public void ChooseMove_NoComputerPieces_ReturnsNull()
    {
        var b = EmptyBoard();
        b[2, 2] = 1; // a lone human piece; the computer has nothing to move
        Assert.IsNull(CheckersAI.ChooseMove(b, 4));
    }

    [Test]
    public void ChooseMove_OnOpeningPosition_ReturnsALegalMove()
    {
        var b = CheckersRules.CreateInitialBoard();
        var move = CheckersAI.ChooseMove(b, 4);

        Assert.IsNotNull(move, "AI should have a move from the opening position.");
        var legal = CheckersRules.GenerateFullMoves(b, -1);
        Assert.IsTrue(legal.Exists(m => SameMove(m, move)),
            "AI returned a move that is not in the legal move list.");
    }

    [Test]
    public void ChooseMove_TakesMandatoryCapture()
    {
        var b = EmptyBoard();
        b[3, 4] = -1; // computer man (moves toward row 0)
        b[2, 3] = 1;  // human man diagonally ahead; landing square (1,2) is empty

        var move = CheckersAI.ChooseMove(b, 4);

        Assert.IsNotNull(move);
        Assert.IsTrue(move.IsCapture, "A capture is available, so it is mandatory.");
        Assert.AreEqual(new Vector2Int(2, 3), move.caps[0], "Captured the wrong square.");
        Assert.AreEqual(new Vector2Int(1, 2), move.To, "Landed on the wrong square.");
    }

    [Test]
    public void ChooseMove_CaptureRemovesHumanPiece()
    {
        var b = EmptyBoard();
        b[3, 4] = -1;
        b[2, 3] = 1;
        Assert.AreEqual(1, CountSide(b, 1));

        var move = CheckersAI.ChooseMove(b, 4);
        CheckersRules.ApplyMove(b, move);

        Assert.AreEqual(0, CountSide(b, 1), "The captured human piece should be gone.");
        Assert.AreEqual(1, CountSide(b, -1), "The computer piece should remain (now relocated).");
    }

    [Test]
    public void ChooseMove_TakesTheCaptureThatWinsTheGame()
    {
        // Two computer men, one lone human man that the (3,4) man can capture.
        // The other computer man (5,4) has no capture, so the jump is the only legal move.
        var b = EmptyBoard();
        b[3, 4] = -1;
        b[5, 4] = -1;
        b[2, 3] = 1;

        var move = CheckersAI.ChooseMove(b, 6);

        Assert.IsNotNull(move);
        Assert.IsTrue(move.IsCapture);
        CheckersRules.ApplyMove(b, move);
        Assert.AreEqual(0, CountSide(b, 1),
            "Capturing the last human piece wins, so the AI must take it.");
    }

    [Test]
    public void ChooseMove_PrefersWinningMaterial_OverAQuietMove()
    {
        // The computer can either capture a human man at (4,3) or make a quiet step
        // with a far-away man. With captures mandatory, it must capture; this also
        // confirms the search treats winning a piece as favourable.
        var b = EmptyBoard();
        b[5, 4] = -1; // computer man that can jump (4,3) -> land (3,2)
        b[4, 3] = 1;  // human man to be captured
        b[0, 6] = -1; // unrelated computer man with only quiet moves

        var move = CheckersAI.ChooseMove(b, 4);

        Assert.IsNotNull(move);
        Assert.IsTrue(move.IsCapture, "A capture exists and is therefore forced.");
        CheckersRules.ApplyMove(b, move);
        Assert.AreEqual(0, CountSide(b, 1), "The human man should have been captured.");
    }
}
