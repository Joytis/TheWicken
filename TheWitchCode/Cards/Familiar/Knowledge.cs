using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Owl familiar token: memorize a card — copy a chosen card in your hand (base-game Dual Wield pattern:
/// <c>CreateClone</c>, added through the generated-card funnel so creation payoffs fire). Upgraded,
/// the copy costs 1 less for the combat.
/// </summary>
public sealed class Knowledge : WitchFamiliarCard
{
    public Knowledge()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Knowledge can't copy Knowledge (by type, not instance — two in hand copying each other is the same infinite loop).
        CardModel? selection = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: Owner,
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            filter: c => c is not Knowledge,
            source: this)).FirstOrDefault();

        if (selection == null)
        {
            return;
        }

        WitchFx.EnchantShimmer();
        CardModel clone = selection.CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, Owner);
        if (IsUpgraded)
        {
            clone.EnergyCost.AddThisCombat(-1, reduceOnly: true);
        }
    }

    protected override void OnUpgrade()
    {
        // Upgrade changes behavior only (the copy costs 1 less); no numbers to bump.
    }
}
