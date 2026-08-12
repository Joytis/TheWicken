using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Lavender and Sage: draw a card and grow a thicket of Brambles.</summary>
public sealed class LavenderAndSage : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new PowerVar<BramblesPower>(3m),
    ];

    public LavenderAndSage()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Brambles().UpgradeValueBy(2m);
}
