using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Random;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Witch-only quality of life: right-clicking a belt potion uses it immediately, skipping the
/// Use/Discard popup and (for enemy-targeted potions) the targeting arrow:
/// - enemy-targeted (AnyEnemy) -> thrown at a random living enemy,
/// - player-targeted (AnyPlayer/Self) and untargeted -> used instantly
///   (EnqueueManualUse(null) auto-fills the owner when it's a valid target),
/// - oddballs that genuinely need a pick (TargetedNoCreature merchant throw, AnyAlly) -> fall back
///   to the normal targeting flow, still skipping the popup.
/// Left-click behavior (popup with Use/Discard) is untouched, so Discard stays reachable and
/// nothing changes for other characters.
/// </summary>
public static class PotionQuickUsePatch
{
    private static readonly AccessTools.FieldRef<NPotionHolder, bool> _isUsableRef =
        AccessTools.FieldRefAccess<NPotionHolder, bool>("_isUsable");

    private static readonly AccessTools.FieldRef<NPotionHolder, bool> _disabledRef =
        AccessTools.FieldRefAccess<NPotionHolder, bool>("_disabledUntilPotionRemoved");

    [HarmonyPatch(typeof(NPotionHolder), "_Ready")]
    private static class HolderReadyPatch
    {
        private static void Postfix(NPotionHolder __instance)
        {
            __instance.Connect(NClickableControl.SignalName.MouseReleased,
                Callable.From<InputEvent>(evt => OnMouseReleased(__instance, evt)));
        }
    }

    private static void OnMouseReleased(NPotionHolder holder, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Right } button
            || button.IsPressed()
            || !CanQuickUse(holder))
        {
            return;
        }
        holder.GetViewport()?.SetInputAsHandled();
        NHoverTipSet.Remove(holder);
        PotionModel potion = holder.Potion!.Model;
        switch (potion.TargetType)
        {
            case TargetType.AnyEnemy:
            {
                Creature owner = potion.Owner.Creature;
                var enemies = owner.CombatState?.GetOpponentsOf(owner).Where(c => c.IsAlive).ToList();
                if (enemies == null || enemies.Count == 0)
                {
                    return;
                }
                UseAt(holder, potion, Rng.Chaotic.NextItem(enemies));
                break;
            }
            case TargetType.TargetedNoCreature:
            case TargetType.AnyAlly:
                // No sensible auto-target; skip the popup but keep the normal targeting flow.
                TaskHelper.RunSafely(UseWithTargeting(holder, potion));
                break;
            default:
                UseAt(holder, potion, null);
                break;
        }
    }

    private static void UseAt(NPotionHolder holder, PotionModel potion, Creature? target)
    {
        holder.DisableUntilPotionRemoved();
        potion.EnqueueManualUse(target);
    }

    private static async System.Threading.Tasks.Task UseWithTargeting(NPotionHolder holder, PotionModel potion)
    {
        potion.BeforeUse += DisableHolder;
        try
        {
            await holder.UsePotion();
        }
        finally
        {
            potion.BeforeUse -= DisableHolder;
        }
        void DisableHolder() => holder.DisableUntilPotionRemoved();
    }

    /// <summary>Mirrors NPotionPopup._Ready/RefreshButtons gating for the Use button.</summary>
    private static bool CanQuickUse(NPotionHolder holder)
    {
        PotionModel? potion = holder.Potion?.Model;
        if (potion == null || potion.IsQueued || !_isUsableRef(holder) || _disabledRef(holder))
        {
            return false;
        }
        if (potion.Owner.Character is not Character.Witch)
        {
            return false;
        }
        Creature owner = potion.Owner.Creature;
        if (potion.Owner.RunState.IsGameOver || owner.IsDead || !potion.Owner.CanRemovePotions
            || !potion.PassesCustomUsabilityCheck)
        {
            return false;
        }
        switch (potion.Usage)
        {
            case PotionUsage.AnyTime:
                return true;
            case PotionUsage.CombatOnly:
                return CombatManager.Instance.IsInProgress
                    && owner.CombatState?.CurrentSide == owner.Side
                    && owner.IsAlive
                    && !CombatManager.Instance.PlayerActionsDisabled
                    && !InACardSelectScreen
                    && NOverlayStack.Instance?.ScreenCount is null or <= 0
                    && NCapstoneContainer.Instance?.InUse != true;
            default:
                return false;
        }
    }

    private static bool InACardSelectScreen =>
        NPlayerHand.Instance?.IsInCardSelection == true
        || NOverlayStack.Instance?.Peek() is ICardSelector;
}
