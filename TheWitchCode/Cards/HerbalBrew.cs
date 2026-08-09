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

    // Seeded from the old live query: every Common utility potion the Witch could roll
    // (shared pool, no healers). Trim/add freely — this list IS the card's roll pool and the
    // upgraded card's selection grid.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<SwiftPotion>(),
        ModelDb.Potion<ColorlessPotion>(),
        ModelDb.Potion<PowerPotion>(),
        ModelDb.Potion<CureAll>(),
        ModelDb.Potion<Clarity>(),
        ModelDb.Potion<StableSerum>(),
    ];
}
