using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Unstable: a per-instance mark on any potion (base-game included). At the end of combat the
/// potion explodes — purely thematic, it's simply removed from the belt.
/// Mark a potion right after procuring it:
/// <code>
///     var result = await PotionCmd.TryToProcure(potion, player);
///     if (result.success) UnstablePotions.Mark(result.potion);
/// </code>
/// Tooltip + belt vfx ride on the mark via Harmony patches (see UnstablePotionPatches);
/// the combat-end explosion is driven by <see cref="UnstablePotionSweeperModel" />.
/// </summary>
public static class UnstablePotions
{
    /// <summary>Marked mutable potion instances. Weak so discarded/used potions don't leak.</summary>
    private static readonly ConditionalWeakTable<PotionModel, object> _marks = [];

    public static HoverTip UnstableHoverTip => new(
        new LocString("static_hover_tips", "THEWITCH-UNSTABLE.title"),
        new LocString("static_hover_tips", "THEWITCH-UNSTABLE.description"));

    public static void Mark(PotionModel potion)
    {
        potion.AssertMutable();
        _marks.GetValue(potion, _ => new object());
        Patches.UnstablePotionPatches.RefreshBeltNode(potion);
    }

    public static void Unmark(PotionModel potion)
    {
        _marks.Remove(potion);
        Patches.UnstablePotionPatches.RefreshBeltNode(potion);
    }

    public static bool IsUnstable(PotionModel potion) => _marks.TryGetValue(potion, out _);

    /// <summary>Comma-joined slot indices of this player's Unstable potions (run-save persistence).</summary>
    public static string SaveState(Player player) =>
        string.Join(",", player.PotionSlots
            .Select((p, i) => p != null && IsUnstable(p) ? i.ToString() : null)
            .OfType<string>());

    public static void RestoreState(Player player, string csv)
    {
        foreach (string entry in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(entry, out int slot) && slot >= 0 && slot < player.PotionSlots.Count
                && player.PotionSlots[slot] is { } potion)
            {
                Mark(potion);
            }
        }
    }
}

/// <summary>
/// Always-on hook listener that explodes Unstable potions when combat ends.
/// Auto-instantiated by ModelDb (like all AbstractModel subtypes in mod assemblies) and fed into
/// every run's hook iteration via ModHelper.SubscribeForRunStateHooks (registered in MainFile).
/// </summary>
public sealed class UnstablePotionSweeperModel : AbstractModel
{
    public override bool ShouldReceiveCombatHooks => true;

    public static void Register()
    {
        UnstablePotionSweeperModel? instance = null;
        ModHelper.SubscribeForRunStateHooks($"{MainFile.ModId}-UnstablePotions", _ =>
        {
            instance ??= ModelDb.GetByIdOrNull<UnstablePotionSweeperModel>(
                ModelDb.GetId<UnstablePotionSweeperModel>());
            return instance == null ? [] : new AbstractModel[] { instance };
        });
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        foreach (Player player in room.CombatState.RunState.Players)
        {
            // Snapshot: Discard() nulls out belt slots while we iterate.
            List<PotionModel> exploding = player.PotionSlots
                .Where(p => p != null && UnstablePotions.IsUnstable(p))
                .Select(p => p!)
                .ToList();
            foreach (PotionModel potion in exploding)
            {
                Patches.UnstablePotionPatches.PlayExplosion(potion);
                potion.Discard();
                await Cmd.Wait(0.1f);
            }
        }
    }
}
