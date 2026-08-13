using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Ui;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Distill: TRANSFORM a chosen belt potion into a random RARE non-healing potion, Unstable. The input is
/// picked via the potion-select overlay (auto-selected when the belt holds exactly one; empty belt = the
/// card plays but fizzles). The input is discarded before the result is procured so its slot is free on a
/// full belt. The result rolls through <see cref="PotionCatalog.Pick" />, so the Separatory Funnel relic
/// turns that roll into a choice.
/// </summary>
public sealed class Distill : WitchCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    public override IEnumerable<CardKeyword> CanonicalKeywords => Witch.Turbo ? [] : [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
    ];

    public Distill()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // Which belt potion gets distilled. Null = empty belt, so there is nothing to transform.
        PotionModel? input = await PotionSelectCmd.FromChoosePotionScreen(
            choiceContext, Owner.Potions.ToList(), Owner, SelectionScreenPrompt);
        if (input == null)
        {
            return;
        }

        PotionModel? potion = await PotionCatalog.Pick(
            // Entropic Brew is excluded by design — filling the belt with more random potions off a
            // random-potion card is a non-effect at a full belt and a recursion at an empty one.
            PotionCatalog.Query(rarity: PotionRarity.Rare).Where(p => p is not EntropicBrew),
            choiceContext,
            Owner,
            Owner.RunState.Rng.CombatPotionGeneration);
        if (potion == null)
        {
            return;
        }

        WitchFx.EnchantShimmer();
        // Consume the input first so its slot is free for the result (matters on a full belt).
        await PotionCmd.Discard(input);
        await Witch.ProducePotion(potion, Owner, Witch.PotionMode.Unstable);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
