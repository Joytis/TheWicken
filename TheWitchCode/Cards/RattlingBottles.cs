using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Config;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Rattling Bottles: an attack that also crams every empty potion slot full of rocks.</summary>
public sealed class RattlingBottles : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    private PotionModel CanonicalPotion => Witch.Turbo ? ModelDb.Potion<PotionShapedRock>() : ModelDb.Potion<PotionShapedPebble>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [ UnstablePotions.UnstableHoverTip, HoverTipFactory.FromPotion(CanonicalPotion) ];
    protected override IEnumerable<DynamicVar> CanonicalVars => [ new DamageVar(10m, ValueProp.Move) ];

    public RattlingBottles()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.rockShatterPath, null, "heavy_attack.mp3")
            .Execute(choiceContext);

        await Cmd.Wait(0.2f);

        int empty = Owner.PotionSlots.Count(p => p == null);
        for (int i = 0; i < empty; i++)
        {
            await Witch.ProducePotion(CanonicalPotion, Owner, Witch.PotionMode.Unstable);
            await Cmd.Wait(0.1f);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
