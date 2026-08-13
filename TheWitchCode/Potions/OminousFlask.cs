using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>Cursed Bottle: throwable Common potion that applies Hex to an enemy (base-game WeakPotion shape).</summary>
public sealed class OminousFlask : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<HexPower>(1m)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HexPower>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("ac54b3"));
        await PowerCmd.Apply<HexPower>(choiceContext, target, DynamicVars.Hex().BaseValue, Owner.Creature, null);
    }
}
