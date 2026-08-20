using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Potions;

public sealed class NoxiousBrew : WitchPotion
{
    // Token rarity keeps NoxiousBrew out of the random drop/shop pool (PotionFactory
    // only rolls Common/Uncommon/Rare) while staying registered so Favorite Spellbook's
    // PotionCmd.TryToProcure<NoxiousBrew> can still grant it. Card-only by design.
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Unpowered)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        DamageVar damage = DynamicVars.Damage;
        await CreatureCmd.Damage(choiceContext, target, damage.BaseValue, damage.Props, Owner.Creature, null);
    }
}
