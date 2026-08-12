using BaseLib.Config;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace TheWitch.TheWitchCode.Config;

internal class WitchConfig : SimpleModConfig
{
    [ConfigHoverTip]
    public static bool TurboWitchery { get; set; } = false;

    [ConfigHoverTip]
    public static bool BetaImages { get; set; } = false;

    [ConfigHoverTip]
    public static bool AnalyticsEnabled { get; set; } = true;

    /// <summary>Host's value, received via <see cref="TurboWitcheryMessage"/>. Null until synced.</summary>
    [ConfigIgnore]
    internal static bool? SyncedTurbo { get; set; }

    /// <summary>What gameplay should read: the host's setting in multiplayer, the local one otherwise.</summary>
    [ConfigIgnore]
    public static bool EffectiveTurboWitchery =>
        RunManager.Instance.NetService?.Type == NetGameType.Client
            ? SyncedTurbo ?? false
            : TurboWitchery;
}
