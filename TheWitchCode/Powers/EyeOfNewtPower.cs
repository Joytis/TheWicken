using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;
using Godot;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Eye of Newt: the owner's single-target potions hit every valid target — enemy potions hit ALL enemies,
/// player/ally potions hit ALL players (multiplayer only; in singleplayer there is just the one).
/// Passive toggle (Single stack); the actual fan-out lives in <see cref="EyeOfNewtPotionFanoutPatch" />.
/// </summary>
public sealed class EyeOfNewtPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>Protected-to-public bridge so the fan-out patch can flash the icon.</summary>
    public void FlashIcon() => Flash();
}

/// <summary>
/// No native hook can widen a potion's target set (`BeforePotionUsed` gets the target by value), so this
/// postfix wraps <c>PotionModel.OnUseWrapper</c>: after the original use resolves against the chosen
/// target, if the potion's owner has <see cref="EyeOfNewtPower" />, re-invoke the potion's protected
/// <c>OnUse</c> (virtual dispatch via reflection — the NeverendingPotionPower replay shape) once per
/// OTHER living creature in the potion's target set, each bracketed in Begin/EndCardOrPotionEffect.
/// The target set is chosen by <c>TargetType</c>: AnyEnemy fans across enemies, AnyPlayer/AnyAlly across
/// player creatures (a no-op in singleplayer, where there is only one player). Already-AoE types
/// (AllEnemies/AllAllies/Self) need no help and are skipped. Deterministic for MP: UsePotionAction is a
/// synced GameAction and the fan-out iterates the shared combat state's lists in order, no RNG. Each extra
/// target gets its own bottle-throw arc (see ThrowBottleAt) so the fan-out reads as a volley rather than
/// damage appearing from nowhere. Neverending Potion replays call OnUse directly (not OnUseWrapper), so
/// replayed potions do NOT fan out — accepted for now.
/// </summary>
[HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
public static class EyeOfNewtPotionFanoutPatch
{
    private static readonly MethodInfo OnUseMethod = AccessTools.Method(typeof(PotionModel), "OnUse");

    public static void Postfix(PotionModel __instance, PlayerChoiceContext choiceContext, Creature? target, ref Task __result)
    {
        __result = FanOut(__result, __instance, choiceContext, target);
    }

    private static async Task FanOut(Task original, PotionModel potion, PlayerChoiceContext choiceContext, Creature? target)
    {
        await original;

        if (target == null || !CombatManager.Instance.IsInProgress)
        {
            return;
        }
        if (potion.Owner is not { } owner)
        {
            return;
        }
        Creature ownerCreature = owner.Creature;
        if (ownerCreature.GetPower<EyeOfNewtPower>() is not { } power || ownerCreature.CombatState is not { } combatState)
        {
            return;
        }

        List<Creature> fanTargets = potion.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.ToList(),
            TargetType.AnyPlayer or TargetType.AnyAlly => combatState.PlayerCreatures.ToList(),
            _ => [], // already AoE (AllEnemies/AllAllies), self-only, or non-creature — nothing to widen
        };
        if (fanTargets.Count == 0)
        {
            return;
        }

        power.FlashIcon();
        foreach (Creature other in fanTargets)
        {
            if (other == target || !other.IsAlive)
            {
                continue;
            }
            
            await ThrowBottleAt(potion, ownerCreature, other);
            await Cmd.Wait(ThrowStagger);

            CombatId? combatId = CombatManager.Instance.BeginCardOrPotionEffect(owner);
            try
            {
                await (Task)OnUseMethod.Invoke(potion, [choiceContext, other])!;
            }
            finally
            {
                await CombatManager.Instance.EndCardOrPotionEffect(combatId, owner);
            }
        }
    }

    /// <summary>
    /// Replays the bottle-throw arc at a fan-out target, mirroring the NItemThrowVfx <c>OnUseWrapper</c>
    /// spawns for the originally chosen target. Without this the extra hits resolve with no visual at all.
    /// Waits only <see cref="ThrowStagger" /> rather than the bottle's full flight, so the arcs overlap into
    /// one volley instead of queueing up. TestMode has no scene tree, so skip it.
    /// </summary>
    private static async Task ThrowBottleAt(PotionModel potion, Creature thrower, Creature target)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (!TestMode.IsOff || room == null)
        {
            return;
        }

        Vector2 source = room.GetCreatureNode(thrower)?.VfxSpawnPosition ?? Vector2.Zero;
        Vector2 destination = room.GetCreatureNode(target)?.GetBottomOfHitbox() ?? Vector2.Zero;
        room.CombatVfxContainer.AddChildSafely(NItemThrowVfx.Create(source, destination, potion.Image));
    }

    /// <summary>Gap between successive throws — short enough that the arcs overlap in flight. (The base
    /// game waits 0.5f for a single bottle to land; staggering that per target would drag badly.)</summary>
    private const float ThrowStagger = 0.15f;
}
