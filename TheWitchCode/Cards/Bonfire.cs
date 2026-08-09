using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Bonfire: feed the flames — a burst of Energy, paid for by exhausting two cards.</summary>
public sealed class Bonfire : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        EnergyHoverTip,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(4),
        new CardsVar(2)
    ];

    public Bonfire()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> toExhaust = await CardSelectCmd.FromHand(
            choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, DynamicVars.Cards.IntValue), null, this);


        foreach (CardModel card in toExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        WitchFx.RedFlame(Owner.Creature);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
