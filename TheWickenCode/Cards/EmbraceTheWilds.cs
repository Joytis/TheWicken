using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>Transform every card in your hand into a random familiar summon (Power) card, free for the rest of combat.</summary>
public sealed class EmbraceTheWilds : WickenCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EmbraceTheWilds()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        List<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.IsTransformable)
            .ToList();
        foreach (CardModel original in handCards)
        {
            CardModel canonical = rng.NextItem(FamiliarCardRegistry.AllSummonCanonical)!;
            CardModel summon = CombatState!.CreateCard(canonical, Owner);
            summon.EnergyCost.SetThisCombat(0);
            await CardCmd.Transform(original, summon);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
