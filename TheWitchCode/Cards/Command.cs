using System;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Command (was Throw Bait): a quick strike and an order barked at the pack — up to two random
/// familiars each do their card production once. Every stack counts as its own familiar (Crow ×2
/// is two birds on screen = two entries in the pick pool), so a doubled familiar can produce twice.
/// </summary>
public sealed class Command : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move)
    ];

    public Command()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // One pool entry per stack, so Crow ×2 counts as two familiars (and can produce twice).
        List<FamiliarPower> familiars = Owner.Creature.Powers.OfType<FamiliarPower>()
            .SelectMany(power => Enumerable.Repeat(power, Math.Max((int)power.Amount, 1)))
            .ToList();
        familiars.UnstableShuffle(Owner.RunState.Rng.CombatCardGeneration);
        foreach (FamiliarPower chosen in familiars.Take(2))
        {
            await chosen.GenerateOneCard(Owner, CombatState!);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
