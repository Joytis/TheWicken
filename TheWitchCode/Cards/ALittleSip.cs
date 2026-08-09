using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// A Little Sip: a Power — whenever you use a potion, gain Strength and draw a card. Each play adds ONE
/// stack (the draw, never upgraded) and folds this copy's Strength value into the power's running total,
/// so two upgraded copies give 4 Strength but only 2 cards. See <see cref="ALittleSipPower" />.
/// </summary>
public sealed class ALittleSip : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(1m)
    ];

    public ALittleSip()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

        // One stack per copy = one card drawn per copy, regardless of upgrade level.
        await PowerCmd.Apply<ALittleSipPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        Owner.Creature.GetPower<ALittleSipPower>()?.AddStrength(DynamicVars.Strength.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Strength.UpgradeValueBy(1m);
}
