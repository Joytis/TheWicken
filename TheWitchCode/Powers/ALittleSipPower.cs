using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// A Little Sip: whenever the player uses a potion, gain Strength and draw cards. The card's two effects
/// scale on DIFFERENT axes, so they are tracked by two separate numbers on this one power:
/// <list type="bullet">
/// <item><see cref="PowerModel.Amount" /> = copies of the card played = cards drawn (one stack per play,
/// upgraded or not).</item>
/// <item><c>DynamicVars["StrengthPower"]</c> = accumulated Strength per potion, which the card ADDS its own
/// (upgrade-scaled) value into via <see cref="AddStrength" />.</item>
/// </list>
/// So two upgraded copies = Amount 2 (draw 2) and Strength 4. Both numbers render in the buff tooltip —
/// smartDescription reads <c>{StrengthPower}</c> and <c>{Amount}</c> (base-game precedent: AUTOMATION_POWER's
/// <c>{BaseCards}</c>, COLOSSUS_POWER's <c>{DamageDecrease}</c>).
/// <c>AfterPotionUsed</c> provides no PlayerChoiceContext, so the applies use
/// <c>ThrowingPlayerChoiceContext</c> (the base-game ReptileTrinket pattern).
/// </summary>
public sealed class ALittleSipPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(0m)
    ];

    /// <summary>Fold another copy's Strength value into the running total (called by the card on play).</summary>
    public void AddStrength(decimal amount) => DynamicVars["StrengthPower"].BaseValue += amount;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), Owner, DynamicVars["StrengthPower"].BaseValue, Owner, null);
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, potion.Owner);
        }
    }
}
