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
    //Source art (any resolution; landscape = normal, taller-than-wide = full art) lives in card_portraits/
    //and is packed into 250x190 (250x351 full art) atlas slices by tools/pack-card-atlas.py — cards render the .tres slice.
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".CardAtlasPath();
    public override string PortraitPath => CustomPortraitPath;
    public override string BetaPortraitPath => CustomPortraitPath;

    /// <summary>Artist credit for this card's art (data-only for now — no in-game display).
    /// Defaults to Kitsu; override per card, or return null for uncredited.</summary>
    public virtual Artists.Artist? ArtBy => Artists.Artist.Kitsu;

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