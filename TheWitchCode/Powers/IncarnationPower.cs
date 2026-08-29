using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Incarnation: capstone of the Wicker chain (<see cref="Cards.WickerConsumation" />). Whenever the owner
/// casts a spell — plays a card — they gain <see cref="BramblesPerStack" /> Brambles PER STACK. The card
/// grants a single stack ("gain Incarnation"); the 5 is a property of the power, not of the card.
/// <c>TotalBrambles</c> is kept in sync with the stack count for the tooltip.
/// </summary>
public sealed class IncarnationPower : WitchPower
{
    public const decimal BramblesPerStack = 5m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(BramblesPerStack),
        new DynamicVar("TotalBrambles", BramblesPerStack)
    ];

    private decimal TotalBrambles => Amount * BramblesPerStack;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            DynamicVars["TotalBrambles"].BaseValue = TotalBrambles;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner, TotalBrambles, Owner, cardPlay.Card);
    }
}
