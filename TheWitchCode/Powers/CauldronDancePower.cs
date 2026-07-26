using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Cauldron Dance: for the rest of this turn, each Skill the player plays draws a card.
/// Removes itself at the end of the turn. The Dance card that applies this buff does NOT count itself —
/// we capture its source card on apply and skip it (the buff is now live when its own AfterCardPlayed fires).
/// </summary>
public sealed class CauldronDancePower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private CardModel? sourceCard;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        sourceCard = cardSource;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner
            || cardPlay.Card.Type != CardType.Skill
            || ReferenceEquals(cardPlay.Card, sourceCard))
        {
            return;
        }
        if (Owner.Player is not { } player)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
