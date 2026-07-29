using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Mulch: compost X cards from your hand, and X fresh random Witch cards sprout in their place —
/// free to play for the rest of the combat.
/// </summary>
public sealed class Mulch : WitchCard
{
    protected override bool HasEnergyCostX => true;

    public Mulch()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        if (IsUpgraded)
        {
            x++;
        }
        if (x <= 0)
        {
            return;
        }

        var picks = await CardSelectCmd.FromHand(
            context: choiceContext,
            player: Owner,
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, x),
            filter: null,
            source: this);
        foreach (CardModel pick in picks)
        {
            await CardCmd.Exhaust(choiceContext, pick);
        }

        // TakeRandom clamps to the pool size, so a huge X (debug energy) can't over-ask; the adds go
        // through ONE batched call — per-card awaited adds stall/lock the game when X is large.
        x = Mathf.Min(x, picks.Count());
        List<CardModel> sprouted = CardFactory.GetDistinctForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            x,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        foreach (CardModel card in sprouted)
        {
            card.SetToFreeThisCombat();
        }
        await CardPileCmd.AddGeneratedCardsToCombat(sprouted, PileType.Hand, Owner);
    }
}
