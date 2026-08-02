using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Prices Paid: bleed yourself for a strike, then brew the spilled blood into Noxious Brews.</summary>
public sealed class PricesPaid : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion<NoxiousBrew>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move),
        new HpLossVar(1m),
        new IntVar("Brews", 1m),
    ];

    public PricesPaid()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Lose HP.
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        VfxCmd.PlayOnCreatureCenter(Owner.Creature, VfxCmd.bloodyImpactPath);
        SfxCmd.Play("slash_attack.mp3");

        // Deal damage.
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.bloodyImpactPath)
            .Execute(choiceContext);

        // Make brews.
        await WitchCmd.TryProcureNoxiousBrews(Owner, DynamicVars["Brews"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HpLoss"].UpgradeValueBy(1m);
        DynamicVars["Brews"].UpgradeValueBy(1m);
    }
}
