using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TheWitch.TheWitchCode.Potions.Treasures;

/// <summary>
/// Controller support for throwing a Treasure at the merchant. The base game's merchant-throw focus
/// handling in <c>NPotionHolder.TargetNode</c> is hard-gated on <c>Potion.Model is FoulPotion</c>, so a
/// Treasure never grabs the merchant button's focus; the confirm press then falls through to
/// <c>NMerchantButton</c>'s global <c>select</c> hotkey and opens the shop instead. This postfix on
/// <c>NTargetManager.StartTargeting</c> replicates the FoulPotion branch (enable + focus the merchant
/// button for the duration of targeting) whenever the targeting was started by a potion holder carrying
/// a <see cref="TreasurePotion" />. The holder is recovered from the exit-early delegate's target
/// (<c>NPotionHolder.ShouldCancelTargeting</c> is an instance method).
/// </summary>
[HarmonyPatch(typeof(NTargetManager), nameof(NTargetManager.StartTargeting),
    typeof(TargetType), typeof(Vector2), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>))]
public static class TreasureControllerThrowPatch
{
    public static void Postfix(NTargetManager __instance, TargetMode startingMode, Func<bool>? exitEarlyCondition)
    {
        if (startingMode != TargetMode.Controller
            || CombatManager.Instance.IsInProgress
            || exitEarlyCondition?.Target is not NPotionHolder holder
            || holder.Potion?.Model is not TreasurePotion treasure)
        {
            return;
        }

        if (treasure.Owner.RunState.CurrentRoom is not { } room)
        {
            return;
        }
        (NMerchantButton? button, Control? screenContext) = FoulPotion.GetFoulPotionMerchantTarget(room);
        if (button == null)
        {
            return;
        }

        Control.FocusBehaviorRecursiveEnum? savedFocusBehavior = null;
        if (screenContext != null)
        {
            savedFocusBehavior = screenContext.FocusBehaviorRecursive;
            screenContext.FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Enabled;
        }
        bool buttonWasDisabled = !button.IsEnabled;
        if (buttonWasDisabled)
        {
            button.Enable();
        }
        button.SetFocusMode(Control.FocusModeEnum.All);
        button.TryGrabFocus();

        // Teardown mirrors the finally block of the base-game FoulPotion branch.
        __instance.Connect(NTargetManager.SignalName.TargetingEnded, Callable.From(() =>
        {
            button.SetFocusMode(Control.FocusModeEnum.None);
            if (buttonWasDisabled)
            {
                button.Disable();
            }
            if (screenContext != null && savedFocusBehavior.HasValue)
            {
                screenContext.FocusBehaviorRecursive = savedFocusBehavior.Value;
            }
        }), (uint)GodotObject.ConnectFlags.OneShot);
    }
}

/// <summary>
/// Defensive guard: while potion targeting is active, a merchant-button press that did NOT come from the
/// focused-while-targeting path must never open the shop overlay (that is exactly the fallthrough that
/// caused the bug). Swallow it instead.
/// </summary>
[HarmonyPatch(typeof(NMerchantButton), "OnRelease")]
public static class TreasureMerchantReleaseGuardPatch
{
    private static readonly AccessTools.FieldRef<NMerchantButton, bool> _focusedWhileTargeting =
        AccessTools.FieldRefAccess<NMerchantButton, bool>("_focusedWhileTargeting");

    public static bool Prefix(NMerchantButton __instance)
    {
        return !NTargetManager.Instance.IsInSelection || _focusedWhileTargeting(__instance);
    }
}
