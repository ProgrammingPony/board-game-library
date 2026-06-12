using System.Collections.Generic;
using UnityEngine; // for Vector2Int only

/// <summary>A full turn's move: one simple step, or a maximal (multi-)jump sequence.</summary>
public class Move
{
    public Vector2Int from;
    public List<Vector2Int> path = new List<Vector2Int>();
    public List<Vector2Int> caps = new List<Vector2Int>();
    public bool IsCapture => caps.Count > 0;
    public Vector2Int To => path[path.Count - 1];
}

/// <summary>A single hop, used for step-by-step human interaction.</summary>
public struct Hop
{
    public int toC, toR;
    public bool isCapture;
    public int capC, capR;
    public bool promoted;
}

/// <summary>
/// Pure checkers (draughts) rules — board representation and move generation only.
/// No Unity scene/GameObject dependencies, so it is straightforward to unit-test.
///
/// Board cell values: positive = human (Red), negative = computer (Blue);
/// 0 empty, +1/-1 man, +2/-2 king. board[col, row]; row 0 = human home, row 7 = computer home.
///
/// Rules: diagonal moves, jumps with multi-jump chaining, mandatory captures,
/// kinging on the far row, and kings that move/capture both directions.
/// </summary>
public static class CheckersRules
{
    public const int N = 8;

    /// <summary>Standard opening position: three rows of men on the dark squares each side.</summary>
    public static int[,] CreateInitialBoard()
    {
        var board = new int[N, N];
        for (int c = 0; c < N; c++)
            for (int r = 0; r < N; r++)
                if (IsDark(c, r))
                {
                    if (r <= 2) board[c, r] = 1;        // human (Red) home rows
                    else if (r >= 5) board[c, r] = -1;  // computer (Blue) home rows
                }
        return board;
    }

    public static bool IsDark(int c, int r) => (c + r) % 2 == 0; // playable squares
    public static bool IsKing(int v) => v == 2 || v == -2;
    public static bool In(int c, int r) => c >= 0 && c < N && r >= 0 && r < N;
    public static int Sign(int v) => v > 0 ? 1 : (v < 0 ? -1 : 0);

    public static bool IsPromo(int v, int destR)
        => !IsKing(v) && ((v > 0 && destR == N - 1) || (v < 0 && destR == 0));

    /// <summary>Immediate single hops (steps and/or jumps) for the piece at (c, r).</summary>
    public static List<Hop> GetHops(int[,] b, int c, int r, bool capturesOnly)
    {
        var list = new List<Hop>();
        int v = b[c, r];
        if (v == 0) return list;

        int sign = v > 0 ? 1 : -1;
        int[] drs = IsKing(v) ? new[] { 1, -1 } : new[] { sign };
        foreach (int dr in drs)
        {
            foreach (int dc in new[] { -1, 1 })
            {
                int nc = c + dc, nr = r + dr;
                if (!In(nc, nr)) continue;
                int nv = b[nc, nr];
                if (nv == 0)
                {
                    if (!capturesOnly)
                        list.Add(new Hop { toC = nc, toR = nr, isCapture = false, promoted = IsPromo(v, nr) });
                }
                else if ((nv > 0) != (v > 0)) // opponent
                {
                    int jc = c + 2 * dc, jr = r + 2 * dr;
                    if (In(jc, jr) && b[jc, jr] == 0)
                        list.Add(new Hop { toC = jc, toR = jr, isCapture = true, capC = nc, capR = nr, promoted = IsPromo(v, jr) });
                }
            }
        }
        return list;
    }

    public static void ApplyHop(int[,] b, int fc, int fr, Hop h)
    {
        int v = b[fc, fr];
        b[fc, fr] = 0;
        if (h.isCapture) b[h.capC, h.capR] = 0;
        b[h.toC, h.toR] = h.promoted ? (v > 0 ? 2 : -2) : v;
    }

    public static bool SideHasCapture(int[,] b, int side)
    {
        for (int c = 0; c < N; c++)
            for (int r = 0; r < N; r++)
                if (Sign(b[c, r]) == side && GetHops(b, c, r, true).Count > 0)
                    return true;
        return false;
    }

    /// <summary>All legal full-turn moves for a side. Captures are mandatory.</summary>
    public static List<Move> GenerateFullMoves(int[,] b, int side)
    {
        var caps = new List<Move>();
        for (int c = 0; c < N; c++)
            for (int r = 0; r < N; r++)
                if (Sign(b[c, r]) == side)
                    CollectCaptures(b, new Vector2Int(c, r), c, r, new List<Vector2Int>(), new List<Vector2Int>(), caps);

        if (caps.Count > 0) return caps; // must capture when possible

        var simples = new List<Move>();
        for (int c = 0; c < N; c++)
            for (int r = 0; r < N; r++)
                if (Sign(b[c, r]) == side)
                    foreach (var h in GetHops(b, c, r, false))
                    {
                        var m = new Move { from = new Vector2Int(c, r) };
                        m.path.Add(new Vector2Int(h.toC, h.toR));
                        simples.Add(m);
                    }
        return simples;
    }

    static void CollectCaptures(int[,] b, Vector2Int from, int curC, int curR,
                                List<Vector2Int> path, List<Vector2Int> caps, List<Move> outList)
    {
        foreach (var h in GetHops(b, curC, curR, true))
        {
            int[,] nb = (int[,])b.Clone();
            ApplyHop(nb, curC, curR, h);

            var npath = new List<Vector2Int>(path) { new Vector2Int(h.toC, h.toR) };
            var ncaps = new List<Vector2Int>(caps) { new Vector2Int(h.capC, h.capR) };

            if (h.promoted)
            {
                outList.Add(MakeMove(from, npath, ncaps)); // promotion ends the turn
                continue;
            }

            int before = outList.Count;
            CollectCaptures(nb, from, h.toC, h.toR, npath, ncaps, outList);
            if (outList.Count == before) // no further jumps: this chain is complete
                outList.Add(MakeMove(from, npath, ncaps));
        }
    }

    static Move MakeMove(Vector2Int from, List<Vector2Int> path, List<Vector2Int> caps)
        => new Move { from = from, path = new List<Vector2Int>(path), caps = new List<Vector2Int>(caps) };

    public static void ApplyMove(int[,] b, Move m)
    {
        int v = b[m.from.x, m.from.y];
        b[m.from.x, m.from.y] = 0;
        foreach (var cap in m.caps) b[cap.x, cap.y] = 0;
        Vector2Int dest = m.To;
        bool promo = !IsKing(v) && ((v > 0 && dest.y == N - 1) || (v < 0 && dest.y == 0));
        b[dest.x, dest.y] = promo ? (v > 0 ? 2 : -2) : v;
    }
}
