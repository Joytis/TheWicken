using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace TheWitch.TheWitchCode.Config;

/// <summary>Host broadcasts its TurboWitchery setting at run start; synced state is cleared when the run ends.</summary>
[HarmonyPatch(typeof(RunManager))]
internal static class TurboWitcherySyncPatch
{
    [HarmonyPatch("InitializeShared")]
    [HarmonyPostfix]
    private static void SendHostConfig(RunManager __instance)
    {
        WitchConfig.SyncedTurbo = null;
        SendIfHost(__instance.NetService);
    }

    [HarmonyPatch("CleanUp")]
    [HarmonyPostfix]
    private static void ClearSynced() => WitchConfig.SyncedTurbo = null;

    internal static void SendIfHost(INetGameService? netService)
    {
        if (netService?.Type == NetGameType.Host)
        {
            CustomMessageWrapper.Send(new TurboWitcheryMessage { Turbo = WitchConfig.TurboWitchery }, netService);
        }
    }
}
