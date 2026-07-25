using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Potions.Brewing;

/// <summary>
/// Query layer over every registered potion (<see cref="ModelDb.AllPotions" />), filtering by
/// <see cref="PotionOrientation" /> and <see cref="PotionRarity" />.
/// Returns canonical potion models; call <c>.ToMutable()</c> + <c>PotionCmd.TryToProcure</c> to actually grant one.
/// </summary>
public static class PotionCatalog
{
    /// <summary>Every registered potion, canonical instances.</summary>
    public static IEnumerable<PotionModel> All => ModelDb.AllPotions;

    /// <summary>
    /// The potions the Witch character can actually roll: the mod's own <see cref="WitchPotionPool" /> plus the
    /// base-game <see cref="SharedPotionPool" />. Excludes other characters' pools (Defect/Ironclad/Silent/...) and
    /// the Event/Token/Deprecated pools — mirrors how <c>PotionFactory.GetPotionOptions</c> builds a character's
    /// options. Use this for any "make a potion" effect so off-color potions never leak in.
    /// </summary>
    public static IEnumerable<PotionModel> WitchAndShared =>
        All.Where(p => p.Pool is WitchPotionPool or SharedPotionPool);

    /// <summary>
    /// Potions that can legitimately appear as a normal random result for the Witch: <see cref="WitchAndShared" />
    /// (so off-character potions never leak in) restricted to Common/Uncommon/Rare (so Token/Event payload-only
    /// potions are excluded). This is the pool every "make / brew / upgrade a potion" effect draws from.
    /// </summary>
    public static IEnumerable<PotionModel> Randomizable =>
        WitchAndShared.Where(p => p.Rarity is PotionRarity.Common or PotionRarity.Uncommon or PotionRarity.Rare);

    /// <summary>Master filter. All arguments are optional and AND-ed together.</summary>
    /// <param name="orientation">Restrict to this orientation if set.</param>
    /// <param name="rarity">Restrict to this rarity if set.</param>
    /// <param name="randomizableOnly">When true, exclude Token/Event payload-only potions.</param>
    /// <param name="excludeHealing">
    /// When true (default), exclude potions that can heal the player (<see cref="PotionTraits.IsHealing" />).
    /// Every Query caller is an in-combat "create a potion" effect, and the design rule is that combat-created
    /// potions can never heal — only pass false for a non-creation query.
    /// </param>
    public static IEnumerable<PotionModel> Query(
        PotionOrientation? orientation = null,
        PotionRarity? rarity = null,
        bool randomizableOnly = true,
        bool excludeHealing = true)
    {
        IEnumerable<PotionModel> q = randomizableOnly ? Randomizable : All;

        if (orientation.HasValue) q = q.Where(p => PotionTraits.OrientationOf(p) == orientation.Value);
        if (rarity.HasValue) q = q.Where(p => p.Rarity == rarity.Value);
        if (excludeHealing) q = q.Where(p => !PotionTraits.IsHealing(p));

        return q;
    }

    /// <summary>Pick a random potion from <paramref name="pool" />, or null if empty.</summary>
    public static PotionModel? Random(IEnumerable<PotionModel> pool, Rng rng)
    {
        List<PotionModel> list = pool.ToList();
        return list.Count == 0 ? null : rng.NextItem(list);
    }
}
