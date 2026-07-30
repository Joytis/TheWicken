using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Hide in a Bush (formerly Spines): Block plus Brambles.</summary>
public sealed class HideInABush : WitchCard
{
	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

	protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(6m, ValueProp.Move),
        new PowerVar<BramblesPower>(3m)
    ];

	public HideInABush()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await PowerCmd.Apply<BramblesPower>(choiceContext, Owner.Creature, DynamicVars.Brambles().BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
		DynamicVars.Brambles().UpgradeValueBy(1m);
	}
}

