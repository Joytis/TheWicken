using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Wicker Consumation: final link of the Wicker chain, created by <see cref="WickerBones" />. Styled as a Witch
/// Rare, lives in the shared <see cref="WitchSpawnedCardPool" /> (never a reward). Big hit; at the last bramble
/// threshold it grants <see cref="IncarnationPower" />. Not upgradable.
/// </summary>
[Pool(typeof(WitchSpawnedCardPool))]
public sealed class WickerConsumation : WitchCard
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
        HoverTipFactory.FromPower<IncarnationPower>(),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(20m, ValueProp.Move),
        new DynamicVar("TargetBrambles", 50m),
        new PowerVar<IncarnationPower>(1m)
    ];

    public WickerConsumation()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        decimal brambles = Owner.Creature.GetPowerAmount<BramblesPower>();
        if (brambles >= DynamicVars["TargetBrambles"].BaseValue)
        {
            await PowerCmd.Apply<IncarnationPower>(choiceContext, Owner.Creature, DynamicVars["IncarnationPower"].BaseValue, Owner.Creature, this);
        }
    }
}
