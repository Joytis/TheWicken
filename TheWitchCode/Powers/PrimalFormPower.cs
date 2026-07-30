using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Primal Form: Attacks the owner plays are played an additional time per stack
/// (base-game <c>OneTwoPunchPower</c>/<c>EchoFormPower</c> play-count shape, but permanent and Attack-only).
/// </summary>
public sealed class PrimalFormPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != Owner || card.Type != CardType.Attack)
        {
            return playCount;
        }
        return playCount + (int)Amount;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        Flash();
        return Task.CompletedTask;
    }
}
