using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Vfx;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Plaguestorm (was Plague): the storm strikes — one hit against ALL enemies for each Rats card played
/// this combat. Hit count renders live via the Barrage pattern.
/// </summary>
public sealed class Plaguestorm : WitchCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    private const string _calculatedHitsKey = "CalculatedHits";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(_calculatedHitsKey)
            .WithMultiplier((card, _) => card.Owner?.Creature is { } creature ? CombatHistoryQueries.RatsPlayedThisCombat(creature) : 0)
    ];

    protected override IEnumerable<string> ExtraRunAssetPaths => [NRatsThrowVfx.scenePath];

    public Plaguestorm()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hits = (int)((CalculatedVar)DynamicVars[_calculatedHitsKey]).Calculate(null);
        if (hits <= 0)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this)
            .TargetingAllOpponents(CombatState!)
            .WithHitVfxNode((Creature c) => NRatsThrowVfx.Create(Owner.Creature, c, WitchFx.White))
            .WithAttackerAnim("Attack", 0.2f)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
