using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All scene visuals for the checkers game: camera, lighting, board, pieces and
/// move markers, plus screen-to-cell ray-picking. Holds no game logic or rules —
/// the controller hands it a board to render and a selection to highlight.
/// </summary>
public class CheckersView
{
    const int N = CheckersRules.N;

    Camera cam;
    Transform boardRoot, piecesRoot, markerRoot;
    Material lightMat, darkMat, humanMat, computerMat, kingRimMat, kingDotMat, frameMat, targetMat, selectMat, aiMat;

    readonly Color lightSquare = new Color(0.86f, 0.84f, 0.76f);
    readonly Color darkSquare = new Color(0.18f, 0.33f, 0.24f);
    readonly Color humanColor = new Color(0.83f, 0.16f, 0.16f);   // Red
    readonly Color computerColor = new Color(0.95f, 0.95f, 0.92f); // Warm white (night-light safe)
    readonly Color kingRimColor = new Color(0.09f, 0.09f, 0.11f); // King crown ring (near-black, high-contrast)
    readonly Color kingDotColor = new Color(0.98f, 0.97f, 0.90f); // King crown center (light)
    readonly Color targetColor = new Color(0.95f, 0.90f, 0.25f);  // Move hints
    readonly Color selectColor = new Color(0.30f, 0.95f, 0.40f);  // Selected piece
    readonly Color aiColor = new Color(1f, 0.62f, 0.18f);         // Last AI move (amber, night-light safe)
    readonly Color frameColor = new Color(0.30f, 0.20f, 0.12f);   // Wooden board frame

    // =======================================================================
    //  Setup
    // =======================================================================
    public void Build(Transform root)
    {
        BuildMaterials();
        SetupCamera();
        SetupLighting();
        BuildBoardVisuals(root);

        piecesRoot = new GameObject("Pieces").transform;
        piecesRoot.SetParent(root, false);
        markerRoot = new GameObject("Markers").transform;
        markerRoot.SetParent(root, false);
    }

    void BuildMaterials()
    {
        // Board and pieces are lit so they catch shading/highlights and read as 3D.
        lightMat = MakeLit(lightSquare, 0.15f, 0f);
        darkMat = MakeLit(darkSquare, 0.15f, 0f);
        humanMat = MakeLit(humanColor, 0.45f, 0f);
        computerMat = MakeLit(computerColor, 0.45f, 0f);
        kingRimMat = MakeLit(kingRimColor, 0.30f, 0f);
        kingDotMat = MakeLit(kingDotColor, 0.30f, 0f);
        frameMat = MakeLit(frameColor, 0.10f, 0f);
        // Highlights stay unlit so they glow as clear UI cues regardless of lighting.
        targetMat = MakeUnlit(targetColor);
        selectMat = MakeUnlit(selectColor);
        aiMat = MakeUnlit(aiColor);
    }

    Material MakeLit(Color c, float smoothness, float metallic)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
        var m = new Material(s);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        m.color = c;
        return m;
    }

    Material MakeUnlit(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Standard");
        var m = new Material(s);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
        return m;
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        // Perspective view from behind the human (Red) side, tilted down — a 3D board look.
        cam.orthographic = false;
        cam.fieldOfView = 42f;
        cam.nearClipPlane = 0.1f;
        cam.transform.position = new Vector3(N / 2f, 9.2f, -4.6f);
        cam.transform.LookAt(new Vector3(N / 2f, 0.3f, N / 2f + 0.4f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.09f, 0.11f);
    }

    void SetupLighting()
    {
        Light dir = null;
#pragma warning disable CS0618
        foreach (var l in Object.FindObjectsOfType<Light>())
#pragma warning restore CS0618
            if (l.type == LightType.Directional) { dir = l; break; }
        if (dir == null)
        {
            var go = new GameObject("Sun");
            dir = go.AddComponent<Light>();
            dir.type = LightType.Directional;
        }
        dir.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        dir.intensity = 1.15f;
        dir.color = Color.white;
        dir.shadows = LightShadows.Soft; // pieces cast soft shadows on the board

        // Flat ambient fill so shaded sides stay readable without washing out the shading.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);
    }

    void BuildBoardVisuals(Transform root)
    {
        boardRoot = new GameObject("Board").transform;
        boardRoot.SetParent(root, false);

        // Wooden frame/base that the playing squares sit on.
        var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Frame";
        frame.transform.SetParent(boardRoot, false);
        frame.transform.localScale = new Vector3(N + 0.8f, 0.36f, N + 0.8f);
        frame.transform.position = new Vector3(N / 2f, -0.12f, N / 2f);
        StripCollider(frame);
        frame.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

        for (int c = 0; c < N; c++)
        {
            for (int r = 0; r < N; r++)
            {
                var sq = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sq.name = $"Sq_{c}_{r}";
                sq.transform.SetParent(boardRoot, false);
                sq.transform.localScale = new Vector3(1f, 0.2f, 1f);
                sq.transform.position = new Vector3(c + 0.5f, 0f, r + 0.5f);
                // Keep the square's collider so clicks can be ray-picked in the tilted 3D view.
                sq.GetComponent<MeshRenderer>().sharedMaterial = CheckersRules.IsDark(c, r) ? darkMat : lightMat;
            }
        }
    }

    void StripCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
    }

    // =======================================================================
    //  Rendering
    // =======================================================================
    public void RenderPieces(int[,] board)
    {
        // DestroyImmediate (not Destroy) so the old pieces are cleared synchronously.
        // Destroy defers to end-of-frame; rebuilding within a single frame would then
        // leave stale/overlapping piece objects (e.g. a promoted piece's old disc).
        for (int i = piecesRoot.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(piecesRoot.GetChild(i).gameObject);

        const float boardTop = 0.10f; // top surface of the board squares
        const float pieceH = 0.16f;   // disc thickness (depth)
        const float diam = 0.80f;

        for (int c = 0; c < N; c++)
        {
            for (int r = 0; r < N; r++)
            {
                int v = board[c, r];
                if (v == 0) continue;

                Material pieceMat = v > 0 ? humanMat : computerMat;

                // Base disc (a proper 3D checker), resting flat on the board (bottom at boardTop).
                // Keeps its collider for click ray-picking.
                var baseDisc = MakeDisc(c, r, boardTop + pieceH / 2f, diam, pieceH, pieceMat, keepCollider: true);
                baseDisc.name = $"P_{c}_{r}";

                if (CheckersRules.IsKing(v))
                {
                    // Classic king = a second disc stacked on top (clear 3D shape cue),
                    // topped with a dark ring + light center. The light/dark marker stays
                    // readable on any piece color and survives Night Light (warm mode),
                    // which only attenuates blue and would wash out a yellow/gold crown.
                    MakeDisc(c, r, boardTop + pieceH * 1.5f, diam, pieceH, pieceMat, keepCollider: false).name = "KingTier";
                    float stackTop = boardTop + pieceH * 2f;
                    MakeDisc(c, r, stackTop + 0.025f, 0.52f, 0.05f, kingRimMat, keepCollider: false).name = "CrownRim";
                    MakeDisc(c, r, stackTop + 0.045f, 0.24f, 0.06f, kingDotMat, keepCollider: false).name = "CrownDot";
                }
            }
        }
    }

    GameObject MakeDisc(int c, int r, float yCenter, float diameter, float height, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(piecesRoot, false);
        go.transform.localScale = new Vector3(diameter, height / 2f, diameter);
        go.transform.position = new Vector3(c + 0.5f, yCenter, r + 0.5f);
        if (!keepCollider) StripCollider(go);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>
    /// Redraw the overlay markers: the last computer move (from/to), the selected
    /// piece, and its legal destination cells. Pass selC &lt; 0 for no selection,
    /// and aiFrom.x &lt; 0 for no AI-move highlight.
    /// </summary>
    public void RenderMarkers(int selC, int selR, List<Vector2Int> targets, Vector2Int aiFrom, Vector2Int aiTo)
    {
        for (int i = markerRoot.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(markerRoot.GetChild(i).gameObject);

        // Highlight the last computer move so the player can follow along.
        if (aiFrom.x >= 0) SpawnMarker(aiFrom.x, aiFrom.y, 0.86f, aiMat, 0.12f);
        if (aiTo.x >= 0) SpawnMarker(aiTo.x, aiTo.y, 0.86f, aiMat, 0.12f);

        if (selC >= 0)
        {
            SpawnMarker(selC, selR, 0.94f, selectMat, 0.13f);
            if (targets != null)
                foreach (var t in targets)
                    SpawnMarker(t.x, t.y, 0.36f, targetMat, 0.14f);
        }
    }

    void SpawnMarker(int c, int r, float size, Material mat, float y)
    {
        var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        m.name = "Marker";
        m.transform.SetParent(markerRoot, false);
        m.transform.localScale = new Vector3(size, 0.04f, size);
        m.transform.position = new Vector3(c + 0.5f, y, r + 0.5f);
        StripCollider(m);
        m.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // =======================================================================
    //  Input mapping
    // =======================================================================
    /// <summary>
    /// Map a screen position to a board cell via a physics ray-pick against the
    /// squares/pieces, so picking stays accurate from the tilted 3D angle
    /// (a flat-plane projection would mis-click behind tall pieces).
    /// </summary>
    public bool ScreenToCell(Vector3 screenPos, out int c, out int r)
    {
        c = r = -1;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return false;
        c = Mathf.FloorToInt(hit.point.x);
        r = Mathf.FloorToInt(hit.point.z);
        return c >= 0 && c < N && r >= 0 && r < N;
    }
}
