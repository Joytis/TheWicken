using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheWicken.TheWickenCode.Character;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Potions;

[Pool(typeof(WickenPotionPool))]
public abstract class WickenPotion : CustomPotionModel
{
    public override string? CustomPackedImagePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
}