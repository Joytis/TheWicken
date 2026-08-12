using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Ui;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Salt and Ash: sweeping damage, and stabilize a CHOSEN Unstable potion (potion-select
/// overlay, auto-picked when only one is Unstable). Exhausts.</summary>
public sealed class SaltAndAsh : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move)
    ];

    public SaltAndAsh()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);

        List<PotionModel> unstable = Owner.PotionSlots
            .Where(p => p != null && UnstablePotions.IsUnstable(p))
            .Select(p => p!)
            .ToList();
        PotionModel? stabilized = await PotionSelectCmd.FromChoosePotionScreen(
            choiceContext, unstable, Owner, SelectionScreenPrompt);
        if (stabilized != null)
        {
            UnstablePotions.Unmark(stabilized);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
