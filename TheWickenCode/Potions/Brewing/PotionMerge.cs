using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWicken.TheWickenCode.Potions.Brewing;

/// <summary>What a merge attempt does to the player's belt.</summary>
public enum MergeKind
{
    /// <summary>Belt is empty — nothing happens.</summary>
    NothingToMerge,
    /// <summary>Exactly one potion held — it is replaced with a <see cref="WickedBrew" />.</summary>
    SingleToWickedBrew,
    /// <summary>Two potions are brewed into a higher-rarity result (see <see cref="MergeOutcome.Brew" />).</summary>
    Brewed,
}

/// <summary>
/// Result of planning a merge. Produced synchronously by <see cref="PotionMerge.Plan" /> (it locks in the brewed
/// potion, including the RNG roll), then carried out by <see cref="PotionMerge.Apply" />. Holds a human-readable
/// <see cref="Description" /> for console / debug output.
/// </summary>
public readonly struct MergeOutcome
{
    public readonly MergeKind Kind;
    public readonly PotionModel? InputA;
    public readonly PotionModel? InputB;
    public readonly BrewResult Brew;
    public readonly string Description;

    public MergeOutcome(MergeKind kind, PotionModel? inputA, PotionModel? inputB, BrewResult brew, string description)
    {
        Kind = kind;
        InputA = inputA;
        InputB = inputB;
        Brew = brew;
        Description = description;
    }

    /// <summary>True unless there was nothing to merge.</summary>
    public bool WillChangeBelt => Kind != MergeKind.NothingToMerge;
}

/// <summary>
/// Shared "merge two potions" behavior, used by both the <c>mergepotions</c> dev command and the Brew card.
///
/// Rules mirror the brewing design: take the first two belt potions and brew them (<see cref="BrewBook" />);
/// with a single potion, replace it with a <see cref="WickedBrew" />; with none, do nothing.
/// </summary>
public static class PotionMerge
{
    /// <summary>
    /// Decide (without mutating state) what merging <paramref name="player" />'s first two belt potions would do.
    /// The brewed result — including its random pick — is resolved here so the plan and its later
    /// <see cref="Apply" /> agree.
    /// </summary>
    public static MergeOutcome Plan(Player player, Rng rng)
    {
        var potions = player.Potions.ToList();

        if (potions.Count == 0)
        {
            return new MergeOutcome(MergeKind.NothingToMerge, null, null, default, "No potions in belt to merge.");
        }

        if (potions.Count == 1)
        {
            return new MergeOutcome(MergeKind.SingleToWickedBrew, potions[0], null, default,
                $"Only one potion ({potions[0].Id.Entry}) — replacing with WICKED_BREW.");
        }

        PotionModel a = potions[0];
        PotionModel b = potions[1];
        BrewResult brew = BrewBook.Brew(a, b, rng);

        string description = brew.Success
            ? $"Merged {a.Id.Entry} + {b.Id.Entry} -> {brew.Potion!.Id.Entry} ({brew.Rarity}, via {brew.MatchKind})."
            : $"Merged {a.Id.Entry} + {b.Id.Entry} -> no candidate; granting WICKED_BREW.";

        return new MergeOutcome(MergeKind.Brewed, a, b, brew, description);
    }

    /// <summary>Carry out a planned merge against the player's belt (discards inputs, procures the result).</summary>
    public static async Task Apply(Player player, MergeOutcome outcome)
    {
        switch (outcome.Kind)
        {
            case MergeKind.SingleToWickedBrew:
                await PotionCmd.Discard(outcome.InputA!);
                await PotionCmd.TryToProcure<WickedBrew>(player);
                break;

            case MergeKind.Brewed:
                await PotionCmd.Discard(outcome.InputA!);
                await PotionCmd.Discard(outcome.InputB!);
                if (outcome.Brew.Success)
                {
                    await PotionCmd.TryToProcure(outcome.Brew.Potion!.ToMutable(), player);
                }
                else
                {
                    await PotionCmd.TryToProcure<WickedBrew>(player);
                }
                break;

            case MergeKind.NothingToMerge:
            default:
                break;
        }
    }

    /// <summary>Plan and apply a merge in one call. Returns the outcome that was applied.</summary>
    public static async Task<MergeOutcome> MergeBeltPotions(Player player, Rng rng)
    {
        MergeOutcome outcome = Plan(player, rng);
        await Apply(player, outcome);
        return outcome;
    }
}
