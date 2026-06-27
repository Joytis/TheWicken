using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheWicken.TheWickenCode.Cards;

namespace TheWicken.TheWickenCode.Potions;

/// <summary>
/// Buddy in a Bottle: uncork a familiar — add a random Familiar summon card to your hand, free to play this
/// turn. (A potion can't "play" a card, so it delivers the summon card free rather than summoning directly.)
/// </summary>
public sealed class BuddyInABottle : WickenPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        CardModel canonical = Owner.RunState.Rng.CombatCardGeneration.NextItem(FamiliarCardRegistry.AllSummonCanonical)!;
        CardModel summon = Owner.Creature.CombatState!.CreateCard(canonical, Owner);
        summon.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(summon, PileType.Hand, Owner);
    }
}
