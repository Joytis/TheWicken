using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Registry of every familiar token-card (all <see cref="WickenFamiliarCard" /> subtypes). Backs the
/// "random familiar card" effects — Woe and Whimsy, Chimera Familiar, Embrace the Wilds.
/// Canonical models come from <see cref="ModelDb.AllCards" />; the <c>WickenFamiliarCardPool</c> is
/// shared (<c>IsShared =&gt; true</c>), so all familiar cards are present there.
/// </summary>
public static class FamiliarCardRegistry
{
    /// <summary>Canonical models for every registered familiar token-card.</summary>
    public static IReadOnlyList<WickenFamiliarCard> AllCanonical =>
        ModelDb.AllCards.OfType<WickenFamiliarCard>().ToList();

    /// <summary>
    /// Canonical models for every familiar *summon* card — the <see cref="IFamiliarSummon" /> Power cards
    /// (Owl, Cat, Rat, Porcupine, Bear, Crow, Wolf, Sloth, Chimera). Backs Embrace the Wilds.
    /// </summary>
    public static IReadOnlyList<CardModel> AllSummonCanonical =>
        ModelDb.AllCards.OfType<IFamiliarSummon>().Cast<CardModel>().ToList();

    /// <summary>
    /// Create <paramref name="amount" /> real familiar cards for <paramref name="owner" />, each a
    /// random familiar type rolled from <paramref name="rng" /> (upgraded if <paramref name="isUpgraded" />).
    /// </summary>
    public static List<CardModel> CreateRandom(Player owner, int amount, ICombatState combatState, Rng rng, bool isUpgraded)
    {
        ArgumentNullException.ThrowIfNull(combatState, "combatState");
        IReadOnlyList<WickenFamiliarCard> pool = AllCanonical;
        var result = new List<CardModel>(amount);
        for (int i = 0; i < amount; i++)
        {
            WickenFamiliarCard canonical = rng.NextItem(pool)!;
            CardModel card = combatState.CreateCard(canonical, owner);
            if (isUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            result.Add(card);
        }
        return result;
    }
}
