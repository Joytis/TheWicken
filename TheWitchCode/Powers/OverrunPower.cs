using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Overrun: for the rest of this turn, each Familiar token card the owner plays strikes the overrun
/// enemy again. Each Overrun play queues its own strike (target + damage), so multiple plays stack
/// naturally; dead targets fizzle. Removes itself at the end of the turn (Cauldron Dance pattern).
/// </summary>
public sealed class OverrunPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private readonly record struct Strike(Creature Target, decimal Damage, CardModel Source);

    // Combat-scoped instance state; model clones are shallow, so the list must be re-created per clone.
    private List<Strike> _strikes = [];

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _strikes = [];
    }

    public void AddStrike(Creature target, decimal damage, CardModel source)
    {
        AssertMutable();
        _strikes.Add(new Strike(target, damage, source));
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || cardPlay.Card is not WitchFamiliarCard)
        {
            return;
        }

        foreach (Strike strike in _strikes)
        {
            if (!strike.Target.IsAlive)
            {
                continue;
            }
            Flash();
            await CreatureCmd.Damage(choiceContext, strike.Target, strike.Damage, ValueProp.Unpowered, Owner, strike.Source);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
