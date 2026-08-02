using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Catalyst (Ancient Power): whenever the player uses a potion, create a copy of a random card in their hand
/// (<see cref="CardModel.CreateClone" /> + the generated-card path, so card-creation payoffs fire).
/// <c>AfterPotionUsed</c> provides no PlayerChoiceContext — the random pick doesn't need one.
/// </summary>
public sealed class CatalystPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner.Player)
        {
            return;
        }

        Flash();
        for (int i = 0; i < (int)Amount; i++)
        {
            List<CardModel> hand = PileType.Hand.GetPile(Owner.Player).Cards.ToList();
            CardModel? pick = Owner.Player.RunState.Rng.CombatCardSelection.NextItem(hand);
            if (pick == null)
            {
                return;
            }

            await CardPileCmd.AddGeneratedCardToCombat(pick.CreateClone(), PileType.Hand, Owner.Player);
        }
    }
}
