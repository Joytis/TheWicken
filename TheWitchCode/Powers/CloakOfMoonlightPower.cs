using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Cloak of Moonlight: whenever the player triggers Hex — their attack cashes in the Hex bonus
/// damage on a hexed creature — gain <see cref="MegaCrit.Sts2.Core.Models.PowerModel.Amount" />
/// Block. There is no game hook for "Hex triggered", so <see cref="HexPower.AfterAttack" /> notifies
/// the attacker's copy of this power directly at its trigger point. Procs once per attack per hexed
/// enemy hit; Torment-style IHexPreserving attacks still count (they trigger Hex without burning it).
/// </summary>
public sealed class CloakOfMoonlightPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Called by <see cref="HexPower" /> when the owner's attack triggers Hex.</summary>
    public async Task OnHexTriggered()
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
