using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode test for the main-menu → game navigation. It loads the MainMenu scene the
/// same way a build does on launch (it is build index 0), exercises the menu's launch
/// action, and asserts the Checkers scene actually loads and its game comes up live.
/// This spans real scene loads across frames, which is why it must be a PlayMode (not
/// EditMode) test.
/// </summary>
public class MainMenuNavigationTests
{
    const int N = 8;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Enter from the menu scene — the same entry point a built player uses.
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator SelectingCheckers_LoadsTheCheckersGame()
    {
        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "The menu scene should be loaded.");

        var menu = Object.FindAnyObjectByType<MainMenuController>();
        Assert.IsNotNull(menu, "The MainMenu scene should contain a MainMenuController.");

        // The catalogue is what the menu draws its buttons from; Checkers must be offered.
        var checkers = System.Array.Find(MainMenuController.Games, g => g.sceneName == "Checkers");
        Assert.AreEqual("Checkers", checkers.sceneName, "The menu catalogue should include the Checkers game.");
        Assert.IsFalse(string.IsNullOrEmpty(checkers.title), "The Checkers entry should have a display title.");

        // Trigger exactly what the "Checkers" button does.
        menu.Launch(checkers.sceneName);

        // The load applies on a later frame — wait for it, with a timeout guard.
        float t = 0f;
        while (SceneManager.GetActiveScene().name != "Checkers" && t < 5f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        Assert.AreEqual("Checkers", SceneManager.GetActiveScene().name,
            "Selecting Checkers should navigate to the Checkers scene.");

        // Give the freshly loaded scene's MonoBehaviours a frame to run Start().
        yield return null;
        yield return null;

        // The Checkers game should come up live in its opening position.
        var game = Object.FindAnyObjectByType<CheckersGame>();
        Assert.IsNotNull(game, "The loaded Checkers scene should host a CheckersGame.");
        Assert.IsTrue(game.IsHumanTurn, "A fresh Checkers game starts on the human's turn.");

        int red = 0;
        for (int c = 0; c < N; c++)
            for (int r = 0; r < N; r++)
                if (game.CellValue(c, r) > 0) red++;
        Assert.AreEqual(12, red, "A fresh Checkers board has 12 human (Red) pieces.");
    }

    [UnityTest]
    public IEnumerator ReturnToDesktop_TriggersTheQuitAction()
    {
        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "The menu scene should be loaded.");

        var menu = Object.FindAnyObjectByType<MainMenuController>();
        Assert.IsNotNull(menu, "The MainMenu scene should contain a MainMenuController.");

        // Substitute the quit action so the test verifies the button's wiring without
        // the real quit firing — in the editor that stops play mode, which would tear
        // down the test run itself. This mirrors how the other test drives Launch()
        // directly rather than simulating an IMGUI click.
        bool quit = false;
        menu.QuitAction = () => quit = true;

        // Trigger exactly what the "Return to Desktop" button does.
        menu.Quit();

        Assert.IsTrue(quit, "The 'Return to Desktop' button should invoke the quit action.");

        // Still on the menu — quitting is the only side effect, no scene navigation.
        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name,
            "Returning to desktop should not navigate to another scene.");
        yield break;
    }
}
