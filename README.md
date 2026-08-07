# TheWicken

A **Slay the Spire 2** character mod ("The Witch") — Godot 4.5 / C# (net9.0), loaded by the game at runtime. See [CLAUDE.md](CLAUDE.md) for architecture and conventions.

## Setup

1. Install Slay the Spire 2 (the build finds it via the Steam registry; override with `local.props` if needed).
2. Place the decompiled game source at `gamedata/` (local-only, gitignored — proprietary, never committed).
3. **Required — create the gamedata junctions:**

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools\setup-gamedata-links.ps1
   ```

   This creates repo-root junctions `scenes/`, `materials/`, `shaders/`, `images/` → `gamedata/<same>` so the Godot editor resolves base-game `res://` paths referenced by mod scenes (at runtime they resolve from the game's pck regardless). The junctions are gitignored and excluded from export in `export_presets.cfg` — don't remove those excludes, or game assets get packed into the mod `.pck`.

## Build

```powershell
dotnet build TheWitch.csproj   # compile + deploy into the game's mods/ folder
dotnet publish                 # additionally exports the .pck (needs Godot 4.5.x mono, see Directory.Build.props)
```

Test by launching Slay the Spire 2 — there is no standalone app.
