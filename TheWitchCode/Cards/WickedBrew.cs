using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

public sealed class WickedBrew : OrientationBrewCard
{
    protected override PotionOrientation Orientation => PotionOrientation.Offensive;

    // First pass: every offensive potion the Witch could previously roll (shared pool + Witch pool,
    // all rarities, no healers). Trim freely — this list IS the card's roll pool.
    protected override IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<AttackPotion>(),
        ModelDb.Potion<ExplosiveAmpoule>(),
        ModelDb.Potion<FirePotion>(),
    ];


    // Potions only the upgraded card can brew — none yet; new potions land here.
    protected override IEnumerable<PotionModel> UpgradedExtras => [
        ModelDb.Potion<PowderedDemise>(),
        ModelDb.Potion<StrengthPotion>(),
    ];

    // Potions the upgraded card can NO LONGER brew — dropped from the base table on upgrade.
    protected override IEnumerable<PotionModel> UpgradedRemovals => [ ];
}
