using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Ritual Casting: whenever the owner plays a card that costs 2 or more (its cost when played,
/// <see cref="ResourceInfo.EnergyValue" /> — so auto-plays of big cards count too), a random card
/// in their hand becomes free to play this turn.
/// </summary>
public sealed class RitualCastingPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Player is not { } player
            || cardPlay.Card.Owner.Creature != Owner
            || cardPlay.Resources.EnergyValue < 2)
        {
            return Task.CompletedTask;
        }

        List<CardModel> hand = PileType.Hand.GetPile(player).Cards.ToList();
        CardModel? pick = player.RunState.Rng.CombatCardSelection.NextItem(hand);
        if (pick == null)
        {
            return Task.CompletedTask;
        }

        Flash();
        pick.SetToFreeThisTurn();
        CardCmd.Preview(pick);
        return Task.CompletedTask;
    }
}
