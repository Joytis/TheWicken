using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

public sealed class ForbiddenMagic : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(9m, ValueProp.Move),
        new EnergyVar(1),
        new CardsVar(1)
    ];

    public ForbiddenMagic()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        bool wasHexed = cardPlay.Target.HasPower<HexPower>();

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_starry_impact", null, "heavy_attack.mp3")
            .Execute(choiceContext);

        if(wasHexed)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);            
        }

    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
