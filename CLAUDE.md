# CLAUDE.md


Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

**When asked to review existing code, "is this correct?" is a question, not a request for changes.** Answer it. Don't edit.

**Never call code broken without verifying it against `gamedata/`.** Trace the actual execution path and check the arithmetic. A defect claim needs a concrete failing case you have confirmed — not a plausible-sounding edge case. If you can't demonstrate the failure, you haven't found one.

**Game design decisions are the user's, not yours.** Behavior that looks wrong (a relic bypassing Exhaust, a Power card returning to hand, an effect silently wasted at the hand cap) is usually intended. Ask what the intent is before labeling it a bug. Never mix design opinions into a correctness review.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## What this is

A **Slay the Spire 2 character mod** ("TheWitch") built from the Alchyr Sts2 BaseLib template. It is a Godot 4.5 / C# (net9.0) project that compiles to a `.dll` + `.pck` loaded by the game at runtime via Harmony patching. There is no standalone runnable app — output is consumed by Slay the Spire 2.

## Build & run

```bash
dotnet build          # compiles, then auto-copies .dll/.pdb/.json into the game's mods/ folder
dotnet publish        # build + invokes headless Godot to export the .pck (full asset packaging)
```

- **Build = deploy.** The `CopyToModsFolderOnBuild` target copies outputs straight into `<Sts2Path>/mods/TheWitch/`. To test in-game: build, then launch Slay the Spire 2.
- The game dir is auto-discovered from the Steam registry/library by [Sts2PathDiscovery.props](Sts2PathDiscovery.props). Override via a `local.props` or `/p:Sts2Path=...` if discovery fails.
- `dotnet publish` requires a real Godot mono executable; path is set in [Directory.Build.props](Directory.Build.props) (`GodotPath`). **Must be Godot 4.5.x** — the game refuses `.pck` files exported by a newer Godot.
- Build references `sts2.dll` and `0Harmony.dll` from the installed game; building fails with a clear error if the game isn't found.
- No test suite. Validation is manual in-game.

## Architecture

The mod assembly is **not** the game's assembly. Game types live under `MegaCrit.Sts2.Core.*`; the BaseLib helper layer under `BaseLib.*`. All mod code is in [TheWitchCode/](TheWitchCode/); all assets/data in [TheWitch/](TheWitch/).

**Entry point:** [MainFile.cs](TheWitchCode/MainFile.cs) is marked `[ModInitializer]`. Its only job is `harmony.PatchAll()` — all integration with the game happens through BaseLib's model registration + Harmony patches, not a main loop.

**Content model pattern.** Each content type has an abstract mod-base class that wires up asset paths, then concrete content subclasses it:
- [Cards/WitchCard.cs](TheWitchCode/Cards/WitchCard.cs) → `CustomCardModel`
- [Powers/WitchPower.cs](TheWitchCode/Powers/WitchPower.cs) → `CustomPowerModel`
- [Relics/WitchRelic.cs](TheWitchCode/Relics/WitchRelic.cs) → `CustomRelicModel`
- [Potions/WitchPotion.cs](TheWitchCode/Potions/WitchPotion.cs) → `CustomPotionModel`

Create new content by subclassing the relevant base in its folder (the BaseLib `ModAnalyzers` and IDE templates assist). Models are looked up at runtime via `ModelDb.Card<T>()`, `ModelDb.Relic<T>()`, etc.

**Pools.** [Character/](TheWitchCode/Character/) defines the character and its `CardPool`/`RelicPool`/`PotionPool`. Content is bound to a pool with the `[Pool(typeof(...))]` attribute on the base class — so individual cards/relics inherit pool membership and don't declare it themselves. [Character/Witch.cs](TheWitchCode/Character/Witch.cs) (`PlaceholderCharacterModel`) defines starting HP/deck/relics and references the pools; it currently uses base-game Ironclad placeholders.

**Asset path convention (important).** Content classes derive their image path from `Id.Entry` — the model id, lowercased with the mod prefix stripped (`RemovePrefix().ToLowerInvariant()`). So a card with id `THEWITCH-Foo` loads `TheWitch/images/card_portraits/foo.png` (and `big/foo.png` for full art). Path helpers live in [Extensions/StringExtensions.cs](TheWitchCode/Extensions/StringExtensions.cs); they fall back to a default placeholder image and log when a file is missing. Add art at the matching path rather than overriding the path methods.

**Localization** is JSON under [TheWitch/localization/eng/](TheWitch/localization/) (`cards.json`, `powers.json`, `relics.json`, `characters.json`, keywords, hover tips). Keys follow `THEWITCH-<ENTRY>.<field>`. These files are fed to the BaseLib analyzer (`AdditionalFiles` in the csproj) so missing/extra localization is caught at build time.

## Content creation

The mod's `Custom{Card,Relic,Potion,Power}Model` bases are thin wrappers over the game's `MegaCrit.Sts2.Core.Models.{CardModel,RelicModel,PotionModel,PowerModel}`. All content shares one root, `AbstractModel`. Two facts drive everything:

- **Registration is reflection-based.** `ModelDb` discovers content via `ReflectionHelper.GetSubtypesInMods<AbstractModel>()` and instantiates with a parameterless ctor. Subclassing a model base + binding it to a pool (`[Pool]`) is the entire "registration" — there is no manual register call.
- **Behavior is hook overrides, not a loop.** `AbstractModel` exposes ~100 async `Task` event hooks (`BeforeCombatStart`, `AfterDamageGiven`, `AfterCardPlayed`, `AfterPlayerTurnStart`, …) plus ~40 synchronous `Modify*` hooks (`ModifyDamageAdditive`, `ModifyEnergyGain`, …). Relics/powers react by overriding the relevant hook. `ShouldReceiveCombatHooks` gates delivery (relics/potions always; cards only while in a combat pile).

**What you override per type:**
- **Card** → ctor `base(energyCost, type, rarity, targetType)`, `CanonicalVars` (the numbers), `OnPlay(ctx, cardPlay)` (the effect), `OnUpgrade()`. Optional: `CanonicalKeywords`, `CanonicalTags`, `MaxUpgradeLevel`, `HasEnergyCostX`, `CanonicalStarCost`.
- **Relic** → abstract `Rarity` + hook overrides. Instant pickup effects use `HasUponPickupEffect` + `AfterObtained()`. Counter relics use `ShowCounter`/`DisplayAmount`.
- **Potion** → abstract `Rarity`/`Usage`/`TargetType` + `OnUse(ctx, target)`. Use `PotionRarity.Token` (or `Event`) for a potion that should only be granted by a card/relic/effect and never appear as a random drop — see the pool-gating gotcha below.

**Stats live in `DynamicVars`.** `CanonicalVars` returns typed vars (`DamageVar`, `BlockVar`, `PowerVar<T>`, `CardsVar`, …) keyed by name; `DynamicVars.Damage` etc. resolve by that key (`PowerVar<VulnerablePower>` → `DynamicVars.Vulnerable`). The same vars are the tokens referenced in the localization JSON, and `UpgradeValueBy` drives green upgrade previews. `ValueProp` (`Move`/`Unpowered`) controls power scaling of the displayed number.

**Effects go through `Cmd` families, never raw state writes:** `DamageCmd.Attack(n).FromCard(this).Targeting(t).Execute(ctx)`, `PowerCmd.Apply<TPower>(...)`, `CreatureCmd.GainBlock(...)`, `CardCmd`/`CardPileCmd`/`CardSelectCmd`. State mutation is guarded by `AssertMutable()` (canonical instances are immutable; real instances are mutable clones).

**Decompiled game source is the reference.** A full decompile lives at `gamedata/` (**gitignored** — local-only, not committed; it's proprietary game code). The ~400 base-game content classes under `gamedata/src/Core/Models/{Cards,Relics,Potions,Powers}/` are the authoritative examples. Workflow: find the closest base-game class, copy its pattern into a `CustomXModel` subclass here, swap the pool + localization keys. Do **not** paste verbatim decompiled code into tracked files.

## Card design docs (keep `cards.json` in sync)

Every card is documented in **`Docs/card-data/cards.json`** (source of truth), rendered by an interactive page `Docs/card-designs.html` with copy-to-console buttons + a `TESTED` flag. The `card-designs` skill drives it.

**Whenever you add, remove, or change a card** (cost/type/rarity/target/`CanonicalVars`/`OnUpgrade`/localization text), regenerate the data so the docs stay current:
```bash
node Docs/card-data/regen.js          # rebuild cards.json from the .cs files + localization
node Docs/card-data/regen.js --check  # no-write drift check (exit 1 if stale)
```
`regen.js` parses the card classes, **preserves each card's `tested` flag, `artFinal` flag, and curated `note`**, and **auto-clears `TESTED` for any card whose mechanics changed** — that is the user's "clear TESTED when the design changes" rule, automated. Treat running `regen.js` as part of finishing any card edit (alongside `dotnet build`) — **especially batches of card changes**: the regenerated data feeds the live, team-facing art tracker (below), so skipping it leaves the published design/status page stale. Do not hand-maintain a parallel markdown list — `cards.json` is the only card doc.

**Art tracker (live, GitHub-Pages-served).** `regen.js` also chains `Docs/art-tracker/regen-art-tracker.js`, which rewrites **`pages/art-tracker.html`** — the static team-facing art-status page, published at `https://joytis.github.io/TheWicken/pages/art-tracker.html` (root `index.html` links to it; Pages serves `main` branch from `/ (root)`). The page is only as current as the last **pushed** regen — after a batch of card edits, regen + commit + push is what actually updates the live view. The `/regen` skill drives this. Details:
- Tabs: Cards (from `cards.json`) + Potions / Relics / Familiar Pets / Character & UI (from hand-maintained **`Docs/art-tracker/assets.json`**).
- Per-card artist + art brief live in **`Docs/art-tracker/card-briefs.json`** — authored via the card-designs page (`Docs/card-designs.cmd`, Witch tab: inline Artist / Art Brief fields, saved through `server.js`'s `/api/brief`; the server also debounce-regens the tracker after any write).
- **Status is derived, never stored**: `artFinal`/`done: true` → Done (green); `artist` set → In Progress (yellow); else Placeholder (grey).
- The generator prints `MISSING IMAGE FILES` (expected PNG absent → "no art" tile); surface notable ones.

**Card categorization + base-class study.** Each card in `cards.json` also carries `mechanics[]` (Brambles/Potions/Familiars) and `role[]` (Generator/Payoff/Enabler/Token/Standalone) tags for reasoning about pool makeup, plus finer-grained `sub[]` (pillar sub-archetype, `Pillar:SubName`) and `threads[]` (cross-class functional threads: AoE/MultiHit/Draw/SelfExhaust/…) — vocabulary and definitions in [Docs/card-data/taxonomy.md](Docs/card-data/taxonomy.md); all four tag arrays are curated and preserved across regens; `card-designs.html` renders a Mechanic × Role matrix. The page additionally has **read-only tabs for the base-game classes** (Silent, Necrobinder, Ironclad) — data in `Docs/card-data/{silent,necrobinder,ironclad}.json`, regenerated by `node Docs/card-data/gen-basegame.js` from `gamedata/` (tags preserved across runs). The design lessons distilled from that study (the "payoff cluster" test for what counts as a pillar, self-acting vs spend-it gen:payoff ratios, neutral-filler share) live in **[Docs/class-pool-analysis.md](Docs/class-pool-analysis.md)** — read it before proposing new Witch mechanics.

## SFX / VFX

Base-game sfx/vfx API + reusable asset catalog: **[Docs/sfx-vfx-catalog.md](Docs/sfx-vfx-catalog.md)** (attack-builder `WithHitFx`/`WithHitVfxNode`, `VfxCmd` paths, `N*Vfx` node factories + which are globally preloaded, FMOD event strings, debug mp3s). Witch house palette + per-card assignments: [Docs/sfx-vfx-proposal.md](Docs/sfx-vfx-proposal.md). Shared mechanic signatures (familiar summon flourish, brew puff, spore puff, hex gaze, bramble slice) live in [Extensions/WitchFx.cs](TheWitchCode/Extensions/WitchFx.cs) — use those helpers, don't hand-roll per card. Gotchas: non-globally-preloaded `N*Vfx` need `ExtraRunAssetPaths` on the card (powers have no such hook — power-spawned vfx must be globally preloaded or registered by the granting card); block/heal/buff/debuff/potion-procure sounds are automatic — never re-add.

## Familiars

The signature mechanic. A **summon Power card** (`WolfFamiliar`, `BearFamiliar`, …, all marked `IFamiliarSummon`) applies one stack of a **`FamiliarPower`** counter via `WitchCard.GainFamiliar<TPower>()`. At each turn start every `FamiliarPower` adds one **token card** to your hand **per stack** (`AfterPlayerTurnStart`). Code: [TheWitchCode/Powers/FamiliarPower.cs](TheWitchCode/Powers/FamiliarPower.cs); summon cards in [TheWitchCode/Cards/](TheWitchCode/Cards/), token cards under `Cards/Familiar/`.

- **Three power bases** (all in `FamiliarPower.cs`): `FamiliarPower<TCard>` (always one token type — Owl→Wisdom, Wolf→Gnash); `LootTableFamiliarPower` (weighted table via `FamiliarLootTable.Add<TCard>(weight)` — Bear, Crow); plain `FamiliarPower` (override `CreateTurnStartCard` for custom rolls — Chimera pulls the whole pool).
- **Token cards** subclass `WitchFamiliarCard` (in the shared `WitchFamiliarCardPool`, `IsShared => true`, `Token` rarity). The base grants **Exhaust** to every token by default (one-shot per-turn payloads — don't clog the deck); a token needing extra keywords must re-include `Exhaust` in its own `CanonicalKeywords`.
- **Generate, don't Add.** Turn-start tokens go in via `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player)` — NOT plain `Add`. Only the generated path fires `AfterCardGeneratedForCombat` + records combat history, which card-creation payoffs (Cloak of Moonlight) listen to. Plain `Add` is for *moving existing* cards (Find Familiar / Pact of Beasts tutor `IFamiliarSummon` cards from your piles).
- **Upgraded summons grant upgraded tokens — PER STACK.** `FamiliarPower.UpgradedStacks` counts how many stacks came from an upgraded summon (`GainFamiliar` increments it when the summon card `IsUpgraded`); upgraded stacks occupy the low indices, and `IsStackUpgraded(i)` feeds the `upgraded` param of every `CreateTurnStartCard` / `CreateAllTurnStartCards` / loot-table call. So 3 normal + 3 upgraded Crows produce 3 normal + 3 upgraded tokens. Clamped to `Amount` in `AfterPowerAmountChanged` so sacrificing stacks spends normal ones first. Summon-card hover tips preview it with `HoverTipFactory.FromCard<X>(IsUpgraded)`. (Caveat: plain power state, not in `DynamicVars` — not serialized for save/MP.)
- **Cosmetic pets**: each `FamiliarPower` declares `protected abstract WitchPet Pet`; the base spawns it on `AfterApplied` and despawns it (`CreatureCmd.Kill`) on `AfterRemoved` (last stack lost). Pets reuse the base-game rocket `creature_visuals` with a swapped sprite — see [Monsters/WitchPetVisualsPatch.cs](TheWitchCode/Monsters/WitchPetVisualsPatch.cs).

## Conventions & gotchas

- `Nullable` is enabled and `<TreatWarningsAsErrors>` is not, but the BaseLib analyzers surface mod-specific mistakes — read analyzer warnings.
- The csproj **excludes** `TheWitch/**`, `materials/`, `shaders/`, `images/` from compilation — that tree is Godot assets, not C#. Don't put `.cs` there.
- `TheWitch.json` is the mod manifest (id, version, `min_game_version`, BaseLib dependency). The build auto-syncs the BaseLib `min_version` in this file to the actually-restored package version (`UpdateDependencyVersions` target) — don't hand-edit that field.
- The mod id `"TheWitch"` (in [MainFile.cs](TheWitchCode/MainFile.cs)) is the resource path root (`res://TheWitch`) and Harmony id; keep it in sync with the manifest and folder name.
- `git` is configured to normalize all text to LF ([.gitattributes](.gitattributes)).
- **A number that scales with combat state must display *live* via `CalculatedDamageVar`, not by mutating `BaseValue`.** Use the base-game Soul Storm shape: `CalculationBaseVar(base)` + `ExtraDamageVar(perUnit)` + `CalculatedDamageVar(prop).WithMultiplier((card, _) => <count>)`; `OnPlay` deals `DynamicVars.CalculatedDamage`, `OnUpgrade` bumps `DynamicVars.ExtraDamage`. Mutating `DynamicVars.Damage.BaseValue` at runtime (the "Maul" buff-every-copy pattern) does **not** reliably re-render and breaks outright for `Exhaust` cards that respawn each turn (e.g. Gnash). Reference: [Cards/Familiar/Gnash.cs](TheWitchCode/Cards/Familiar/Gnash.cs).
- **Localization for a created/added card that can be upgraded** uses the base-game convention `[gold]{IfUpgraded:show:Name+|Name}[/gold]` — the card name gains a `+` and stays `[gold]`; the `+` *is* the upgrade marker (never `[green]`; that's only for flavor/prose). Base-game examples: `REAVE`, `DIRGE`, `HIDDEN_DAGGERS`. Pair it with `HoverTipFactory.FromCard<T>(IsUpgraded)` on the source card so the preview matches.
- **Combat-scoped persistent effects revert in `AfterCombatEnd`.** To grant something for one fight only, apply a tracker power that undoes it at combat end rather than leaving run-level state mutated — e.g. the cut Roomy Satchel card granted potion slots + applied a tracker power whose `AfterCombatEnd` called `LoseMaxPotionCount(Amount)` (pattern lives in git history: `RoomySatchelPower`). The decrement-to-zero auto-remove path fires `AfterRemoved`, the combat-end teardown fires `AfterCombatEnd`.
- **Art is authored "big" and scaled down.** Author `images/.../big/<name>.png`; generate the small variant with `py tools/gen-image-sizes.py big/<name>.png` (single-image mode reuses the existing small's dimensions, else falls back to ¼). No-arg runs the bulk both-sizes pass over all categories. It then calls `tools/gen-outlines.py` for every relic/potion small it wrote (logged as `[outlines] N impacted …`), so silhouettes never go stale — `--no-outlines` opts out.
- **Relics and potions need a `<name>_outline.png`.** Not a stroke: an opaque WHITE silhouette of the icon, dilated a few px. The game draws it behind the *small* icon and tints it flat to render undiscovered relics (relic collection), unbrewed potions (potion lab), and the char-select starting relic; `IconSize.Large` hides it, so big art never needs one. Generate with `py tools/gen-outlines.py [source.png]` (no-arg = all missing/stale; plain dilation + `--blur` softening, enclosed holes deliberately kept — 47/341 base-game relic outlines have them); `--sheet` renders a contact sheet of every source + outline + game-tinted preview to review them in bulk. Paths: `images/relics/<entry>_outline.png` (94x94), `images/potions/<entry>_outline.png` (256x256). A missing potion outline is null-safe (no outline drawn); a missing relic outline falls back to `relic_outline.png`.
- **Pool membership ≠ random availability for potions.** `[Pool]` on the base registers content; for potions, *random* drop/shop availability is gated separately by **rarity**. `PotionFactory.CreateRandomPotion` only ever rolls `Common`/`Uncommon`/`Rare` (combat drops *and* merchant stock both route through it), so a potion with rarity `PotionRarity.Token` or `Event` is never generated randomly while remaining registered — so `PotionCmd.TryToProcure<T>()` from a card/relic still grants it. This is how you make a **card-only payload potion** (base-game example: `PotionShapedRock` = `Token`; `FoulPotion`/`GlowwaterPotion` = `Event`). Don't try to exclude it by removing `[Pool]` — that unregisters it and breaks `TryToProcure`.
- **Potions serialize as id + slot ONLY** (`SerializablePotion` in `gamedata/src/Core/Saves/Runs/`) — no per-potion state survives save/quit/resume or syncs in MP. Any stateful potion (state stored in the mutable instance's `DynamicVars`) silently resets to canonical on reload. Same caveat class as `FamiliarPower.GrantsUpgradedCards`. If persistence matters, use **BaseLib's `ExtendedSaveTypes.RegisterSavedValue<TModel, TVal>`** (namespace `BaseLib.Patches.Saves`; undocumented on the wiki) — it bakes a value into the run save attached to the model's serializable entry AND into its net serialization (so it rides MP full-state sync too). Register in the mod initializer with a getter/setter on the model + `PacketWriter`/`PacketReader` delegates; `TVal` must be JSON-known to `MegaCritSerializerContext` (game types like `SerializableCard` are; custom classes need `ExtendedSaveTypes.RegisterObjectSaveType`). Works for Card/Relic/Potion/Enchantment/Player/Reward/IRunState models. Reference: [Potions/BottledMessage.cs](TheWitchCode/Potions/BottledMessage.cs) (`SaveState`/`RestoreState`) + its registration in [MainFile.cs](TheWitchCode/MainFile.cs). (The older user-dir sidecar-JSON pattern from git history — `CauldronSavePatch.cs` — is superseded: it was a single floating file shared across all saves.)
- **Plain C# fields on a power/card instance are safe — mid-combat state is never restored.** Run saves contain NO combat state at all (`SerializableRoom` = room type + encounter id + rewards; no creatures/powers/piles), so save/quit/resume restarts the encounter fresh. Live MP is deterministic lockstep (`ActionQueueSynchronizer`) — every client runs the same hooks, so instance fields (NeverendingPotionPower's bottled list, `FamiliarPower.GrantsUpgradedCards`) stay consistent across clients. Players never rejoin mid-combat, so there is no restore path to design around: use an ordinary field when a power needs state beyond its stack `Amount`, and don't split a power or reach for `RegisterSavedValue` on serialization grounds alone (that's for RUN-level state that must survive save/quit — see the potion bullet above).
- **Conditional loc text**: the loc engine is SmartFormat with `ConditionalFormatter` registered — `{Var:cond:>0?shown when positive|}` (also `==1?`, `<0?`, chained options). Base-game examples: `SOVEREIGN_BLADE`, `SHRINK_POWER`, `DEXTERITY_POWER`. Lets a tooltip hide zero-valued effect lines entirely (The Cauldron).
- **Live *hit-count* display = the base-game Barrage pattern** (complement to the Soul Storm damage shape): `CalculationBaseVar(0m)` + `CalculationExtraVar(1m)` + `new CalculatedVar("CalculatedHits").WithMultiplier((card, _) => <count>)`; `OnPlay` reads `(int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(target)` for `WithHitCount`. Loc shows it in-combat only: `{InCombat:\n(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}`. Reference: `gamedata` `Barrage.cs`; mod example [Cards/BottleBarrage.cs](TheWitchCode/Cards/BottleBarrage.cs).
- **Front-of-hand insertion is a built-in param**: `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top)` (enum: `Bottom` default / `Top` / `Random`). No patch needed.
- **Belt-shrink trap:** `Player.SetMaxPotionCountInternal` migrates potions from doomed slots into earlier empty slots **silently — no event fires**. Any UI code that trims potion holders must re-seat stranded potion nodes against `player.PotionSlots` or the migrated potion's node is freed with its holder (invisible + unusable until reload). (Reference fix in git history: `PotionBeltShrinkPatch.cs`, cut with Roomy Satchel — nothing shrinks the belt anymore.)
- **Localization keys share ONE namespace across `cards.json`/`potions.json`/`powers.json`** — keys are id-derived (`THEWITCH-<ENTRY>.*`), so a card and a potion cannot both be named e.g. `WickedBrew` (both would claim `THEWITCH-WICKED_BREW.title`). Rename one class before reusing a display name (this is why the Wicked Brew *potion* became `NoxiousBrew`).
- **Encounter tier** = `Owner.RunState.CurrentMapPoint?.PointType` (`MapPointType.Monster`/`Elite`/`Boss`/…). Nullable — guard with `?.` and default the `switch` (Extract Essence pattern: Boss→Rare, Elite→Uncommon, else Common).
- **No native cost-substitution hook exists** ("pay X resource instead of Energy"). The working shape (Bonfire): `TryModifyEnergyCostInCombat` zeroes the cost while affordable and remembers the card in a transient `HashSet<CardModel>`; `BeforeCardPlayed` consumes the set entry and spends the resource. Guard `originalCost > 0` so natively-free cards don't pay, and never let re-applying the power stack its price. See [Powers/BonfirePower.cs](TheWitchCode/Powers/BonfirePower.cs).
- **Mutable reference-type fields on a model must be re-created in `DeepCloneFields()`.** Model instances come from `MemberwiseClone()` (shallow) — a `List`/`HashSet`/`Dictionary` field initialized at declaration is SHARED between the canonical model and every mutable clone, so state written in one combat leaks into every future instance for the rest of the process (the Crystal Bottle "bottled potions replay in every later combat" bug). Fix: make the field non-`readonly` and override `DeepCloneFields()` (call `base`, assign a fresh collection — never `Clear()`, that wipes the shared canonical list). Base-game examples: `PowerModel`, `CardModel`, `ActModel`. Mod examples: [Powers/NeverendingPotionPower.cs](TheWitchCode/Powers/NeverendingPotionPower.cs), [Powers/BonfirePower.cs](TheWitchCode/Powers/BonfirePower.cs).
- **The turn-start window shares ONE `HookPlayerChoiceContext` per player, good for exactly ONE player choice.** All turn-start + AutoPrePlay hooks receive the same context (created in `CombatManager.SetupPlayerTurn`), and `HookPlayerChoiceContext` backs a single game action — a second `SignalPlayerChoiceBegun` after the first action finished hits the "Tried to interrupt action" error path and the selection screen opens **unsynced → breaks multiplayer**. A power that may open a selection (or replay something that does, e.g. a bottled draft potion) at turn start must give each such effect its own context: `new HookPlayerChoiceContext(this, LocalContext.NetId.Value, combatState, GameActionType.CombatPlayPhaseOnly)` + `AssignTaskAndWaitForPauseOrCompletion(task)` (the base-game `Hook.AfterDeath` pattern; on pause the effect resolves in its own queued action at Play-phase start, like Mayhem). See [Powers/NeverendingPotionPower.cs](TheWitchCode/Powers/NeverendingPotionPower.cs).
- Handy: `list.UnstableShuffle(rng)` (in `MegaCrit.Sts2.Core.Extensions`) is the game's own in-place shuffle — use it instead of hand-rolling Fisher–Yates. (`BramblesPower` now lives in `TheWitch.TheWitchCode.Powers` like every other mod power — it was migrated out of the game's `MegaCrit.Sts2.Core.Models.Powers` namespace, and its loc keys are `THEWITCH-BRAMBLES_POWER.*`. The model-id prefix is derived from the namespace root, so never declare mod types in `MegaCrit.*` namespaces.)
- **A custom card pool must reach `ModelDb.AllCardPools`, or its cards crash on render with `InvalidOperationException: You monster!`.** `AllCardPools = AllCharacterCardPools.Concat(AllSharedCardPools)`. A pool only enters `AllCharacterCardPools` if some `CharacterModel.CardPool` returns it (one pool per character), and only enters `AllSharedCardPools` if it overrides `IsShared => true` (which calls `ModelDbSharedCardPoolsPatch.Register` from the `CustomCardPoolModel` ctor). A pool that is *neither* referenced by a character *nor* shared is invisible. `CardModel.Pool` then finds no pool, falls through to the `MockCardPool` branch, and `MockCardPool.GenerateAllCards()` calls `NeverEverCallThisOutsideOfTests_ClearOwner()` → **"You monster!"** — and it fires on *render/hover*, not on play (e.g. a `HoverTipFactory.FromCard<T>` preview). **Secondary/utility pools that aren't a character's main `CardPool` (familiar, spawned, token-payload cards) must be `IsShared => true`** — mirrors base-game shared pools `ColorlessCardPool`/`StatusCardPool`/`TokenCardPool`. Token rarity still keeps such cards out of random rewards. (Fixed: [Character/WitchFamiliarCardPool.cs](TheWitchCode/Character/WitchFamiliarCardPool.cs) for `Wisdom`, a `Token` familiar previewed by `OwlFamiliar`.)

- **Editor junctions into `gamedata/`.** Repo-root `scenes/`, `materials/`, `shaders/`, `images/`, `src/`, `themes/`, `fonts/` are NTFS junctions → `gamedata/<same>` so the Godot editor resolves base-game `res://` paths referenced by mod scenes (they resolve from the game's pck at runtime either way). Gitignored — recreate on fresh clone with `New-Item -ItemType Junction <name> -Target gamedata\<name>`. They are excluded from export in `export_presets.cfg` (`exclude_filter`) — never remove those excludes; verified (2026-08-08 pck audit): the excludes DO apply to auto-included dependencies, so junctioned game assets never land in the mod `.pck`. The `src/` junction exists so mod scenes can **instance base-game scenes directly** (e.g. `common_banner.tscn`, `selection_reticle.tscn`): the editor must resolve their `res://src/...cs` scripts or export strips them (symptom: instanced node arrives as a bare `TextureRect`, `InvalidCastException` at runtime). The decompiled `.cs.uid` sidecars carry the game's ORIGINAL script uids, so references bind correctly. `src/**` is kept out of compilation via `Compile Remove` in the csproj (it would otherwise conflict with `sts2.dll` types).

## Task workflow (TODO loop)

Backlog work for this mod runs through a saved loop — the **`todo-loop`** skill ([.claude/skills/todo-loop/SKILL.md](.claude/skills/todo-loop/SKILL.md)) plus three docs under [Docs/](Docs/):
- **`TODO_STAGING.md`** — raw-notes inbox; the user drops half-formed ideas here.
- **`TODO.md`** — formatted, prioritized queue; its header holds the loop protocol.
- **`DONE.md`** — completed items (what changed, the design calls made, verification).

Procedure: ingest each staging note into a self-contained `TODO.md` item (then delete that line from staging); claim the top item (`IN PROGRESS`), implement, `dotnet build` green is the gate, move it to `DONE.md`. Flag truncated/ambiguous notes as `BLOCKED` and ask rather than guess; serialize shared-JSON edits but parallelize file-isolated work. Run `/todo-loop` to drive it.
