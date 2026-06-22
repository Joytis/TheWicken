using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWicken.TheWickenCode.Potions.Brewing;

/// <summary>
/// Query layer over every registered potion (<see cref="ModelDb.AllPotions" />), filtering by
/// <see cref="PotionTrait" />, <see cref="PotionRarity" />, <see cref="PotionUsage" /> and orientation.
/// Returns canonical potion models; call <c>.ToMutable()</c> + <c>PotionCmd.TryToProcure</c> to actually grant one.
/// </summary>
public static class PotionCatalog
{
    /// <summary>Every registered potion, canonical instances.</summary>
    public static IEnumerable<PotionModel> All => ModelDb.AllPotions;

    /// <summary>
    /// Potions that can legitimately appear as a normal random result (Common/Uncommon/Rare).
    /// Excludes Token/Event payload-only potions and None.
    /// </summary>
    public static IEnumerable<PotionModel> Randomizable =>
        All.Where(p => p.Rarity is PotionRarity.Common or PotionRarity.Uncommon or PotionRarity.Rare);

    /// <summary>Potions whose traits contain ALL of <paramref name="traits" />.</summary>
    public static IEnumerable<PotionModel> WithAll(PotionTrait traits) =>
        All.Where(p => HasAll(p, traits));

    /// <summary>Potions whose traits contain ANY of <paramref name="traits" />.</summary>
    public static IEnumerable<PotionModel> WithAny(PotionTrait traits) =>
        All.Where(p => (PotionTraits.Of(p) & traits) != 0);

    public static IEnumerable<PotionModel> OfRarity(PotionRarity rarity) =>
        All.Where(p => p.Rarity == rarity);

    public static IEnumerable<PotionModel> OfOrientation(PotionOrientation orientation) =>
        All.Where(p => PotionTraits.OrientationOf(p) == orientation);

    /// <summary>
    /// Master filter. All arguments are optional and AND-ed together.
    /// </summary>
    /// <param name="require">Trait bits to require. <see cref="PotionTrait.None" /> means "don't filter by trait".</param>
    /// <param name="matchAll">When true, a potion must have ALL <paramref name="require" /> bits; when false, ANY.</param>
    /// <param name="rarity">Restrict to this rarity if set.</param>
    /// <param name="usage">Restrict to this usage if set.</param>
    /// <param name="randomizableOnly">When true, exclude Token/Event payload-only potions.</param>
    public static IEnumerable<PotionModel> Query(
        PotionTrait require = PotionTrait.None,
        bool matchAll = true,
        PotionRarity? rarity = null,
        PotionUsage? usage = null,
        bool randomizableOnly = true)
    {
        IEnumerable<PotionModel> q = randomizableOnly ? Randomizable : All;

        if (require != PotionTrait.None)
        {
            q = matchAll
                ? q.Where(p => HasAll(p, require))
                : q.Where(p => (PotionTraits.Of(p) & require) != 0);
        }

        if (rarity.HasValue) q = q.Where(p => p.Rarity == rarity.Value);
        if (usage.HasValue) q = q.Where(p => p.Usage == usage.Value);

        return q;
    }

    /// <summary>Pick a random potion from <paramref name="pool" />, or null if empty.</summary>
    public static PotionModel? Random(IEnumerable<PotionModel> pool, Rng rng)
    {
        List<PotionModel> list = pool.ToList();
        return list.Count == 0 ? null : rng.NextItem(list);
    }

    private static bool HasAll(PotionModel potion, PotionTrait traits) =>
        (PotionTraits.Of(potion) & traits) == traits;
}
