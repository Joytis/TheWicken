# Card Implementation Plan — `import/STS2 TheWicken - Cards.csv`

Durable TODO + design record for implementing the 56 designed cards. One row in the
CSV (Bear Familiar's upgrade) is a line-continuation, so the count is **56 cards**,
not 57.

## Progress log

- **2026-06-26 (feedback pass, compiles clean):**
  - **Embrace the Wilds** now transforms hand cards into random familiar **summon Power cards** (the
    `IFamiliarSummon` cards: Owl/Cat/Rat/…/Chimera), not token cards. New registry method
    `FamiliarCardRegistry.AllSummonCanonical` (`ModelDb.AllCards.OfType<IFamiliarSummon>().Cast<CardModel>()`).
  - **Sloth Familiar** → Uncommon (was Common).
  - **Bottomless Cauldron** now skips when the used potion **is** a `WickedBrew` (`potion is not WickedBrew`),
    closing an infinite-potion loop (using a created Wicked Brew would have created another).
  - **Power tooltips:** filled `description` + `smartDescription` (with `{Amount}`/`[blue]`/`[gold]` tokens,
    `{Amount:plural:…}`) for every power in `powers.json` so all powers show real tooltips. Counter powers use
    `{Amount}`; Single toggles (Hedge Prison, Bottomless, Cloak, Dance, Witch's Curse) omit it.
  - **Note:** deploy-copy to the game's mods/ folder is blocked while Slay the Spire 2 is running (DLL locked);
    the compile + BaseLib analyzers pass. Close the game to redeploy.
- **2026-06-26 (Rancid Smoke — 🎉 ALL 56 CARDS DONE, 0/0):** Q4 resolved (source = your own debuffs).
  `Cards/RancidSmoke.cs` mirrors base-game `Misery`: clone each `PowerType.Debuff` power on the Wicken
  (`ClonePreservingMutability`) and apply to all enemies — stacking onto enemies that already have it
  (`PowerCmd.FindExistingInstanceForStacking` → `ModifyAmount`), `ITemporaryPower.IgnoreNextInstance` on
  fresh applies so temp-powers don't double-tick. Exhaust; upg removes Exhaust. **The plan is complete.**
- **2026-06-26 (Gather Herbs — bucket C COMPLETE, 0/0):** Q resolved as **"+1 rarity, own cards only."**
  `Powers/NextPotionUpgradedPower.cs` (counter buff) + static `UpgradeRarity(player, rarity)` that consumes a
  stack and bumps the tier (Common→Uncommon→Rare, capped). Wired into `SomethingWicked` + `ToilAndTrouble`
  (`var rarity = await NextPotionUpgradedPower.UpgradeRarity(Owner, …)` replacing their inline rarity). Gather
  Herbs applies one stack (Exhaust; upg removes Exhaust). Bucket C done → only **Rancid Smoke (Q4)** remains.
- **2026-06-26 (potion infra — bucket C, 2 of 3, 0/0):**
  - **Bottle Barrage** — needed a "potions created this combat" count, but the game logs **no potion-procured
    history entry**. Built `Patches/PotionsCreatedTracker.cs`: Harmony-prefixes the procurement chokepoint
    `PotionCmd.TryToProcure(PotionModel, Player, int)` (the generic overload routes through it), counting into a
    `ConditionalWeakTable<ICombatState, StrongBox<int>>` so it's per-combat and self-cleaning. Card =
    `WithHitCount(CountFor(CombatState))`, 10 dmg/hit (+3 upg). Counts procure *calls* (belt-full still counts);
    combat-keyed → team total in MP. (Project already Harmony-patches — see `Monsters/WickenPetVisualsPatch.cs`.)
  - **Witch's Curse** — potion damage carries **no card/potion source** (`CreatureCmd.Damage(.., player, null)`),
    so it can't be spotted in the damage pipeline. `WitchsCursePower` (on the enemy) instead flags the window
    between `BeforePotionUsed`/`AfterPotionUsed` — which **bracket the potion's `OnUse` body** (PotionModel
    `OnUseWrapper`: BeforePotionUsed → OnUse[damage] → AfterPotionUsed) — and returns `2m` from
    `ModifyDamageMultiplicative` while that flag is set and `target == Owner`. **Does NOT gate on
    `IsPoweredAttack`** (potion damage is `Unpowered`, unlike Vulnerable). Turn-scoped: self-removes on
    `AfterSideTurnEnd(side == Player)`. Reusable: any "react to a potion's effect mid-use" power = the
    Before/After-PotionUsed bracket + a flag.
  - ⬜ **Gather Herbs** still open — "next potion is higher quality" is undefined (potions have only `Rarity`,
    no upgrade flag) and the consume/scope mechanics differ by approach. Pending a design decision.
- **2026-06-26 (MP co-op — bucket B DONE, 0/0):** 4 `MultiplayerOnly` cards (`CardMultiplayerConstraint.MultiplayerOnly`,
  ref `Flanking`; auto-excluded from single-player pools). Ally infra: `CombatState.GetTeammatesOf(Owner.Creature)`
  returns the **whole side incl. self** → filter `c.IsAlive && c.IsPlayer`; single-ally pick = `TargetType.AnyAlly`
  + `cardPlay.Target.Player`; per-ally energy = `PlayerCmd.GainEnergy/LoseEnergy(n, player)`; per-ally potion =
  `PotionFactory.CreateRandomPotionInCombat(player, Rng.CombatPotionGeneration).ToMutable()` + `PotionCmd.TryToProcure`.
  - **Circle of Rot** (AllAllies) — each teammate gains Block + 2 Weak; all enemies gain 2 Weak. **Interpretation:**
    the "gain 2 weak" is self/ally weak (Wicken debuff-synergy theme), "apply 2 weak" hits enemies; one `WeakVar` drives both, +1 on upg.
  - **Creeping Vines** (X-cost, `HasEnergyCostX`, `ResolveEnergyXValue()`) — X times, 5 Brambles to a random ally
    rolled fresh each hit (`Rng.CombatTargets.NextItem`). Random pool = all teammates incl. self.
  - **Tiny Bottle** (AnyAlly) — drains up to 2 energy from the targeted ally (capped at their current energy); **you**
    gain 1 random potion per energy drained (energy is converted to brew, not gained). Upg = +1 energy drained.
  - **Share the Brew** (AllAllies) — you + every ally each procure a random potion (mirrors HuddleUp/EnergySurge incl. self).
  - These interpretations resolved the under-specified CSV text; tweak numbers/targets freely (one-liners).
  Reusable: block-to-any-creature = `CreatureCmd.GainBlock(creature, amount, ValueProp.Move, cardPlay)`;
  `PowerCmd.Apply` accepts a creature **or** `CombatState.HittableEnemies` collection.
  **Gotcha:** `BramblesPower` is namespaced `MegaCrit.Sts2.Core.Models.Powers` (shares base loc), not `TheWicken...Powers`.
- **2026-06-26 (familiar utility — bucket A DONE, 0/0):** 5 cards + `Cards/Familiar/FamiliarCardRegistry.cs`
  (the "random familiar card" registry = `ModelDb.AllCards.OfType<WickenFamiliarCard>()`, valid because
  `WickenFamiliarCardPool.IsShared`; `CreateRandom` rolls `Rng.NextItem` + `ICombatState.CreateCard(canonical, owner)`):
  - **Woe and Whimsy** — 1 random familiar → hand (upgraded when card is upgraded).
  - **Chimera Familiar** — Power; `ChimeraFamiliarPower` + `GainFamiliar` + `IFamiliarSummon`; shuffles 3
    (+1 upg) random familiars into Draw, matching the other familiar Power cards.
  - **Find Familiar** — tutor: `CardSelectCmd.FromCombatPile(Draw, filter: c is WickenFamiliarCard)` →
    `CardPileCmd.Add(.., Hand)` (ref `SecretWeapon`).
  - **Pact of Beasts** — gather all `WickenFamiliarCard` from Draw+Discard `.Cards` → `CardPileCmd.Add(.., Hand)`.
  - **Embrace the Wilds** — for each transformable hand card: create random familiar, `EnergyCost.SetThisCombat(0)`,
    `CardCmd.Transform(original, familiar)` (refs `Begone`/`AdaptiveStrike`).
  - Reusable: pile reads = `PileType.X.GetPile(Owner).Cards`; random card gen rng = `Owner.RunState.Rng.CombatCardGeneration`;
    make-free = `card.EnergyCost.SetThisCombat(0)`. **`CardModel` lives in `MegaCrit.Sts2.Core.Models`** — add that using.
- **2026-06-26 (cont.)** — Wolf Familiar + Gnash (Q2 resolved → escalate +5/Gnash via
  `CardsPlayedThisCombat`) and Cackle + `TheCauldron` potion (Q5 resolved) shipped in commit
  `a2fc709`; plan rows now reconciled to ✅. **Weathered Witch Hat** built (10 block + 1× existing
  `NextSkillDiscountPower`; +3 block upg) — the last fully-unblocked quick win. 0/0.
- **Remaining unbuilt: NONE — all 56 cards built & compiling 0/0.** (Art is still placeholder for the
  new cards; loc is final text.)
- **2026-06-25** — Plan ingested (56 cards). Decisions Q1/Q3 made (below). **Phase 0 — Familiar power
  system built & compiling:**
  - `Powers/FamiliarPower.cs` — abstract counter base; one stack = one familiar of that type.
  - `Powers/OwlFamiliarPower.cs`, `Powers/CatFamiliarPower.cs` — concrete powers for existing familiars.
  - `Powers/Familiars.cs` — `Count` / `On` / `Any` / `RemoveRandom(rng)` helpers (familiar count = sum
    of all `FamiliarPower` stacks; sacrifice = decrement a random one, auto-removed at 0).
  - `Cards/WickenCard.cs` — `GainFamiliar<TPower>()` helper; wired into `OwlFamiliar` + `CatFamiliar`.
  - Loc added for both powers in `powers.json`. **Each new familiar card must add its own
    `XFamiliarPower` + loc + a `GainFamiliar<>` call.**
- **2026-06-25 (cont.) — more Phase 0 infra built & compiling:**
  - `Extensions/CombatHistoryQueries.cs` — history-backed counters (no tracker powers needed):
    `PotionsUsedThisTurn`, `BramblesCreatedThisTurn`. Pattern reusable for any "X this turn/combat"
    via `CombatManager.Instance.History` + `entry.Actor` + `HappenedThisTurn(state)`.
  - `Powers/ViciousBarbsPower.cs` + `Powers/HedgePrisonPower.cs` + edited `Powers/BramblesPower.cs`
    so bramble retaliation reads them (Vicious Barbs = +Amount damage per trigger; Hedge Prison =
    skip decrement / permanent). Loc added. **Cards Vicious Barbs / Hedge Prison now reduce to a
    one-line `PowerCmd.Apply<>` Power card.**
  - `Cards/IFamiliarSummon.cs` — marker on familiar-summon cards (Owl, Cat implement it). Enables
    Broom Strike / Pact of Beasts to detect familiar powers generically.
- **2026-06-25 (Phase 1) — 16 quick-win cards built, compiling 0/0, placeholder big art + loc done:**
  Serrated Bones, Lavender and Sage, Nettles, Brambleburst, Wild Growth, Needle Whip, Forbidden Magic,
  Stuck in the Bush, Moondrop Tea, Hexburst, Something Wicked, Toil and Trouble, Blood Boiling,
  Rattling Bottles, Bag of Teeth, Hidden in Smoke. All `WickenCard` → auto-pooled via inherited
  `[Pool(WickenCardPool)]`; placeholder `big/<snake>.png` copies (no `.import` — Godot regenerates on
  publish); loc in `cards.json`.
  - Patterns established for reuse: AoE attack `DamageCmd.Attack(..).TargetingAllOpponents(CombatState!)`;
    multi-hit `.WithHitCount(DynamicVars["Repeat"].IntValue)` + `{Repeat:diff()} {Repeat:plural:time|times}`;
    bramble-scaling via `Owner.Creature.GetPowerAmount<BramblesPower>()`; self life-loss `HpLossVar` +
    `CreatureCmd.Damage(.. Unblockable|Unpowered ..)`; offensive/utility potion create via
    `PotionCatalog.Query(PotionTrait.X, matchAll:false, rarity)` + `PotionCatalog.Random`.
  - **Enemy "lose Strength this turn"**: `Powers/NeedleWhipStrengthPower.cs` = `TemporaryStrengthPower`
    (IsPositive=false) **+ `ICustomModel`** (BaseLib prefix marker — required for mod powers that can't
    extend `CustomPowerModel`; silences STS003; uses shared temp-strength loc, no powers.json entry).
    Reuse this pattern for any future temp-stat card.
- **2026-06-26 (Phase 2) — 3 brambles cards built on Phase 0 infra, compiling 0/0, art + loc done:**
  - `Cards/BrambleShield.cs` — 7 block + (PerBramble × `CombatHistoryQueries.BramblesCreatedThisTurn`).
    Bonus block computed in OnPlay via `GainBlock(creature, decimal, ValueProp.Move, cardPlay)`; the card
    preview shows only the base 7 (no live scaling) — fine for placeholder, could move to a `CalculatedVar`
    later if live preview is wanted.
  - `Cards/ViciousBarbs.cs` — applies `ViciousBarbsPower` (3, +2 upg).
  - `Cards/HedgePrison.cs` — applies `HedgePrisonPower` (toggle; -1 energy upg).
  - Both Power cards are one-liners over the Phase 0 powers, exactly as predicted. Power tooltips
    (powers.json desc for VICIOUS_BARBS_POWER/HEDGE_PRISON_POWER) left empty — card text carries the
    explanation; fill power descriptions later for polish.
- **2026-06-26 (Phase 3) — 6 trigger cards + powers built, compiling 0/0, art + loc done.** Each card
  applies a persistent `WickenPower` that overrides one global combat hook:
  - `RottingRootsPower` — `AfterPowerAmountChanged`, filter `applier==Owner && power.Owner!=Owner &&
    Type==Debuff && amount>0` → gain Brambles (excludes self-debuffs; brambles is Buff so no loop).
  - `CursedBloodlinePower` / `BindInBloodPower` — `AfterDamageReceived`, `target==Owner &&
    result.UnblockedDamage>0`. Cursed = flat per event; Bind = `Amount × UnblockedDamage`, self-removes
    via `AfterSideTurnEnd` (turn-scoped; **not** `ITemporaryPower` — that contract is for stat-wrapping
    powers).
  - `BottomlessCauldronPower` / `BitterRootPower` — `AfterPotionUsed`, `potion.Owner==Owner.Player`.
    These hooks have **no PlayerChoiceContext** → use `new ThrowingPlayerChoiceContext()` (Bitter Root);
    `TryToProcure<WickedBrew>(Player)` needs none (Bottomless).
  - `CloakOfMoonlightPower` — both `AfterCardGeneratedForCombat` (creator==player) and
    `AfterPotionProcured` (potion.Owner==player) → gain Block.
  - Reusable note: global hooks (`AfterPowerAmountChanged`, `AfterDamageReceived`, `AfterPotionUsed`,
    `AfterPotionProcured`, `AfterCardGeneratedForCombat`) are delivered to every combat-hook model — a
    listener power just filters by `Owner`. Hooks without a context use `ThrowingPlayerChoiceContext`.
- **2026-06-26 (Phase 4) — 5 of 8 potion cards built, 0/0:** Bottle Wall (`PotionsUsedThisTurn`),
  Herbal Remedy + Unstable Reaction (`PotionCmd.Discard` all belt potions, then energy / AoE per),
  Dance Around the Cauldron (`DanceAroundTheCauldronPower`, end-of-turn `AfterSideTurnEnd` reads
  `PlayerCombatState.Energy`, procures Wicked Brews, self-removes), Roomy Satchel
  (`PlayerCmd.GainMaxPotionCount`). **Deferred (need infra/design):** Bottle Barrage (no potions-created
  history → tracker), Witch's Curse (no way to tag "potion damage" — damage from potions carries no
  potion source), Gather Herbs ("higher quality" potion-upgrade semantics undefined). **Watch:** Roomy
  Satchel raises run-level max potion count (persists; could stack if duplicated) — flagged in card.
- **2026-06-26 (Phase 5) — familiars + tokens built, 0/0.** APIs: `CreatureCmd.Heal`,
  `PlayerCmd.GainGold`, ignore-block = `CreatureCmd.Damage(.. Move|Unblockable)` (no attack-builder
  option exists), tokens added to hand via `AddGeneratedCardsToCombat(.., PileType.Hand, ..)`.
  - **5 familiars** (each: `XFamiliarPower` + `GainFamiliar<>` + `IFamiliarSummon` + token spawn, **no
    pet sprite** — cosmetic, skipped): Rat→Plague×3, Porcupine→Quills×2, Bear→Hibernate+Mutilate,
    Crow→Scout×2, Sloth→Laze×2. Token "+" upgrades flow through `CreateFamiliarCards(.., IsUpgraded)`.
  - **7 token cards** (`WickenFamiliarCard`, Token, in `Cards/Familiar/`): Plague, Quills, Hibernate,
    Mutilate, Scout, Laze, Rats.
  - **Pocket Rats** (Rats×3 → hand) and **Broom Strike** (15 dmg + `NextFamiliarFreePower` from Phase 0).
  - **Deferred:** Wolf Familiar + Gnash (Pack Tactics = Open Q2); Chimera Familiar / Woe and Whimsy
    (need a random-familiar-token registry); Find Familiar / Pact of Beasts (pile tutor/search by
    "is a familiar card"); Embrace the Wilds (hand transform + make free). Pillage / Stampede / Ritual
    Sacrifice are now **unblocked** (Q1 familiar-count done) but not yet built.

## Per-card workflow (what "done" means for each)

1. C# card class in `TheWickenCode/Cards/` (familiar token-cards go in `Cards/Familiar/`).
2. Placeholder `big/<snake_name>.png` (copy of `big/card.png`) so art loads with the right name.
3. Localization `title` + `description` in `TheWicken/localization/eng/cards.json` with correct dynamic-var tokens.
4. Pool/rarity correct (`WickenCard` → main pool; familiar/token cards → `WickenFamiliarCard`, `Token` rarity).

**Image filename = `snake_case` of the class name.** e.g. class `BrambleShield` → id `THEWICKEN-BRAMBLE_SHIELD`
→ loads `card_portraits/big/bramble_shield.png` and `card_portraits/bramble_shield.png`. Missing files fall
back to `card.png` and log (see `StringExtensions.BigCardImagePath`), so placeholders are low-risk.

## Status legend

- ⬜ Not started
- 🟦 Ready — uses only existing infra, can implement immediately
- 🟧 Blocked — needs new shared infra first (see Phase 0)
- ❓ Design undefined — needs a decision before it can be built (see Open Questions)
- MP — multiplayer-only card: `MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly`
  (ref `Flanking`); auto-excluded from single-player pools

---

## Phase 0 — Shared infrastructure (build FIRST, unblocks most cards)

These are reused across many cards. Order roughly by how many cards they unblock.

### 0a. Counters (turn/combat-scoped trackers)
| Counter | Scope | Cards that need it | Notes |
|---|---|---|---|
| `BramblesCreatedThisTurn` | turn | Bramble Shield | ✅ DONE — `CombatHistoryQueries` (PowerReceived log) |
| `PotionsUsedThisTurn` | turn | Bottle Wall | ✅ DONE — `CombatHistoryQueries` (PotionUsed log) |
| `PotionsCreatedThisCombat` | combat | Bottle Barrage | ⬜ TODO — **no history entry for potion procurement**; needs a small tracker (combat-start-applied counter power, or Harmony patch on `PotionCmd` procurement). Decide when building Bottle Barrage. |
| `FamiliarCount` | combat | Pillage, Stampede | ✅ DONE — `Familiars.Count` over `FamiliarPower` stacks (Q1 resolved) |

Implementation: likely a hidden/counter `WickenPower` per tracker (StackType.Counter), incremented
from the relevant hook. Base-game pattern: counter powers + `AfterPlayerTurnEnd`/`BeforeCombatStart` resets.

### 0b. Brambles upgrades (edit existing `BramblesPower`) — ✅ DONE
- **Vicious Barbs** → `ViciousBarbsPower` built; `BramblesPower` adds its amount to each retaliation.
- **Hedge Prison** → `HedgePrisonPower` built; `BramblesPower` skips `PowerCmd.Decrement` when present.
- Both powers + loc done. The cards themselves are still TODO but are now trivial one-line
  `PowerCmd.Apply<...>` Power cards (Phase 2).

### 0c. Trigger powers ("Whenever you …")
**No shared base needed** — each is a one-hook `WickenPower` built with its card (Phase 3). Listed here
only so the hooks are known. Reference base-game powers in `gamedata/src/Core/Models/Powers/`.
| Power card | Hook | Effect |
|---|---|---|
| Rotting Roots | after a debuff is applied (by player) | gain N brambles |
| Bottomless Cauldron | after another potion is used | create a Wicked Brew |
| Bitter Root | after a potion is used | gain N brambles |
| Cloak of Moonlight | after a card or potion is created | gain N block |
| Cursed Bloodline | after player loses life | gain N brambles |

### 0d. Conditional cost reduction — ✅ DONE
- `Powers/NextSkillDiscountPower.cs` — next Skill costs 1 (Math.Min, only ever a discount). Weathered Witch Hat.
- `Powers/NextFamiliarFreePower.cs` — next `IFamiliarSummon` card costs 0. Broom Strike.
- Both mirror base-game `FreeSkillPower`/`FreePowerPower`: discount in `TryModifyEnergyCostInCombatLate`,
  consume in `BeforeCardPlayed` (runs before the applying card's play → no self-consume). Loc added.
  Cards still TODO but are now trivial (`PowerCmd.Apply<>` + the block/damage half).

### 0e. Potion helpers (partly DONE — rest deferred to their cards)
- ✅ **Offensive / Utility / Defensive potion creation** — already have `PotionCatalog.Query(trait, …)`
  (see `StoneSkin`). Reuse for Something Wicked, Toil and Trouble, Stone Skin pattern.
- **Rarity-up on create** ("next potion higher quality" / "+1 rarity") — ✅ DONE. `Powers/NextPotionUpgradedPower.cs`
  (counter buff) + static `UpgradeRarity(player, rarity)` (consumes a stack, bumps Common→Uncommon→Rare, caps at
  Rare). Honored only by the Wicken's rarity-rolling creators (`SomethingWicked`, `ToilAndTrouble`) — **not** a
  global procurement hook. Gather Herbs applies one stack. Fixed-output creators (Blood Boiling/Cackle/Brew) skip it.
- **Rock potion** = base-game `PotionShapedRock` (already `Token` rarity) — used by Rattling Bottles.
- **Destroy potions + payoff** — Herbal Remedy / Unstable Reaction need a "discard/destroy all belt
  potions, count them" helper.

### 0f. Familiar system extensions
- ✅ **Core built** (see Progress log): `FamiliarPower` + `Familiars` helper + `GainFamiliar<>()`.
  Familiar count, sacrifice, and per-type tracking all run off this.
- **New familiars** (Power cards that add token-cards + summon a cosmetic pet, like `OwlFamiliar`):
  Rat, Chimera, Porcupine, Bear, Crow, Wolf, Sloth. Each needs: a `XFamiliarPower` + loc, a
  `GainFamiliar<XFamiliarPower>()` call in `OnPlay`, and may need a `WickenPet` sprite (currently
  only Owl/Cat pets exist) — placeholder pet or reuse generic.
- **New token cards** (all `WickenFamiliarCard`, `Token` rarity, in `Cards/Familiar/`):
  Plague, Quills, Hibernate, Mutilate, Scout, Gnash, Laze, Rats. (Existing: Ferocity, Wisdom.)
- **Pack Tactics** mechanic (Wolf/Gnash) — **❓ undefined, see Open Q2**.
- **Familiar card search / gather** — Find Familiar (tutor from draw), Pact of Beasts (all familiar
  cards from piles → hand), Embrace the Wilds (transform hand → random familiars).
- **Sacrifice a familiar** — Ritual Sacrifice — **❓ depends on Open Q1 familiar definition**.

---

## Card catalog (grouped by mechanic)

Cost / rarity / type from the CSV. "Ref" = closest base-game class in `gamedata/` to copy.

### Brambles
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Serrated Bones | 2 / Common / Skill | Gain 10 brambles | +3 | none (like `Spines`) | 🟦 |
| Lavender and Sage | 1 / Common / Skill | Draw 2, gain 3 brambles | +2 brambles | none | 🟦 |
| Nettles | 1 / Common / Attack | 6 dmg AoE +1 per bramble | +3 dmg | read bramble count | 🟦 |
| Brambleburst | 2 / Uncommon / Attack | 4 dmg × brambles, lose all | +1 dmg | read+clear bramble | 🟦 |
| Wild Growth | 1 / Uncommon / Skill | Double brambles. Exhaust | remove exhaust | read+apply bramble | 🟦 |
| Needle Whip | 1 / Uncommon / Skill | Remove all brambles, enemies lose that much Str this turn. Exhaust | remove exhaust | temp Str-down AoE | 🟦 |
| Bramble Shield | 1 / Uncommon / Skill | 7 block +2 per bramble made this turn | +1/bramble | 0a counter | ✅ |
| Vicious Barbs | 1 / Uncommon / Power | Brambles deal +3 dmg | +2 | 0b | ✅ |
| Rotting Roots | 1 / Uncommon / Power | On debuff applied, gain 3 brambles | +2 | 0c | ✅ |
| Cursed Bloodline | 1 / Uncommon / Power | On life loss, gain 3 brambles | +1 | 0c | ✅ |
| Bind in Blood | 1 / Common / Skill | This turn, gain brambles per life lost | 2/life | 0c (turn buff) | ✅ |
| Hedge Prison | 3 / Rare / Power | Brambles are permanent | -1 energy | 0b | ✅ |
| Creeping Vines | X / Uncommon / Skill | 5 brambles to random ally, X times | +2 brambles | X-cost; MP-only | ✅ MP |

### Debuff
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Forbidden Magic | 1 / Common / Attack | 15 dmg, gain 2 weak | +5 dmg, +1 weak | none | 🟦 |
| Stuck in the Bush | 2 / Uncommon / Skill | 20 block, gain 2 vulnerable | +5 block, +1 vuln | none | 🟦 |
| Moondrop Tea | 1 / Common / Skill | 8 block, remove a random debuff | +3 block | remove-debuff-from-self | 🟦 |
| Circle of Rot | 2 / Uncommon / Skill | You(+allies) gain 10 block, apply 2 weak, gain 2 weak | +2 block, +1 weak | MP-only (Flanking) | ✅ MP |
| Hexburst | 1 / Uncommon / Attack | 6 dmg per unique debuff on enemy | +2 dmg | count unique debuffs | 🟦 |
| Rancid Smoke | 2 / Uncommon / Skill | Spread all your debuffs to ALL enemies. Exhaust | remove exhaust | debuff-copy (Q4) | ✅ |

### Potions
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Something Wicked | 2 / Common / Skill | Create an Offensive potion | +1 rarity | 0e catalog | 🟦 |
| Toil and Trouble | 2 / Common / Skill | Create a Utility potion | +1 rarity | 0e catalog | 🟦 |
| Gather Herbs | 1 / Common / Skill | Next potion made is higher quality. Exhaust | remove exhaust | 0e NextPotionUpgraded | ✅ |
| Blood Boiling | 1 / Rare / Skill | Lose 10 life, create a Rare potion. Exhaust | -1 energy | 0e (rare via catalog) | ✅ |
| Dance Around the Cauldron | 1 / Common / Skill | End of turn: 1 Wicked Brew per unspent energy | -1 energy | end-of-turn hook | ✅ |
| Bottle Wall | 1 / Uncommon / Skill | 5 block +4 per potion used this turn | +2 / +2 | 0a counter | ✅ |
| Bottle Barrage | 2 / Rare / Attack | 10 dmg per potion created this combat | +3 dmg | 0a counter | ✅ |
| Herbal Remedy | 0 / Uncommon / Skill | Destroy all potions, +1 energy each. Exhaust | remove exhaust | 0e destroy+count | ✅ |
| Unstable Reaction | 2 / Common / Attack | Destroy all potions, 10 AoE dmg each | +3 dmg | 0e destroy+count | ✅ |
| Rattling Bottles | 3 / Rare / Skill | Fill potion slots with Rock potions. Exhaust | -1 energy | PotionShapedRock | ✅ |
| Witch's Curse | 1 / Uncommon / Skill | Enemy takes double potion dmg this turn. Exhaust | remove exhaust | enemy power | ✅ |
| Bottomless Cauldron | 3 / Rare / Power | On using another potion, create Wicked Brew | -1 energy | 0c | ✅ |
| Bitter Root | 1 / Uncommon / Power | On potion use, gain 4 brambles | +2 | 0c | ✅ |
| Cloak of Moonlight | 2 / Uncommon / Power | On creating a card or potion, gain 3 block | -1 energy | 0c | ✅ |
| Roomy Satchel | 1 / Uncommon / Power | Gain 2 potion slots | +1 slot | potion-slot stat mod | 🟧 |
| Tiny Bottle | 0 / Uncommon / Skill | Steal 2 energy from ally, give potion per energy | potion+ | MP-only (Flanking) | ✅ MP |
| Share the Brew | 2 / Uncommon / Skill | Allies gain a random potion | -1 energy | MP-only (Flanking) | ✅ MP |
| Cackle | 2 / Rare / Skill | Create "The Cauldron" (big potion). Exhaust | -1 energy | new potion (Q5) | ✅ |

### Familiars
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Woe and Whimsy | 1 / Common / Skill | Create a random familiar card | upgraded | random familiar pick | ✅ |
| Find Familiar | 1 / Uncommon / Skill | Tutor a familiar card from deck → hand | -1 energy | card search | ✅ |
| Pact of Beasts | 2 / Rare / Skill | All created familiar cards → hand | -1 energy | pile search | ✅ |
| Embrace the Wilds | 3 / Rare / Skill | Transform hand → random familiar **summon powers**, free. Exhaust | -1 energy | hand transform | ✅ |
| Pillage | 1 / Common / Attack | 5 dmg, draw per familiar | +3 dmg | 0a FamiliarCount (Q1) | ✅ |
| Stampede | 1 / Common / Attack | Each familiar deals 5 dmg | +3 dmg | 0a FamiliarCount (Q1) | ✅ |
| Ritual Sacrifice | 1 / Uncommon / Skill | Sacrifice a familiar, gain 20 block | +5 block | sacrifice (Q1) | ✅ |
| Broom Strike | 2 / Common / Attack | 15 dmg, next familiar power free | +3 dmg | 0d cost reduction | ✅ |
| Rat Familiar | 1 / Rare / Power | Add 3 Plague. Exhaust | each loses 2 Str | new familiar + Plague | ✅ |
| Chimera Familiar | 1 / Rare / Power | Add 3 random familiar cards | +1 card | random familiar pick | ✅ |
| Porcupine Familiar | 1 / Uncommon / Power | Add 2 Quills | Quills+ | new familiar + Quills | ✅ |
| Bear Familiar | 2 / Rare / Power | Add Hibernate + Mutilate | both+ | new familiar + 2 tokens | ✅ |
| Crow Familiar | 1 / Rare / Power | Add 2 Scout | +5 gold | new familiar + Scout | ✅ |
| Wolf Familiar | 1 / Uncommon / Power | Add 3 Gnash | +3 dmg | new familiar + Gnash + Pack Tactics (Q2) | ✅ |
| Sloth Familiar | 2 / Uncommon / Power | Add 2 Laze | Laze+ | new familiar + Laze | ✅ |
| Pocket Rats! | 1 / Rare / Skill | Add 3 Rats → hand. Exhaust | +1 rat | Rats token | ✅ |

### No special mechanic (quick wins)
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Bag of Teeth | 1 / Common / Attack | 1 dmg × 6 | +2 hits | none (multi-hit) | 🟦 |
| Hidden in Smoke | 2 / Rare / Skill | Gain 1 Intangible. Exhaust | -1 energy | IntangiblePower (base) | 🟦 |
| Weathered Witch Hat | 2 / Common / Skill | 10 block, next skill costs 1 | +3 block | 0d cost reduction | ✅ |

### New token cards (created by the above, not in reward pool)
`WickenFamiliarCard`, `Token` rarity, `Cards/Familiar/`. Image path `familiar/<snake_name>.png`.
| Token | From | Effect | Status |
|---|---|---|---|
| Plague | Rat Familiar | 0 cost: draw a card, lose 1 Str, enemies lose 1 Str | ✅ |
| Quills | Porcupine Familiar | 0: 3 dmg, gain 8 brambles | ✅ |
| Hibernate | Bear Familiar | 2: gain 15 block, heal 3 | ✅ |
| Mutilate | Bear Familiar | 2: 30 dmg, ignores block | ✅ |
| Scout | Crow Familiar | 1: apply 2 vuln + 1 weak, gain 5 gold. Exhaust | ✅ |
| Gnash | Wolf Familiar | 5 dmg, +5 per Gnash played this combat (Q2 resolved: escalate, not buff-in-hand) | ✅ |
| Laze | Sloth Familiar | 0: draw a card, gain 8 block | ✅ |
| Rats | Pocket Rats | 0: 5 dmg, heal 1. Exhaust | ✅ |

---

## Phased rollout (recommended order)

1. **Phase 1 — 🟦 quick wins** (no new infra): ✅ **DONE** (16 cards — see Progress log). Serrated Bones,
   Lavender and Sage, Nettles, Brambleburst, Wild Growth, Needle Whip, Forbidden Magic, Stuck in the
   Bush, Moondrop Tea, Hexburst, Something Wicked, Toil and Trouble, Blood Boiling, Rattling Bottles,
   Bag of Teeth, Hidden in Smoke. Art/loc/class pipeline validated at volume.
2. **Phase 2 — Brambles infra (0a/0b) + dependent cards**: ✅ **DONE**. Infra was built in Phase 0;
   cards Bramble Shield, Vicious Barbs, Hedge Prison shipped (see Progress log).
3. **Phase 3 — Trigger powers (0c)**: ✅ **DONE**. Rotting Roots, Cursed Bloodline, Bind in Blood,
   Bottomless Cauldron, Bitter Root, Cloak of Moonlight (see Progress log).
4. **Phase 4 — Potion infra (0e) + counters**: ✅ **8/8 DONE** (Dance Around the Cauldron, Bottle Wall,
   Herbal Remedy, Unstable Reaction, Roomy Satchel + Gather Herbs, Bottle Barrage, Witch's Curse — bucket C).
5. **Phase 5 — Familiars (0f)**: ✅ **mostly DONE** — 5 familiars (Rat/Porcupine/Bear/Crow/Sloth) + 7
   token cards + Pocket Rats + Broom Strike. ⬜ Deferred: Wolf (Q2), Chimera, Woe and Whimsy, Find
   Familiar, Pact of Beasts, Embrace the Wilds.
6. **Phase 6** — ✅ Familiar-count trio **DONE** (Pillage, Stampede, Ritual Sacrifice). Pack Tactics
   (Wolf/Gnash, Q2) and Cackle/The Cauldron (Q5) ✅ DONE. Co-op cards (Circle of Rot, Tiny Bottle,
   Share the Brew, Creeping Vines) ✅ DONE (bucket B). Random-familiar utility (Chimera, Woe and Whimsy,
   Find Familiar, Pact of Beasts, Embrace the Wilds) ✅ DONE (bucket A). Potion-infra trio (Bottle Barrage,
   Gather Herbs, Witch's Curse) ✅ DONE (bucket C). Rancid Smoke (Q4) ✅ DONE. **🎉 All 56 cards built.**

### 2026-06-26 (Phase 6 partial)
Pillage / Stampede / Ritual Sacrifice built on the Q1 familiar system, 0/0. Stampede = `WithHitCount(Familiars.Count)`;
Pillage draws per familiar; Ritual Sacrifice = `Familiars.RemoveRandom` then block only if one was sacrificed.

---

## Open design questions (need user decisions)

- **Q1 — What is a "familiar"? ✅ RESOLVED.** Each familiar type grants one stack of an associated
  `FamiliarPower` (e.g. playing Owl Familiar → +1 `OwlFamiliarPower`). Total familiar count = sum of
  all `FamiliarPower` stacks. Sacrifice = pick a random familiar power and decrement it (auto-removed
  at 0). Built — see Progress log / `Powers/Familiars.cs`.
- **Q2 — Pack Tactics (Wolf / Gnash). ✅ RESOLVED.** Implemented as *escalation*: each Gnash deals its
  base 5 dmg **+5 for every Gnash already played this combat** (`CombatHistoryQueries.CardsPlayedThisCombat<Gnash>`).
  No buff-in-hand / shared-tag keyword — the pack "grows" as you spend them. Built: `Cards/Familiar/Gnash.cs`,
  `Cards/WolfFamiliar.cs`, `Powers/WolfFamiliarPower.cs`.
- **Q3 — Multiplayer "ally" cards. ✅ RESOLVED.** Circle of Rot, Tiny Bottle, Share the Brew, Creeping
  Vines are **multiplayer-only**: override `MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly`
  (base-game ref: `Flanking`). The game auto-excludes them from single-player reward/shop pools, so no
  single-player fallback logic is needed. Build them with their full co-op ally behavior.
- **Q4 — Rancid Smoke "spread all of YOUR debuffs". ✅ RESOLVED.** Source = the Wicken's own debuffs.
  Built `Cards/RancidSmoke.cs` mirroring base-game `Misery`: clone each `PowerType.Debuff` power on
  `Owner.Creature` (`ClonePreservingMutability`) and apply to every enemy (stack via
  `FindExistingInstanceForStacking` / `ModifyAmount`; `ITemporaryPower.IgnoreNextInstance` on fresh applies).
  The Wicken keeps hers — enemies get copies.
- **Q5 — "The Cauldron" (Cackle). ✅ RESOLVED.** Built as a concrete potion `Potions/TheCauldron.cs`
  (granted via `PotionCmd.TryToProcure<TheCauldron>`); Cackle is a Rare Exhaust skill that creates it.
  See that file for the effect/rarity that was chosen.
- **Minor:** "Wicked Brew" capitalization in loc (existing key is `WickedBrew`/"Wicked Brew"); keep
  consistent. Several upgrades say "-1 energy" on already-cheap cards — confirm intended floors.
