using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Checkers controller. Owns the board state and turn flow, and wires together the
/// three collaborators: <see cref="CheckersRules"/> (logic), <see cref="CheckersAI"/>
/// (opponent) and <see cref="CheckersView"/> (visuals + input mapping).
///
/// The human plays Red (bottom, moves first); the computer plays Blue (top).
/// Attach this single component to an empty GameObject and press Play.
/// </summary>
public class CheckersGame : MonoBehaviour
{
    const int AI_DEPTH = 6;

    int[,] board;
    readonly CheckersView view = new CheckersView();

    // --- Game state ---
    bool humanTurn = true;
    bool gameOver = false;
    bool aiThinking = false;
    string statusText = "";
    int selC = -1, selR = -1;     // currently selected human piece
    bool mustContinue = false;    // mid multi-jump: locked to the selected piece
    Vector2Int aiFrom = new Vector2Int(-1, -1), aiTo = new Vector2Int(-1, -1);

    // --- Read-only state accessors (for play-mode tests / alternative input sources) ---
    public bool IsHumanTurn => humanTurn;
    public bool IsAIThinking => aiThinking;
    public bool IsGameOver => gameOver;
    public int CellValue(int col, int row) => board[col, row];

    void Start()
    {
        view.Build(transform);
        NewGame();
    }

    void NewGame()
    {
        board = CheckersRules.CreateInitialBoard();
        humanTurn = true;
        gameOver = false;
        aiThinking = false;
        selC = selR = -1;
        mustContinue = false;
        aiFrom = aiTo = new Vector2Int(-1, -1);
        statusText = "Your move (Red)";
        view.RenderPieces(board);
        RefreshMarkers();
    }

    /// <summary>Recompute the selected piece's legal destinations and redraw all markers.</summary>
    void RefreshMarkers()
    {
        var targets = new List<Vector2Int>();
        if (selC >= 0)
        {
            bool capturesOnly = mustContinue || CheckersRules.SideHasCapture(board, 1);
            foreach (var h in CheckersRules.GetHops(board, selC, selR, capturesOnly))
                targets.Add(new Vector2Int(h.toC, h.toR));
        }
        view.RenderMarkers(selC, selR, targets, aiFrom, aiTo);
    }

    // =======================================================================
    //  Input
    // =======================================================================
    void Update()
    {
        if (gameOver || !humanTurn || aiThinking) return;
        if (TryGetClick(out Vector3 screenPos))
            HandleClick(screenPos);
    }

    bool TryGetClick(out Vector3 screenPos)
    {
        screenPos = Vector3.zero;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            var p = mouse.position.ReadValue();
            screenPos = new Vector3(p.x, p.y, 0f);
            return true;
        }
        return false;
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }
        return false;
#endif
    }

    void HandleClick(Vector3 screenPos)
    {
        if (view.ScreenToCell(screenPos, out int c, out int r))
            ClickCell(c, r);
        else if (!mustContinue) { selC = selR = -1; RefreshMarkers(); }
    }

    /// <summary>
    /// Apply a click on board cell (col, row). This is the game-action entry point,
    /// decoupled from how the click was produced — real mouse input calls it via
    /// <see cref="HandleClick"/>, and play-mode tests can call it directly.
    /// </summary>
    public void ClickCell(int c, int r)
    {
        if (gameOver || !humanTurn || aiThinking) return; // only the human, only on their turn
        if (!CheckersRules.In(c, r)) return;

        bool sideMustCap = CheckersRules.SideHasCapture(board, 1);

        // If a piece is selected, see if the click is a legal destination.
        if (selC >= 0)
        {
            foreach (var h in CheckersRules.GetHops(board, selC, selR, mustContinue || sideMustCap))
            {
                if (h.toC == c && h.toR == r) { DoHumanHop(h); return; }
            }
        }

        // Otherwise try to (re)select a human piece.
        int v = board[c, r];
        if (v > 0)
        {
            if (mustContinue) return; // locked to the jumping piece
            if (sideMustCap && CheckersRules.GetHops(board, c, r, true).Count == 0) return; // must pick a capturer
            selC = c; selR = r;
            RefreshMarkers();
        }
        else if (!mustContinue)
        {
            selC = selR = -1;
            RefreshMarkers();
        }
    }

    void DoHumanHop(Hop h)
    {
        // Clear the AI-move highlight once the player acts.
        aiFrom = aiTo = new Vector2Int(-1, -1);

        CheckersRules.ApplyHop(board, selC, selR, h);
        selC = h.toC; selR = h.toR;
        view.RenderPieces(board);

        if (h.isCapture && !h.promoted && CheckersRules.GetHops(board, selC, selR, true).Count > 0)
        {
            mustContinue = true;          // chain the multi-jump
            statusText = "Keep jumping!";
            RefreshMarkers();
            return;
        }

        // Turn over.
        mustContinue = false;
        selC = selR = -1;
        RefreshMarkers();
        EndHumanTurn();
    }

    void EndHumanTurn()
    {
        humanTurn = false;
        if (CheckersRules.GenerateFullMoves(board, -1).Count == 0) { Finish("You win!  🎉"); return; }
        aiThinking = true;
        statusText = "Computer thinking…";
        StartCoroutine(AITurn());
    }

    // =======================================================================
    //  Computer turn
    // =======================================================================
    IEnumerator AITurn()
    {
        yield return new WaitForSeconds(0.45f); // small pause so the move is readable

        Move best = CheckersAI.ChooseMove(board, AI_DEPTH);
        if (best == null) { aiThinking = false; Finish("You win!  🎉"); yield break; }

        aiFrom = best.from;
        aiTo = best.To;
        CheckersRules.ApplyMove(board, best);
        view.RenderPieces(board);
        RefreshMarkers();

        aiThinking = false;
        humanTurn = true;

        if (CheckersRules.GenerateFullMoves(board, 1).Count == 0) { Finish("Computer wins.  ☹"); yield break; }
        statusText = "Your move (Red)";
    }

    void Finish(string text)
    {
        gameOver = true;
        statusText = text;
        selC = selR = -1;
        mustContinue = false;
        RefreshMarkers();
    }

    // =======================================================================
    //  On-screen UI
    // =======================================================================
    void OnGUI()
    {
        var label = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(16, 12, 600, 34), statusText, label);

        var sub = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
        GUI.Label(new Rect(16, 46, 600, 22), "You are Red (bottom). Captures are mandatory.", sub);

        if (GUI.Button(new Rect(16, 74, 120, 30), "New Game"))
            NewGame();

        // Always-visible return to the game library, directly below New Game.
        if (GUI.Button(new Rect(16, 110, 120, 30), "Main Menu"))
            SceneManager.LoadScene("MainMenu");
    }
}
