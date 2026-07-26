using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Cloak of Moonlight: whenever the player creates a card or a potion, gain <see cref="PowerModel.Amount" />
/// Block. Listens to both <c>AfterCardGeneratedForCombat</c> and <c>AfterPotionProcured</c>. Passive toggle
/// (Single stack).
/// </summary>
public sealed class CloakOfMoonlightPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator == Owner.Player)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
        }
    }

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
        }
    }
}
