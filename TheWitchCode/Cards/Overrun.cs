using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Overrun: mark one enemy for the pack — every Familiar card played this turn tramples it again.
/// </summary>
public sealed class Overrun : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<OverrunPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move),
        new ExtraDamageVar(5m)
    ];

    public Overrun()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_thrash")
            .Execute(choiceContext);

        if (Owner.Creature.GetPower<OverrunPower>() is null)
        {
            await PowerCmd.Apply<OverrunPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        }
        Owner.Creature.GetPower<OverrunPower>()?.AddStrike(cardPlay.Target, DynamicVars.ExtraDamage.BaseValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.ExtraDamage.UpgradeValueBy(2m);
    }
}
