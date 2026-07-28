---
name: card-db-update
description: Regenerate Docs/card-data/cards.json from the card .cs sources AND audit the curated mechanics/role category tags for new or mechanically-changed cards. Use when the user says "update the card database", "regen cards.json", "retag the cards", or after adding/editing card classes.
---

# Card DB Update

Two-phase update of `Docs/card-data/cards.json`: (1) mechanical skim via the existing script, (2) curated-tag audit that the script cannot do (it only *preserves* tags — new cards get empty `mechanics[]`/`role[]`, and changed cards keep possibly-stale tags).

## Phase 1 — regen (script)

```bash
node Docs/card-data/regen.js
git diff --stat Docs/card-data/cards.json
```

`regen.js` parses the card classes + localization, preserves `tested`/`artFinal`/curated `note`/`mechanics`/`role`, and auto-clears `tested` for cards whose mechanics changed. Note the script's stdout: it lists ADDED cards and TESTED-cleared cards — those are exactly the tag-audit worklist for Phase 2.

## Phase 2 — tag audit (you)

Worklist = every card that is **(a)** newly added (empty `mechanics`/`role`), or **(b)** had `tested` auto-cleared this run (its effect changed, so its tags may be stale). For each: read the card's `.cs` file (path is in its `file` field) and judge tags from the **actual effect** (OnPlay / powers / hooks), not the name.

### Taxonomy (from Docs/class-pool-analysis.md — read it if unsure)

`mechanics[]` — which Wicken pillar(s) the card touches:
- `Potions` — creates, uses, upgrades, copies, or scales off potions/slots.
- `Familiars` — summons familiars, is a familiar token, or scales off/tutors/sacrifices familiars.
- `Brambles` — gains, doubles, spends, or scales off Brambles.
- `Hex` — Hex-specific: applies Hex (to enemies or self) or pays off Hex/debuffs as the card's point (a minor rider on a pillar card does not earn the tag). Generic Weak/Vulnerable/Vigor cards with no Hex interaction are `None`.
- `None` — no pillar interaction.

`role[]` — what the card does for its mechanic(s):
- `Generator` — produces the resource (make a potion, summon a familiar, gain Brambles, apply the debuff).
- `Payoff` — scales off / consumes it (damage per potion, detonate Brambles, sacrifice a familiar).
- `Enabler` — synergy glue: tutor, discount, copy, upgrade, slots, draw that feeds the mechanic.
- `Token` — a generated payload card (familiar tokens, Rake, Wormy).
- `Standalone` — generic stats, no pillar interaction (pairs with mechanics `None`).

Multi-tagging is normal (e.g. Extract Essence = Potions + Generator; Unstable Reaction = Potions + Payoff). Keep judgments consistent with existing entries — grep `cards.json` for a similar card before inventing a new combination.

Edit the `mechanics`/`role` arrays in `cards.json` directly (they are curated fields; regen never overwrites them).

## Phase 3 — verify

```bash
node Docs/card-data/regen.js --check   # exit 0 = no drift
node -e "const d=require('./Docs/card-data/cards.json');const bad=d.cards.filter(c=>!c.mechanics.length||!c.role.length);if(bad.length){console.error('UNTAGGED: '+bad.map(c=>c.name).join(', '));process.exit(1)}console.log('all cards tagged')"
```

Both must pass. Then summarize to the user: cards added/removed, tags assigned or changed (with one-line reasoning for anything non-obvious), and any `tested` flags that were cleared. Remind them the Mechanic × Role matrix in `Docs/card-designs.html` reflects the new tags, and that `/art-tracker-sync` pushes the changes to the artist sheet.
