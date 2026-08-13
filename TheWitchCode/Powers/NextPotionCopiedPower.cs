using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Gather Herbs buff (counter): the next potion the player creates is duplicated — when it's procured, a fresh
/// copy is procured straight back into the belt. One stack is consumed per creation. The instance-scoped
/// <c>_copying</c> guard makes the copy's own procurement a no-op, so a single creation only ever yields one copy
/// (and stacked Gather Herbs spreads across multiple distinct creations rather than cascading on one). Belt-full
/// copies just fail silently. Combat-scoped: the buff is cleared at combat end, so only in-combat creation
/// triggers it.
/// </summary>
public sealed class NextPotionCopiedPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _copying;

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (_copying || potion.Owner != Owner.Player)
        {
            return;
        }
        PotionModel? canonical = ModelDb.AllPotions.FirstOrDefault(p => p.GetType() == potion.GetType());
        if (canonical == null)
        {
            return;
        }
        await PowerCmd.Decrement(this);
        Flash();
        _copying = true;
        try
        {
            var result = await PotionCmd.TryToProcure(canonical.ToMutable(), Owner.Player);
            if (result.success && result.potion != null)
            {
                // The copy exists before the creator marks the original Unstable —
                // register it so a later Mark(original) propagates (Gather Herbs + Unstable brews).
                UnstablePotions.RegisterCopy(potion, result.potion);
            }
        }
        finally
        {
            _copying = false;
        }
    }
}
