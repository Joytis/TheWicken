# DONE

Completed items moved out of [TODO.md](TODO.md). Newest at top. Each entry: what changed, files touched, verification.

---

> **Merge note (2026-07-11):** entries 173–175 below were done 2026-07-08 on another machine and merged in after the 123–172 rework batch (renumbered from their original 122/132/133 to avoid collisions). Two other entries from that machine were dropped as superseded by the rework: *Rename Plunder → The Hunt* (remote renamed it Pick Clean instead, entry 123) and the *Oxidizers choice-prompt replay fix* (Oxidizers was cut entirely, entry 125 — the `OxidizersReplayPatch.cs` it introduced was removed in the merge).

### 316. Art By presentation removed, tagging kept (2026-08-12) — user disliked the tooltip look; deleted ArtistHoverTip/CardArtistHoverTipPatch/ArtistTipRenderPatch + AlwaysShowArtCredits setting + loc keys (prototypes in git history). `Artist` record + `WitchCard.ArtBy` (Kitsu default, 5 per-card overrides) remain data-only.

### 315. 'Art By' artist-credit system (2026-08-12) — Downfall pattern adapted: `Artists/Artist.cs` (singleton marker classes, hardcoded names, "Art By" loc key in static_hover_tips.json) + `CardArtistHoverTipPatch` (postfix on non-virtual `CardModel.HoverTips` getter) + `WitchCard.ArtBy` virtual; seeded 7 cards from card-briefs.json (Defend/Strike/Bag of Teeth/Wicked Brew/Torment/Capture Soul/Bonfire). Skipped Downfall's custom NHoverTipSet render patch — plain `HoverTip(title, description)` rides the normal tip pipeline. Needs in-game playtest (hover a credited card).

### 314. Rename Extract Life → Transfer Life (2026-08-12) — class/file/uid/loc keys/art pngs (main + beta) renamed, mechanics unchanged; card_portraits is .gdignore'd so no .import cleanup needed.

### 313. Call the Pack+ now shuffles Gnash+ tokens (2026-08-12) — passes IsUpgraded to CreateFamiliarCards + hover tip/loc show Gnash+; Block+2 kept; needs in-game playtest.

### 312. Nimble loses Block, no upgrade (2026-08-12) — token is now "Gain 1 Energy. Exhaust.", MaxUpgradeLevel 0 (user choice: Cat+ tokens identical to base); needs in-game playtest.

### 310. Gather Herbs copy now inherits Unstable (2026-08-12) — Mark propagates to a registered original→copy pair; needs in-game playtest.

### 311. Blood Wall hitch — closed as base-game bug (2026-08-12): PreloadManager only preloads party pools' card vfx; off-class cards (vanilla-reachable via Prismatic Gem) sync-load their vfx each room. Not ours; no code change.

### 307. Bug — Cloak of Moonlight "double-triggers on cards created by cards" — NOT A BUG (closed)
- **Done:** 2026-08-08
- **Changed:** No code change. Second full trace confirms the hook fires exactly once per generated card. `AfterCardGeneratedForCombat` has two call sites game-wide (`CardPileCmd.AddGeneratedCardsToCombat`, one dispatch per card in the batch, and `CardCmd.Transform`, one per transformed card). Listener enumeration cannot yield the power twice: `CombatState.IterateHookListeners` takes powers from `creature.Powers` per creature (familiar pets are `Player == null` monsters, so they add only their own MonsterModel), and `RunState.IterateHookListeners` skips relics/potions when a combat state is passed. BaseLib was decompiled and has **zero** `ModHelper.AddCombatHookSubscriber` registrations and no patch on the generation path. `CloakOfMoonlightPower` matches base-game `Regalite`/`ArsenalPower` one-for-one.
- **Explanation:** the power is per-CARD, not per-PLAY, and the loc text says so ("Whenever you create a card or Potion"). Any card that creates N cards gives N Block procs — Pocket Rats (3 Rats), Swarm, Refuse Pile, Call the Pack, Mulch, Polymorph (transform + extra = 2 per choice). That is the reported "double". Base-game Regalite behaves identically — in-game A/B if confirmation is wanted.
- **If per-play is the desired design instead** (user's call, not made here): gate on `cardPlay`/a per-play flag in `CloakOfMoonlightPower` and reword the loc to "the first time you create a card each play".
- **Files:** Docs/TODO.md (item removed), Docs/DONE.md
- **Verified:** trace only — no source change, nothing to build.

### 312. Potion selection screen gains the base-game peek button (2026-08-12) — choose_potion_screen.tscn instances `scenes/combat/peek_button.tscn` (runtime-load fallback if the script doesn't bind, same diagnostic as the banner) and NChoosePotionScreen mirrors NCardGridSelectionScreen wiring (hide grid+banner, MouseFilter Ignore while peeking, controller focus → NCombatRoom, un-peek on close, enable only mid-combat). Verified: dotnet build OK; needs in-game check.

### 313. Cloak of Moonlight redesign → Hex payoff (2026-08-13) — 1 energy (was 2), "Whenever you trigger Hex, gain 2 Block" (was create-card/Potion); upgrade +1 Block unchanged. HexPower.AfterAttack notifies the attacker's CloakOfMoonlightPower at its trigger point (before the IHexPreserving early-out, so Torment still procs; once per attack per hexed enemy). Player-self-Hex triggered by enemies does NOT proc (attacker-side lookup). Loc + cards.json retag (Hex/Payoff/Hex:Exploit) + regen done. Verified: dotnet build OK; TESTED cleared, needs in-game check.

### 314. Hidden in Smoke brews Unstable (2026-08-13) — HiddenInSmokePower turn-start PuffOfSmoke now `Witch.PotionMode.Unstable` (one-line); card + power loc gain "[gold]Unstable[/gold]" per the Smolder/Hasty Brew wording. Verified: dotnet build OK; TESTED cleared, needs in-game check.

<!-- Append completed items above this line. Template:

### <title>
- **Done:** <date>
- **Changed:** <one line>
- **Files:** <list>
- **Verified:** dotnet build OK
-->
