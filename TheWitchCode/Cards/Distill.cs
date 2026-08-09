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

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Distill: brew a random RARE non-healing potion out of nothing, Unstable. No belt input — the roll goes
/// through <see cref="PotionCatalog.Pick" /> so the Separatory Funnel relic turns it into a choice.
/// </summary>
public sealed class Distill : WitchCard
{
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
        await Witch.ProducePotion(potion, Owner, Witch.PotionMode.Unstable);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
