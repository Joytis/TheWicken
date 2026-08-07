using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>"Double, double, toil and trouble" — hit twice, then enchant a random card in your Draw Pile with Replay.</summary>
public sealed class ToilAndTrouble : WitchCard
{
    private const int Hits = 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move),
        new IntVar("Replay", 1m)
    ];

    public ToilAndTrouble()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(Hits)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);

        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList();
        CardModel? chosen = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (chosen != null)
        {
            chosen.BaseReplayCount += DynamicVars["Replay"].IntValue;
            CardCmd.Preview(chosen);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
