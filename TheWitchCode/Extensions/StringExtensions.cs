using Godot;
using TheWitch.TheWitchCode.Config;

namespace TheWitch.TheWitchCode.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    /// <summary>Join under images/<paramref name="relDir"/>; fall back to the category placeholder (with a log) when missing.</summary>
    private static string Resolve(string relDir, string path, string placeholder)
    {
        string full = Path.Join(MainFile.ResPath, "images", relDir, path);
        if (ResourceLoader.Exists(full)) return full;

        MainFile.Logger.Info($"Could not find image path: {full}");
        return Path.Join(MainFile.ResPath, "images", relDir, placeholder);
    }

    /// <summary>The card_portraits/beta/ variant of a card image when the Beta Images config is on and the file exists, else null. <paramref name="subDir"/> is the folder inside beta/ ("" for small, "big" for big art).</summary>
    private static string? BetaCardImage(string subDir, string path)
    {
        if (!WitchConfig.BetaImages) return null;
        string beta = Path.Join(MainFile.ResPath, "images", "card_portraits", "beta", subDir, path);
        return ResourceLoader.Exists(beta) ? beta : null;
    }

    public static string ImagePath(this string path)
    {
        return Path.Join(MainFile.ResPath, "images", path);
    }

    public static string CardImagePath(this string path)
    {
        return BetaCardImage("", path) ?? Resolve("card_portraits", path, "card.png");
    }

    public static string BigCardImagePath(this string path)
    {
        return BetaCardImage("big", path) ?? Resolve(Path.Join("card_portraits", "big"), path, "card.png");
    }

    public static string PotionImagePath(this string path) => Resolve("potions", path, "potion.png");
    public static string PowerImagePath(this string path) => Resolve("powers", path, "power.png");
    public static string BigPowerImagePath(this string path) => Resolve(Path.Join("powers", "big"), path, "power.png");
    public static string RelicImagePath(this string path) => Resolve("relics", path, "relic.png");

    /// <summary>Relic outline (white silhouette drawn behind the small icon); falls back to the outline placeholder, not the colored relic art.</summary>
    public static string RelicOutlineImagePath(this string path) => Resolve(Path.Join("relics", "outlines"), path, "relic.png");

    /// <summary>Potion outline; no placeholder — the game treats a missing outline as "no outline" rather than drawing a wrong one.</summary>
    public static string PotionOutlineImagePath(this string path) => Path.Join(MainFile.ResPath, "images", "potions", "outlines", path);
    public static string BigRelicImagePath(this string path) => Resolve(Path.Join("relics", "big"), path, "relic.png");

    public static string PetImagePath(this string path)
    {
        string petPath = Path.Join(MainFile.ResPath, "images", "pets", path);
        if (ResourceLoader.Exists(petPath)) return petPath;

        MainFile.Logger.Info("Could not find pet image path: " + petPath + " — falling back to card art");
        return path.CardImagePath();
    }

    public static string CharacterUiPath(this string path) => Path.Join(MainFile.ResPath, "images", "charui", path);
    public static string CharacterSfxPath(this string path) => Path.Join(MainFile.ResPath, "sfx", path);
    public static string CharacterScenePath(this string path) => Path.Join(MainFile.ResPath, "scenes", path);
    public static string VfxScenePath(this string path) => Path.Join(MainFile.ResPath, "scenes", "vfx", path);
    public static string ShaderPath(this string path) => Path.Join(MainFile.ResPath, "Shaders", path);
    public static string PetConfigPath(this string path) => Path.Join(MainFile.ResPath, "data", "pets", path);
}
