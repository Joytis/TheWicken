using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Call the Pack: hunker down behind the wolves — gain Block, shuffle Gnash tokens into the Draw Pile.</summary>
public sealed class CallThePack : WitchCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Gnash>(IsUpgraded),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(7m, ValueProp.Move),
        new CardsVar(2)
    ];

    public CallThePack()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        var gnashes = FamiliarCardRegistry.CreateFamiliarCards<Gnash>(Owner, DynamicVars.Cards.IntValue, CombatState, IsUpgraded);
        var generated = await CardPileCmd.AddGeneratedCardsToCombat(gnashes, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generated);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
