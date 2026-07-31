using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Primal Form: each turn, the owner's first <see cref="PowerModel.Amount" /> Attacks are each played an
/// additional time. <see cref="Amount" /> is the per-turn allowance (so the card's upgrade raises it);
/// the spend counter resets at the owner's turn start.
/// </summary>
public sealed class PrimalFormPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /* Plain instance field, not a DynamicVar: it's a per-turn scratch counter. Value type, so
       MemberwiseClone handles it (no DeepCloneFields needed), and it never has to survive a save —
       run saves hold no combat state, and MP lockstep runs these hooks on every client. */
    private int _duplicatedThisTurn;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            _duplicatedThisTurn = 0;
        }
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != Owner || card.Type != CardType.Attack || _duplicatedThisTurn >= Amount)
        {
            return playCount;
        }
        return playCount + 1;
    }

    /* Only fires for models that actually changed the play count, and only on a real play (never a
       preview) — so this is the spend. */
    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        Flash();
        _duplicatedThisTurn++;
        return Task.CompletedTask;
    }
}
