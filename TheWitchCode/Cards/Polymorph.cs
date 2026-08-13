using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Polymorph (was Repurpose): choose {Cards} cards in your draw pile (1, upgraded 2) — each becomes
/// TWO Rats tokens (combat-scoped <c>CardCmd.Transform</c> plus one generated extra per choice).
/// </summary>
public sealed class Polymorph : WitchCard
{
    private const int RatsPerCard = 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
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
            new CardSelectorPrefs(SelectionScreenPrompt, DynamicVars.Cards.IntValue))).ToList();

        foreach (CardModel card in chosen)
        {
            CardModel replacement = CombatState!.CreateCard<Rats>(Owner);
            await CardCmd.Transform(card, replacement);

            // Each choice becomes RatsPerCard Rats — generate the extras alongside the transform, and give each
            // one its own transform-overlay vfx (chosen card morphing into the Rats) so the creation
            // overlay shows EVERY Rats created, not just the direct transforms. AddGeneratedCardToCombat
            // itself has no center-screen visual. Vfx is local-only, mirroring CardCmd.Transform's
            // LocalContext.IsMine gate.
            for (int i = 1; i < RatsPerCard; i++)
            {
                CardModel extra = CombatState!.CreateCard<Rats>(Owner);
                await CardPileCmd.AddGeneratedCardToCombat(extra, PileType.Draw, Owner, CardPilePosition.Random);
                ShowCreationOverlay(card, extra);
            }
        }
    }

    private static void ShowCreationOverlay(CardModel original, CardModel created)
    {
        if (TestMode.IsOn || !LocalContext.IsMine(created))
        {
            return;
        }
        NCombatRoom.Instance?.Ui.CardPreviewContainer
            .AddChildSafely(NCardTransformVfx.Create(original, created, null));
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
