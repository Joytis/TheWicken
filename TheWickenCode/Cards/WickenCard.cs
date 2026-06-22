using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheWicken.TheWickenCode.Character;
using TheWicken.TheWickenCode.Extensions;
using TheWicken.TheWickenCode.Monsters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(WickenCardPool))]
public abstract class WickenCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190
    
    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

	public static IEnumerable<T> CreateFamiliarCards<T>(Player owner, int amount, ICombatState? combatState, bool isUpgraded)
        where T : WickenFamiliarCard
	{
		ArgumentNullException.ThrowIfNull(combatState, "combatState");
		List<T> list = new List<T>();
		for (int i = 0; i < amount; i++)
		{
            var newCard = combatState.CreateCard<T>(owner);
			list.Add(newCard);
            if(isUpgraded)
            {
                CardCmd.Upgrade(newCard);
            }
		}
		return list;
	}

    /// <summary>
    /// Spawn the cosmetic pet sprite for a familiar near the player (like Byrdpip's Byrd).
    /// Purely visual — the familiar's mechanics live on its token-cards. The pet is
    /// combat-scoped, so it disappears at combat end. Guards against stacking duplicates
    /// when the same familiar is replayed in one combat; drop the guard for one-per-play.
    /// </summary>
    protected static async Task SummonFamiliarPet<TPet>(Player owner) where TPet : WickenPet
    {
        // Only in combat (PlayerCombatState != null), and not if this pet is already out.
        if (owner.PlayerCombatState is { } combat && combat.GetPet<TPet>() == null)
        {
            await PlayerCmd.AddPet<TPet>(owner);
        }
    }
}