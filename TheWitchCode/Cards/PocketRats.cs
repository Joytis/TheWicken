using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Pocket Rats: dump a handful of one-shot Rats straight into your hand. Exhausts.</summary>
public sealed class PocketRats : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public PocketRats()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState, "CombatState");

        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
            await WitchFamiliarCard.CreateInHand<Rats>(Owner, 1, CombatState);
			await Cmd.Wait(0.1f);
		}
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
