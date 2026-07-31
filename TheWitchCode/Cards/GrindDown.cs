using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;
using MegaCrit.Sts2.Core.HoverTips;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Grind Down: Exhaust a card from your hand and grind it into a potion. The card's type sets the potion's
/// orientation (Attack→Offensive, Skill→Defensive, Power→Utility) and its rarity sets the potion's rarity.
/// </summary>
public sealed class GrindDown : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
    ];

    public GrindDown()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? chosen = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: Owner,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            filter: null,
            source: this)).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        PotionOrientation orientation = OrientationFor(chosen.Type);
        PotionRarity rarity = RarityFor(chosen.Rarity);

        await CardCmd.Exhaust(choiceContext, chosen);

        Rng rng = Owner.RunState.Rng.CombatPotionGeneration;
        PotionModel? potion = PotionCatalog.Random(PotionCatalog.Query(orientation: orientation, rarity: rarity), rng)
            ?? PotionCatalog.Random(PotionCatalog.Query(orientation: orientation), rng);
        if (potion != null)
        {
            await Witch.ProducePotion(potion, Owner, Witch.PotionMode.Unstable);
        }
    }

    private static PotionOrientation OrientationFor(CardType type) => type switch
    {
        CardType.Attack => PotionOrientation.Offensive,
        CardType.Skill => PotionOrientation.Defensive,
        CardType.Power => PotionOrientation.Utility,
        _ => PotionOrientation.Utility,
    };

    private static PotionRarity RarityFor(CardRarity rarity) => rarity switch
    {
        CardRarity.Rare => PotionRarity.Rare,
        CardRarity.Uncommon => PotionRarity.Uncommon,
        _ => PotionRarity.Common,
    };

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
