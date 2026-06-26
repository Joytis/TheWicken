using MegaCrit.Sts2.Core.Entities.Powers;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Marker base for "familiar" counter powers. Each familiar type (Owl, Cat, …) has its own
/// <see cref="FamiliarPower" /> subclass; playing that familiar applies one stack
/// (see <c>WickenCard.GainFamiliar</c>). The player's total familiar count is the sum of all
/// <see cref="FamiliarPower" /> stacks across the creature (see <see cref="Familiars" />).
///
/// Because the stack type is <see cref="PowerStackType.Counter" />, decrementing a stack to zero
/// auto-removes the power — i.e. "sacrificing a familiar" is just <c>PowerCmd.Decrement</c> on a
/// randomly chosen familiar power.
/// </summary>
public abstract class FamiliarPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
