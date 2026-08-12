using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Shared base for the "brew a potion of orientation X" trio (Wicked / Stony / Herbal Brew). Each card
/// CREATES an Unstable potion rolled from its HARD-CODED loot table (hand-tuned per card — trim/add
/// freely). The card Exhausts; upgrading removes Exhaust. The player does not pick the result — only
/// the Separatory Funnel relic turns the roll into a choice (see <see cref="PotionCatalog.Pick" />).
/// The Gather Herbs next-is-Rare buff restricts the roll to the table's Rare entries (consumed only
/// when the table has any — inert while tables are all-Common).
/// </summary>
public abstract class OrientationBrewCard : WitchCard
{
    protected abstract PotionOrientation Orientation { get; }

    /// <summary>The card's roll pool (canonical models).</summary>
    protected abstract IEnumerable<PotionModel> LootTable { get; }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected OrientationBrewCard(int energyCost = 1, CardRarity rarity = CardRarity.Common)
        : base(energyCost, CardType.Skill, rarity, TargetType.Self)
    {
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        List<PotionModel> pool = LootTable.ToList();
        List<PotionModel> rares = pool.Where(p => p.Rarity == PotionRarity.Rare).ToList();
        if (rares.Count > 0 && await NextPotionRarePower.TryConsume(Owner))
        {
            pool = rares;
        }

        PotionModel? potion = await PotionCatalog.Pick(
            pool, choiceContext, Owner, Owner.RunState.Rng.CombatPotionGeneration);

        if (potion != null)
        {
            // Orientation-coded splash: red = offensive, blue = defensive, green = utility.
            WitchFx.Splash(Owner.Creature, Orientation switch
            {
                PotionOrientation.Offensive => new Godot.Color("d04545"),
                PotionOrientation.Defensive => new Godot.Color("4a7bd0"),
                _ => WitchFx.WitchGreen,
            });

            await Witch.ProducePotion(potion, Owner, Witch.PotionMode.Unstable);
        }
    }
}
