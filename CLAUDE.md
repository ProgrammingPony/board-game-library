# Board Game Library

A small collection of classic board games built in **Unity 6** (editor `6000.4.9f1`). The
repo currently ships one game, **Checkers**, reached from a main menu; the structure is set
up so more games slot in alongside it.

## Layout

- The Unity project lives in **`BoardGameLibrary/`** (the repo root itself is not the Unity
  project — open `BoardGameLibrary/` in the editor). The folder name matches the repo name in
  PascalCase.
- `Build/` is generated output and is git-ignored.

```
BoardGameLibrary/
  Assets/
    MainMenu/           MainMenuController.cs — IMGUI menu, lists games & launches scenes
    Scenes/             MainMenu.unity (build idx 0), Checkers.unity (build idx 1)
    Scripts/            CheckersGame.cs, CheckersView.cs (MonoBehaviour layer)
      Core/             CheckersRules.cs, CheckersAI.cs (engine-independent game logic)
    Tests/
      EditMode/         CheckersAITests, etc. — pure-logic tests against Core
      PlayMode/         CheckersPlayTests, MainMenuNavigationTests
    Settings/           URP render pipeline assets (PC + Mobile)
  Packages/             UPM manifest; Core uses URP, Input System, UGUI, Test Framework
  ProjectSettings/
```

## Architecture notes

- **Checkers is split into a UI-free core and a Unity view.** `Assets/Scripts/Core`
  (`CheckersCore` asmdef, no engine references) holds the rules and AI; `Assets/Scripts`
  (`Checkers` asmdef) holds the MonoBehaviours that render and drive it. Keep game logic in
  Core so it stays unit-testable without entering Play mode.
- **Assembly definitions** mirror that split: `CheckersCore`, `Checkers`, `MainMenu`, plus
  `CheckersCore.EditModeTests` and `Checkers.PlayModeTests`.
- **The main menu is data-driven.** To add a game, add a `GameEntry { title, sceneName }` to
  the `Games` array in `MainMenuController.cs`, add the scene, and register it in
  **Build Settings** (the `sceneName` must match a scene listed there). `Launch()` and
  `Quit()` are exposed as public methods so PlayMode tests can drive navigation without IMGUI
  clicks.
- **Menu art is generated procedurally in code** (no image assets to import/sync).

## Unity MCP — preferred way to drive the editor

This project has the **Unity MCP** package (`com.ivanmurzak.unity.mcp`) installed, exposing
the editor as MCP skills. When the editor is open, prefer these over computer-use/clicking:

- `editor-application-get-state` / `editor-application-set-state` — inspect/enter/exit Play mode.
- `screenshot-game-view` — see what's on screen.
- `tests-run` — run EditMode/PlayMode tests (every open scene must be saved first, or the run aborts).
- `scene-open`, `scene-list-opened`, `console-get-logs`, `gameobject-*`, `script-*`, etc.

Example — verify the game runs: `scene-open` MainMenu → `editor-application-set-state` to
enter Play mode → `screenshot-game-view`, then call `Launch("Checkers")` (via reflection or
the menu button) and screenshot again.

## Conventions / gotchas

- Shell is **PowerShell** on Windows; the Bash tool is also available.
- `productName` / `metroPackageName` in `ProjectSettings.asset` are `BoardGameLibrary`,
  matching the project folder.
- If a project-setting change doesn't show up in a build, suspect cached editor state rather
  than the setting itself: (1) the open editor holds settings in memory and can overwrite
  external file edits on its next save, so change settings via the editor UI or with it closed;
  (2) some build outputs come from cached state, not the obvious field — e.g. the standalone
  `.exe` name is the filename in the build-location path saved in `EditorUserBuildSettings`
  (reused by "Build And Run"), not `productName`. Inspect/repair live editor state with the
  Unity MCP (`script-execute`) rather than guessing from files on disk.
- Don't commit `Library/` or `Build/` (already git-ignored).
