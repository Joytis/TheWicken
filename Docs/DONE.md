# DONE

Completed items moved out of [TODO.md](TODO.md). Newest at top. Each entry: what changed, files touched, verification.

---

> **Merge note (2026-07-11):** entries 173–175 below were done 2026-07-08 on another machine and merged in after the 123–172 rework batch (renumbered from their original 122/132/133 to avoid collisions). Two other entries from that machine were dropped as superseded by the rework: *Rename Plunder → The Hunt* (remote renamed it Pick Clean instead, entry 123) and the *Oxidizers choice-prompt replay fix* (Oxidizers was cut entirely, entry 125 — the `OxidizersReplayPatch.cs` it introduced was removed in the merge).

### 307. Bug — Cloak of Moonlight "double-triggers on cards created by cards" — NOT A BUG (closed)
- **Done:** 2026-08-08
- **Changed:** No code change. Second full trace confirms the hook fires exactly once per generated card. `AfterCardGeneratedForCombat` has two call sites game-wide (`CardPileCmd.AddGeneratedCardsToCombat`, one dispatch per card in the batch, and `CardCmd.Transform`, one per transformed card). Listener enumeration cannot yield the power twice: `CombatState.IterateHookListeners` takes powers from `creature.Powers` per creature (familiar pets are `Player == null` monsters, so they add only their own MonsterModel), and `RunState.IterateHookListeners` skips relics/potions when a combat state is passed. BaseLib was decompiled and has **zero** `ModHelper.AddCombatHookSubscriber` registrations and no patch on the generation path. `CloakOfMoonlightPower` matches base-game `Regalite`/`ArsenalPower` one-for-one.
- **Explanation:** the power is per-CARD, not per-PLAY, and the loc text says so ("Whenever you create a card or Potion"). Any card that creates N cards gives N Block procs — Pocket Rats (3 Rats), Swarm, Refuse Pile, Call the Pack, Mulch, Polymorph (transform + extra = 2 per choice). That is the reported "double". Base-game Regalite behaves identically — in-game A/B if confirmation is wanted.
- **If per-play is the desired design instead** (user's call, not made here): gate on `cardPlay`/a per-play flag in `CloakOfMoonlightPower` and reword the loc to "the first time you create a card each play".
- **Files:** Docs/TODO.md (item removed), Docs/DONE.md
- **Verified:** trace only — no source change, nothing to build.

<!-- Append completed items above this line. Template:

### <title>
- **Done:** <date>
- **Changed:** <one line>
- **Files:** <list>
- **Verified:** dotnet build OK
-->
