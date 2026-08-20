using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Temporary Strength granted by Pack Tactics — Strength for the rest of the turn, removed at turn end.
/// Subclasses the base-game <see cref="TemporaryStrengthPower" /> (Coordinate / Flex Potion pattern);
/// Title/Description/icon come from that base + <see cref="OriginModel" />, so this needs no mod localization.
/// </summary>
public sealed class PackTacticsPower : TemporaryStrengthPower, ICustomModel
{
    public override AbstractModel OriginModel => ModelDb.Card<PackTactics>();
}
