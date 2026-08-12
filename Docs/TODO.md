# TODO

Work queue for TheWitch mod. Source of new items: [TODO_STAGING.md](TODO_STAGING.md) (raw notes; ingest from there, then clear).

## Loop protocol (read before working)

1. **Pick** the top-most item whose status is `TODO`. Skip `BLOCKED` and `IN PROGRESS`.
2. **Claim** it: change status to `IN PROGRESS (<who> — <date>)` and save this file *before* editing code. This prevents two agents touching the same item.
3. **Implement** to the item's Acceptance criteria. Touch only the files listed (or note new ones). Update matching localization JSON under `TheWitch/localization/eng/`.
4. **Verify**: `dotnet build` must succeed (it auto-copies into the game mods folder). No test suite — build is the gate.
5. **Finish**: remove the item from this file and append it to [DONE.md](DONE.md) with a one-line note on what changed + the commit/file list.
6. Loop.

**Conflict rule:** items that edit the same file must not run in parallel. Each item lists its files; an orchestrator dispatching agents must serialize overlapping file sets.

**Status legend:** `TODO` · `IN PROGRESS (who — date)` · `BLOCKED (reason)` · (done items live in DONE.md)

---

## Queue (top = next)

### 309. Pocket Rats — remove Exhaust; 1 Rat to hand + 2 to discard
- **Status:** BLOCKED (staging note truncated at "Upgrade: Put " and AskUserQuestion answer ("the Pocket Rats Change") didn't specify the upgrade — need the upgraded numbers/piles before implementing)
- **Type:** Card change
- **Rule (base, confirmed):** Remove Exhaust. On play: put 1 Rats into your hand and 2 into your discard pile. Upgrade: **unknown**.
- **Files:** TheWitchCode/Cards/PocketRats.cs, TheWitch/localization/eng/cards.json
- **Acceptance:** TBD pending upgrade spec. Note: discard-pile insertion should still use the generated-card path (`AddGeneratedCardToCombat(..., PileType.Discard, ...)`) so create-payoffs fire.

