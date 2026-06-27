using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// A stronger <see cref="WickedBrew" />: deals 11 damage. Card-only (Token rarity keeps it out of the
/// random drop/shop pool while staying registered for Concoct's <c>PotionCmd.TryToProcure</c>).
/// </summary>
public sealed class VillainousBrew : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(11m, ValueProp.Unpowered)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        DamageVar damage = DynamicVars.Damage;
        await CreatureCmd.Damage(choiceContext, target, damage.BaseValue, damage.Props, Owner.Creature, null);
    }
}
