using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Wicker Bones: second link of the Wicker chain, created by <see cref="WickerHeart" />. Styled as a Witch Rare
/// but lives in the shared <see cref="WitchSpawnedCardPool" /> so it never shows up in rewards. Gains Block;
/// at the second bramble threshold it creates <see cref="WickerConsumation" />.
/// </summary>
[Pool(typeof(WitchSpawnedCardPool))]
public sealed class WickerBones : WitchCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
        HoverTipFactory.FromCard<WickerConsumation>(),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(12m, ValueProp.Move),
        new DynamicVar("TargetBrambles", 30m)
    ];

    public WickerBones()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        decimal brambles = Owner.Creature.GetPowerAmount<BramblesPower>();
        if (brambles >= DynamicVars["TargetBrambles"].BaseValue)
        {
            CardModel consumation = CombatState!.CreateCard<WickerConsumation>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(consumation, PileType.Hand, Owner));
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
