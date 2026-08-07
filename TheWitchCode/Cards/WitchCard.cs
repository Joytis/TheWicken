using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// This is the base class for your mod's cards, which is set up to load the card's images from your mod's resources.
/// When creating a card, right click the Cards folder and create a new file with the Custom Card template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
[Pool(typeof(WitchCardPool))]
public abstract class WitchCard(int cost, CardType type, CardRarity rarity, TargetType target) :
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

    /// <summary>
    /// Register that this card summoned a familiar by applying one stack of its <typeparamref name="TPower" />
    /// counter to the player. The total of all <see cref="FamiliarPower" /> stacks is the player's familiar
    /// count (see <see cref="Familiars" />), which familiar-scaling cards read and which "sacrifice" effects
    /// consume. Call this from a familiar Power card's <c>OnPlay</c> alongside the pet/token-card spawn.
    /// </summary>
    protected async Task GainFamiliar<TPower>(PlayerChoiceContext choiceContext) where TPower : FamiliarPower
    {
        await PowerCmd.Apply<TPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);

        // An upgraded summon card makes ITS stack produce Upgraded token cards — per stack, so mixing
        // normal and upgraded summons of the same familiar yields a matching mix of tokens.
        if (IsUpgraded && Owner.Creature.GetPower<TPower>() is { } power)
        {
            power.UpgradedStacks++;
        }
    }
}