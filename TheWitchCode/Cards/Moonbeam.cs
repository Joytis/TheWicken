using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Moonbeam: strike, then leave the beam on the target — it takes the same damage again at the start of
/// each of your turns (<see cref="MoonbeamPower" />, stacks on repeat plays).
/// </summary>
public sealed class Moonbeam : WitchCard
{
    private const string _beamDamageKey = "BeamDamage";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10m, ValueProp.Move),
        // Unscaled twin of Damage: the power's flat per-turn tick (ValueProp.Unpowered at deal time),
        // so the card face doesn't show it inflated by Strength/Vigor.
        new DynamicVar(_beamDamageKey, 10m)
    ];

    public Moonbeam()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode((Creature c) => WitchFx.MoonbeamNode(Owner.Creature, c))
            // Cast sound on the attacker, impact thud on the hit — the Sovereign Blade split.
            .WithHitFx(null, WitchFx.CelestialSfx, null)
            .Execute(choiceContext);

        if (cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<MoonbeamPower>(choiceContext, cardPlay.Target, DynamicVars[_beamDamageKey].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[_beamDamageKey].UpgradeValueBy(2m);
    }
}
