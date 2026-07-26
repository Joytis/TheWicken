using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TheWitch.TheWitchCode.Relics;

public sealed class Morganne : WitchRelic
{
	private bool _isActivating;
	private int _cardsPlayed;

	public override RelicRarity Rarity => RelicRarity.Uncommon;
	public override string FlashSfx => "event:/sfx/ui/relic_activate_draw";
	public override bool ShowCounter => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(10)];

	public override int DisplayAmount
	{
		get
		{
			if (!IsActivating)
			{
				return CardsPlayed % DynamicVars.Cards.IntValue;
			}
			return DynamicVars.Cards.IntValue;
		}
	}

    [SavedProperty]
	public int CardsPlayed
	{
		get
		{
			return _cardsPlayed;
		}
		set
		{
			AssertMutable();
			_cardsPlayed = value;
			UpdateDisplay();
		}
	}

	private bool IsActivating
	{
		get
		{
			return _isActivating;
		}
		set
		{
			AssertMutable();
			_isActivating = value;
			UpdateDisplay();
		}
	}

	private void UpdateDisplay()
	{
		if (IsActivating)
		{
			Status = RelicStatus.Normal;
		}
		else
		{
			int intValue = DynamicVars.Cards.IntValue;
			Status = (CardsPlayed % intValue == intValue - 1) ? RelicStatus.Active : RelicStatus.Normal;
		}
		InvokeDisplayAmountChanged();
	}

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == Owner)
		{
			CardsPlayed++;
			int intValue = DynamicVars.Cards.IntValue;
			if (CombatManager.Instance.IsInProgress && CardsPlayed % intValue == 0)
			{
                // Activated!
                _ = TaskHelper.RunSafely(DoActivateVisuals());

                // Move the card back into your hand. 
				await CardPileCmd.Add(cardPlay.Card, PileType.Hand, CardPilePosition.Bottom);
			}
		}
	}

	private async Task DoActivateVisuals()
	{
		IsActivating = true;
		Flash();
		await Cmd.Wait(1f);
		IsActivating = false;
	}
}
