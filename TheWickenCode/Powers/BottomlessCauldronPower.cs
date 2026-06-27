using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Potions;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Bottomless Cauldron: whenever the player uses a potion <em>other than a Wicked Brew</em>, create a
/// <see cref="WickedBrew" />. The Wicked Brew exclusion is essential — without it, using a created Wicked
/// Brew would create another, looping into infinite potions. Passive toggle (Single stack). Uses the
/// context-free <c>PotionCmd.TryToProcure&lt;T&gt;(Player)</c> overload.
/// </summary>
public sealed class BottomlessCauldronPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player && potion is not WickedBrew)
        {
            Flash();
            await PotionCmd.TryToProcure<WickedBrew>(Owner.Player);
        }
    }
}
