using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Bind in Blood: seed the target with Hex.</summary>
public sealed class BindInBlood : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(2m)
    ];

    public BindInBlood()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        VfxCmd.PlayOnCreatureCenter(Owner.Creature, VfxCmd.bloodyImpactPath);
        await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, DynamicVars.Hex().BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Hex().UpgradeValueBy(1m);
}
