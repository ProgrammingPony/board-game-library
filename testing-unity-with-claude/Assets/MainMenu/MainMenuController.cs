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

    /// <summary>
    /// Launch a game by its build-settings scene name. This is the navigation action
    /// the menu buttons trigger, exposed separately so it can be driven directly
    /// (e.g. from a PlayMode test) without going through an IMGUI click.
    /// </summary>
    public void Launch(string sceneName) => SceneManager.LoadScene(sceneName);

    void OnGUI()
    {
        var title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        var sub = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };
        var button = new GUIStyle(GUI.skin.button) { fontSize = 20 };

        const float panelW = 360f;
        float x = (Screen.width - panelW) / 2f;
        float y = Mathf.Max(60f, Screen.height * 0.18f);

        GUI.Label(new Rect(x, y, panelW, 48f), "Game Library", title);
        GUI.Label(new Rect(x, y + 50f, panelW, 24f), "Select a game to play", sub);

        float by = y + 96f;
        foreach (var game in Games)
        {
            if (GUI.Button(new Rect(x, by, panelW, 52f), game.title, button))
                Launch(game.sceneName);
            by += 64f;
        }
    }
}
