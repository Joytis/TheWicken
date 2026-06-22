using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// ExtractEssence: deal damage; on unblocked damage, create a Common potion themed around the enemy hit
/// (offensive enemy -> offensive potion, blocker -> defensive, debuffer -> utility). See <see cref="EnemyEssence" />.
/// </summary>
public sealed class ExtractEssence : WickenCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8m, ValueProp.Move)
    ];

    public ExtractEssence()
        : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        int unblocked = attack.Results
            .SelectMany(hit => hit)
            .Where(d => d.Receiver == target)
            .Sum(d => d.UnblockedDamage);

        if (unblocked <= 0)
        {
            return;
        }

        PotionModel? potion = EnemyEssence.RollThematicPotion(target, Owner.RunState.Rng.CombatPotionGeneration);
        ArgumentNullException.ThrowIfNull(potion, "potion");
        if (potion != null)
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
