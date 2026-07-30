using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Wolf familiar token. Escalates with the "pack": deals its base damage plus a per-Gnash bonus for every Gnash
/// already played this combat. Built on a <see cref="CalculatedDamageVar" /> (the base-game Soul Storm pattern) so
/// the growing total is computed live and shown correctly on the card face — base + ExtraDamage × Gnash-played.
/// </summary>
public sealed class Gnash : WitchFamiliarCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(3m),
        new ExtraDamageVar(3m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((card, _) =>
                card.Owner?.Creature is { } creature
                    ? CombatHistoryQueries.CardsPlayedThisCombat<Gnash>(creature)
                    : 0),
    ];

    public Gnash()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bite")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1m);
}
