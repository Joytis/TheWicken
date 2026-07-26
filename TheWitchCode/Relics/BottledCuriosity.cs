using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Bottled Curiosity: whenever you enter an unknown ("?") map node, obtain a random potion.
/// Room-entry trigger is the base-game MealTicket shape; the "?" check is the map POINT type
/// (<see cref="MapPointType.Unknown" />), so it fires no matter what the node resolves into.
/// Rolls the Randomizable pool (Witch + Shared) with the game's own drop weights; healing
/// potions stay in — this is an out-of-combat reward, not a combat-creation effect.
/// </summary>
public sealed class BottledCuriosity : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner.Creature.IsDead || Owner.RunState.CurrentMapPoint?.PointType != MapPointType.Unknown)
        {
            return;
        }

        Rng rng = Owner.RunState.Rng.CombatPotionGeneration;
        // Base-game PotionFactory rarity thresholds: <=0.1 Rare, <=0.35 Uncommon, else Common.
        float roll = rng.NextFloat();
        PotionRarity rarity = roll <= 0.1f ? PotionRarity.Rare
            : roll <= 0.35f ? PotionRarity.Uncommon
            : PotionRarity.Common;
        PotionModel? potion = PotionCatalog.Random(PotionCatalog.Query(rarity: rarity, excludeHealing: false), rng)
            ?? PotionCatalog.Random(PotionCatalog.Query(excludeHealing: false), rng);
        if (potion != null)
        {
            Flash();
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }
}
