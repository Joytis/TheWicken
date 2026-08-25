using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Wolf familiar token. Escalates with the "pack": deals its base damage plus a per-Gnash bonus for every Gnash
/// already played this combat — each played Gnash contributes ITS OWN ExtraDamage (3, or 4 for Gnash+), so the pack
/// bonus is the sum over played Gnashes, not count × this card's bonus. Built on a <see cref="CalculatedDamageVar" />
/// (the base-game Soul Storm pattern) so the growing total is computed live and shown on the card face. The
/// calc var multiplies a hidden unit <see cref="CalculationExtraVar" /> (1) by the pack sum, so damage = base + sum;
/// <c>ExtraDamage</c> stays the displayed/upgradable "increase ALL Gnash by" number and this card's contribution.
/// </summary>
public sealed class Gnash : WitchFamiliarCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(3m),
        new ExtraDamageVar(3m),
        new CalculationExtraVar(1m),
        new PackDamageVar(ValueProp.Move)
            .WithMultiplier((card, _) =>
                card.Owner?.Creature is { } creature
                    ? CombatHistoryQueries.GnashBonusThisCombat(creature)
                    : 0m),
    ];

    /// <summary><see cref="CalculatedDamageVar" /> whose per-unit extra is the hidden unit
    /// <see cref="CalculationExtraVar" /> rather than <c>ExtraDamage</c>, so the multiplier can be the pack's summed bonus.</summary>
    private sealed class PackDamageVar(ValueProp props) : CalculatedDamageVar(props)
    {
        protected override DynamicVar GetExtraVar() => ((CardModel)_owner!).DynamicVars["CalculationExtra"];
    }

    public Gnash()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.bitePath)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1m);
}
