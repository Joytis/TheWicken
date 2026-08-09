using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Embrace the Wilds: summon a burst of random familiars at the cost of drawing fewer cards each turn for the
/// rest of combat. The draw penalty rides on <see cref="EmbraceTheWildsPower" />; each summon applies one stack
/// of a random familiar's <see cref="FamiliarPower" /> (pulled from every registered familiar power).
/// </summary>
public sealed class EmbraceTheWilds : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("DrawReduction", 2m),
        new DynamicVar("Familiars", 3m)
    ];

    public EmbraceTheWilds()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

        await PowerCmd.Apply<EmbraceTheWildsPower>(
            choiceContext, Owner.Creature, DynamicVars["DrawReduction"].BaseValue, Owner.Creature, this);

        List<PowerModel> familiarPowers = ModelDb.AllPowers.Where(p => p is FamiliarPower).ToList();
        if (familiarPowers.Count == 0)
        {
            return;
        }

        Rng rng = Owner.RunState.Rng.CombatCardGeneration;
        int summons = DynamicVars["Familiars"].IntValue;
        for (int i = 0; i < summons; i++)
        {
            PowerModel familiar = rng.NextItem(familiarPowers)!;
            await PowerCmd.Apply(choiceContext, familiar.ToMutable(), Owner.Creature, 1m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Familiars"].UpgradeValueBy(1m);
}
