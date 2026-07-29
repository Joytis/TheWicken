using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Mulch: compost the whole discard pile — EXHAUST it for Brambles and Block per card.</summary>
public sealed class Mulch : WitchCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(2m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) => PileType.Discard.GetPile(card.Owner).Cards.Count),
        new CalculatedVar("CalculatedBrambles").WithMultiplier((card, _) => PileType.Discard.GetPile(card.Owner).Cards.Count)
    ];

    public Mulch()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> discard = PileType.Discard.GetPile(Owner).Cards.ToList();
        if (discard.Count == 0)
        {
            return;
        }

        var brambles = ((CalculatedVar)DynamicVars["CalculatedBrambles"]).Calculate(null);
        var block = DynamicVars.CalculatedBlock.Calculate(null);

        // Cards still exhaust one by one, but the pile-move tweens run at triple speed
        // (the game's durations are hard-coded — see CardPileTweenSpeedPatch).
        CardPileTweenSpeedPatch.Scale = 3f;
        try
        {
            foreach (CardModel card in discard)
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }
        finally
        {
            CardPileTweenSpeedPatch.Scale = 1f;
        }
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, brambles, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, block, DynamicVars.CalculatedBlock.Props, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationExtra.UpgradeValueBy(1m);
    }
}
