using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Bottomless Cauldron: whenever the player uses a potion <em>other than a Noxious Brew</em>, create one
/// <see cref="NoxiousBrew" /> per stack. The Noxious Brew exclusion is essential — without it, using a created
/// Noxious Brew would create another, looping into infinite potions. Counter stack so repeat casts of the card
/// stack up. Uses the context-free <c>PotionCmd.TryToProcure&lt;T&gt;(Player)</c> overload.
/// </summary>
public sealed class BottomlessCauldronPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player && potion is not NoxiousBrew)
        {
            Flash();
            for (int i = 0; i < Amount; i++)
            {
                await Witch.ProducePotion<NoxiousBrew>(Owner.Player, Witch.PotionMode.Unstable);
            }
        }
    }
}
