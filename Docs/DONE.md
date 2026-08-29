# DONE

Completed items moved out of [TODO.md](TODO.md). Newest at top. Each entry: what changed, files touched, verification.

---
> **Merge note (2026-07-11):** entries 173–175 below were done 2026-07-08 on another machine and merged in after the 123–172 rework batch (renumbered from their original 122/132/133 to avoid collisions). Two other entries from that machine were dropped as superseded by the rework: *Rename Plunder → The Hunt* (remote renamed it Pick Clean instead, entry 123) and the *Oxidizers choice-prompt replay fix* (Oxidizers was cut entirely, entry 125 — the `OxidizersReplayPatch.cs` it introduced was removed in the merge).

### 345. Headless play-all-cards debug flag (2026-08-28) — `--witch-cardtest` runs `Debug/WitchCardTest` (Downfall TestCode shape: TestMode + FirstCardSelector, fresh test combat per Witch card; play/draw/exhaust/discard + Strike/Defend) and logs failures via AutoSlayLog. Needs in-game playtest (`launch-witch.ps1` with `--witch-debug --witch-cardtest`).
### 344. Unstable potion FTUE (2026-08-28) — first Unstable potion created by the local player shows a one-time right-click tip (`Potions/NUnstablePotionFtue`, base `obtain_potion_ftue` scene, `localization/eng/ftues.json`), gated by the progress-save FTUE flags. Needs in-game playtest.
### 343. Tasty Herbs rework (2026-08-28) — using an Unstable potion has a 25% chance to use it an additional time (one bonus per potion, `OnUseWrapper` replay); old end-of-combat heal removed. Needs in-game playtest.

### 342. Witch Yummy Cookie variant wired (2026-08-24) — art moved charui/yummy_cookie_witch.png → relics/yummy_cookie.png (downsized 512→256); `CustomYummyCookie` override on Witch returns the standard relic path triple (BigRelicImagePath / RelicAtlasPath / RelicOutlineAtlasPath); needs `dotnet publish` to pack + in-game check (`relic YUMMY_COOKIE` on a Witch run).

### 341. MP card fixes (2026-08-24) — Blood Anointment target `AnyPlayer`→`AnyAlly` ("Choose another player" — can't target self); Plague Tide now iterates `CombatState.Players` directly so the caster unambiguously gets a Rat Familiar too; needs in-game MP playtest.

### 340. Smolder rework (2026-08-24) — now "Gain 4 Block. Create an Unstable Ember Jar." (immediate, via Witch.ProducePotion); SmolderPower deleted (orphaned — .cs, loc keys, no art existed); upgrade still +3 Block; needs in-game playtest.

### 339. Cat+ hover shows Nimble+ (2026-08-24) — removed the stale MaxUpgradeLevel-0 workaround: CatFamiliar's Nimble hover tip now takes IsUpgraded like Ferocity's; needs in-game hover check.

### 338. Brambleburst multiplier fix (2026-08-24) — dropped the stray `* 2` in the CalculatedDamageVar multiplier: now +2 damage per Bramble as the card text says (was silently dealing +4); needs in-game playtest.

### 337. Crystal Bottle card hover tip (2026-08-24) — card now shows the CrystalBottlePower tooltip (carries the "Healing Potions cannot be bottled" note) via ExtraHoverTips.

### 336. Dark Omen hits ALL enemies (2026-08-24) — Crow token now `TargetType.AllEnemies`, applies Hex to `CombatState.HittableEnemies` (Hexblast pattern); loc "to ALL enemies"; needs in-game playtest.

### 335. New MP Card: Bottle Bombardment (2026-08-24) — 2e Rare Attack (MP-only): 6 damage (+4 upgraded) per potion created by ANY player this combat; `PotionProcureHistory.CountAll()` + Barrage live hit count; NO ART; needs in-game MP playtest.

### 334. New MP Card: Blood Anointment (2026-08-24) — 1e Uncommon Skill (MP-only): choose a player, their Attacks apply 1 Hex this turn (`BloodAnointmentPower`: Envenom rider + Cauldron Dance end-of-turn removal); upgrade −1e; NO ART (card + power icon); needs in-game MP playtest. (User note spelled "Annointment" — implemented as "Anointment".)

### 333. New MP Card: Plague Tide (2026-08-24) — 2e Rare Power (MP-only): ALL players summon a Rat Familiar (upgraded: Rat Familiar+ via per-player `UpgradedStacks`); `IFamiliarSummon` so tutors find it; NO ART; needs in-game MP playtest.

### 332½. Familiar cycling hardening (2026-08-24) — cycle positions now TRIMMED on stack loss (`AfterPowerAmountChanged` tail-trim mirroring the UpgradedStacks clamp) so sacrifice→resummon starts at the first card; loot-table entries restored to a named `Entry` record (`.Create(...)` call sites).

### 332. Analytics full pull + 8-21/22 gap fix (2026-08-24) — no 7-day filter existed: Supabase caps one response at 1000 rows and the unordered fetch truncated the NEWEST runs (row #999 ended 8-20). `common.fetch_runs` now pages (order=created_at.asc, offset loop); verified export covers all 1322 runs through 8-24; analytics-data regenerated.

### 331. Cozy Nest rarity (2026-08-24) — code was already Rare; fixed stale art-tracker entry (said Shop).

### 330. Separatory Funnel → Touch of Orobas Ancient (2026-08-24) — Orobas now upgrades Large Pockets to Separatory Funnel (rarity Rare→Starter, out of random pool); Bottomless Pockets relic deleted (.cs/.uid/loc/art/tracker entry); needs in-game playtest (Orobas event preview + swap).

### 329. Knowledge: no Exhaust targets, discount this turn (2026-08-24) — REVERTED same day at user request (copying Exhaust cards is fun; combat-long discount stays).

### 328. Witchcraft overflow auto-play (2026-08-24) — REVERTED same day at user request (auto-playing overflow felt overwhelming): overflow brews are simply lost again. `UseWithoutBelt` removed; the `PotionAutoPlay` helper (ResolveTarget/PlayThrowVfx/OnUse reflection) stays — NeverendingPotionPower's replay dedup kept.

### 327. Crystal Bottle excludes healing potions (2026-08-24) — `CrystalBottlePower.AfterPotionUsed` skips `PotionTraits.IsHealing` (potion works normally, bottle stays armed); power tooltip notes "Healing Potions cannot be bottled"; needs in-game playtest.

### 326. Familiars cycle instead of random (2026-08-24) — LootTableFamiliarPower now cycles deterministically through its card list per stack (turn 1 → first card, turn 2 → second, wrap; per-stack `_cyclePositionByStack`, DeepCloneFields-safe); weights + `Roll` removed from FamiliarLootTable; `CreateTurnStartCard` gained a stackIndex param; power loc "a random X card" → "the next X card"; needs in-game playtest (cycle order, Sack of Treats, Command's GenerateOneCard).

### 325. Nimble upgrade added (2026-08-24) — Nimble+ = Gain 1 Energy + Gain 2 Block (was MaxUpgradeLevel 0, so Cat+ granted plain Nimble); `GainsBlock => IsUpgraded`, loc `{GainsBlock:cond:...}` line; needs in-game playtest.

### 324. Stony Brew strictly defensive (2026-08-24) — Fertilizer removed from Stony Brew loot table; PotionTraits orientation flipped Defensive→Offensive (affects Grind Down pools: Fertilizer now rolls from Attack exhausts).

### 323. Call the Pack upgrade text fix (2026-08-24) — loc token was `{IfUpgraded:Gnash+|Gnash}` missing `show:` option; now matches base-game convention.

### 322. Bottle Barrage MP divergence fix (2026-08-24) — PotionProcureHistory now gates recording on `CombatManager.Instance.IsInProgress`: players keep CombatState through the reward screen, so reward potion picks were recorded AFTER the combat-end History.Clear and leaked into next combat's count, with per-client timing skew via RewardSynchronizer's fire-and-forget replay → divergence; needs in-game MP playtest.

### 321. Tinder leaves an Ash (2026-08-17) — 1e Common Skill: Exhaust a card (Ash filtered out of the pick via `FromHand` filter), Next turn +2e, add an Ash to Discard; hover tip for Ash; needs in-game playtest.

### 320. Bonfire rework (2026-08-17) — 0e Rare Skill: Exhaust 2 cards (Ash not selectable), gain 4e (+1 upgraded), add 2 Ash to Discard (`Ashes` var, FightThrough pattern); needs in-game playtest.

### 319. New Card: Ash (2026-08-17) — Unplayable + Ethereal Status ("blows away" at turn end), no upgrade, `CanBeGeneratedInCombat=false` (Soot/Wormy pattern), lives in WitchCardPool; loc `THEWITCH-ASH.*`; NO ART (placeholder fallback).

### 318. New Card: Pack Tactics (Wolf token) (2026-08-17) — 0e Skill familiar token "Gain 2 Strength this turn" (+2 upgraded) via `PackTacticsPower : TemporaryStrengthPower` (Coordinate pattern, no own loc/icon like RottingRootsStrengthDown); `WolfFamiliarPower` → `LootTableFamiliarPower` (Gnash + Pack Tactics, equal weight); WolfFamiliar hover tip added; NO ART; needs in-game playtest (loot roll, temp-strength icon).

### 317. Bug: Gnash+ scaling (2026-08-17) — each played Gnash now raises ALL Gnash by ITS OWN ExtraDamage (3 / 4 for +): new `CombatHistoryQueries.GnashBonusThisCombat` sums played Gnashes' ExtraDamage; Gnash uses a `CalculatedDamageVar` subclass whose extra var is a hidden unit `CalculationExtraVar(1)` so damage = base + sum with an integer multiplier (fractional multiplier avoided — decimal 4/3×3 rounds wrong); needs in-game playtest (card-face preview).

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
