using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Debug;

/// <summary>
/// Headless smoke tests, one fresh throwaway test combat (TestMode on, no scene) per item, any
/// exception logged as a failure:
///   --witch-cardtest    plays every card the mod defines (all pools): play from hand, draw it,
///                       exhaust it, discard it, then play Strike+Defend alongside it — the shape
///                       of Downfall's TestCode harness.
///   --witch-potiontest  procures + uses every mod potion (target auto-resolved by TargetType),
///                       then procures + discards it.
///   --witch-relictest   obtains EVERY mod relic on the player before the combat is built, then
///                       runs the card exercise for every card and the potion exercise for every
///                       potion with the whole relic set equipped.
///   --witch-testall     all three in sequence (cards, potions, then relics) in one process; one
///                       combined pass/fail summary + exit code.
/// Selection prompts auto-pick the first eligible cards.
/// </summary>
public static class WitchCardTest
{
    public enum Mode { Cards, Potions, Relics, All }

    public static string TagFor(Mode mode) => mode switch
    {
        Mode.Potions => "[witch-potiontest]",
        Mode.Relics => "[witch-relictest]",
        Mode.All => "[witch-testall]",
        _ => "[witch-cardtest]",
    };

    public static async Task RunAll(string seed, Mode mode = Mode.Cards)
    {
        string tag = TagFor(mode);
        bool wasTestMode = TestMode.IsOn;
        TestMode.IsOn = true;
        IDisposable selectorScope = CardSelectCmd.UseSelector(new FirstCardSelector());
        List<(string item, Exception ex)> failures = [];
        int total = 0;
        try
        {
            if (!Godot.FileAccess.FileExists($"res://{MainFile.ModId}/localization/eng/cards.json"))
            {
                AutoSlayLog.Warn($"{tag} mod .pck not loaded (no res://{MainFile.ModId}/localization) — localization-dependent content will fail; run with Build=Publish");
            }
            CharacterModel witch = ModelDb.Character<Witch>();
            System.Reflection.Assembly mod = typeof(WitchCardTest).Assembly;

            // Every card this assembly defines, whatever pool it sits in: the main Witch pool,
            // the shared familiar-token pool, and the StatusCardPool strays (Ash, Wormy).
            List<CardModel> cards = ModelDb.AllCards
                .Where(c => c.GetType().Assembly == mod)
                .OrderBy(c => c.GetType().Name)
                .ToList();
            List<PotionModel> potions = ModelDb.AllPotions
                .Where(p => p.GetType().Assembly == mod)
                .OrderBy(p => p.GetType().Name)
                .ToList();
            List<RelicModel> allRelics = ModelDb.AllRelics
                .Where(r => r.GetType().Assembly == mod)
                .OrderBy(r => r.GetType().Name)
                .ToList();

            Mode[] phases = mode == Mode.All ? [Mode.Cards, Mode.Potions, Mode.Relics] : [mode];
            foreach (Mode phase in phases)
            {
                List<RelicModel> relics = phase == Mode.Relics ? allRelics : [];

                async Task Run(string name, Func<CombatState, Player, Task> exercise)
                {
                    total++;
                    AutoSlayLog.Info($"{tag} {name}");
                    try
                    {
                        (CombatState combat, Player player) = await NewCombat(witch, seed, relics);
                        await exercise(combat, player);
                    }
                    catch (Exception e)
                    {
                        Exception actual = e.InnerException ?? e;
                        failures.Add(($"{phase}/{name}", actual));
                        AutoSlayLog.Error($"{tag} FAILED {name}: {actual}");
                    }
                    finally
                    {
                        EndCombat();
                    }
                }

                switch (phase)
                {
                    case Mode.Cards:
                        AutoSlayLog.Action($"{tag} {cards.Count} cards, seed '{seed}'");
                        break;
                    case Mode.Potions:
                        AutoSlayLog.Action($"{tag} {potions.Count} potions, seed '{seed}'");
                        break;
                    case Mode.Relics:
                        AutoSlayLog.Action($"{tag} {relics.Count} relics equipped ({string.Join(", ", relics.Select(r => r.GetType().Name))}) for {cards.Count} cards + {potions.Count} potions, seed '{seed}'");
                        break;
                }
                if (phase is Mode.Cards or Mode.Relics)
                {
                    foreach (CardModel model in cards)
                    {
                        await Run(model.GetType().Name, (combat, player) => ExerciseCard(model, combat, player));
                    }
                }
                if (phase is Mode.Potions or Mode.Relics)
                {
                    foreach (PotionModel model in potions)
                    {
                        await Run(model.GetType().Name, (combat, player) => ExercisePotion(model, combat, player));
                    }
                }
            }
        }
        finally
        {
            selectorScope.Dispose();
            TestMode.IsOn = wasTestMode;
            if (failures.Count == 0)
            {
                AutoSlayLog.Action($"{tag} all {total} items passed");
            }
            else
            {
                AutoSlayLog.Warn($"{tag} {failures.Count}/{total} items failed:");
                foreach ((string item, Exception ex) in failures)
                {
                    AutoSlayLog.Warn($"  - {item}: {ex.Message}");
                }
            }
            // Same contract as AutoSlay: quit with 0 = all passed, 1 = failures, so a headless
            // launch (launch-witch.ps1 -Headless) propagates the result as its exit code.
            NGame.Instance?.GetTree().Quit(failures.Count == 0 ? 0 : 1);
        }
    }

    private static async Task ExerciseCard(CardModel model, CombatState combat, Player player)
    {
        BlockingPlayerChoiceContext ctx = new();
        Creature? Target(CardModel c) => c.TargetType == TargetType.AnyEnemy ? combat.HittableEnemies.FirstOrDefault() : null;

        CardModel card = combat.CreateCard(model, player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        Creature? target = Target(card);
        if (target != null)
        {
            target.SetMaxHpInternal(9999m);
            target.SetCurrentHpInternal(9999m);
        }
        await CardCmd.AutoPlay(ctx, card, target);
        await EndTurnAndWait(player);

        CardModel card2 = combat.CreateCard(model, player);
        await CardPileCmd.AddGeneratedCardToCombat(card2, PileType.Draw, player, CardPilePosition.Top);
        await CardPileCmd.Draw(ctx, player);
        await EndTurnAndWait(player);

        CardModel card3 = combat.CreateCard(model, player);
        await CardPileCmd.AddGeneratedCardToCombat(card3, PileType.Hand, player);
        await CardCmd.Exhaust(ctx, card3);
        await EndTurnAndWait(player);

        CardModel card4 = combat.CreateCard(model, player);
        await CardPileCmd.AddGeneratedCardToCombat(card4, PileType.Hand, player);
        await CardCmd.Discard(ctx, card4);
        await EndTurnAndWait(player);

        CardModel strike = combat.CreateCard(player.Character.StartingDeck.First(c => c.Type == CardType.Attack), player);
        CardModel defend = combat.CreateCard(player.Character.StartingDeck.First(c => c.Type == CardType.Skill), player);
        CardModel card5 = combat.CreateCard(model, player);
        await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, player);
        await CardPileCmd.AddGeneratedCardToCombat(defend, PileType.Hand, player);
        await CardPileCmd.AddGeneratedCardToCombat(card5, PileType.Hand, player);
        await CardCmd.AutoPlay(ctx, strike, Target(strike));
        await CardCmd.AutoPlay(ctx, defend, Target(defend));
        await CardCmd.AutoPlay(ctx, card5, Target(card5));
    }

    /// <summary>
    /// Procure the potion into the belt and use it (the real OnUseWrapper path: use-hooks, history,
    /// removal from the belt), end the turn, then procure it again and discard it.
    /// </summary>
    private static async Task ExercisePotion(PotionModel model, CombatState combat, Player player)
    {
        BlockingPlayerChoiceContext ctx = new();
        if (model.Usage == PotionUsage.None)
        {
            AutoSlayLog.Info("  (Usage=None; skipping use)");
            return;
        }

        PotionModel potion = await Procure(model, player);
        Creature? target = model.TargetType switch
        {
            TargetType.AnyEnemy => combat.HittableEnemies.FirstOrDefault(),
            TargetType.Self or TargetType.AnyPlayer or TargetType.AnyAlly => player.Creature,
            _ => null,
        };
        if (target != null && !target.IsPlayer)
        {
            target.SetMaxHpInternal(9999m);
            target.SetCurrentHpInternal(9999m);
        }
        await potion.OnUseWrapper(ctx, target);
        await EndTurnAndWait(player);

        PotionModel potion2 = await Procure(model, player);
        await PotionCmd.Discard(potion2);
    }

    private static async Task<PotionModel> Procure(PotionModel model, Player player)
    {
        PotionProcureResult result = await PotionCmd.TryToProcure(model.ToMutable(), player);
        if (!result.success)
        {
            throw new InvalidOperationException($"could not procure {model.GetType().Name}: {result.failureReason}");
        }
        return result.potion;
    }

    /// <summary>
    /// PlayerCmd.EndTurn only flags the player ready; the enemy turn runs on its own queued task.
    /// Wait for the round-trip back to the Play phase so the next step (and the combat Reset in
    /// EndCombat) doesn't race it — otherwise EndPlayerTurnPhaseTwoInternal NREs on a null state.
    /// </summary>
    private static async Task EndTurnAndWait(Player player)
    {
        PlayerCmd.EndTurn(player, false);
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        while (player.PlayerCombatState?.Phase == PlayerTurnPhase.Play && sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Yield();
        }
        while (player.PlayerCombatState?.Phase != PlayerTurnPhase.Play && sw.Elapsed < TimeSpan.FromSeconds(20))
        {
            await Task.Yield();
        }
        if (player.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
        {
            throw new InvalidOperationException("turn never came back to the Play phase after EndTurn");
        }
    }

    private static async Task<(CombatState, Player)> NewCombat(CharacterModel character, string seed, IReadOnlyList<RelicModel> relics)
    {
        if (CombatManager.Instance.DebugOnlyGetState() != null)
        {
            CombatManager.Instance.Reset(true);
        }

        Player player = Player.CreateForNewRun(character, UnlockState.all, 1UL);
        RunState run = RunState.CreateForTest(players: [player], seed: seed);

        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        LocalContext.NetId = RunManager.Instance.NetService.NetId;
        player = run.Players[0];

        // Relic mode: equip the whole set before the combat exists so BeforeCombatStart / turn
        // hooks all see it. Starting relics (Large Pockets) are already on the player — skip dupes.
        foreach (RelicModel relic in relics)
        {
            if (player.Relics.All(r => r.Id != relic.Id))
            {
                await RelicCmd.Obtain(relic.ToMutable(), player);
            }
        }

        EncounterModel encounter = ModelDb.AllEncounters.First().ToMutable();
        encounter.DebugRandomizeRng();
        CombatState combat = new(encounter, run, run.Modifiers, run.BadgeModels, run.MultiplayerScalingModel);
        combat.AddPlayer(player);

        if (!encounter.HaveMonstersBeenGenerated)
        {
            encounter.GenerateMonstersWithSlots(run);
        }
        foreach ((MonsterModel monster, string? slot) in encounter.MonstersWithSlots)
        {
            combat.AddCreature(combat.CreateCreature(monster, CombatSide.Enemy, slot));
        }
        combat.SortEnemiesBySlotName();
        CombatManager.Instance.SetUpCombat(combat);
        CombatManager.Instance.AfterCombatRoomLoaded();

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        while (!CombatManager.Instance.IsInProgress && sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Yield();
        }
        if (!CombatManager.Instance.IsInProgress)
        {
            throw new InvalidOperationException("combat never reached IsInProgress");
        }
        while (player.PlayerCombatState?.Phase != PlayerTurnPhase.Play && sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Yield();
        }
        return (combat, player);
    }

    private static void EndCombat()
    {
        try { CombatManager.Instance.Reset(true); } catch { /* best effort */ }
        try
        {
            // RunManager.State is private-set; clear it so the next test run can SetUpTest again.
            AccessTools.PropertySetter(typeof(RunManager), "State").Invoke(RunManager.Instance, [null]);
            LocalContext.NetId = null;
        }
        catch { /* best effort */ }
    }

    /// <summary>Auto-picks the first eligible cards for any selection prompt.</summary>
    private sealed class FirstCardSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
            => Task.FromResult<IEnumerable<CardModel>>(options.Take(maxSelect).ToList());

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
            => new() { card = options.FirstOrDefault()?.Card };
    }
}
