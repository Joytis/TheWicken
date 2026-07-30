using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using TheWitch.TheWitchCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(WitchFamiliarCardPool))]
public abstract class WitchFamiliarCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    // Familiar token-cards are one-shot per-turn payloads — Exhaust by default so they never clog the deck.
    // A subclass that needs extra keywords must re-include Exhaust in its own CanonicalKeywords override.
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>
    /// The familiar power instance that generated this card, and which of its stacks (0-based) rolled it —
    /// used to play the matching cosmetic pet's animation when the card is played. Null/0 for cards that
    /// reached the deck some other way (tutors, test hands). Plain C# state: consistent in SP and lockstep
    /// MP, lost only on mid-combat MP rejoin (cosmetic, acceptable).
    /// </summary>
    public FamiliarPower? SourceFamiliar { get; set; }
    public int SourceStackIndex { get; set; }


    public static async Task<IEnumerable<T>> CreateInHand<T>(Player owner, int count, ICombatState combatState, bool upgraded = false)
        where T : WitchFamiliarCard
	{
		if (count == 0)
		{
			return Array.Empty<T>();
		}
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<T>();
		}
		List<T> familiars = new List<T>();
		for (int i = 0; i < count; i++)
		{
			T newCard = combatState.CreateCard<T>(owner);
			if (upgraded)
			{
				CardCmd.Upgrade(newCard);
			}
			familiars.Add(newCard);
		}
		await CardPileCmd.AddGeneratedCardsToCombat(familiars, PileType.Hand, owner);
		return familiars;
	}

    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => $"familiar/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    
    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190
    
    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"familiar/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"familiar/beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}