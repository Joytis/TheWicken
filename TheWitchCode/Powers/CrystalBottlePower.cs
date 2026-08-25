using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Crystal Bottle (armed): the next potion the player uses is bottled — its consumed instance is handed to
/// <see cref="NeverendingPotionPower" />, which replays its effect at the start of each turn. One stack per potion.
/// Healing potions (<see cref="PotionTraits.IsHealing" /> — Fruit Juice, Blood Potion, …) are never bottled:
/// the potion works normally and the bottle stays armed.
/// </summary>
public sealed class CrystalBottlePower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (Amount <= 0 || potion.Owner != Owner.Player || PotionTraits.IsHealing(potion))
        {
            return;
        }

        Flash();
        NeverendingPotionPower? bottled = await PowerCmd.Apply<NeverendingPotionPower>(
            new ThrowingPlayerChoiceContext(), Owner, 1m, Owner, null);
        bottled?.Bottle(potion);
        await PowerCmd.Decrement(this);
    }
}
