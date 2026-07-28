# Base-Game Class Pool Analysis

Reference study of how Slay the Spire 2's base characters are built, done to inform Witch
design. For all five base characters we categorized every card on two axes and looked at the resulting
shape. The point is not the raw counts — it's the **patterns** that separate a real mechanical
*pillar* from a splashed keyword, and the healthy ratios a pillar should hit.

## Where the data lives

- **Interactive view:** [card-designs.html](card-designs.html) — tabs for Witch and all five
  base classes (Silent / Necrobinder / Ironclad / Defect / Regent), each with filter chips and a
  live Mechanic × Role matrix.
- **Data:** `Docs/card-data/{silent,necrobinder,ironclad,defect,regent}.json` (base-game,
  read-only) and `cards.json` (Witch). Each card carries `mechanics[]` and `role[]` tags.
- **Regen:** `node Docs/card-data/gen-basegame.js [class]` rebuilds the base-game files from the
  decompiled `gamedata/` source, **preserving the curated tags** (like `regen.js` preserves
  `tested`). gamedata/ is gitignored, so the committed JSON is the snapshot.

## Method — the two axes

**Axis 1 — Mechanic (pillar):** which of the class's core themes a card touches. Determined by
reading the card's actual effect (OnPlay / powers / hooks), not its name. `None` = no pillar.

**Axis 2 — Role:** what the card *does* for that mechanic.
- **Generator** — produces the resource (gain Strength, summon a minion, apply Poison, make a potion).
- **Payoff** — scales off / consumes it (damage per minion, double the debuff, spend the resource).
- **Enabler** — synergy glue: tutor, retain, discount, draw, energy that feeds the mechanic.
- **Standalone** — generic stats with no pillar interaction (plain damage/block/rider debuff).
- **Token** — (Witch only) a generated payload card, e.g. familiar tokens.

A card can carry several of each. In the matrices below, a multi-tag card counts in every cell
it spans, so rows/columns can exceed the card total.

---

## The three big lessons

### 1. A mechanic is a *pillar* only if it has a payoff cluster

The single sharpest test. Applying something is not a theme — **exploiting** it is. The clearest
proof is the same keyword landing differently in different classes:

| Keyword | Class | Appliers | Payoffs | Verdict |
|---|---|---|---|---|
| **Vulnerable** | Ironclad | 8 | 5 (Bully, Dismantle, Cruelty, Dominate, +amp) | **Pillar** |
| **Ethereal** | Necrobinder | 2 | 4 (Pagestorm, SpiritOfAsh, PullFromBelow, Banshee's Cry) | **Pillar** |
| **Weak** | Ironclad | many | 1 (a single conditional) | Not a theme |
| **Weak** | Silent | 6 | 1 (Tracking) | Not a theme |
| **Sly** | Silent | 2 grant | 0 (rides the Discard pillar) | Not a theme |

Rule of thumb for promoting a candidate to a pillar: **~3–4 dedicated payoff cards** plus
appliers. Below that it's a rider, not an archetype.

### 2. Generator-heavy is fine *if the resource acts on its own*; spend-it resources need ~1:1

Two distinct resource shapes, and they want different gen:payoff ratios:

- **Self-acting** (ticks/scales passively): **Poison** (ticks every turn), **Strength**
  (multiplies every attack). These run heavily generator-weighted and that's *correct* — Silent
  Poison is 9:3, Ironclad Strength is 8:2, both top-tier archetypes. The resource is its own payoff.
- **Spend-it** (inert until cashed out): **Osty** (summon → attack/sacrifice), **Exhaust**
  (exhaust → trigger), **Doom** (apply → detonate). These need a real payoff cluster, ~1:1.
  Necrobinder Osty is 11 gen : 13 payoff; Ironclad Exhaust is 11 : 10. Balanced both ways.

The diagnostic question for any new mechanic: *does it do something each turn on its own, or
sit there until a payoff spends it?* That answer sizes how many payoff cards you owe it.
The cautionary counter-example is Necrobinder **Soul** (8 gen : 1 payoff) — a hoarded token
resource with almost nothing to spend it on; it feels thin precisely because it's spend-it but
was built like a self-acting one.

### 3. Base classes are ~half "neutral" filler — that flexibility is deliberate

Share of each pool that touches **no** pillar (`None`):

| Class | None-share |
|---|---|
| Silent | 63% |
| Ironclad | 46% |
| Regent | 41% |
| Defect | 36% |
| Necrobinder | 41% |
| **Witch (for contrast)** | **18%** |

Base classes devote roughly half their cards to generic block / draw / vanilla attacks /
splashed debuffs. That neutral mass is what makes a deck *draftable* — flexible glue that works
in any build. Witch is ~3× more mechanic-saturated: tight identity, but thin on the connective
tissue that smooths draws. Not necessarily wrong, but a deliberate trade to be aware of.

### Payoff-light themes can still be cheap to add — the "Ethereal model"

Necrobinder's Ethereal is the inverse of every other pillar: **payoffs (4) outnumber generators
(2).** It works because the deck's *incidental* Ethereal cards (Ethereal as a stat-balancing
downside on plain cards) become free fuel for the payoffs. Lesson: you can bolt on a pillar with
payoffs **only**, harvesting keyword cards already in the pool, without building a new generator
suite. The cheapest way to widen.

---

## Ironclad — 87 cards · Strength · Exhaust · Self-Harm · Vulnerable

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Strength | 8 | 2 | 0 | 0 | 8 |
| Exhaust | 11 | 10 | 5 | 0 | 19 |
| Self-Harm | 10 | 4 | 1 | 0 | 12 |
| Vulnerable | 8 | 5 | 1 | 0 | 12 |
| None | 0 | 3 | 15 | 22 | 40 |

- **Exhaust** is the textbook balanced engine — biggest pillar (19), near-1:1 gen:payoff, plus
  enablers. Exhaust-for-value (Cinder, True Grit, Burning Pact) ↔ exhaust-payoff (Feel No Pain,
  Dark Embrace, Fiend Fire, Ashen Strike).
- **Strength** — pure self-acting; 8 generators, almost no payoffs needed (Inflame, Demon Form,
  Limit Break-style). Multiplies every attack on its own.
- **Self-Harm** — HP as a cost (Bloodletting, Offering, Hemokinesis) with real payoffs that
  reward losing HP (Rupture gains Strength, Spite, Tear Asunder).
- **Vulnerable** — a *debuff* pillar: appliers (Bash, Thunderclap, Break) feeding a payoff cluster
  (Bully scales per Vulnerable, Dismantle double-hits, Cruelty +25%, Dominate → Strength). The
  proof that an apply-then-exploit debuff can be a full archetype.

## Silent — 88 cards · Poison · Shivs · Discard

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Poison | 9 | 3 | 1 | 0 | 12 |
| Shivs | 10 | 2 | 0 | 0 | 12 |
| Discard | 9 | 1 | 7 | 0 | 11 |
| None | 0 | 1 | 11 | 43 | 55 |

- **Poison** — self-acting damage-over-time; generator-heavy is correct (Deadly Poison, Snakebite,
  Noxious Fumes) with a few exploiters (Mirage = Block per enemy Poison, Accelerant, Bubble Bubble).
- **Shivs** — generated 0-cost knife tokens (Blade Dance, Infinite Blades, Phantom Blades); the
  Shivs themselves are the payoff, so few dedicated payoff cards (Accuracy buffs them).
- **Discard** — note the **7 enablers**: Discard is as much a card-selection engine as a payoff
  theme (Survivor, Acrobatics, Tools of the Trade discard *for value*); only Memento Mori truly
  scales off it.
- **63% None** — the most neutral-heavy pool; lots of flexible block/draw/0-cost glue.
- **Rejected candidates** (checked, not pillars): **Sly** (rides Discard — a Sly card auto-plays
  when discarded; no dedicated payoffs), **Weak** (1 payoff: Tracking), **card-draw** (3 scattered
  payoffs — Murder/Speedster/Corrosive Wave — but no shared engine; a *minor* sub-theme).

## Necrobinder — 88 cards · Osty (summons) · Soul · Doom · Ethereal

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Osty | 11 | 13 | 6 | 0 | 24 |
| Soul | 8 | 1 | 1 | 0 | 9 |
| Doom | 8 | 5 | 3 | 0 | 13 |
| Ethereal | 2 | 4 | 1 | 0 | 7 |
| None | 0 | 1 | 6 | 29 | 36 |

- **Osty** — the summoner identity and the **benchmark for a minion archetype**: 11 gen : 13
  payoff. Summon (Bodyguard, Reanimate, Legion of Bone) ↔ command/sacrifice (Unleash, Sacrifice,
  Bone Shards, Rattle scales per Osty-attack). This is what a *spend-it* pillar should look like.
- **Doom** — apply-and-detonate debuff (Blight Strike, Negative Pulse apply; Countdown, Death's
  Door, Time's Up pay off). Balanced ~8:5.
- **Soul** — a generated token-card resource (Grave Warden, Reave, Capture Spirit make Souls;
  Soul Storm cashes them). 8:1 — thin, the spend-it-built-like-self-acting cautionary case.
- **Ethereal** — the payoff-heavy "harvest the incidentals" pillar (see lesson above).

## Defect — 88 cards · Orbs · Focus · Status

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Orbs | 24 | 17 | 5 | 0 | 41 |
| Focus | 4 | 2 | 0 | 0 | 6 |
| Status | 5 | 6 | 0 | 0 | 11 |
| None | 0 | 0 | 0 | 32 | 32 |

- **Orbs is one giant engine** (41 cards, 24 gen : 17 payoff). Channel an orb (Zap, Cold Snap,
  Chaos, Tempest) ↔ evoke / trigger all orbs (Dualcast, Multicast, Tesla Coil, Shatter, Barrage
  scales per orb). Textbook spend-it balance.
- **Focus** is not really a second pillar — it's the orb *amplifier* (6 cards: Defragment, Biased
  Cognition, Hotfix raise Focus; Hyperbeam spends it). It only matters *because* of orbs.
- **Status** (found on a re-pass; initially misread as generic filler) is a real second pillar,
  5 gen : 6 payoff. The generators are **above-rate cards whose downside is the Status** — Boost
  Away (6 Block for 0 + Dazed), TURBO (2 Energy + Void), Overclock (draw 2 for 0 + Burn), Gunk Up,
  Fight Through — and the payoffs convert that cost into a resource: Smokestack / Rocket Punch /
  Trash to Treasure trigger **on creating** a Status (Trash to Treasure bridges straight into
  Orbs), while Iteration (on draw), Compact (transform to Fuel), and Flak Cannon (exhaust-all,
  damage-per) exploit Statuses **in hand** — including ones enemies inflicted, so the payoffs
  double as anti-Status tech. Structurally this is the Ethereal model *with* its own generator
  suite: self-inflicted downsides recast as fuel.
- **"Powers"** felt like a theme (Defect has the most Power-type cards) but has **zero payoff
  cards** — nobody rewards *playing* a power. Card-type density ≠ a mechanic. Good reminder that
  the pillar test is about payoffs, not flavor or card-type counts.

## Regent — 88 cards · Stars · Forge · Colorless (· Vigor, vestigial)

| mech \ role | Gen | Payoff | Enabler | Standalone | Σ |
|---|---|---|---|---|---|
| Stars | 13 | 19 | 0 | 0 | 30 |
| Forge | 11 | 1 | 6 | 0 | 14 |
| Colorless | 5 | 5 | 0 | 0 | 9 |
| Vigor | 2 | 0 | 0 | 0 | 2 |
| None | 0 | 0 | 0 | 36 | 36 |

- **Stars** — a *spend-it* resource done right, and an instructive variant: payoffs (19) outnumber
  generators (12) because the payoffs are **star-cost cards** (Devastate, Seven Stars, Meteor
  Shower, etc. — pay Stars on play for a big effect). Gaining Stars (Venerate, Gather Light, Glow)
  fuels a deck full of expensive star-sinks. The cost-to-cast *is* the payoff distribution.
- **Forge** — a *self-acting* resource (permanently enhances your cards), so it's generator-heavy
  (11 gen : 1 payoff) and that's **correct**, same logic as Strength/Poison. You stack Forge and
  it keeps paying you passively; you don't need cards to "spend" it. The pillar includes the
  **Sovereign Blade** suite — SB is the signature card Forge enhances, and five cards consider it
  directly: Conqueror (SB double damage), Parry (SB gains Block), Summon Forth (tutor SB),
  Seeking Edge (SB hits ALL), Sword Sage (SB gains Replay). These are Forge *enablers* — they
  widen what the forged blade does rather than generating or spending Forge.
- **Colorless** (found on a re-pass, like Defect's Status) is a real third pillar, 5 gen : 4
  dedicated payoffs. Generators pull from the ColorlessCardPool: Manifest Authority, Quasar
  (star-cost, doubles into Stars), Spectrum Shift (every turn), Bundle of Joy (3 at once),
  Largesse (MP, gifts an ally one). Payoffs are **card-creation scalers** — Supermassive (damage
  per card created this combat), Pillar of Creation (Block per creation), Arsenal (Strength per
  creation) all hook `AfterCardGeneratedForCombat` — plus Heirloom Hammer (copy a Colorless in
  hand, the only strictly-Colorless payoff). Note the payoffs count *any* creation, so the
  minion-transform cards (BEGONE!, CHARGE!!, GUARDS!!) and Debris producers (Collision Course,
  Crash Landing) feed them too — left untagged as incidental fuel, same call as Defect's
  enemy-inflicted Statuses. This is the same hook family Witch's Cloak of Moonlight listens to.
- **Vigor** (2 cards) is vestigial — kept tagged to show it's *not* a pillar.
- **No debuff pillar despite heavy Weak/Vulnerable.** Regent applies them widely (15 refs each)
  but **nothing scales off them** — the exact Weak-is-a-rider finding from Silent and Ironclad,
  now a third time. Cosmic/royal flavor (Kingly*, Hegemony, Monarch's Gaze, Royalties' gold)
  is *flavor*, not a mechanic — no shared payoff.

### Note: classes have 1–4 pillars, not a fixed 3

The five base classes don't all hit the same count. Necrobinder (4: Osty/Soul/Doom/Ethereal) and
Ironclad (4: +Vulnerable) are pillar-rich; Silent (3) is classic; **Defect is 2** (Orbs done very
deep, with Focus as its amplifier, plus the smaller Status pillar); **Regent is 3** (Stars +
Forge + Colorless). More pillars isn't better — Defect's deep Orb engine is one of the most
beloved archetypes. The takeaway for Witch: a
character can be excellent with **few pillars done deep** (each with a real payoff cluster) rather
than many pillars spread thin.

---

## How this maps onto Witch (the reason for the study)

Witch pillars: **Brambles · Potions · Familiars · Hex** (103 cards incl. 14 tokens, 18% None). *(Pillar renamed from Debuff/Buff 2026-07-27: hex-specific cards only; generic Weak/Vulnerable cards moved to None.)*

| mech \ role | Gen | Payoff | Enabler | Token | Σ |
|---|---|---|---|---|---|
| Familiars | 12 | 5 | 5 | 13 | 32 |
| Potions | 14 | 6 | 7 | 0 | 24 |
| Brambles | 12 | 5 | 2 | 1 | 17 |
| Hex | 10 | 3 | 1 | 3 | 17 |
| None | 0 | 0 | 3 | 0 | 19 |

- **Familiars** (12 gen : 5 payoff : 5 enabler, +13 token cards) is a summon archetype — its
  benchmark is Necrobinder's Osty at ~1:1. The gap is payoff cards (scale-per-familiar,
  sacrifice-for-value), not more summons.
- **Brambles** (12 gen : 5 payoff) is the open question: it depends whether Brambles is
  *self-acting* (then generator-heavy is fine, à la Poison/Strength) or *spend-it* (then it owes
  payoffs, à la Osty/Exhaust). Decide which, then size accordingly.
- **Potions** (14 : 6 : 7) remains the healthiest pillar — generators, payoffs, and enablers all
  present.
- **Hex** (hex enemies / hex yourself → multi-attacks milk the stacks) is the 4th
  pillar, added 2026-07. 10 gen : 3 payoff looks lopsided, but the resource is *half self-acting*:
  stolen Strength and Vigor amplify every attack on their own (Strength/Poison logic), so only the
  debuff-exploit half (Hexblast, Soul Knot, Bag of Teeth) needs dedicated payoffs. Another 1–2
  Uncommon/Rare payoffs (a Vigor-scaling or per-debuff attack) would firm it up.
- Witch's **18% neutral** vs. the base ~36–63% means far less draftable glue — a deliberate
  saturation trade.

The "payoff cluster" test (lesson 1) is the gate for any proposed 5th Witch mechanic: it's only
a pillar if 3–4 cards *exploit* it, not just apply it.

---

## Addendum — 2026-07 sub-archetype audit (revisions to the above)

A full per-card re-audit of all six pools (with keyword ground-truthing against decompiled
`CanonicalKeywords`/`CanonicalStarCost`) added two curated tag layers to the card JSONs —
`sub[]` (pillar sub-archetypes) and `threads[]` (cross-class functional threads). Definitions:
[card-data/taxonomy.md](card-data/taxonomy.md). Several claims above need revision:

1. **Silent "Sly: 2 grant, 0 payoffs — not a theme" is wrong.** The JSON text field drops
   keywords: 8 cards *carry* Sly (auto-play when discarded), and Tactician (3-cost: gain 1
   Energy), Reflex (3-cost: draw 2), Untouchable (2-cost: 6 Block) are deliberately overcosted —
   their real rate is the free discard-triggered play. The Discard pillar's payoff column is
   functionally ~6–8, not the 1 (Memento Mori) counted above. Sly IS the Discard payoff suite.
2. **Necrobinder Soul 8:1 overstates the gap** — Haunt and Devour Life are per-Soul-played
   payoffs (retagged); the real ratio is 7 make : 3 payoff. Also: Calcify is an Osty amplifier
   (was tagged None), and the Ethereal pillar is effectively **15 cards** once the 8 hidden
   Ethereal-keyword carriers (Defile, Defy, Fear, Enfeebling Touch, Lethality, Parse, Demesne,
   Seance) are counted as fuel.
3. **Defect "nobody rewards playing a Power" is wrong** — Storm and Subroutine literally trigger
   per Power played; a 6-card `PowersMatter` cluster exists (was scattered across Orbs/None).
   Defect also hides a 5-card `ZeroCostMatters` (Claw-deck) engine under `None`.
4. **Ironclad hides two near-pillars under `None`:** a 12-card `AttacksMatter` aggro-tempo
   thread with its own payoff cluster (Rage, Expect a Fight, Stomp, Juggling), and a 4-payoff
   block-synergy suite (Body Slam, Juggernaut, Unmovable, Barricade). Its Exhaust pillar is ~28
   cards once the 9 hidden self-Exhaust keyword carriers are counted.
5. **Regent's Stars footprint is ~40% of the pool** (5 star-cost cards were invisible in text,
   incl. starter Falling Star), and its real texture is threads the pillar tags miss:
   `TopdeckControl` (5), `SelfRecursion` (5), `PlayVolume` (5), `DelayedEffect` (7). Regent pays
   costs in Stars and tempo — never HP or cards.
6. **Brambles open question above is resolved: it is BOTH.** Self-acting (thorns retaliation) and
   spend-it (Tinder / Bonfire / Brambleburst = exactly 3 spenders, the floor). The Engine sub
   (Deep Roots, Wicker Form) is what makes Spend sustainable.
7. **The recurring cross-class pattern is the "incidental fuel" layer** (Ethereal carriers,
   self-Exhaust carriers, Sly carriers, self-Status generators, star-cost riders): above-rate
   cards whose downside quietly feeds a payoff cluster. **Witch has the fuel without the
   harvest**: 25/100 cards self-exhaust + 3 exhaust-outlets with ZERO exhaust payoffs, and 6
   self-debuff cards with exactly 1 payoff (Salt and Ash). Both are one or two payoff cards away
   from being real sub-archetypes — the cheapest new-archetype opportunities in the pool.
