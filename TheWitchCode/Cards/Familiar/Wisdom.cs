using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Owl familiar token: draw 2, discard 1 — the upgrade drops the discard.</summary>
public sealed class Wisdom : WitchFamiliarCard
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
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        if (!IsUpgraded)
        {
            await CardCmd.Discard(choiceContext,
                await CardSelectCmd.FromHandForDiscard(
                    choiceContext,
                    Owner,
                    new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this));
        }
    }

    protected override void OnUpgrade()
    {
        // Upgrade changes behavior only (no discard); no numbers to bump.
    }
}
