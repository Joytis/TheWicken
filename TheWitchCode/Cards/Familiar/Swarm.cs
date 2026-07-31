using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Rat familiar token: flood everything — one Rat to hand, plus Rats shuffled into BOTH the draw
/// and discard piles (Refuse Pile pattern).
/// </summary>
public sealed class Swarm : WitchFamiliarCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Rats>(IsUpgraded),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    public Swarm()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var handRat = FamiliarCardRegistry.CreateFamiliarCards<Rats>(Owner, 1, CombatState, IsUpgraded);
        await CardPileCmd.AddGeneratedCardsToCombat(handRat, PileType.Hand, Owner);

        int perPile = DynamicVars.Cards.IntValue;
        var drawRats = FamiliarCardRegistry.CreateFamiliarCards<Rats>(Owner, perPile, CombatState, IsUpgraded);
        var generatedDraw = await CardPileCmd.AddGeneratedCardsToCombat(drawRats, PileType.Draw, Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(generatedDraw);

        var discardRats = FamiliarCardRegistry.CreateFamiliarCards<Rats>(Owner, perPile, CombatState, IsUpgraded);
        var generatedDiscard = await CardPileCmd.AddGeneratedCardsToCombat(discardRats, PileType.Discard, Owner);
        CardCmd.PreviewCardPileAdd(generatedDiscard);
    }
}
