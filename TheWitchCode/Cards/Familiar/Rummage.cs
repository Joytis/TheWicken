using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Rat familiar token: dig a card back out of the discard pile (base-game Dredge pattern).</summary>
public sealed class Rummage : WitchFamiliarCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    public Rummage()
        : base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int count = Math.Min(DynamicVars.Cards.IntValue, CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count);
        if (count > 0)
        {
            IEnumerable<CardModel> retrieved = await CardSelectCmd.FromCombatPile(
                choiceContext, PileType.Discard.GetPile(Owner), Owner, new CardSelectorPrefs(SelectionScreenPrompt, count));
            List<CardModel> cards = retrieved.ToList();
            await CardPileCmd.Add(cards, PileType.Hand);

            if (IsUpgraded)
            {
                foreach (CardModel card in cards)
                {
                    // "This turn" on a cost reduction means this turn OR until played (see CardEnergyCost docs).
                    card.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
                }
            }
        }
    }
}
