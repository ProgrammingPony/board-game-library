# Icons

Tooling for the Board Game Library application icon.

## Contents

- **`make_app_icon.py`** — regenerates the app icon. Requires Python with Pillow
  (`pip install pillow`). Run from anywhere:

  ```bash
  python tools/icons/make_app_icon.py
  ```

  It writes `BoardGameLibrary/Assets/Icons/AppIcon.png` (1024×1024). The design is a
  computer mouse (top-left) layered over a checkerboard (bottom-right) on a bright teal
  rounded tile with a light rim — the bright fill + rim keep it legible against the dark
  Windows taskbar when night mode is on.

## How the icon reaches a build

The PNG is wired into Unity as the **Default Icon** and all **Standalone** icon sizes
(see `m_BuildTargetIcons` in `ProjectSettings/ProjectSettings.asset`). That covers the
desktop `.exe` and its OS taskbar icon. Set it through the editor (Player Settings, or
the Unity MCP) rather than hand-editing the asset, so live editor state doesn't overwrite
the change on its next save.

## Deferred: WebGL / browser favicon

We intentionally did **not** create browser-tab images yet. Rationale:

- Unity's Default/Standalone icons do **not** become the browser favicon. A WebGL build's
  tab icon comes from the **WebGL Template** (`Assets/WebGLTemplates/<Name>/`), whose
  `index.html` points at its own `favicon.ico` in `TemplateData/`. The stock template ships
  the Unity-logo favicon.
- Favicons render at 16/32 px. The current `AppIcon.png` is detailed (mouse + board +
  pieces) and goes muddy at 16 px, so a browser version wants a **simplified favicon
  variant** (bolder board, larger mouse, no small checker pieces), not a straight downscale.
- There is **no browser/WebGL build yet**. Rather than guess at template layout and sizes,
  we deferred the favicon work until the WebGL build actually exists.

When the browser version is implemented:
1. Add a simplified favicon generator here (or extend `make_app_icon.py`) producing
   `favicon.ico` at 16/32/48 px.
2. Create a custom WebGL template under `Assets/WebGLTemplates/`, drop the favicon in its
   `TemplateData/`, reference it from the template `index.html`, and select that template in
   Player Settings → Resolution and Presentation → WebGL Template.
