using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Patches;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Bottle Bombardment (MP-only): the whole party's brews rain down — one hit per potion created by ANY
/// player this combat. Same live hit-count shape as <see cref="BottleBarrage" /> (Barrage pattern over
/// <see cref="PotionProcureHistory" />), but summed across every player. Only SUCCESSFUL procures count.
/// </summary>
public sealed class BottleBombardment : WitchCard
{
    private const string _calculatedHitsKey = "CalculatedHits";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(_calculatedHitsKey)
            .WithMultiplier((_, _) => PotionProcureHistory.CountAll())
    ];

    public BottleBombardment()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int potions = (int)((CalculatedVar)DynamicVars[_calculatedHitsKey]).Calculate(cardPlay.Target);
        if (potions <= 0)
        {
            return;
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(potions)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.rockShatterPath, null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
