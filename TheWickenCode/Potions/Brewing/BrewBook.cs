using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWicken.TheWickenCode.Potions.Brewing;

/// <summary>How the brew chose its output potion. Useful for tuning / dev console output.</summary>
public enum BrewMatchKind
{
    /// <summary>No candidate found at all (shouldn't happen if any randomizable potion exists at the rarity).</summary>
    None,
    /// <summary>A potion matching the full union of both inputs' traits was found (primary path).</summary>
    TraitUnion,
    /// <summary>Fell back to a potion sharing one of the inputs' orientations.</summary>
    SharedOrientation,
    /// <summary>Last resort: any potion of the output rarity.</summary>
    RarityOnly,
}

/// <summary>Outcome of a brew. <see cref="Potion" /> is a canonical model; mutate + procure to actually grant it.</summary>
public readonly struct BrewResult
{
    public readonly PotionModel? Potion;
    public readonly PotionRarity Rarity;
    public readonly PotionTrait CombinedTraits;
    public readonly BrewMatchKind MatchKind;

    public BrewResult(PotionModel? potion, PotionRarity rarity, PotionTrait combinedTraits, BrewMatchKind matchKind)
    {
        Potion = potion;
        Rarity = rarity;
        CombinedTraits = combinedTraits;
        MatchKind = matchKind;
    }

    public bool Success => Potion != null;
}

/// <summary>
/// Brewing rules (skeleton). Recipes are RULE-based, not keyed to specific potion ids, so the space stays additive
/// rather than multiplicative:
///   * Inputs: exactly 2 potions.
///   * Output rarity: the higher input rarity, bumped up one tier (capped at Rare).
///   * Output traits: the UNION of both inputs' traits is sought first; if nothing matches, fall back to a potion
///     that shares an orientation with an input; last resort, any potion of the output rarity.
///   * Output potion: a real existing potion drawn from <see cref="PotionCatalog" /> (no bespoke fused potions yet).
/// </summary>
public static class BrewBook
{
    /// <summary>Combine two potions into a higher-rarity result. Returns <see cref="BrewResult.Success" /> = false if
    /// no candidate exists (e.g. no randomizable potion of the output rarity is registered).</summary>
    public static BrewResult Brew(PotionModel a, PotionModel b, Rng rng)
    {
        PotionRarity outRarity = NextRarity(a.Rarity, b.Rarity);
        PotionTrait union = PotionTraits.Of(a) | PotionTraits.Of(b);

        // Pool of legitimate, distinct-from-inputs candidates at the target rarity.
        List<PotionModel> pool = PotionCatalog
            .Query(rarity: outRarity, randomizableOnly: true)
            .Where(p => !IsInput(p, a, b))
            .ToList();

        // 1. Primary: a potion carrying the full union of input traits.
        List<PotionModel> candidates = pool.Where(p => HasAll(p, union)).ToList();
        BrewMatchKind kind = BrewMatchKind.TraitUnion;

        // 2. Fallback: shares an orientation with either input.
        if (candidates.Count == 0)
        {
            HashSet<PotionOrientation> allowed = new()
            {
                PotionTraits.OrientationOf(a),
                PotionTraits.OrientationOf(b),
            };
            allowed.Remove(PotionOrientation.Neutral);

            candidates = pool.Where(p => allowed.Contains(PotionTraits.OrientationOf(p))).ToList();
            kind = BrewMatchKind.SharedOrientation;
        }

        // 3. Last resort: anything of the right rarity.
        if (candidates.Count == 0)
        {
            candidates = pool;
            kind = BrewMatchKind.RarityOnly;
        }

        PotionModel? pick = PotionCatalog.Random(candidates, rng);
        return new BrewResult(pick, outRarity, union, pick == null ? BrewMatchKind.None : kind);
    }

    /// <summary>The higher of two rarities bumped one tier, capped at Rare. Always yields at least Uncommon.</summary>
    public static PotionRarity NextRarity(PotionRarity a, PotionRarity b)
    {
        int top = Math.Max(Rank(a), Rank(b));
        int next = Math.Min(top + 1, 3);
        return next switch
        {
            >= 3 => PotionRarity.Rare,
            _ => PotionRarity.Uncommon,
        };
    }

    // Common=1, Uncommon=2, Rare=3. None/Event/Token treated as Common-tier so they still brew sensibly.
    private static int Rank(PotionRarity rarity) => rarity switch
    {
        PotionRarity.Uncommon => 2,
        PotionRarity.Rare => 3,
        _ => 1,
    };

    private static bool IsInput(PotionModel candidate, PotionModel a, PotionModel b)
    {
        Type t = candidate.GetType();
        return t == a.GetType() || t == b.GetType();
    }

    private static bool HasAll(PotionModel potion, PotionTrait traits) =>
        (PotionTraits.Of(potion) & traits) == traits;
}
