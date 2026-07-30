using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Volatile Vapors: whenever the player uses or creates a potion, deal <see cref="PowerModel.Amount" />
/// damage to a random enemy. Counter stack — recasting the card raises the damage per trigger.
/// Same use/create hook pair as <see cref="CloakOfMoonlightPower" />.
/// </summary>
public sealed class VolatileVaporsPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner.Player)
        {
            await DamageRandomEnemy();
        }
    }

    public override async Task AfterPotionProcured(PotionModel potion)
    {
        if (potion.Owner == Owner.Player)
        {
            await DamageRandomEnemy();
        }
    }

    private async Task DamageRandomEnemy()
    {
        if (Owner.CombatState is not { } combat || Owner.Player is not { } player)
        {
            return;
        }

        List<Creature> targets = combat.HittableEnemies.ToList();
        if (targets.Count == 0)
        {
            return;
        }

        if (player.RunState.Rng.CombatTargets.NextItem(targets) is not { } target)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), target, Amount, ValueProp.Unpowered, Owner, null);
    }
}
