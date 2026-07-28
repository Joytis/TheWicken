using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class HerbalBrew : OrientationBrewCard
{
    protected override PotionOrientation Orientation => PotionOrientation.Utility;

    public HerbalBrew() : base(1, CardRarity.Uncommon)
    {
        
    }

    // First pass: every utility potion the Witch could previously roll (shared pool + Witch pool,
    // all rarities, no healers). Trim freely — this list IS the card's roll pool.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<PowerPotion>(),
        ModelDb.Potion<CureAll>(),
        ModelDb.Potion<SwiftPotion>(),
    ];


    // Potions only the upgraded card can brew — none yet; new potions land here.
    protected override IEnumerable<PotionModel> UpgradedExtras => [        
        ModelDb.Potion<RadiantTincture>(),
        ModelDb.Potion<DropletOfPrecognition>(),
    ];

    // Potions the upgraded card can NO LONGER brew — dropped from the base table on upgrade.
    protected override IEnumerable<PotionModel> UpgradedRemovals => [];
}
