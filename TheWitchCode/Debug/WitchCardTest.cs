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
/// Headless smoke test (--witch-cardtest): plays every card the mod defines (all pools) in a throwaway test combat
/// (TestMode on, no scene), one fresh combat per card, and logs any exception. Per card: play
/// from hand, draw it, exhaust it, discard it, then play Strike+Defend alongside it — the shape
/// of Downfall's TestCode harness. Selection prompts auto-pick the first eligible cards.
/// </summary>
public static class WitchCardTest
{
    private const string Tag = "[witch-cardtest]";

    public static async Task RunAll(string seed)
    {
        bool wasTestMode = TestMode.IsOn;
        TestMode.IsOn = true;
        IDisposable selectorScope = CardSelectCmd.UseSelector(new FirstCardSelector());
        List<(string card, Exception ex)> failures = [];
        int total = 0;
        try
        {
            if (!Godot.FileAccess.FileExists($"res://{MainFile.ModId}/localization/eng/cards.json"))
            {
                AutoSlayLog.Warn($"{Tag} mod .pck not loaded (no res://{MainFile.ModId}/localization) — localization-dependent cards will fail; run with Build=Publish");
            }
            CharacterModel witch = ModelDb.Character<Witch>();

            // Every card this assembly defines, whatever pool it sits in: the main Witch pool,
            // the shared familiar-token pool, and the StatusCardPool strays (Ash, Wormy).
            List<CardModel> cards = ModelDb.AllCards
                .Where(c => c.GetType().Assembly == typeof(WitchCardTest).Assembly)
                .OrderBy(c => c.GetType().Name)
                .ToList();
                
            AutoSlayLog.Action($"{Tag} {cards.Count} cards, seed '{seed}'");
            foreach (CardModel model in cards)
            {
                total++;
                string name = model.GetType().Name;
                AutoSlayLog.Info($"{Tag} {name}");
                try
                {
                    (CombatState combat, Player player) = await NewCombat(witch, seed);
                    await ExerciseCard(model, combat, player);
                }
                catch (Exception e)
                {
                    Exception actual = e.InnerException ?? e;
                    failures.Add((name, actual));
                    AutoSlayLog.Error($"{Tag} FAILED {name}: {actual}");
                }
                finally
                {
                    EndCombat();
                }
            }
        }
        finally
        {
            selectorScope.Dispose();
            TestMode.IsOn = wasTestMode;
            if (failures.Count == 0)
            {
                AutoSlayLog.Action($"{Tag} all {total} cards passed");
            }
            else
            {
                AutoSlayLog.Warn($"{Tag} {failures.Count}/{total} cards failed:");
                foreach ((string card, Exception ex) in failures)
                {
                    AutoSlayLog.Warn($"  - {card}: {ex.Message}");
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

    private static async Task<(CombatState, Player)> NewCombat(CharacterModel character, string seed)
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
