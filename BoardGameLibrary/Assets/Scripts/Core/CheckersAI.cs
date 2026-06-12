using System.Collections.Generic;
using UnityEngine; // Mathf, Random

/// <summary>
/// The computer opponent (plays the negative / Blue side). Chooses moves with an
/// alpha-beta minimax search over <see cref="CheckersRules"/>. Pure logic, no scene access.
/// </summary>
public static class CheckersAI
{
    const int N = CheckersRules.N;

    /// <summary>
    /// Pick the computer's move (side -1), minimising the human-favouring evaluation.
    /// Returns null if the computer has no legal move (it has lost).
    /// </summary>
    public static Move ChooseMove(int[,] board, int depth)
    {
        var moves = CheckersRules.GenerateFullMoves(board, -1);
        if (moves.Count == 0) return null;

        // Shuffle so equally-good moves vary between games.
        for (int i = moves.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (moves[i], moves[j]) = (moves[j], moves[i]);
        }

        Move best = moves[0];
        int bestVal = int.MaxValue; // the computer minimises the human-favouring score
        foreach (var m in moves)
        {
            int[,] nb = (int[,])board.Clone();
            CheckersRules.ApplyMove(nb, m);
            int val = Minimax(nb, depth - 1, int.MinValue, int.MaxValue, true);
            if (val < bestVal) { bestVal = val; best = m; }
        }
        return best;
    }

    static int Minimax(int[,] b, int depth, int alpha, int beta, bool humanToMove)
    {
        var moves = CheckersRules.GenerateFullMoves(b, humanToMove ? 1 : -1);
        if (moves.Count == 0)
            return humanToMove ? (-100000 - depth) : (100000 + depth); // side to move has lost
        if (depth == 0)
            return Eval(b);

        if (humanToMove)
        {
            int best = int.MinValue;
            foreach (var m in moves)
            {
                int[,] nb = (int[,])b.Clone();
                CheckersRules.ApplyMove(nb, m);
                best = Mathf.Max(best, Minimax(nb, depth - 1, alpha, beta, false));
                alpha = Mathf.Max(alpha, best);
                if (beta <= alpha) break;
            }
            return best;
        }
        else
        {
            int best = int.MaxValue;
            foreach (var m in moves)
            {
                int[,] nb = (int[,])b.Clone();
                CheckersRules.ApplyMove(nb, m);
                best = Mathf.Min(best, Minimax(nb, depth - 1, alpha, beta, true));
                beta = Mathf.Min(beta, best);
                if (beta <= alpha) break;
            }
            return best;
        }
    }

    /// <summary>Positive favours the human (Red); material + king bonus + advancement.</summary>
    static int Eval(int[,] b)
    {
        int s = 0;
        for (int c = 0; c < N; c++)
        {
            for (int r = 0; r < N; r++)
            {
                int v = b[c, r];
                if (v == 0) continue;
                int center = (c == 0 || c == N - 1) ? 0 : 2;
                if (v == 1) s += 100 + r * 6 + center;                  // human man, advance up
                else if (v == 2) s += 180 + center;                    // human king
                else if (v == -1) s -= 100 + (N - 1 - r) * 6 + center; // computer man, advance down
                else if (v == -2) s -= 180 + center;                   // computer king
            }
        }
        return s;
    }
}
