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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.TestSupport;
using Godot;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Eye of Newt: the owner's single-target potions hit one additional random target from the potion's
/// target set — or, once upgraded (HitsAll), every valid target: enemy potions across ALL enemies,
/// player/ally potions across ALL players (multiplayer only; in singleplayer there is just the one).
/// Passive toggle (Single stack); the actual fan-out lives in <see cref="EyeOfNewtPotionFanoutPatch" />.
/// </summary>
public sealed class EyeOfNewtPower : WitchPower
{
    private const string HitsAllKey = "HitsAll";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(HitsAllKey, 0m)
    ];

    /// <summary>True once applied by an upgraded Eye of Newt: fan out to ALL valid targets instead of one
    /// random extra. A DynamicVar (not a field) so the power tooltip can switch text on it.</summary>
    public bool HitsAll => DynamicVars[HitsAllKey].BaseValue > 0m;

    /// <summary>One-way: an upgraded application never downgrades back to single-extra-target.</summary>
    public void EnableHitsAll() => DynamicVars[HitsAllKey].BaseValue = 1m;

    /// <summary>Protected-to-public bridge so the fan-out patch can flash the icon.</summary>
    public void FlashIcon() => Flash();
}

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

        List<Creature> fanTargets = (potion.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies,
            TargetType.AnyPlayer or TargetType.AnyAlly => combatState.PlayerCreatures,
            _ => [], // already AoE (AllEnemies/AllAllies), self-only, or non-creature — nothing to widen
        }).Where(other => other != target && other.IsAlive).ToList();
        if (fanTargets.Count == 0)
        {
            return;
        }

        if (!power.HitsAll)
        {
            // Unupgraded: exactly one extra hit, on a synced-RNG random pick from the widened set.
            fanTargets = [owner.RunState.Rng.CombatTargets.NextItem(fanTargets)!];
        }

        power.FlashIcon();
        foreach (Creature other in fanTargets)
        {
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
