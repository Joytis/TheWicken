using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Potions;

public sealed class OwlFeather : WitchCardPotionBase
{
    protected override CardModel CreatedCard => ModelDb.Card<Wisdom>();
}