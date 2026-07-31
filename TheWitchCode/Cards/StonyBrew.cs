using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class StonyBrew : OrientationBrewCard
{
    protected override PotionOrientation Orientation => PotionOrientation.Defensive;

    // First pass: every defensive potion the Witch could previously roll (shared pool + Witch pool,
    // all rarities, no healers). Trim freely — this list IS the card's roll pool.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<SkillPotion>(),
        ModelDb.Potion<BlockPotion>(),
    ];


    // Potions only the upgraded card can brew — none yet; new potions land here.
    protected override IEnumerable<PotionModel> UpgradedExtras => [
        ModelDb.Potion<HeartOfIron>(),
        ModelDb.Potion<ShipInABottle>(),
    ];

    // Potions the upgraded card can NO LONGER brew — dropped from the base table on upgrade.
    protected override IEnumerable<PotionModel> UpgradedRemovals => [ ];
}
