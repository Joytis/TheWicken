using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Moonbeam (enemy debuff): the beam stays trained on the target — at the start of each of the APPLIER's
/// turns the owner takes <see cref="PowerModel.Amount" /> damage (blockable, non-attack, Unpowered so the
/// caster's Strength/Vigor don't re-scale it every turn). Stacks add damage; repeat plays intensify the beam.
/// </summary>
public sealed class MoonbeamPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Applier == null || player.Creature != Applier || Amount <= 0 || !Owner.IsAlive)
        {
            return;
        }

        Flash();
        WitchFx.PurpleFlame(Owner);
        await CreatureCmd.Damage(choiceContext, [Owner], Amount, ValueProp.Unpowered, Applier, null);
    }
}
