using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Message in a Bottle: bottle a chosen card from your hand into a Bottled Message potion; using the
/// potion returns the card to your hand. The potion is procured FIRST — if the belt is full the card
/// is never removed (no card lost to a failed procure).
/// </summary>
public sealed class MessageInABottle : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion<BottledMessage>(),
    ];

    public MessageInABottle()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? chosen = (await CardSelectCmd.FromHand(
            choiceContext, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), null, this)).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        var potion = (BottledMessage)ModelDb.Potion<BottledMessage>().ToMutable();
        potion.Bottle(chosen);

        if ((await PotionCmd.TryToProcure(potion, Owner)).success)
        {
            await CardPileCmd.RemoveFromCombat(chosen);
        }
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
