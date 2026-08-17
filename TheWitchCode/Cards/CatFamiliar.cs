using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

public sealed class CatFamiliar : WitchCard, IFamiliarSummon
{
    public CatFamiliar()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

	protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<CatFamiliarPower>(),
        HoverTipFactory.FromCard<Ferocity>(IsUpgraded),
        // Nimble has MaxUpgradeLevel 0 — FromCard(upgrade: true) would throw in UpgradeInternal, killing every hover tip on Cat+.
        HoverTipFactory.FromCard<Nimble>(),
    ];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await GainFamiliar<CatFamiliarPower>(choiceContext);
	}
}
