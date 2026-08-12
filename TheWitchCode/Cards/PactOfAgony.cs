using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Pact of Agony: hex yourself to charge your potions — they hit far harder for the rest of the turn.</summary>
public sealed class PactOfAgony : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(2m),
        new DamageVar(13m, ValueProp.Move),
        // The MULTIPLIER applied to potion damage, not a stack count: 2x, upgrading to 3x.
        new PowerVar<PotionDamageMultiplierPower>(2m)
    ];

    public PactOfAgony()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await PowerCmd.Apply<HexPower>(choiceContext, Owner.Creature, DynamicVars["HexPower"].BaseValue, Owner.Creature, this);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath, null, "heavy_attack.mp3")
            .Execute(choiceContext);

        await PowerCmd.Apply<PotionDamageMultiplierPower>(
            choiceContext, Owner.Creature, DynamicVars["PotionDamageMultiplierPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["PotionDamageMultiplierPower"].UpgradeValueBy(1m);
}
