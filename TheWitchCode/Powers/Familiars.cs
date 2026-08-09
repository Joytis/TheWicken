using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Random;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Query/mutate layer over a creature's <see cref="FamiliarPower" /> stacks — the single source of
/// truth for "how many familiars do I have". Cards that scale with familiars (Pick Clean, Overrun) read
/// <see cref="Count" />; cards that consume one (Ritual Sacrifice) call <see cref="RemoveRandom" />.
/// </summary>
public static class Familiars
{
    /// <summary>All familiar powers currently on the creature (one entry per familiar type present).</summary>
    public static IReadOnlyList<FamiliarPower> On(Creature creature) =>
        creature.Powers.OfType<FamiliarPower>().ToList();

    /// <summary>Total familiar count = sum of all familiar power stacks.</summary>
    public static int Count(Creature creature) =>
        creature.Powers.OfType<FamiliarPower>().Sum(p => p.Amount);

    public static bool Any(Creature creature) =>
        creature.Powers.OfType<FamiliarPower>().Any();

    /// <summary>
    /// Sacrifice one familiar: pick a random familiar power present and decrement it by one
    /// (the power auto-removes if it hits zero). Returns false if the creature has no familiars.
    /// </summary>
    public static async Task<bool> RemoveRandom(Creature creature, Rng rng)
    {
        List<FamiliarPower> familiars = creature.Powers.OfType<FamiliarPower>().ToList();
        FamiliarPower? chosen = familiars.Count == 0 ? null : rng.NextItem(familiars);
        if (chosen == null)
        {
            return false;
        }

        await PowerCmd.Decrement(chosen);
        return true;
    }

    /// <summary>
    /// Sacrifice every familiar at once (Broken Pact): removes each familiar power outright and returns
    /// the total number of stacks that were lost, for callers that pay out per familiar.
    /// </summary>
    public static async Task<int> RemoveAll(Creature creature)
    {
        List<FamiliarPower> familiars = creature.Powers.OfType<FamiliarPower>().ToList();
        int sacrificed = familiars.Sum(p => p.Amount);
        foreach (FamiliarPower familiar in familiars)
        {
            await PowerCmd.Remove(familiar);
        }
        return sacrificed;
    }
}
