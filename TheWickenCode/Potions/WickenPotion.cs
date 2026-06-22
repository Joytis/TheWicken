using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TheWicken.TheWickenCode.Character;
using TheWicken.TheWickenCode.Extensions;

namespace TheWicken.TheWickenCode.Potions;

[Pool(typeof(TheWickenPotionPool))]
public abstract class TheWickenPotion : CustomPotionModel
{
    public override string? CustomPackedImagePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
}