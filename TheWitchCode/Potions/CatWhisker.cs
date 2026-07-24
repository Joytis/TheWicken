using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Potions;

public sealed class CatWhisker : WitchCardPotionBase
{
    protected override CardModel CreatedCard => ModelDb.Card<Ferocity>();
}


