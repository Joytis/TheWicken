# Potion Trait & Brewing System

> Design record for TheWitch's potion classification + brewing feature.
> Code lives in [`TheWitchCode/Potions/Brewing/`](../TheWitchCode/Potions/Brewing/).

> **⚠️ SUPERSEDED — orientation only.** The fine-grained `PotionTrait` `[Flags]` taxonomy described below was
> **removed**. The system now classifies every potion into a single `PotionOrientation` (Offensive / Defensive /
> Utility / Neutral) and brews/upgrades by orientation alone — matching on sub-traits made the candidate pools too
> narrow (a Heal potion only ever upgraded into the one Uncommon healer). `PotionTraits.OrientationOf(potion)`
> replaces `PotionTraits.Of`; `PotionCatalog.Query(orientation:, …)` replaces the trait filters; `BrewBook` matches
> on shared orientation → rarity fallback; `PotionUpgrade.UpgradeRandomPotions` upgrades by brewing a potion with
> itself. The authoritative `PotionTraits.Manual` table now maps `Type → PotionOrientation` (with a `// description`
> per row). The *motivation* and *brewing rules* below still apply; mentally substitute "orientation" for "trait".

## Why this exists

Potions are a **core identity** of TheWitch. The character needs to:

1. **Query** the set of all potions by *what they do* — "give me a defensive Common", "any potion that
   generates block" — rather than by hard-coded id lists.
2. **Brew**: combine two potions in the player's inventory into a new, higher-rarity potion, with recipes based on
   *properties* (offensive/defensive, block, etc.) instead of exact potion identities. Property-based recipes keep
   the design space **additive** rather than multiplicative — emergent combinations stay predictable.

The base game gives us no such classification. `PotionModel` only exposes `Rarity`, `Usage`, `TargetType`,
`CanBeGeneratedInCombat`, and a typed `DynamicVarSet`. Base-game potions are **game-assembly types we cannot edit**
(the decompile under `gamedata/` is gitignored, local-only reference). So classification must live externally, in
mod code, layered over `ModelDb.AllPotions`.

## Design decisions (locked)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Trait model | `[Flags] PotionTrait` (multi-tag) + derived `PotionOrientation` | A potion can be several things at once (damage + debuff). |
| Classification source | **Manual table primary** (`PotionTraits.Manual`), auto-derive from `DynamicVars`/`TargetType` as **fallback** | A hand-curated table is the source of truth so the loot table can be tuned directly; inference keeps un-listed / future-DLC potions classified automatically. |
| Table key | concrete model **`Type`** | Compile-checked against the game assembly — a renamed class breaks the build instead of silently mis-tagging. |
| Table semantics | a `Manual` entry fully **replaces** the inferred result; **every shipped potion must have an entry** | Loot-table behavior is then explicit and editable, not emergent. |
| Brew inputs | exactly **2** | Simplest, cleanest UI. Expand later if needed. |
| Brew output rarity | **highest input rarity + 1**, capped at Rare (always ≥ Uncommon) | "Brewing improves" feel; predictable. |
| Brew trait combine | **union** of inputs' traits (primary), **shared orientation** fallback, **rarity-only** last resort | Union is thematic but may not match a real potion; orientation fallback almost always resolves. |
| Brew output | a **real existing potion** drawn from the catalog | No bespoke fused-effect potions yet — reuses all content, no per-recipe authoring. |

## Trait taxonomy

`PotionTrait` ([PotionTrait.cs](../TheWitchCode/Potions/Brewing/PotionTrait.cs)):

- **Offensive**: `Damage`, `Debuff`, `Poison`
- **Defensive**: `Block`, `Heal`, `MaxHp`
- **Utility**: `Buff`, `Energy`, `Draw`, `CardGen`, `CardManip`, `PotionGen`, `Upgrade`
- **Modifier**: `Aoe` (derived from `TargetType.AllEnemies`)
- **Masks**: `Offensive`, `Defensive`, `Utility` (OR of the above groups)

`PotionOrientation` = `Neutral | Offensive | Defensive | Utility`, derived with priority
Offensive > Defensive > Utility > Neutral.

## Auto-derivation rules

In [`PotionTraits.Derive`](../TheWitchCode/Potions/Brewing/PotionTraits.cs), per var in `potion.DynamicVars.Values`:

| Var type | Trait |
|----------|-------|
| `DamageVar` / `CalculatedDamageVar` / `ExtraDamageVar` | `Damage` |
| `BlockVar` / `CalculatedBlockVar` | `Block` |
| `EnergyVar` | `Energy` |
| `MaxHpVar` | `MaxHp` |
| `HealVar` | `Heal` |
| `CardsVar` | `Draw` |
| `PowerVar<PoisonPower>` | `Poison` |
| `PowerVar<T>` (any other) | `Debuff` if `TargetType` targets an enemy, else `Buff` |

Plus `Aoe` when `TargetType == AllEnemies`.

`PowerVar<T>` is detected by walking the var's type hierarchy for the open generic `PowerVar<>`, then reading the
power type argument — so the disambiguation is robust to new powers.

## Manual classification table (`PotionTraits.Manual`)

The authoritative, hand-curated table (best-guess values — tune freely). Anything **not** listed falls back to
auto-derivation. Grouped by primary orientation; fine-grained flags drive both the orientation queries
(Something Wicked = `Offensive`, Toil and Trouble = `Utility`, Stone Skin = `Defensive`) and brew trait-union.

| Group | Potions → trait |
|-------|-----------------|
| **Offensive** | FirePotion, PotionShapedRock → `Damage`; ExplosiveAmpoule, FoulPotion → `Damage\|Aoe`; WeakPotion, VulnerablePotion, BeetleJuice, PowderedDemise, PotionOfDoom → `Debuff`; ShacklingPotion, PotionOfBinding → `Debuff\|Aoe`; PoisonPotion → `Poison` |
| **Defensive** | BlockPotion, ShipInABottle, Fortifier → `Block`; HeartOfIron → `Block\|Buff`; StrengthPotion, DexterityPotion, FocusPotion, FlexPotion, SpeedPotion, FyshOil, GigantificationPotion, LiquidBronze, LuckyTonic, MazalethsGift, GhostInAJar, Duplicator, EssenceOfDarkness, SoldiersStew, StableSerum, BoneBrew, PotionOfCapacity → `Buff`; RegenPotion, BloodPotion, FairyInABottle → `Heal`; FruitJuice → `MaxHp` |
| **Utility** | EnergyPotion, RadiantTincture, StarPotion → `Energy`; SwiftPotion, BottledPotential, Clarity, DropletOfPrecognition, GamblersBrew, LiquidMemories → `Draw`; CureAll → `Energy\|Draw`; AttackPotion, SkillPotion, PowerPotion, ColorlessPotion, CosmicConcoction, CunningPotion, OrobicAcid, PotOfGhouls → `CardGen`; Ashwater, TouchOfInsanity, DistilledChaos → `CardManip`; SneckoOil, GlowwaterPotion → `Draw\|CardManip`; EntropicBrew → `PotionGen`; BlessingOfTheForge, KingsCourage → `Upgrade` |
| **Modded** | WickedBrew, **SlicingBrew** (card-only multi-hit payload from Prices Paid), VillainousBrew, **Fertilizer** (gains Brambles, tagged offensive by design) → `Damage`; **BottledRot** → `Poison\|Aoe`; **BuddyInABottle** (adds a random Familiar summon card) → `CardGen`; **VialOfSmoke** (card-only Block potion from Light the Candle) → `Block`; TheCauldron → `Damage\|Block\|Buff\|Debuff\|Aoe` |

**To extend**: add a `[typeof(SomePotion)] = PotionTrait.X` entry (combine flags with `\|` for cross-orientation
potions) and update this table. Every potion the mod ships **must** be listed here. Best-guess values are
expected to get a manual balance pass.

## API

[`PotionCatalog`](../TheWitchCode/Potions/Brewing/PotionCatalog.cs) — query over `ModelDb.AllPotions` (canonical models):

```csharp
PotionCatalog.All                 // every registered potion
PotionCatalog.Randomizable        // Common/Uncommon/Rare only (excludes Token/Event payloads)
PotionCatalog.WithAll(traits)     // has ALL of these trait bits
PotionCatalog.WithAny(traits)     // has ANY of these trait bits
PotionCatalog.OfRarity(r)
PotionCatalog.OfOrientation(o)
PotionCatalog.Query(require, matchAll, rarity?, usage?, randomizableOnly)   // master filter, AND-ed
PotionCatalog.Random(pool, rng)   // pick one, or null
```

[`PotionTraits`](../TheWitchCode/Potions/Brewing/PotionTraits.cs):

```csharp
PotionTraits.Of(potion)            // -> PotionTrait  (cached per Type)
PotionTraits.OrientationOf(potion) // -> PotionOrientation
```

[`BrewBook`](../TheWitchCode/Potions/Brewing/BrewBook.cs):

```csharp
BrewResult r = BrewBook.Brew(potA, potB, rng);
// r.Success, r.Potion (canonical), r.Rarity, r.CombinedTraits, r.MatchKind
BrewBook.NextRarity(a, b);         // output rarity rule
```

`BrewResult.MatchKind` (`TraitUnion` / `SharedOrientation` / `RarityOnly` / `None`) records which fallback tier
resolved the brew — useful for tuning and dev-console output.

### Granting a result

Catalog/brew returns **canonical** models. To actually give one to a player:

```csharp
if (r.Success)
    await PotionCmd.TryToProcure(r.Potion!.ToMutable(), player);
```

## Dev console commands

Debug-only console commands (auto-registered in the mod via `AbstractConsoleCmd` subclasses in
[`TheWitchCode/DevConsole/`](../TheWitchCode/DevConsole/); require the game's debug console enabled):

- **`potiontraits`** — dumps every registered potion with its classified traits + orientation, grouped by rarity,
  to the console and logs. Use this to eyeball the classification (auto-derive + overrides).
- **`mergepotions`** — brews the first two potions in the issuing player's belt into a higher-rarity result via
  `BrewBook.Brew`, consuming both. If the player holds exactly one potion, it's replaced with a `WickedBrew`
  (Token-rarity card-only potion). Errors on an empty belt. Networked (multiplayer-safe).

## Enemy essence (Harvest card)

A second consumer of the classification layer: [`EnemyEssence`](../TheWitchCode/Potions/Brewing/EnemyEssence.cs)
turns a *defeated-ish enemy's behavior* into a thematic potion, used by the
[Harvest card](../TheWitchCode/Cards/Harvest.cs) ("deal damage; on unblocked damage, create a potion
themed after the enemy").

- **`EnemyEssence.Classify(Creature)`** → `PotionOrientation`. It tallies the enemy's intent types across its
  **whole moveset** (`Monster.MoveStateMachine.States` → each `MoveState.Intents` → `IntentType`; falls back to the
  telegraphed `NextMove` if there's no state machine), so the result reflects the enemy's overall character, not just
  this turn's intent. Votes map:
  - `Attack` / `DeathBlow` / `Buff` → **Offensive**
  - `Defend` / `Heal` → **Defensive**
  - `Debuff` / `DebuffStrong` / `CardDebuff` / `StatusCard` / `Summon` → **Utility**
  - Highest vote wins; ties break Offensive > Defensive > Utility. No classifiable intents (or a non-monster) →
    defaults to Offensive.
- **`EnemyEssence.RollThematicPotion(Creature, rng)`** → a random **Common** potion (canonical) whose
  `PotionTraits.OrientationOf` matches the classification, via `PotionCatalog.Query`; falls back to any Common if that
  orientation bucket is empty. The card mutates + `TryToProcure`s it.

The card reads `AttackCommand.Results` → `DamageResult.UnblockedDamage` on the target to gate the potion on actually
landing unblocked damage.

## Known limitations / open knobs

- **No bespoke fused potions.** Brews resolve to an existing potion. If TheWitch's identity wants potions that
  literally combine two effects, that needs a flexible payload `PotionModel` — bigger build, deferred.
- `Brew` excludes the two input Types and outputs only `Randomizable` potions (no Token/Event payloads). Loosen if
  brews should be able to produce card-only payload potions.
- Trait granularity (`Aoe` / `CardManip` / `Draw` vs `CardGen`) is a first read — adjust the enum as the character's
  mechanics firm up.
- **Not yet verified in-game** (project has no test suite; validation is manual). Use the `potiontraits` console
  command to eyeball the full classification, and `mergepotions` to exercise brewing live.
- `mergepotions` merges the *first two* belt potions deterministically — no slot-selection args yet.
- `EnemyEssence` classifies by the enemy's **whole moveset** (not current intent) and maps `Buff` → Offensive — both
  are judgment calls; switch to current-intent-only or remap `Buff` if the feel is off.
