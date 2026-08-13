using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Spend X energy to pile Brambles on yourself, 7 at a time, X times — a fast way to stack a big Bramble count.
/// </summary>
public sealed class CreepingVines : WitchCard
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
		CardKeyword.Exhaust,
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BramblesPower>(7m)
    ];

    public CreepingVines()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal brambles = DynamicVars.Brambles().BaseValue;
        int times = ResolveEnergyXValue();
        for (int i = 0; i < times; i++)
        {
            await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, brambles, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Brambles().UpgradeValueBy(2m);
}
