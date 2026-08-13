namespace TheWitch.TheWitchCode.Artists;

/// <summary>
/// Artist credit for card art. Cards are tagged via <see cref="Cards.WitchCard.ArtBy" />
/// (e.g. <c>Artist.Joytis</c>); Kitsu is the base-class default. Data-only for now — the
/// in-game "Art By" presentation was removed pending a design the user likes (the deleted
/// tooltip prototype lives in git history: ArtistHoverTip / ArtistTipRenderPatch).
/// </summary>
public sealed record Artist(string Name)
{
    public static readonly Artist Kitsu = new("Kitsu");
    public static readonly Artist JakeRuesch = new("Jake Ruesch");
    public static readonly Artist Phenelia = new("Phenelia");
    public static readonly Artist Joytis = new("Joytis");
}
