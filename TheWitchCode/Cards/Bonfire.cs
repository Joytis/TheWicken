using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Bonfire: feed the flames — a burst of Energy, paid for by exhausting two cards; the fire leaves
/// <see cref="Ash" /> behind in the discard pile. Ash can't be fed to the fire (excluded from the exhaust pick).</summary>
public sealed class Bonfire : WitchCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        EnergyHoverTip,
        HoverTipFactory.FromCard<Ash>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(4),
        new CardsVar(2),
        new DynamicVar("Ashes", 2m),
    ];

    public Bonfire()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> toExhaust = await CardSelectCmd.FromHand(
            choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, DynamicVars.Cards.IntValue),
            c => c is not Ash, this);

        foreach (CardModel card in toExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        WitchFx.RedFlame(Owner.Creature);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

        for (int i = 0; i < DynamicVars["Ashes"].IntValue; i++)
        {
            CardModel ash = CombatState!.CreateCard<Ash>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(ash, PileType.Discard, Owner));
        }
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
