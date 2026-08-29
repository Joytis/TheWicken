using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Wicker Heart: first link of the Wicker chain. Gain Brambles; at the first bramble threshold it creates
/// <see cref="WickerBones" /> (upgraded if this card is upgraded). Only this card lives in the reward pool —
/// the rest of the chain is spawned.
/// </summary>
public sealed class WickerHeart : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
        HoverTipFactory.FromCard<WickerBones>(IsUpgraded),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(5m),
        new DynamicVar("TargetBrambles", 10m)
    ];

    public WickerHeart()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue, Owner.Creature, this);

        decimal brambles = Owner.Creature.GetPowerAmount<BramblesPower>();
        if (brambles >= DynamicVars["TargetBrambles"].BaseValue)
        {
            CardModel bones = CombatState!.CreateCard<WickerBones>(Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(bones);
            }
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(bones, PileType.Hand, Owner));
        }
    }

    protected override void OnUpgrade() => DynamicVars.Brambles().UpgradeValueBy(2m);
}
