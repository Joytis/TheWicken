using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWicken.TheWickenCode.Cards;

public sealed class Wisdom : WickenFamiliarCard
{
    public Wisdom()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int cardCount = DynamicVars.Cards.IntValue;
		await CardPileCmd.Draw(choiceContext, cardCount, Owner);
		await CardCmd.Discard(choiceContext, 
            await CardSelectCmd.FromHandForDiscard(
                choiceContext, 
                Owner, 
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this));
    }
}
