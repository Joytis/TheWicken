using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWicken.TheWickenCode.Powers;

namespace TheWicken.TheWickenCode.Cards;

public sealed class SlothFamiliar : WickenCard, IFamiliarSummon
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Laze>(IsUpgraded),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public SlothFamiliar()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await GainFamiliar<SlothFamiliarPower>(choiceContext);
        List<Laze> cards = CreateFamiliarCards<Laze>(Owner, DynamicVars.Cards.IntValue, CombatState, IsUpgraded).ToList();
        var generated = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generated);
    }
}
