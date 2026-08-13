using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Potions;

[Pool(typeof(WitchPotionPool))]
public abstract class WitchPotion : CustomPotionModel
{
    public override string? CustomPackedImagePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".PotionAtlasPath();
    public override string? CustomPackedOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.tres".PotionOutlineAtlasPath();
}