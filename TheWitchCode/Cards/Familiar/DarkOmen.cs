using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HexPower = TheWitch.TheWitchCode.Powers.HexPower;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Crow familiar token (formerly Claw Eyes): an ill portent — Hex a single target. Exhausts.</summary>
public sealed class DarkOmen : WitchFamiliarCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(1m),
    ];

    public DarkOmen()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, DynamicVars.Hex().BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Hex().UpgradeValueBy(1m);
}
