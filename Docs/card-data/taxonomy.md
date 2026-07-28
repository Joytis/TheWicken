# Card Tag Taxonomy — `sub` and `threads`

Every card in `Docs/card-data/{cards,silent,necrobinder,ironclad,defect,regent}.json` carries two
curated tag arrays beyond the original `mechanics[]`/`role[]`:

- **`sub[]`** — *sub-archetype* within a class pillar, named `Pillar:SubName` (e.g. `Potions:Brew`,
  `Poison:Engine`). Class-scoped: splits each pillar into its functionally distinct wings
  (generate vs spend vs engine vs payoff). A sub-archetype needs ≥2 member cards to exist.
- **`threads[]`** — *cross-class functional threads* from the shared vocabulary below. These are
  the axes on which pools are directly comparable (how much AoE does each class get, who has
  draw engines, who pays costs in HP vs cards vs tempo).

Both fields are **curated** (hand/agent-audited against card text and the decompiled source) and
**preserved across regens** by `regen.js` / `gen-basegame.js`, keyed by `entry` — exactly like
`mechanics`/`role`. When a card's *mechanics* change, re-audit its tags (the `card-db-update`
skill covers this).

Provenance: taxonomy mined 2026-07 by a per-class audit of all 539 cards (6 pools), with
keyword ground-truthing against `gamedata/` decompiled `CanonicalKeywords`/`CanonicalStarCost`
(the JSON `text` field does NOT include keyword lines — see Data-quality notes).

## Thread vocabulary

Multi-tag allowed; empty allowed. Counts are cards carrying the tag across all six pools at
mining time.

### Damage shape
| Tag | Meaning | n |
|---|---|---|
| `AoE` | Hits/affects ALL enemies (widened to any all-enemies offensive effect) | 46 |
| `MultiHit` | Multiple hit instances in one play | 38 |
| `BurstFinisher` | Big scaling one-shot payout | 17 |

### Scaling
| Tag | Meaning | n |
|---|---|---|
| `StatRamp` | Permanent stat ramp (Strength/Focus-like) | 17 |
| `Snowball` | Effect grows during the combat (Claw, Rampage, Gnash) | 24 |

### Defense
| Tag | Meaning | n |
|---|---|---|
| `BlockEngine` | Repeatable / per-turn block source | 17 |
| `BlockPayoff` | Rewards having Block (Body Slam, Juggernaut) | 6 |
| `Heal` / `MaxHP` | HP recovery / max-HP gain | 3 / 1 |

### Enemy debuffs
| Tag | Meaning | n |
|---|---|---|
| `Weak` / `Vulnerable` | Applies that debuff | 20 / 20 |
| `OtherDebuff` | Any other enemy debuff (Hex, Doom, Poison excluded — pillar tags carry those) | 26 |
| `DebuffPayoff` | Scales off / amplifies existing debuffs | 17 |

### Resources
| Tag | Meaning | n |
|---|---|---|
| `Draw` / `Energy` | Card draw / energy gain | 57 / 35 |
| `CostReduction` | Discounts card costs | 20 |
| `Retain` | Has or grants Retain | 14 |
| `Tutor` | Searches a pile for a specific card | 6 |

### Card economy
| Tag | Meaning | n |
|---|---|---|
| `CardGen` | Creates new cards mid-combat | 49 |
| `SelfExhaust` | The card itself Exhausts (keyword) | 79 |
| `ExhaustOutlet` | Exhausts OTHER cards as cost/fuel | 13 |
| `ExhaustPayoff` | Rewards exhausting / reads the exhaust pile | 10 |
| `Discard` | Discards cards (cost or filter) | 11 |
| `SelfStatus` | Adds Statuses/Wounds to your OWN piles | 8 |
| `Recursion` | Retrieves other cards from Discard/Exhaust piles | 6 |
| `SelfRecursion` | Returns/replays ITSELF for repeat value | 6 |
| `Transform` | Converts existing cards into token cards | 4 |
| `KeywordGrant` | Adds a keyword (Ethereal/Retain/Replay) to another card | 4 |

### Costs & drawbacks
| Tag | Meaning | n |
|---|---|---|
| `HPLoss` | Pays own HP for the effect | 13 |
| `SelfDebuff` | Debuffs the PLAYER as a cost (or pays off being debuffed) | 7 |
| `Drawback` | Any other downside priced into the card | 32 |
| `DirectHPLoss` | Deals untyped HP loss to enemies, bypassing Block | 3 |

### Type-tribal ("X matters")
| Tag | Meaning | n |
|---|---|---|
| `AttacksMatter` | Cares about the Attack card type (count/trigger/discount/auto-play) | 12 |
| `SkillsMatter` | Cares about the Skill card type | 10 |
| `PowersMatter` | Cares about the Power card type | 6 |
| `ZeroCostMatters` | References 0-cost cards explicitly (Claw-deck engine) | 5 |

### Tempo & engines
| Tag | Meaning | n |
|---|---|---|
| `TurnEngine` | Power/effect that triggers every turn | 46 |
| `DelayedEffect` | Effect banks for a FUTURE turn (pay now, collect later) | 19 |
| `PlayVolume` | Rewards playing many cards (or type-N cards) in one turn | 9 |
| `DrawPayoff` | Triggers/scales per card drawn | 5 |
| `AutoPlay` | Plays cards without paying/choosing normally (Havoc-likes) | 7 |
| `TopdeckControl` | Manipulates draw-pile ORDER (place-on-top / trigger-on-top) | 5 |
| `Innate` | Guaranteed in opening hand | 3 |

**Defined-but-zero tags** (signal, not omission): `Frail` — no card in any of the six pools
applies Frail; `Scry` — no scry mechanic in StS2 base pools; `SelfDamage` — folded into `HPLoss`.

## Sub-archetypes per class

Format: `Pillar:Sub` — definition (count).

### Witch (cards.json)
- `Potions:Brew` — creates potions in combat (16) · `Potions:UsePayoff` — triggers when a potion is USED (6) · `Potions:CreatePayoff` — scales off potions CREATED this combat (3) · `Potions:Economy` — improves stock quality: rarity-upgrade, copy-next (2). *UsePayoff vs CreatePayoff are different hooks — only Volatile Vapors bridges both.*
- `Familiars:Summon` — applies a FamiliarPower stack (7) · `Familiars:TokenGen` — injects token cards directly (7) · `Familiars:CountPayoff` — scales per active familiar (3) · `Familiars:Sacrifice` — consumes a familiar for a payout (2) · `Familiars:Token` — the generated payload cards (12).
- `Brambles:Rider` — small gain stapled to a normal card (5) · `Brambles:Ramp` — big one-shot stacking (3) · `Brambles:Engine` — repeatable passive generation (2) · `Brambles:Spend` — consumes Brambles as currency (3).
- `Hex:Apply` — applies Hex to enemies (13) · `Hex:Exploit` — dedicated hex/debuff payoff (3) · `Hex:Self` — gains Hex on the PLAYER as a cost (2). *(Pillar renamed from `Debuff/Buff` 2026-07-27; non-hex Weak/Vulnerable cards are now generic `None`.)*

### Silent
- `Poison:Burst` (5) · `Poison:Engine` — repeating application (3) · `Poison:Payoff` (4).
- `Shivs:Gen` (9) · `Shivs:Buff` — makes every Shiv better (3).
- `Discard:Cycle` — filtering (5) · `Discard:Cost` — discard as price (4) · `Discard:Sly` — carries/grants Sly, several deliberately overcosted = de facto Discard payoffs (10).

### Necrobinder
- `Osty:Summon` (12) · `Osty:Command` — attacks BY Osty, dead without him (12) · `Osty:SwarmTurn` — rewards multiple Osty attacks per turn (3) · `Osty:HPScaling` (2) · `Osty:Sacrifice` (2).
- `Doom:Apply` (6) · `Doom:Engine` — repeating application (3) · `Doom:Payoff` — apply-triggers AND accumulate-detonators (5).
- `Soul:Make` (7) · `Soul:Payoff` (3).
- `Ethereal:Grant` (2) · `Ethereal:Payoff` (4) · `Ethereal:Fuel` — CARRIES Ethereal as a stat-balancing drawback; the incidental fuel the payoffs harvest (8).

### Ironclad
- `Strength:Ramp` (6) — payoffs are the MultiHit suite, not dedicated cards.
- `Vulnerable:Applier` (7) · `Vulnerable:Exploit` (6).
- `Exhaust:Outlet` — exhausts others (10) · `Exhaust:SelfFuel` — carries Exhaust keyword, incidental fuel (9) · `Exhaust:TriggerEngine` — Feel No Pain / Dark Embrace (2) · `Exhaust:TurnConditional` (2) · `Exhaust:PileScaler` (2) · `Exhaust:WantsExhausted` — rewards being exhausted (2).
- `SelfHarm:Cost` (9) · `SelfHarm:Payoff` (4).

### Defect
- `Orbs:Lightning` (9) · `Orbs:Frost` (7) · `Orbs:Dark` (5) · `Orbs:Plasma` (3) · `Orbs:Glass` (3) · `Orbs:RandomChannel` (2) — channel-type wings, depth very uneven (Plasma/Glass have zero type-specific payoffs).
- `Orbs:EvokePayoff` — consume (6) · `Orbs:TriggerPassive` — fire without consuming (3) · `Orbs:CountScaling` — per-orb / per-unique-orb, the "rainbow" wing (4) · `Orbs:SlotManip` (3).
- `Focus:TurnBurst` — temp Focus (3) · `Focus:PermRamp` — deliberately scarce (2).
- `Status:SelfInflictGen` — above-rate cards costed by self-Statuses (5) · `Status:OnCreatePayoff` (3) · `Status:InHandPayoff` (3).

### Regent
- `Stars:Generate` (12) · `Stars:Spend` — has a star cost, incl. 5 cards whose star cost is invisible in the JSON text (22) · `Stars:FlowTrigger` — throughput, not stockpile (3).
- `Forge:Generate` (11) · `Forge:SovereignBlade` — the pillar's payoff surface: 5 cards modifying one signature card (5).
- `Colorless:Generate` (5) · `Colorless:CreationPayoff` (3).
- `Vigor:Gain` (2) — vestigial, no payoff.

## Data-quality notes (affect any text-derived analysis)

1. **The base-game JSON `text` field drops `CanonicalKeywords`** — Sly, Exhaust, Innate, Retain
   are invisible (~25 Silent cards alone). Untouchable reads as an unplayable "2-cost 6 Block"
   but is Sly. Regent star costs are likewise absent from text (5 cards). The `threads` tags
   carry this ground truth now; if numeric efficiency comparison is built, remember self-exhaust
   and star-cost cards are deliberately above-rate.
2. **Hex** (Witch): +3 damage per stack on EVERY hit of an incoming attack, −1 stack per attack —
   every multi-hit card is an incidental Hex payoff.
3. **Sly** (Silent): auto-plays when discarded; Tactician/Reflex/Untouchable are deliberately
   overcosted because their real rate is the free discard-triggered play.
