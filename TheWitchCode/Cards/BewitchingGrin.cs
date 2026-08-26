using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Artists;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Bewitching Grin: a cheap one-shot skill — block up while sapping every enemy with Weak.</summary>
public sealed class BewitchingGrin : WitchCard
{
    public override Artist? ArtBy => Artist.Kitsu;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(5m, ValueProp.Move),
        new PowerVar<WeakPower>(1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BewitchingGrin()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
        await PowerCmd.Apply<WeakPower>(choiceContext, CombatState!.HittableEnemies, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Weak.UpgradeValueBy(1m);
}
