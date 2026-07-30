using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Polymorph (was Repurpose): choose TWO cards in your draw pile — each becomes two Rats tokens
/// (combat-scoped <c>CardCmd.Transform</c> plus one generated extra per choice, 4 Rats total).
/// </summary>
public sealed class Polymorph : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public Polymorph()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var chosen = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Draw.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 2))).ToList();

        foreach (CardModel card in chosen)
        {
            CardModel replacement = CombatState!.CreateCard<Rats>(Owner);
            await CardCmd.Transform(card, replacement);

            // Each choice becomes TWO Rats — generate the extras alongside the transform.
            for (int i = 1; i < DynamicVars.Cards.IntValue; i++)
            {
                CardModel extra = CombatState!.CreateCard<Rats>(Owner);
                await CardPileCmd.AddGeneratedCardToCombat(extra, PileType.Draw, Owner, CardPilePosition.Random);
            }
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
