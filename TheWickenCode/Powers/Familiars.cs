using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Random;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Query/mutate layer over a creature's <see cref="FamiliarPower" /> stacks — the single source of
/// truth for "how many familiars do I have". Cards that scale with familiars (Pillage, Stampede) read
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
}
