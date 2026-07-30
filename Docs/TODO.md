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

### 2. Herbal Brew+ — MP state divergence on power potion
- **Status:** BLOCKED (parked 2026-07-28 — full trace found no desync in the brew path: roll uses synced `Rng.CombatPotionGeneration`, upgraded table is a pure function of `IsUpgraded` (synced via deck serialization) + hard-coded canonical lists, and PowerPotion/RadiantTincture/DropletOfPrecognition are deterministic base-game potions. User confirmed park once table propagation verified — it is. Known separate risk noted: TurboWitchery sync message only sent at run start, so a mid-run joiner's Brew/Distill cards diverge on Exhaust keyword if host Turbo differs. Reopen with a desync log / exact repro.)
- **Type:** Bug
- **Files:** TheWitchCode/Cards/HerbalBrew.cs, TheWitchCode/Cards/OrientationBrewCard.cs, TheWitchCode/Config/TurboWitcherySyncPatch.cs
- **Acceptance:** TBD pending repro.

### 22. Neverending Potion — third unknown breakage
- **Status:** BLOCKED (no repro — user: "Something else also broke Neverending Potion — unknown repro". Likely resolved by DONE 193 (cross-combat persistence: shared canonical `_bottled` list) and/or DONE 194 (MP draft-potion selection breakage: shared turn-start choice context). Re-test after those; close if no longer reproducible.)
- **Type:** Bug
- **Files:** TBD
- **Acceptance:** TBD
