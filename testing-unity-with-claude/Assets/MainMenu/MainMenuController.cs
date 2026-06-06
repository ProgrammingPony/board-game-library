using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight main-menu for the demo repository. Lists the games that ship in
/// this project and launches the chosen one by loading its scene. Add a new entry
/// to <see cref="Games"/> as more games are added — each is just a display name
/// plus the build-settings scene name to load.
///
/// Uses IMGUI (<see cref="OnGUI"/>) to stay consistent with the in-game UI and to
/// avoid pulling in a Canvas / EventSystem just for a list of buttons.
///
/// The menu draws the game list inside a rounded "card". The top of the card is a
/// procedurally generated, colorful illustration of game objects (a controller, a
/// Connect Four board, dice, confetti); the lower part is a calm dark panel that the
/// buttons sit on. The art lifts the whole card off the dark scene background so the
/// buttons read with strong contrast, while staying decorative rather than loud. All
/// art is generated in code, so there are no image assets to import or keep in sync.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [System.Serializable]
    public struct GameEntry
    {
        public string title;     // shown on the button
        public string sceneName; // scene to load (must be in Build Settings)
    }

    // The catalogue of playable games. Currently just Checkers; new games slot in here.
    public static readonly GameEntry[] Games =
    {
        new GameEntry { title = "Checkers", sceneName = "Checkers" },
    };

    // ---- Layout constants (logical pixels) ----
    const float CardW = 380f;   // card width
    const float HeaderH = 156f; // height of the colorful illustration band
    const float TopPad = 20f;   // padding above the title, inside the body
    const float TitleH = 40f;
    const float SubH = 22f;
    const float TitleGap = 16f; // gap between subtitle and first button
    const float ButtonH = 52f;
    const float ButtonGap = 12f;
    const float QuitGap = 18f;  // extra space separating the games from "Return to Desktop"
    const float BottomPad = 22f;

    // ---- Generated art (built once, reused every frame) ----
    Texture2D _card;       // the rounded card: colorful header + dark body
    Texture2D _btnNormal;  // button backgrounds
    Texture2D _btnHover;
    Texture2D _btnActive;
    Texture2D _quitNormal; // "Return to Desktop" button — subdued, secondary look
    Texture2D _quitHover;
    Texture2D _quitActive;
    float _cardH;          // total card height, derived from the game count

    /// <summary>
    /// Launch a game by its build-settings scene name. This is the navigation action
    /// the menu buttons trigger, exposed separately so it can be driven directly
    /// (e.g. from a PlayMode test) without going through an IMGUI click.
    /// </summary>
    public void Launch(string sceneName) => SceneManager.LoadScene(sceneName);

    /// <summary>
    /// Overridable quit action. Defaults to the real platform quit (see
    /// <see cref="DefaultQuit"/>); a PlayMode test swaps it so it can verify the
    /// "Return to Desktop" button's wiring without actually terminating the editor
    /// (and the test run) along with it.
    /// </summary>
    public System.Action QuitAction;

    /// <summary>
    /// Exit the application, returning the player to the desktop. Exposed separately
    /// (like <see cref="Launch"/>) so it can be driven directly from a test without an
    /// IMGUI click.
    /// </summary>
    public void Quit() => (QuitAction ?? DefaultQuit)();

    // The real quit. In the editor, Application.Quit is a no-op, so stop play mode
    // instead to give the same "back to where you were" behaviour.
    static void DefaultQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDisable()
    {
        // Generated textures aren't garbage-collected; release them explicitly.
        Destroy(_card);
        Destroy(_btnNormal);
        Destroy(_btnHover);
        Destroy(_btnActive);
        Destroy(_quitNormal);
        Destroy(_quitHover);
        Destroy(_quitActive);
        _card = null;
    }

    void OnGUI()
    {
        EnsureArt();

        float x = Mathf.Round((Screen.width - CardW) / 2f);
        float y = Mathf.Round(Mathf.Max(40f, (Screen.height - _cardH) / 2f - 10f));

        // Soft drop shadow: re-draw the card's silhouette (its alpha mask gives us the
        // rounded shape for free) in translucent black, offset down-right.
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(x + 6f, y + 8f, CardW, _cardH), _card, ScaleMode.StretchToFill, true);
        GUI.color = prev;

        // The card itself.
        GUI.DrawTexture(new Rect(x, y, CardW, _cardH), _card, ScaleMode.StretchToFill, true);

        // Title + subtitle sit on the dark body, just below the illustration band.
        var title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        var sub = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = new Color(0.72f, 0.74f, 0.82f) }
        };
        var button = new GUIStyle(GUI.skin.button)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = _btnNormal, textColor = new Color(0.10f, 0.11f, 0.15f) },
            hover = { background = _btnHover, textColor = new Color(0.06f, 0.07f, 0.10f) },
            active = { background = _btnActive, textColor = new Color(0.06f, 0.07f, 0.10f) },
            border = new RectOffset(0, 0, 0, 0)
        };

        float innerX = x + 26f;
        float innerW = CardW - 52f;
        float ty = y + HeaderH + TopPad;
        GUI.Label(new Rect(innerX, ty, innerW, TitleH), "Game Library", title);
        GUI.Label(new Rect(innerX, ty + TitleH, innerW, SubH), "Select a game to play", sub);

        float by = ty + TitleH + SubH + TitleGap;
        foreach (var game in Games)
        {
            if (GUI.Button(new Rect(innerX, by, innerW, ButtonH), game.title, button))
                Launch(game.sceneName);
            by += ButtonH + ButtonGap;
        }

        // "Return to Desktop" sits apart from the games, styled as a subdued, secondary
        // exit action so it doesn't compete with the game buttons.
        var quit = new GUIStyle(button)
        {
            normal = { background = _quitNormal, textColor = new Color(0.82f, 0.84f, 0.90f) },
            hover = { background = _quitHover, textColor = Color.white },
            active = { background = _quitActive, textColor = new Color(0.82f, 0.84f, 0.90f) }
        };
        by += QuitGap - ButtonGap;
        if (GUI.Button(new Rect(innerX, by, innerW, ButtonH), "Return to Desktop", quit))
            Quit();
    }

    // ---------------------------------------------------------------------------
    // Procedural art
    // ---------------------------------------------------------------------------

    void EnsureArt()
    {
        if (_card != null) return;

        float bodyH = TopPad + TitleH + SubH + TitleGap
                      + Games.Length * ButtonH + (Games.Length - 1) * ButtonGap
                      + QuitGap + ButtonH   // the "Return to Desktop" button
                      + BottomPad;
        _cardH = HeaderH + bodyH;

        _card = BuildCard((int)CardW, (int)_cardH, (int)HeaderH);
        _btnNormal = Solid(new Color(0.92f, 0.94f, 0.98f));
        _btnHover = Solid(new Color(1f, 1f, 1f));
        _btnActive = Solid(new Color(0.80f, 0.84f, 0.92f));
        _quitNormal = Solid(new Color(0.22f, 0.24f, 0.30f));
        _quitHover = Solid(new Color(0.30f, 0.32f, 0.40f));
        _quitActive = Solid(new Color(0.17f, 0.18f, 0.23f));
    }

    static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    /// <summary>
    /// Build the card texture: a colorful illustration band on top, a calm dark panel
    /// below for the buttons, with rounded corners. Authored in top-down image space
    /// (y grows downward); <see cref="Blend"/> converts to the texture's bottom-up rows.
    /// </summary>
    Texture2D BuildCard(int w, int h, int headerH)
    {
        var px = new Color[w * h];

        var body = new Color(0.137f, 0.149f, 0.184f); // calm panel the buttons sit on
        for (int i = 0; i < px.Length; i++) px[i] = body;

        // Colorful header gradient (indigo -> purple -> teal, on the diagonal).
        var c0 = new Color(0.16f, 0.18f, 0.42f);
        var c1 = new Color(0.42f, 0.20f, 0.50f);
        var c2 = new Color(0.13f, 0.46f, 0.50f);
        for (int y = 0; y < headerH; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = Mathf.Clamp01((x / (float)w + y / (float)headerH) * 0.5f);
                Color g = t < 0.5f ? Color.Lerp(c0, c1, t * 2f) : Color.Lerp(c1, c2, (t - 0.5f) * 2f);
                px[Idx(x, y, w, h)] = g;
            }
        }

        // Scattered confetti dots (the "random objects" vibe), low alpha so they recede.
        var rng = new System.Random(7);
        Color[] confetti =
        {
            new Color(1f, 0.85f, 0.30f), new Color(0.30f, 0.80f, 0.95f),
            new Color(0.95f, 0.45f, 0.55f), new Color(0.55f, 0.90f, 0.55f),
            new Color(1f, 0.60f, 0.30f),
        };
        for (int i = 0; i < 16; i++)
        {
            int cx = rng.Next(8, w - 8);
            int cy = rng.Next(8, headerH - 8);
            Disc(px, w, h, cx, cy, rng.Next(3, 6), confetti[rng.Next(confetti.Length)], 0.22f);
        }

        // Three recognizable game objects across the band.
        DrawController(px, w, h, 78, headerH / 2 + 6, 1.0f);
        DrawConnectFour(px, w, h, 196, headerH / 2 - 2);
        DrawDice(px, w, h, 312, headerH / 2 + 4);

        // Subtle dark gradient at the bottom of the header so it reads as settling into
        // the body, and the title below stays legible.
        for (int y = headerH - 22; y < headerH; y++)
        {
            float a = (y - (headerH - 22)) / 22f * 0.5f;
            for (int x = 0; x < w; x++) Blend(px, w, h, x, y, body, a);
        }

        RoundCorners(px, w, h, 18);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // ---- pixel helpers (image space: y grows downward) ----

    static int Idx(int x, int yTop, int w, int h) => (h - 1 - yTop) * w + x;

    static void Blend(Color[] px, int w, int h, int x, int yTop, Color c, float a)
    {
        if (x < 0 || x >= w || yTop < 0 || yTop >= h || a <= 0f) return;
        int i = Idx(x, yTop, w, h);
        px[i] = Color.Lerp(px[i], c, Mathf.Clamp01(a));
    }

    static void FillRect(Color[] px, int w, int h, int x0, int y0, int x1, int y1, Color c, float a = 1f)
    {
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                Blend(px, w, h, x, y, c, a);
    }

    static void Disc(Color[] px, int w, int h, int cx, int cy, float r, Color c, float a = 1f)
    {
        int r0 = Mathf.CeilToInt(r) + 1;
        for (int y = cy - r0; y <= cy + r0; y++)
            for (int x = cx - r0; x <= cx + r0; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float edge = Mathf.Clamp01(r - d);          // 1px antialiased edge
                if (edge > 0f) Blend(px, w, h, x, y, c, a * edge);
            }
    }

    /// <summary>Rounded-rect fill via per-corner distance test.</summary>
    static void RoundRect(Color[] px, int w, int h, int x0, int y0, int x1, int y1, float rad, Color c, float a = 1f)
    {
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                float dx = 0f, dy = 0f;
                if (x < x0 + rad) dx = x0 + rad - x; else if (x > x1 - 1 - rad) dx = x - (x1 - 1 - rad);
                if (y < y0 + rad) dy = y0 + rad - y; else if (y > y1 - 1 - rad) dy = y - (y1 - 1 - rad);
                float edge = Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                if (dx == 0f && dy == 0f) edge = 1f;
                if (edge > 0f) Blend(px, w, h, x, y, c, a * edge);
            }
    }

    /// <summary>Knock the card's outer corners transparent so it reads as a rounded card.</summary>
    static void RoundCorners(Color[] px, int w, int h, float rad)
    {
        for (int yTop = 0; yTop < h; yTop++)
            for (int x = 0; x < w; x++)
            {
                float dx = 0f, dy = 0f;
                if (x < rad) dx = rad - x; else if (x > w - 1 - rad) dx = x - (w - 1 - rad);
                if (yTop < rad) dy = rad - yTop; else if (yTop > h - 1 - rad) dy = yTop - (h - 1 - rad);
                if (dx == 0f && dy == 0f) continue;
                float inside = Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                int i = Idx(x, yTop, w, h);
                var col = px[i];
                col.a = Mathf.Min(col.a, inside);
                px[i] = col;
            }
    }

    // ---- game-object icons (drawn around center cx,cy in image space) ----

    static void DrawController(Color[] px, int w, int h, int cx, int cy, float s)
    {
        var bodyCol = new Color(0.20f, 0.22f, 0.30f);
        var grip = new Color(0.16f, 0.17f, 0.24f);
        // pill body: two grip discs joined by a bar
        Disc(px, w, h, cx - 22, cy + 4, 16, grip);
        Disc(px, w, h, cx + 22, cy + 4, 16, grip);
        RoundRect(px, w, h, cx - 24, cy - 14, cx + 24, cy + 14, 12, bodyCol);
        // left thumbstick
        Disc(px, w, h, cx - 13, cy - 1, 6, new Color(0.10f, 0.11f, 0.16f));
        Disc(px, w, h, cx - 13, cy - 1, 3, new Color(0.45f, 0.48f, 0.58f));
        // d-pad (cross)
        FillRect(px, w, h, cx - 16, cy + 7, cx - 10, cy + 17, new Color(0.10f, 0.11f, 0.16f));
        FillRect(px, w, h, cx - 19, cy + 10, cx - 7, cy + 14, new Color(0.10f, 0.11f, 0.16f));
        // four face buttons (diamond), classic colors
        Disc(px, w, h, cx + 13, cy - 6, 3.2f, new Color(0.95f, 0.80f, 0.25f)); // top - yellow
        Disc(px, w, h, cx + 13, cy + 6, 3.2f, new Color(0.40f, 0.80f, 0.45f)); // bottom - green
        Disc(px, w, h, cx + 7, cy, 3.2f, new Color(0.40f, 0.65f, 0.95f));      // left - blue
        Disc(px, w, h, cx + 19, cy, 3.2f, new Color(0.92f, 0.42f, 0.45f));     // right - red
    }

    static void DrawConnectFour(Color[] px, int w, int h, int cx, int cy)
    {
        var board = new Color(0.20f, 0.42f, 0.88f);
        int cols = 5, rows = 4, gap = 11, r = 4;
        int bw = cols * gap + 6, bh = rows * gap + 6;
        RoundRect(px, w, h, cx - bw / 2, cy - bh / 2, cx + bw / 2, cy + bh / 2, 6, board);
        int x0 = cx - bw / 2 + 8, y0 = cy - bh / 2 + 8;
        var hole = new Color(0.10f, 0.13f, 0.22f);
        var red = new Color(0.93f, 0.32f, 0.34f);
        var yellow = new Color(0.98f, 0.82f, 0.28f);
        for (int c = 0; c < cols; c++)
            for (int rr = 0; rr < rows; rr++)
            {
                int hx = x0 + c * gap, hy = y0 + rr * gap;
                // a few discs landed at the bottom rows; rest are empty holes
                Color fill = hole;
                if (rr == rows - 1 && (c % 2 == 0)) fill = red;
                else if (rr == rows - 1) fill = yellow;
                else if (rr == rows - 2 && c == 2) fill = red;
                Disc(px, w, h, hx, hy, r, fill);
            }
    }

    static void DrawDice(Color[] px, int w, int h, int cx, int cy)
    {
        var face = new Color(0.96f, 0.96f, 0.98f);
        var pip = new Color(0.12f, 0.13f, 0.18f);
        // die one (5)
        RoundRect(px, w, h, cx - 18, cy - 16, cx + 4, cy + 6, 5, face);
        int ax = cx - 7, ay = cy - 5;
        Disc(px, w, h, ax - 6, ay - 6, 1.8f, pip);
        Disc(px, w, h, ax + 6, ay - 6, 1.8f, pip);
        Disc(px, w, h, ax, ay, 1.8f, pip);
        Disc(px, w, h, ax - 6, ay + 6, 1.8f, pip);
        Disc(px, w, h, ax + 6, ay + 6, 1.8f, pip);
        // die two (3), overlapping, tinted
        var face2 = new Color(0.98f, 0.86f, 0.40f);
        RoundRect(px, w, h, cx - 4, cy - 4, cx + 18, cy + 18, 5, face2);
        int bx = cx + 7, by = cy + 7;
        Disc(px, w, h, bx - 6, by - 6, 1.8f, pip);
        Disc(px, w, h, bx, by, 1.8f, pip);
        Disc(px, w, h, bx + 6, by + 6, 1.8f, pip);
    }
}
