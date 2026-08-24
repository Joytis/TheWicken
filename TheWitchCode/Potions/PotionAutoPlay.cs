using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Machinery for playing a potion that is NOT in the belt (bypassing <c>PotionModel.OnUseWrapper</c>, whose
/// RemoveBeforeUse throws for an unowned potion) — used by <see cref="Powers.NeverendingPotionPower" /> to
/// replay bottled potions (no use-hooks — the potion was already "used" once).
/// Replay targeting: single-enemy potions hit a random living enemy; self/player potions target the owner;
/// everything else (AllEnemies/None) passes no target and lets the potion's own OnUse fan out.
/// </summary>
public static class PotionAutoPlay
{
    internal static readonly MethodInfo OnUseMethod = AccessTools.Method(typeof(PotionModel), "OnUse");

    public static Creature? ResolveTarget(PotionModel potion, Player player, Creature ownerCreature, ICombatState combat) => potion.TargetType switch
    {
        TargetType.AnyEnemy => player.RunState.Rng.CombatTargets.NextItem(
            combat.HittableEnemies.Where(e => e.IsAlive).ToList()),
        TargetType.Self or TargetType.AnyPlayer or TargetType.AnyAlly => ownerCreature,
        _ => null,
    };

    /// <summary>
    /// The bottle-throw animation from <c>PotionModel.OnUseWrapper</c> (which the out-of-belt paths bypass):
    /// arc the potion's image from <paramref name="thrower" /> to the resolved target — single-target potions
    /// to the target's hitbox, side-wide potions to the average vfx position of the affected side.
    /// NItemThrowVfx is globally preloaded (VfxCmd.AssetPaths), so spawning it from a power is safe.
    /// </summary>
    public static async Task PlayThrowVfx(PotionModel potion, Creature thrower, Creature? target, ICombatState combat)
    {
        if (TestMode.IsOn || NCombatRoom.Instance is not { } room)
        {
            return;
        }

        Vector2 targetPosition;
        if (potion.TargetType.IsSingleTarget())
        {
            targetPosition = room.GetCreatureNode(target ?? thrower)?.GetBottomOfHitbox() ?? Vector2.Zero;
        }
        else
        {
            CombatSide side = potion.TargetType == TargetType.AllEnemies ? CombatSide.Enemy : CombatSide.Player;
            List<Creature> creatures = combat.GetCreaturesOnSide(side).Where(c => c.IsHittable).ToList();
            targetPosition = Vector2.Zero;
            foreach (Creature creature in creatures)
            {
                targetPosition += room.GetCreatureNode(creature)?.VfxSpawnPosition ?? Vector2.Zero;
            }
            if (creatures.Count > 0)
            {
                targetPosition /= creatures.Count;
            }
        }

        Vector2 sourcePosition = room.GetCreatureNode(thrower)?.VfxSpawnPosition ?? Vector2.Zero;
        room.CombatVfxContainer.AddChildSafely(NItemThrowVfx.Create(sourcePosition, targetPosition, potion.Image));
        await Cmd.Wait(0.5f);
    }
}
