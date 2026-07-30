using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Eye of Newt: the owner's potions deal ×(1 + Amount) damage, and stacking is MULTIPLICATIVE — applying a
/// new copy multiplies the current multiplier rather than adding to it (double + double = ×4, triple + triple
/// = ×9). Implemented by scaling the incoming stack offset in <see cref="TryModifyPowerAmountReceived" />:
/// applying x onto Amount A adds x·(1+A), so the new multiplier is (1+A)(1+x). The displayed Amount therefore
/// always equals "+{Amount}00%", keeping the buff text truthful. Potion damage carries no potion identity into
/// the damage pipeline, so <c>BeforePotionUsed</c> / <c>AfterPotionUsed</c> bracket the use with a transient
/// flag and <c>ModifyDamageMultiplicative</c> amplifies the owner's damage only while a potion is resolving.
/// Plain instance field is safe: single-player has no mid-combat restore and MP is deterministic lockstep.
/// </summary>
public sealed class EyeOfNewtPower : WitchPower
{
    private bool _potionInUse;

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        // Only fires while stacking onto THIS live instance (a brand-new application isn't a hook listener
        // yet), so the first copy applies unscaled and every later copy composes multiplicatively.
        if (canonicalPower == this && amount > 0)
        {
            modifiedAmount = amount * (1m + Amount);
            return true;
        }

        modifiedAmount = amount;
        return false;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            _potionInUse = true;
        }
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        _potionInUse = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!_potionInUse || dealer != Owner)
        {
            return 1m;
        }
        Flash();
        return 1m + Amount;
    }
}
