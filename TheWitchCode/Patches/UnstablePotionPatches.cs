using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// UI surfacing for Unstable potions (see <see cref="UnstablePotions" />):
/// - appends the Unstable keyword tip to any marked potion's hover tips (belt hover + click popup
///   both read PotionModel.HoverTips, so one patch covers base-game and mod potions alike),
/// - keeps the ambient belt vfx in sync whenever a potion node (re)loads.
/// </summary>
public static class UnstablePotionPatches
{
    private static readonly AccessTools.FieldRef<NPotionContainer, List<NPotionHolder>> _holdersRef =
        AccessTools.FieldRefAccess<NPotionContainer, List<NPotionHolder>>("_holders");

    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.HoverTips), MethodType.Getter)]
    private static class HoverTipsPatch
    {
        private static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (__instance.IsMutable && UnstablePotions.IsUnstable(__instance))
            {
                __result = __result.Append(UnstablePotions.UnstableHoverTip);
            }
        }
    }

    [HarmonyPatch(typeof(NPotion), "Reload")]
    private static class AmbientVfxPatch
    {
        private static void Postfix(NPotion __instance, PotionModel? ____model)
        {
            if (____model != null && __instance.IsNodeReady())
            {
                SyncAmbientVfx(__instance, ____model);
            }
        }
    }

    /// <summary>The local player's belt node for this potion, if it's on screen right now.</summary>
    private static NPotion? FindBeltNode(PotionModel potion)
    {
        if (TestMode.IsOn || !LocalContext.IsMine(potion))
        {
            return null;
        }
        NPotionContainer? container = NRun.Instance?.GlobalUi.TopBar.PotionContainer;
        if (container == null)
        {
            return null;
        }
        return _holdersRef(container)
            .FirstOrDefault(h => h.Potion != null && h.Potion.Model == potion)?.Potion;
    }

    private static void SyncAmbientVfx(NPotion node, PotionModel potion)
    {
        NUnstablePotionVfx? existing = node.GetNodeOrNull<NUnstablePotionVfx>(NUnstablePotionVfx.NodeName);
        bool unstable = UnstablePotions.IsUnstable(potion);
        if (unstable && existing == null)
        {
            node.AddChild(NUnstablePotionVfx.Create(node));
        }
        else if (!unstable && existing != null)
        {
            existing.QueueFree();
        }
    }

    /// <summary>Called by UnstablePotions.Mark/Unmark for potions whose belt node already exists.</summary>
    public static void RefreshBeltNode(PotionModel potion)
    {
        NPotion? node = FindBeltNode(potion);
        if (node != null && node.IsNodeReady())
        {
            SyncAmbientVfx(node, potion);
        }
    }

    /// <summary>Rattles the bottle in the run-up to its explosion (local player only).</summary>
    public static void ShakeBeltNode(PotionModel potion)
    {
        FindBeltNode(potion)?
            .GetNodeOrNull<NUnstablePotionVfx>(NUnstablePotionVfx.NodeName)?
            .Shake();
    }

    /// <summary>
    /// Combat-end explosion: burst + sting on the potion's belt holder (local player only), then
    /// the bottle vanishes. Hiding it matters — the Discard() that follows tweens the bottle up
    /// and off the belt (NPotionHolder.DiscardPotion), which reads as the potion escaping rather
    /// than being destroyed. The burst lives on the holder, so it outlives the potion node.
    /// Must run BEFORE Discard(): that clears NPotionHolder.Potion, and the lookup goes with it.
    /// </summary>
    public static void PlayExplosion(PotionModel potion)
    {
        NPotion? node = FindBeltNode(potion);
        if (node?.GetParent() is NPotionHolder holder)
        {
            NUnstablePotionVfx.PlayExplosion(holder);
            node.Visible = false;
        }
    }
}
