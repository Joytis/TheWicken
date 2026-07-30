using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Cat familiar token: one 7-damage hit per Attack you played this turn, INCLUDING itself (always ≥1).
/// The history query still excludes this card and a flat +1 is added instead — during OnPlay its own play is
/// already in history, so this keeps the pre-play preview equal to the hits dealt. Hit count renders via the
/// base-game Barrage pattern; the count queries combat history like the base-game Normality.
/// </summary>
public sealed class Ferocity : WitchFamiliarCard
{
    private const string _calculatedHitsKey = "CalculatedHits";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(_calculatedHitsKey)
            .WithMultiplier((card, _) => AttacksPlayedThisTurnBefore(card) + 1)
    ];

    public Ferocity()
        : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    private static int AttacksPlayedThisTurnBefore(CardModel card) =>
        CombatManager.Instance.History.CardPlaysStarted.Count(e =>
            e.HappenedThisTurn(card.CombatState)
            && e.CardPlay.Card.Owner == card.Owner
            && e.CardPlay.Card != card
            && e.CardPlay.Card.Type == CardType.Attack);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int hits = (int)((CalculatedVar)DynamicVars[_calculatedHitsKey]).Calculate(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_scratch")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
