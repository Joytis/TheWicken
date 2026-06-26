# Card Implementation Plan — `import/STS2 TheWicken - Cards.csv`

Durable TODO + design record for implementing the 56 designed cards. One row in the
CSV (Bear Familiar's upgrade) is a line-continuation, so the count is **56 cards**,
not 57.

## Progress log

- **2026-06-25** — Plan ingested (56 cards). Decisions Q1/Q3 made (below). **Phase 0 — Familiar power
  system built & compiling:**
  - `Powers/FamiliarPower.cs` — abstract counter base; one stack = one familiar of that type.
  - `Powers/OwlFamiliarPower.cs`, `Powers/CatFamiliarPower.cs` — concrete powers for existing familiars.
  - `Powers/Familiars.cs` — `Count` / `On` / `Any` / `RemoveRandom(rng)` helpers (familiar count = sum
    of all `FamiliarPower` stacks; sacrifice = decrement a random one, auto-removed at 0).
  - `Cards/WickenCard.cs` — `GainFamiliar<TPower>()` helper; wired into `OwlFamiliar` + `CatFamiliar`.
  - Loc added for both powers in `powers.json`. **Each new familiar card must add its own
    `XFamiliarPower` + loc + a `GainFamiliar<>` call.**

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
| `BramblesCreatedThisTurn` | turn | Bramble Shield | increment whenever `BramblesPower` is applied to player |
| `PotionsUsedThisTurn` | turn | Bottle Wall | hook potion-use |
| `PotionsCreatedThisCombat` | combat | Bottle Barrage | hook potion-procure/create |
| `FamiliarCount` | combat | Pillage, Stampede | ✅ DONE — `Familiars.Count` over `FamiliarPower` stacks (Q1 resolved) |

Implementation: likely a hidden/counter `WickenPower` per tracker (StackType.Counter), incremented
from the relevant hook. Base-game pattern: counter powers + `AfterPlayerTurnEnd`/`BeforeCombatStart` resets.

### 0b. Brambles upgrades (edit existing `BramblesPower`)
- **Vicious Barbs** → brambles deal extra damage. `BramblesPower.BeforeDamageReceived` must read a
  `ViciousBarbsPower` on the owner and add its amount to the retaliation.
- **Hedge Prison** → brambles don't decrement. Same hook must skip `PowerCmd.Decrement` when a
  `HedgePrisonPower` is present.
- Both are persistent buff powers (one-shot apply from a Power card). `BramblesPower.cs` is the single
  edit point for both.

### 0c. Trigger powers ("Whenever you …")
Each is a persistent `WickenPower` overriding one hook. Reference base-game powers in
`gamedata/src/Core/Models/Powers/`.
| Power card | Hook | Effect |
|---|---|---|
| Rotting Roots | after a debuff is applied (by player) | gain N brambles |
| Bottomless Cauldron | after another potion is used | create a Wicked Brew |
| Bitter Root | after a potion is used | gain N brambles |
| Cloak of Moonlight | after a card or potion is created | gain N block |
| Cursed Bloodline | after player loses life | gain N brambles |

### 0d. Conditional cost reduction
- **Weathered Witch Hat** → next Skill costs 1 (cost-set power consumed on next skill play).
- **Broom Strike** → next Familiar Power is free (cost-set, gated to familiar power cards).
- Reference: base-game "next card costs less" effects (e.g. Setup-style / Madness-style cost mods).

### 0e. Potion helpers
- **Offensive / Utility / Defensive potion creation** — already have `PotionCatalog.Query(trait, …)`
  (see `StoneSkin`). Reuse for Something Wicked, Toil and Trouble, Stone Skin pattern.
- **Rarity-up on create** ("next potion higher quality" / "+1 rarity") — Gather Herbs needs a
  `NextPotionUpgraded` buff; several upgrades read "potion is 1 rarity higher".
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
| Bramble Shield | 1 / Uncommon / Skill | 7 block +2 per bramble made this turn | +1/bramble | 0a counter | 🟧 |
| Vicious Barbs | 1 / Uncommon / Power | Brambles deal +3 dmg | +2 | 0b | 🟧 |
| Rotting Roots | 1 / Uncommon / Power | On debuff applied, gain 3 brambles | +2 | 0c | 🟧 |
| Cursed Bloodline | 1 / Uncommon / Power | On life loss, gain 3 brambles | +1 | 0c | 🟧 |
| Bind in Blood | 1 / Common / Skill | This turn, gain brambles per life lost | 2/life | 0c (turn buff) | 🟧 |
| Hedge Prison | 3 / Rare / Power | Brambles are permanent | -1 energy | 0b | 🟧 |
| Creeping Vines | X / Uncommon / Skill | 5 brambles to random ally, X times | +2 brambles | X-cost; MP-only | 🟧 MP |

### Debuff
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Forbidden Magic | 1 / Common / Attack | 15 dmg, gain 2 weak | +5 dmg, +1 weak | none | 🟦 |
| Stuck in the Bush | 2 / Uncommon / Skill | 20 block, gain 2 vulnerable | +5 block, +1 vuln | none | 🟦 |
| Moondrop Tea | 1 / Common / Skill | 8 block, remove a random debuff | +3 block | remove-debuff-from-self | 🟦 |
| Circle of Rot | 2 / Uncommon / Skill | You(+allies) gain 10 block, apply 2 weak, gain 2 weak | +2 block, +1 weak | MP-only (Flanking) | 🟦 MP |
| Hexburst | 1 / Uncommon / Attack | 6 dmg per unique debuff on enemy | +2 dmg | count unique debuffs | 🟦 |
| Rancid Smoke | 2 / Uncommon / Skill | Spread all your debuffs to ALL enemies. Exhaust | remove exhaust | debuff-copy (Q4) | 🟧❓ |

### Potions
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Something Wicked | 2 / Common / Skill | Create an Offensive potion | +1 rarity | 0e catalog | 🟦 |
| Toil and Trouble | 2 / Common / Skill | Create a Utility potion | +1 rarity | 0e catalog | 🟦 |
| Gather Herbs | 1 / Common / Skill | Next potion made is higher quality. Exhaust | remove exhaust | 0e NextPotionUpgraded | 🟧 |
| Blood Boiling | 1 / Rare / Skill | Lose 10 life, create a Rare potion. Exhaust | -1 energy | 0e (rare via catalog) | 🟦 |
| Dance Around the Cauldron | 1 / Common / Skill | End of turn: 1 Wicked Brew per unspent energy | -1 energy | end-of-turn hook | 🟧 |
| Bottle Wall | 1 / Uncommon / Skill | 5 block +4 per potion used this turn | +2 / +2 | 0a counter | 🟧 |
| Bottle Barrage | 2 / Rare / Attack | 10 dmg per potion created this combat | +3 dmg | 0a counter | 🟧 |
| Herbal Remedy | 0 / Uncommon / Skill | Destroy all potions, +1 energy each. Exhaust | remove exhaust | 0e destroy+count | 🟧 |
| Unstable Reaction | 2 / Common / Attack | Destroy all potions, 10 AoE dmg each | +3 dmg | 0e destroy+count | 🟧 |
| Rattling Bottles | 3 / Rare / Skill | Fill potion slots with Rock potions. Exhaust | -1 energy | PotionShapedRock | 🟦 |
| Witch's Curse | 1 / Uncommon / Skill | Enemy takes double potion dmg this turn. Exhaust | remove exhaust | enemy power | 🟧 |
| Bottomless Cauldron | 3 / Rare / Power | On using another potion, create Wicked Brew | -1 energy | 0c | 🟧 |
| Bitter Root | 1 / Uncommon / Power | On potion use, gain 4 brambles | +2 | 0c | 🟧 |
| Cloak of Moonlight | 2 / Uncommon / Power | On creating a card or potion, gain 3 block | -1 energy | 0c | 🟧 |
| Roomy Satchel | 1 / Uncommon / Power | Gain 2 potion slots | +1 slot | potion-slot stat mod | 🟧 |
| Tiny Bottle | 0 / Uncommon / Skill | Steal 2 energy from ally, give potion per energy | potion+ | MP-only (Flanking) | 🟧 MP |
| Share the Brew | 2 / Uncommon / Skill | Allies gain a random potion | -1 energy | MP-only (Flanking) | 🟧 MP |
| Cackle | 2 / Rare / Skill | Create "The Cauldron" (big potion). Exhaust | -1 energy | new potion (Q5) | 🟧❓ |

### Familiars
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Woe and Whimsy | 1 / Common / Skill | Create a random familiar card | upgraded | random familiar pick | 🟧 |
| Find Familiar | 1 / Uncommon / Skill | Tutor a familiar card from deck → hand | -1 energy | card search | 🟧 |
| Pact of Beasts | 2 / Rare / Skill | All created familiar cards → hand | -1 energy | pile search | 🟧 |
| Embrace the Wilds | 3 / Rare / Skill | Transform hand → random familiars, free. Exhaust | -1 energy | hand transform | 🟧 |
| Pillage | 1 / Common / Attack | 5 dmg, draw per familiar | +3 dmg | 0a FamiliarCount (Q1) | 🟧❓ |
| Stampede | 1 / Common / Attack | Each familiar deals 5 dmg | +3 dmg | 0a FamiliarCount (Q1) | 🟧❓ |
| Ritual Sacrifice | 1 / Uncommon / Skill | Sacrifice a familiar, gain 20 block | +5 block | sacrifice (Q1) | 🟧❓ |
| Broom Strike | 2 / Common / Attack | 15 dmg, next familiar power free | +3 dmg | 0d cost reduction | 🟧 |
| Rat Familiar | 1 / Rare / Power | Add 3 Plague. Exhaust | each loses 2 Str | new familiar + Plague | 🟧 |
| Chimera Familiar | 1 / Rare / Power | Add 3 random familiar cards | +1 card | random familiar pick | 🟧 |
| Porcupine Familiar | 1 / Uncommon / Power | Add 2 Quills | Quills+ | new familiar + Quills | 🟧 |
| Bear Familiar | 2 / Rare / Power | Add Hibernate + Mutilate | both+ | new familiar + 2 tokens | 🟧 |
| Crow Familiar | 1 / Rare / Power | Add 2 Scout | +5 gold | new familiar + Scout | 🟧 |
| Wolf Familiar | 1 / Uncommon / Power | Add 3 Gnash | +3 dmg | new familiar + Gnash + Pack Tactics (Q2) | 🟧❓ |
| Sloth Familiar | 2 / Common / Power | Add 2 Laze | Laze+ | new familiar + Laze | 🟧 |
| Pocket Rats! | 1 / Rare / Skill | Add 3 Rats → hand. Exhaust | +1 rat | Rats token | 🟧 |

### No special mechanic (quick wins)
| Card | C/R/Type | Effect | Upgrade | Infra | Status |
|---|---|---|---|---|---|
| Bag of Teeth | 1 / Common / Attack | 1 dmg × 6 | +2 hits | none (multi-hit) | 🟦 |
| Hidden in Smoke | 2 / Rare / Skill | Gain 1 Intangible. Exhaust | -1 energy | IntangiblePower (base) | 🟦 |
| Weathered Witch Hat | 2 / Common / Skill | 10 block, next skill costs 1 | +3 block | 0d cost reduction | 🟧 |

### New token cards (created by the above, not in reward pool)
`WickenFamiliarCard`, `Token` rarity, `Cards/Familiar/`. Image path `familiar/<snake_name>.png`.
| Token | From | Effect |
|---|---|---|
| Plague | Rat Familiar | 0 cost: draw a card, lose 1 Str, enemies lose 1 Str |
| Quills | Porcupine Familiar | 0: 3 dmg, gain 8 brambles |
| Hibernate | Bear Familiar | 2: gain 15 block, heal 3 |
| Mutilate | Bear Familiar | 2: 30 dmg, ignores block |
| Scout | Crow Familiar | 1: apply 2 vuln + 1 weak, gain 5 gold. Exhaust |
| Gnash | Wolf Familiar | 5 dmg, Pack Tactics +5 (Q2) |
| Laze | Sloth Familiar | 0: draw a card, gain 8 block |
| Rats | Pocket Rats | 0: 5 dmg, heal 1. Exhaust |

---

## Phased rollout (recommended order)

1. **Phase 1 — 🟦 quick wins** (no new infra): Serrated Bones, Lavender and Sage, Nettles,
   Brambleburst, Wild Growth, Needle Whip, Forbidden Magic, Stuck in the Bush, Moondrop Tea,
   Hexburst, Something Wicked, Toil and Trouble, Blood Boiling, Rattling Bottles, Bag of Teeth,
   Hidden in Smoke. (~16 cards) Validates art/loc pipeline at volume.
2. **Phase 2 — Brambles infra (0a/0b) + dependent cards**: counters + BramblesPower edits, then
   Bramble Shield, Vicious Barbs, Hedge Prison.
3. **Phase 3 — Trigger powers (0c)**: Rotting Roots, Cursed Bloodline, Bind in Blood, Bottomless
   Cauldron, Bitter Root, Cloak of Moonlight.
4. **Phase 4 — Potion infra (0e) + counters**: Gather Herbs, Dance Around the Cauldron, Bottle Wall,
   Bottle Barrage, Herbal Remedy, Unstable Reaction, Witch's Curse, Roomy Satchel.
5. **Phase 5 — Familiars (0f)**: new familiars + token cards, then Woe and Whimsy, Find Familiar,
   Pact of Beasts, Embrace the Wilds, Broom Strike (0d).
6. **Phase 6 — Design-gated (❓)**: Familiar-count cards (Pillage, Stampede, Ritual Sacrifice),
   Pack Tactics (Wolf/Gnash), multiplayer cards (Circle of Rot, Tiny Bottle, Share the Brew,
   Creeping Vines), Rancid Smoke, Cackle/The Cauldron — after Open Questions answered.

---

## Open design questions (need user decisions)

- **Q1 — What is a "familiar"? ✅ RESOLVED.** Each familiar type grants one stack of an associated
  `FamiliarPower` (e.g. playing Owl Familiar → +1 `OwlFamiliarPower`). Total familiar count = sum of
  all `FamiliarPower` stacks. Sacrifice = pick a random familiar power and decrement it (auto-removed
  at 0). Built — see Progress log / `Powers/Familiars.cs`.
- **Q2 — Pack Tactics (Wolf / Gnash):** "all Pack Tactics deal +5 dmg." Define the keyword: does
  playing one Gnash buff the rest in hand? Is "Pack Tactics" a tag shared by all wolf tokens? Need
  the exact rule.
- **Q3 — Multiplayer "ally" cards. ✅ RESOLVED.** Circle of Rot, Tiny Bottle, Share the Brew, Creeping
  Vines are **multiplayer-only**: override `MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly`
  (base-game ref: `Flanking`). The game auto-excludes them from single-player reward/shop pools, so no
  single-player fallback logic is needed. Build them with their full co-op ally behavior.
- **Q4 — Rancid Smoke "spread all of YOUR debuffs":** does the Wicken accumulate debuffs on herself
  that this then copies to all enemies? Confirm source = player's own debuffs.
- **Q5 — "The Cauldron" (Cackle):** undefined ("a big potion that does something really big"). Needs
  a concrete effect + rarity before build.
- **Minor:** "Wicked Brew" capitalization in loc (existing key is `WickedBrew`/"Wicked Brew"); keep
  consistent. Several upgrades say "-1 energy" on already-cheap cards — confirm intended floors.
