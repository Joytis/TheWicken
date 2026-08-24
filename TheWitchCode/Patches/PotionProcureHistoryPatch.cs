using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Combat-scoped record of how many potions each player procured, keyed by player net id. The game's
/// <see cref="CombatHistory" /> has no potion-procure entry (only <c>PotionUsedEntry</c>), and the
/// <c>AfterPotionProcured</c> hook only reaches models that are listening at the time — so a card that
/// enters combat late (generated, drafted, tutored from outside the deck) can never see earlier procures.
/// Recording centrally instead lets any instance read the true combat total.
/// Cleared alongside the real combat history, so lifetime matches one combat.
/// </summary>
public static class PotionProcureHistory
{
    private static readonly Dictionary<ulong, int> _countsByPlayer = new();

    public static int CountFor(Player? player)
    {
        if (player == null)
        {
            return 0;
        }
        return _countsByPlayer.TryGetValue(player.NetId, out int count) ? count : 0;
    }

    /// <summary>Potions procured this combat by ANY player (Bottle Bombardment).</summary>
    public static int CountAll() => _countsByPlayer.Values.Sum();

    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.Clear))]
    private static class ClearPatch
    {
        private static void Postfix() => _countsByPlayer.Clear();
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddPotionInternal))]
    private static class RecordProcurePatch
    {
        private static void Postfix(Player __instance, PotionProcureResult __result)
        {
            // Gate on IsInProgress, not just a live CombatState: players keep their CombatState reference
            // through the post-combat reward screen (it's only nulled on room Reset), and CombatHistory.Clear
            // fires at combat END — so a reward-screen potion pick would be recorded after the clear and leak
            // into the next combat. In MP, remote clients apply reward picks via a fire-and-forget
            // RewardSynchronizer message with arbitrary timing, so the leak differed per client → divergence.
            // IsInProgress flips true before BeforeCombatStart hooks (combat-start brews still count) and
            // false at EndCombatInternal, before rewards exist.
            if (!__result.success
                || !CombatManager.Instance.IsInProgress
                || __instance.Creature.CombatState?.IsLiveCombat() != true)
            {
                return;
            }
            _countsByPlayer[__instance.NetId] = CountFor(__instance) + 1;
        }
    }
}
