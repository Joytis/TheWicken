using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Potions.Treasures;

/// <summary>
/// A shiny bauble the Crow drags home. Throw it at an enemy for damage, or sell it to the
/// shopkeeper for gold (the base-game FoulPotion merchant-throw shape, minus the insult).
/// Token rarity: only ever created by Shiny, never a random drop.
/// </summary>
public abstract class TreasurePotion : WitchPotion
{
    protected abstract decimal DamageAmount { get; }
    protected abstract int GoldAmount { get; }

    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override TargetType TargetType =>
        CombatManager.Instance.IsInProgress ? TargetType.AnyEnemy : TargetType.TargetedNoCreature;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(DamageAmount, ValueProp.Unpowered),
        new GoldVar(GoldAmount)
    ];

    public override bool PassesCustomUsabilityCheck
    {
        get
        {
            if (CombatManager.Instance.IsInProgress)
            {
                return true;
            }
            if (Owner.RunState.CurrentRoom is MerchantRoom room)
            {
                return FoulPotion.GetFoulPotionMerchantTarget(room).button != null;
            }
            return false;
        }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (CombatManager.Instance.IsInProgress)
        {
            AssertValidForTargetedPotion(target);
            await CreatureCmd.Damage(choiceContext, target, DynamicVars.Damage, Owner.Creature, null);
        }
        else if (Owner.RunState.CurrentRoom is MerchantRoom)
        {
            SfxCmd.Play("event:/sfx/npcs/merchant/merchant_thank_yous");
            await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        }
    }
}

/// <summary>A small Treasure: 5 damage or 15 gold.</summary>
public abstract class SmallTreasurePotion : TreasurePotion
{
    protected override decimal DamageAmount => 5m;
    protected override int GoldAmount => 15;
}

/// <summary>A Big Treasure: 7 damage or 30 gold.</summary>
public abstract class BigTreasurePotion : TreasurePotion
{
    protected override decimal DamageAmount => 7m;
    protected override int GoldAmount => 30;
}

public sealed class PolishedBottlecap : SmallTreasurePotion;
public sealed class ShinyHairpin : SmallTreasurePotion;
public sealed class ForeignCoin : SmallTreasurePotion;

public sealed class CrackedGemstone : BigTreasurePotion;
public sealed class FancyComb : BigTreasurePotion;
public sealed class ImpeccableSilverware : BigTreasurePotion;
