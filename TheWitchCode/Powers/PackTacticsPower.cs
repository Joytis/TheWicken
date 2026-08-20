using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Temporary Strength granted by Pack Tactics — Strength for the rest of the turn, removed at turn end.
/// Subclasses the base-game <see cref="TemporaryStrengthPower" /> (Coordinate / Flex Potion pattern);
/// Title/Description come from that base + <see cref="OriginModel" />, so this needs no mod localization.
/// Can't inherit <see cref="WitchPower" /> on top of the game base, so it implements
/// <see cref="ICustomPower" /> and mirrors WitchPower's atlas paths for the mod's own icon art.
/// </summary>
public sealed class PackTacticsPower : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<PackTactics>();

    // Mirrors WitchPower: renders the power_atlas slice packed from TheWitch/images/powers/pack_tactics_power.png
    public string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.tres".PowerAtlasPath();
    public string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
}
