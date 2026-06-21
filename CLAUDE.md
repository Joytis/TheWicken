# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A **Slay the Spire 2 character mod** ("TheWicken") built from the Alchyr Sts2 BaseLib template. It is a Godot 4.5 / C# (net9.0) project that compiles to a `.dll` + `.pck` loaded by the game at runtime via Harmony patching. There is no standalone runnable app — output is consumed by Slay the Spire 2.

## Build & run

```bash
dotnet build          # compiles, then auto-copies .dll/.pdb/.json into the game's mods/ folder
dotnet publish        # build + invokes headless Godot to export the .pck (full asset packaging)
```

- **Build = deploy.** The `CopyToModsFolderOnBuild` target copies outputs straight into `<Sts2Path>/mods/TheWicken/`. To test in-game: build, then launch Slay the Spire 2.
- The game dir is auto-discovered from the Steam registry/library by [Sts2PathDiscovery.props](Sts2PathDiscovery.props). Override via a `local.props` or `/p:Sts2Path=...` if discovery fails.
- `dotnet publish` requires a real Godot mono executable; path is set in [Directory.Build.props](Directory.Build.props) (`GodotPath`). **Must be Godot 4.5.x** — the game refuses `.pck` files exported by a newer Godot.
- Build references `sts2.dll` and `0Harmony.dll` from the installed game; building fails with a clear error if the game isn't found.
- No test suite. Validation is manual in-game.

## Architecture

The mod assembly is **not** the game's assembly. Game types live under `MegaCrit.Sts2.Core.*`; the BaseLib helper layer under `BaseLib.*`. All mod code is in [TheWickenCode/](TheWickenCode/); all assets/data in [TheWicken/](TheWicken/).

**Entry point:** [MainFile.cs](TheWickenCode/MainFile.cs) is marked `[ModInitializer]`. Its only job is `harmony.PatchAll()` — all integration with the game happens through BaseLib's model registration + Harmony patches, not a main loop.

**Content model pattern.** Each content type has an abstract mod-base class that wires up asset paths, then concrete content subclasses it:
- [Cards/TheWickenCard.cs](TheWickenCode/Cards/TheWickenCard.cs) → `CustomCardModel`
- [Powers/TheWickenPower.cs](TheWickenCode/Powers/TheWickenPower.cs) → `CustomPowerModel`
- [Relics/TheWickenRelic.cs](TheWickenCode/Relics/TheWickenRelic.cs) → `CustomRelicModel`
- [Potions/TheWickenPotion.cs](TheWickenCode/Potions/TheWickenPotion.cs) → `CustomPotionModel`

Create new content by subclassing the relevant base in its folder (the BaseLib `ModAnalyzers` and IDE templates assist). Models are looked up at runtime via `ModelDb.Card<T>()`, `ModelDb.Relic<T>()`, etc.

**Pools.** [Character/](TheWickenCode/Character/) defines the character and its `CardPool`/`RelicPool`/`PotionPool`. Content is bound to a pool with the `[Pool(typeof(...))]` attribute on the base class — so individual cards/relics inherit pool membership and don't declare it themselves. [Character/TheWicken.cs](TheWickenCode/Character/TheWicken.cs) (`PlaceholderCharacterModel`) defines starting HP/deck/relics and references the pools; it currently uses base-game Ironclad placeholders.

**Asset path convention (important).** Content classes derive their image path from `Id.Entry` — the model id, lowercased with the mod prefix stripped (`RemovePrefix().ToLowerInvariant()`). So a card with id `THEWICKEN-Foo` loads `TheWicken/images/card_portraits/foo.png` (and `big/foo.png` for full art). Path helpers live in [Extensions/StringExtensions.cs](TheWickenCode/Extensions/StringExtensions.cs); they fall back to a default placeholder image and log when a file is missing. Add art at the matching path rather than overriding the path methods.

**Localization** is JSON under [TheWicken/localization/eng/](TheWicken/localization/) (`cards.json`, `powers.json`, `relics.json`, `characters.json`, keywords, hover tips). Keys follow `THEWICKEN-<ENTRY>.<field>`. These files are fed to the BaseLib analyzer (`AdditionalFiles` in the csproj) so missing/extra localization is caught at build time.

## Conventions & gotchas

- `Nullable` is enabled and `<TreatWarningsAsErrors>` is not, but the BaseLib analyzers surface mod-specific mistakes — read analyzer warnings.
- The csproj **excludes** `TheWicken/**`, `materials/`, `shaders/`, `images/` from compilation — that tree is Godot assets, not C#. Don't put `.cs` there.
- `TheWicken.json` is the mod manifest (id, version, `min_game_version`, BaseLib dependency). The build auto-syncs the BaseLib `min_version` in this file to the actually-restored package version (`UpdateDependencyVersions` target) — don't hand-edit that field.
- The mod id `"TheWicken"` (in [MainFile.cs](TheWickenCode/MainFile.cs)) is the resource path root (`res://TheWicken`) and Harmony id; keep it in sync with the manifest and folder name.
- `git` is configured to normalize all text to LF ([.gitattributes](.gitattributes)).
