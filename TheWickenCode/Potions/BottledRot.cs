using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>Bottled Rot: uncork the decay — apply Poison to every enemy.</summary>
public sealed class BottledRot : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AllEnemies;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PoisonPower>(4m)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<PoisonPower>(),
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature.CombatState!.HittableEnemies, DynamicVars.Poison.BaseValue, Owner.Creature, null);
    }
}
