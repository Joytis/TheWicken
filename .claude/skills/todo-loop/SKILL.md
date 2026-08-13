---
name: todo-loop
description: Ingest raw notes from Docs/TODO_STAGING.md into a formatted Docs/TODO.md, then work the queue one item at a time — take the top item, implement it, verify with a build, and log one line to Docs/DONE.md. Use when the user wants to process the TODO backlog, "run the todo loop", ingest staging notes, or grind through pending tasks for this mod.
---

# TODO loop

Turn rough design notes into implemented, build-verified changes for TheWitch mod. This file is the ONLY copy of the protocol — `TODO.md`/`TODO_STAGING.md` headers just point here. Three docs under `Docs/`:

- **`TODO_STAGING.md`** — raw inbox. The user drops half-formed notes here.
- **`TODO.md`** — the formatted, prioritized work queue.
- **`DONE.md`** — completed items, newest first, one line each.

The user works alongside the loop and can answer questions at any moment — ask at the point of doubt rather than batching questions or filing bureaucratic blockers.

## Step 1 — Ingest staging → TODO

- **The `BENCHED` section of `TODO_STAGING.md` is off-limits.** Never ingest it, never clear its lines — those are ideas the user is deliberately sitting on.
- For each other raw note: decide **task or idea**.
  - **Task** (design shape is clear): add `### N. <title>` to `TODO.md` with a one-line **Rule** (the decision/behavior). Add **Files** / **Acceptance** lines only when they carry real information — skip "build 0 errors" boilerplate and file lists you'd find at implement time anyway. Then **delete the note from `TODO_STAGING.md`** (cleared note = ingested; the user relies on this).
  - **Idea** (shape underspecified — "maybe X?", "rework Y", "make Z more busted"): don't invent the design. `AskUserQuestion` right then, with a recommended option; ingest the answer as a task. If the user defers, leave the note in staging (or they'll move it to BENCHED).
- A truncated/garbled note: ask, don't guess.

## Step 2 — Work the loop

1. **Pick** the top-most `TODO` item (skip `BLOCKED`).
2. **Implement** to the Rule. Touch only what the item needs. Update matching localization JSON under `TheWitch/localization/eng/`.
3. **Verify**: `dotnet build "<repo-root>/TheWitch.csproj"` must succeed — **0 errors** is the gate (build = deploy; no test suite). Run from the **repo root**, never `gamedata/`.
4. **Finish**: remove the item from `TODO.md`; append ONE line to `DONE.md` — title + date, plus a "needs in-game playtest" flag when runtime behavior is compile-check only. No status updates, no narrative — rationale goes in the commit message if it matters.
5. Loop. Stop at a real blocker, a build failure, or an empty queue. Honor any pacing the user sets.

**Batching:** cheap same-type items (content cuts, one-line stat changes) may share ONE build gate — implement the batch, build once, then write each item's DONE line. Never batch across types or past anything risky.

**Staging is live:** the user may drop new notes into `TODO_STAGING.md` while the loop runs — a note can even revise an item just finished. Re-check staging after each build gate and before declaring the queue empty.

**Mid-item questions:** when an implementation choice is genuinely the user's (which card a vague name maps to, scope of a cut, a design tradeoff), `AskUserQuestion` immediately with a recommendation. Things with an obvious default or that the code answers — pick, note it, proceed.

## Conventions & gotchas

- Work items solo and serially — no subagent fan-out; the project is small and shared JSON (`cards.json`/`powers.json`/`relics.json`) is a single conflict surface.
- **Localization plurals:** use the `Plural` tag `{Var:plural:singular|plural}` (and `{Var:diff()}` for the number + upgrade preview), never "card(s)".
- **Removing content:** delete the `.cs` (+ `.cs.uid`), the localization keys (the analyzer flags orphans → build fails), and the art (`.png` + `.png.import`); grep for dangling references first (hover tips, `[Pool]` lists, powers applied by other cards). Registration is reflection-based, so deleting the file deregisters it. **Also check what the cut content *grants*:** a cut card can orphan a payload potion/power that only it procured (Concoct → Villainous Brew) — surface the orphan and ask (`AskUserQuestion`) whether to cascade the cut; deleting user-unnamed files unprompted gets denied.
- **Renaming a card:** full rename = class + file + id-derived localization keys + art path (`Id.Entry`-derived). Rename the `.png`, delete the stale `.import`, and tell the user to run the **Godot: Import assets** task.
- **New content** can ship with placeholder art (paths fall back + log); flag the missing art and the `Images: Generate missing sizes` → `Godot: Import assets` follow-up.
- **Can't verify runtime.** Harmony patches, combat hooks, and MP behavior compile-check only — explicitly flag these for an in-game playtest in the DONE entry.
- Decompiled game source under `gamedata/src/` is the authoritative API reference — find the closest base-game example and copy its pattern.
