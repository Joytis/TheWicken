using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Capture Soul: each kill is bottled forever — this copy permanently applies +1 Hex per enemy it has
/// killed, across combats and save/load (<c>[SavedProperty]</c> + DeckVersion propagation, the base-game
/// GeneticAlgorithm pattern). Live Hex number via the Barrage CalculatedVar shape.
/// </summary>
public sealed class CaptureSoul : WitchCard
{
    private const string _calculatedHexKey = "CalculatedHex";

    private int _bonusHex;

    [SavedProperty]
    public int BonusHex
    {
        get => _bonusHex;
        set
        {
            AssertMutable();
            _bonusHex = value;
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10m, ValueProp.Move),
        new CalculationBaseVar(1m),
        new CalculationExtraVar(1m),
        new CalculatedVar(_calculatedHexKey)
            .WithMultiplier((card, _) => ((CaptureSoul)card).BonusHex)
    ];

    public CaptureSoul()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        decimal hex = ((CalculatedVar)DynamicVars[_calculatedHexKey]).Calculate(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "heavy_attack.mp3")
            .Execute(choiceContext);

        if (cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, hex, Owner.Creature, this);
        }
        else
        {
            // Kill: this copy grows permanently — buff the combat instance AND its deck version so the
            // bonus survives the combat and the run save (GeneticAlgorithm pattern).
            BonusHex++;
            if (DeckVersion is CaptureSoul deckCopy)
            {
                deckCopy.BonusHex++;
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
