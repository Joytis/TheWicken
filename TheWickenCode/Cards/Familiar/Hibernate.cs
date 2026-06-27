using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Bear familiar token: defensive — block and a heal.</summary>
public sealed class Hibernate : WickenFamiliarCard
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(15m, ValueProp.Move),
        new DynamicVar("Heal", 3m)
    ];

    public Hibernate()
        : base(2, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
        DynamicVars["Heal"].UpgradeValueBy(2m);
    }
}
