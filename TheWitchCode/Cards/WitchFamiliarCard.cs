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
    /// MP, and mid-combat state is never restored, so it needs no serialization.
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

    //Source art lives in card_portraits/familiar/ and is packed into atlas slices by
    //tools/pack-card-atlas.py — cards render the .tres slice.
    public override string CustomPortraitPath => $"familiar/{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".CardAtlasPath();
    public override string PortraitPath => CustomPortraitPath;
    public override string BetaPortraitPath => CustomPortraitPath;
}