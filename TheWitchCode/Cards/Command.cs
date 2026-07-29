using System;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Command (was Throw Bait): a quick strike and an order barked at the pack — one random familiar
/// does its card production once (a single roll, regardless of stacks).
/// </summary>
public sealed class Command : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move)
    ];

    public Command()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        List<FamiliarPower> familiars = Owner.Creature.Powers.OfType<FamiliarPower>().ToList();
        if (familiars.Count > 0)
        {
            FamiliarPower chosen = Owner.RunState.Rng.CombatCardGeneration.NextItem(familiars)!;
            await chosen.GenerateOneCard(Owner, CombatState!);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
