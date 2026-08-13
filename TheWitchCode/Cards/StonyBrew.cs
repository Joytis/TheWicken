using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class StonyBrew : OrientationBrewCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    protected override PotionOrientation Orientation => PotionOrientation.Defensive;

    // Seeded from the old live query: every Common defensive potion the Witch could roll
    // (shared pool, no healers). Trim/add freely — this list IS the card's roll pool and the
    // upgraded card's selection grid.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<BlockPotion>(),
        ModelDb.Potion<DexterityPotion>(),
        ModelDb.Potion<SkillPotion>(),
        ModelDb.Potion<SpeedPotion>(),
        ModelDb.Potion<WeakPotion>(),
        ModelDb.Potion<Fertilizer>(),
    ];
}
