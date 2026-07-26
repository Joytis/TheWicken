using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Shared base for the "brew a potion of orientation X" trio (Wicked / Stony / Herbal Brew). Each card
/// rolls from its own HARD-CODED loot table (not a live catalog query) so the pool is tuned per card.
/// Hovering the card previews every potion in its table; the upgraded card rolls (and previews) the
/// table plus <see cref="UpgradedExtras" />. The Gather Herbs next-is-Rare buff is honored by
/// restricting the roll to the table's Rare entries (consumed only when the table has any).
/// </summary>
public abstract class OrientationBrewCard : WitchCard
{
    protected abstract PotionOrientation Orientation { get; }

    /// <summary>The card's roll table (canonical models). Hand-curated — trim/add freely per card.</summary>
    protected abstract IEnumerable<PotionModel> LootTable { get; }

    /// <summary>Entries the upgraded card ADDS to <see cref="LootTable" />.</summary>
    protected virtual IEnumerable<PotionModel> UpgradedExtras => [];

    /// <summary>Entries the upgraded card REMOVES from <see cref="LootTable" />.</summary>
    protected virtual IEnumerable<PotionModel> UpgradedRemovals => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => Witch.Turbo ? [] : [CardKeyword.Exhaust];

    private IEnumerable<PotionModel> CurrentTable =>
        IsUpgraded ? LootTable.Except(UpgradedRemovals).Concat(UpgradedExtras) : LootTable;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        CurrentTable.Select(HoverTipFactory.FromPotion);


    protected OrientationBrewCard(int energyCost = 1)
        : base(energyCost, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        List<PotionModel> table = CurrentTable.ToList();
        List<PotionModel> rares = table.Where(p => p.Rarity == PotionRarity.Rare).ToList();
        if (rares.Count > 0 && await NextPotionRarePower.TryConsume(Owner))
        {
            table = rares;
        }

        PotionModel? potion = PotionCatalog.Random(table, Owner.RunState.Rng.CombatPotionGeneration);
        if (potion != null)
        {
            // Orientation-coded splash: red = offensive, blue = defensive, green = utility.
            WitchFx.Splash(Owner.Creature, Orientation switch
            {
                PotionOrientation.Offensive => new Godot.Color("d04545"),
                PotionOrientation.Defensive => new Godot.Color("4a7bd0"),
                _ => WitchFx.WitchGreen,
            });
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }
}
