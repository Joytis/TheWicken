using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class WickedBrew : OrientationBrewCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.KaiThePhaux;

    protected override PotionOrientation Orientation => PotionOrientation.Offensive;

    // Seeded from the old live query: every Common offensive potion the Witch could roll
    // (shared pool + Witch pool, no healers). Trim/add freely — this list IS the card's roll
    // pool and the upgraded card's selection grid.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<FirePotion>(),
        ModelDb.Potion<ExplosiveAmpoule>(),
        ModelDb.Potion<VulnerablePotion>(),
        ModelDb.Potion<StrengthPotion>(),
        ModelDb.Potion<FlexPotion>(),
        ModelDb.Potion<AttackPotion>(),
        ModelDb.Potion<CursedBottle>(),
    ];
}
