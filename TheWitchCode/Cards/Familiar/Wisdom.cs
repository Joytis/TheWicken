using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Owl familiar token: draw 2, discard 1 — the upgrade drops the discard.</summary>
public sealed class Wisdom : WitchFamiliarCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    public Wisdom()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await CardCmd.Discard(choiceContext,
            await CardSelectCmd.FromHandForDiscard(
                choiceContext,
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this));
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
