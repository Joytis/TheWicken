using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWicken.TheWickenCode.Monsters;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

public sealed class CatFamiliar : WickenCard
{
    public CatFamiliar()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
		new CardsVar(2)
    ];

	protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Ferocity>(IsUpgraded)
    ];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await GainFamiliar<CatFamiliarPower>(choiceContext);
		await SummonFamiliarPet<CatPet>(Owner);
		List<Ferocity> cards = CreateFamiliarCards<Ferocity>(Owner, 1, CombatState, IsUpgraded).ToList();
        var cardsGenerated = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, Owner, CardPilePosition.Random);
		CardCmd.PreviewCardPileAdd(cardsGenerated);
	}
}
