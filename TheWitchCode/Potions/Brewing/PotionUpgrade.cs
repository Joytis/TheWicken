using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWitch.TheWitchCode.Potions.Brewing;

/// <summary>
/// "Distill a potion": replace a belt potion with a random RARE potion of the same orientation.
/// The input itself is excluded from the roll, and healing potions (<see cref="PotionTraits.IsHealing" />)
/// never come out. If the orientation has no eligible Rare, any eligible Rare is used instead (mirrors
/// the BrewBook same-orientation fallback). Used by the Distill card (which picks the input via the
/// potion-select overlay).
/// </summary>
public static class PotionUpgrade
{
    /// <summary>Distill <paramref name="target" /> (a potion on the player's belt) to a Rare.</summary>
    public static async Task UpgradePotion(PlayerChoiceContext context, Player player, PotionModel target, Rng rng)
    {
        PotionOrientation orientation = PotionTraits.OrientationOf(target);
        List<PotionModel> pool = PotionCatalog.Query(orientation: orientation, rarity: PotionRarity.Rare)
            .Where(p => p.GetType() != target.GetType())
            .ToList();
        if (pool.Count == 0)
        {
            pool = PotionCatalog.Query(rarity: PotionRarity.Rare)
                .Where(p => p.GetType() != target.GetType())
                .ToList();
        }

        PotionModel? result = await PotionCatalog.Pick(pool, context, player, rng);
        if (result == null)
        {
            return; // nothing eligible to distill into — leave the potion as-is.
        }

        await PotionCmd.Discard(target);
        await PotionCmd.TryToProcure(result.ToMutable(), player);
    }
}
