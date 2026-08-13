using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;
using TheWitch.TheWitchCode;

namespace TheWitch.CommonCode.Steam;

/// <summary>
/// Self-update check for our own Steam Workshop item, ported (scoped-down) from RitsuLib's
/// SteamWorkshopUpdates. The Steam client's local manifest can desync and report a stale install
/// as current, so we ask the Workshop servers directly for the item's last-update time
/// (bypassing Steam's cached response), compare it against the local install timestamp, and
/// force a high-priority re-download when the install is stale. The freshly downloaded files
/// only load on the next boot, so we tell the player a restart is needed via the game's
/// generic confirmation popup.
/// </summary>
internal static class WorkshopSelfUpdate
{
    private static readonly TimeSpan s_downloadPollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_downloadStartGrace = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_downloadTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan s_popupRetryInterval = TimeSpan.FromSeconds(1);

    private const uint InProgressStates = (uint)(EItemState.k_EItemStateNeedsUpdate
        | EItemState.k_EItemStateDownloading | EItemState.k_EItemStateDownloadPending);

    public static void Initialize()
    {
        // Debug flags (see WitchDebug's flag catalog): --witch-test-update-popup shows the
        // restart popup directly; --witch-force-workshop-download[=ITEMID] skips the staleness
        // gate and forces the download path.
        if (CommandLineHelper.HasArg("witch-test-update-popup"))
        {
            MainFile.Logger.Info("[WorkshopSelfUpdate] --witch-test-update-popup: showing the restart popup directly.");
            TaskHelper.RunSafely(ShowRestartPopup());
            return;
        }
        TaskHelper.RunSafely(RunAsync(CommandLineHelper.HasArg("witch-force-workshop-download")));
    }

    private static async Task RunAsync(bool forceDownload)
    {
        PublishedFileId_t itemId;
        // The force flag's optional value lets a local mods/-folder build target the live item
        // (a local build has no Workshop install path to parse an id from).
        string? forcedId = forceDownload ? CommandLineHelper.GetValue("witch-force-workshop-download") : null;
        if (!string.IsNullOrWhiteSpace(forcedId) && ulong.TryParse(forcedId, out ulong parsedId))
        {
            itemId = new(parsedId);
            MainFile.Logger.Info($"[WorkshopSelfUpdate] Forced item id {parsedId} from command line.");
        }
        else
        {
            Mod? mod = FindOwnMod();
            if (mod == null)
            {
                MainFile.Logger.Info("[WorkshopSelfUpdate] Could not find our own mod entry; skipping update check.");
                return;
            }
            if (mod.modSource != ModSource.SteamWorkshop)
            {
                MainFile.Logger.Info("[WorkshopSelfUpdate] Mod not loaded from Steam Workshop; skipping update check.");
                return;
            }

            // Workshop installs live at .../steamapps/workshop/content/<appid>/<itemid>/...
            Match match = Regex.Match(mod.path.Replace('\\', '/'), "/workshop/content/\\d+/(\\d+)(?:/|$)");
            if (!match.Success)
            {
                MainFile.Logger.Warn($"[WorkshopSelfUpdate] Could not parse Workshop item id from mod path '{mod.path}'; skipping update check.");
                return;
            }
            itemId = new(ulong.Parse(match.Groups[1].Value));
        }

        uint remoteUpdated = await QueryRemoteUpdateTime(itemId);
        if (remoteUpdated == 0)
        {
            return; // Query failed; already logged.
        }

        uint state = SteamUGC.GetItemState(itemId);
        bool haveInstallInfo = SteamUGC.GetItemInstallInfo(itemId, out _, out _, 256u, out uint localTimestamp);
        bool needsUpdateFlag = (state & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;
        bool remoteNewer = haveInstallInfo && remoteUpdated > localTimestamp;
        MainFile.Logger.Info($"[WorkshopSelfUpdate] Item {itemId.m_PublishedFileId}: state={state}, localTimestamp={(haveInstallInfo ? localTimestamp : 0)}, remoteUpdated={remoteUpdated}.");
        if (!needsUpdateFlag && !remoteNewer && !forceDownload)
        {
            MainFile.Logger.Info("[WorkshopSelfUpdate] Install is up to date.");
            return;
        }

        MainFile.Logger.Info("[WorkshopSelfUpdate] Install is stale; forcing a high-priority Workshop download.");
        if (!SteamUGC.DownloadItem(itemId, bHighPriority: true))
        {
            MainFile.Logger.Warn("[WorkshopSelfUpdate] Steam rejected the download request.");
            return;
        }

        if (await WaitForDownload(itemId, localTimestamp))
        {
            await ShowRestartPopup();
        }
    }

    private static Mod? FindOwnMod()
    {
        foreach (Mod mod in ModManager.Mods)
        {
            if (mod.assemblies != null && mod.assemblies.Contains(typeof(WorkshopSelfUpdate).Assembly))
            {
                return mod;
            }
        }
        return null;
    }

    /// <summary>
    /// Asks the Workshop servers for the item's last-update time. Server truth: we disable
    /// Steam's cached response so a desynced local manifest can't feed us a stale answer.
    /// Returns 0 on failure.
    /// </summary>
    private static async Task<uint> QueryRemoteUpdateTime(PublishedFileId_t itemId)
    {
        UGCQueryHandle_t queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new PublishedFileId_t[1] { itemId }, 1u);
        try
        {
            SteamUGC.SetAllowCachedResponse(queryHandle, 0u);
            using SteamCallResult<SteamUGCQueryCompleted_t> callResult =
                new(SteamUGC.SendQueryUGCRequest(queryHandle), SteamInitializer.DisconnectToken);
            SteamUGCQueryCompleted_t completed = await callResult.Task;
            if (completed.m_eResult != EResult.k_EResultOK ||
                !SteamUGC.GetQueryUGCResult(completed.m_handle, 0u, out SteamUGCDetails_t details))
            {
                MainFile.Logger.Warn($"[WorkshopSelfUpdate] Workshop details query failed: {completed.m_eResult}.");
                return 0;
            }
            return details.m_rtimeUpdated;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[WorkshopSelfUpdate] Workshop details query failed: {e.Message}");
            return 0;
        }
        finally
        {
            SteamUGC.ReleaseQueryUGCRequest(queryHandle);
        }
    }

    /// <summary>
    /// Polls until the download lands (download-state flags cleared and the local install
    /// timestamp actually changed — the ground truth that new files are on disk). If Steam
    /// never starts a download within the grace period, it silently ignored the request.
    /// </summary>
    private static async Task<bool> WaitForDownload(PublishedFileId_t itemId, uint initialTimestamp)
    {
        DateTime startedAt = DateTime.UtcNow;
        bool sawActivity = false;
        while (DateTime.UtcNow - startedAt < s_downloadTimeout)
        {
            await Task.Delay(s_downloadPollInterval, SteamInitializer.DisconnectToken);
            uint state = SteamUGC.GetItemState(itemId);
            bool inProgress = (state & InProgressStates) != 0;
            sawActivity |= inProgress;
            SteamUGC.GetItemInstallInfo(itemId, out _, out _, 256u, out uint timestamp);
            if (!inProgress && timestamp != initialTimestamp)
            {
                MainFile.Logger.Info($"[WorkshopSelfUpdate] Download complete; install timestamp {initialTimestamp} -> {timestamp}.");
                return true;
            }
            if (!inProgress && !sawActivity && DateTime.UtcNow - startedAt > s_downloadStartGrace)
            {
                MainFile.Logger.Warn("[WorkshopSelfUpdate] Steam never started the download; giving up.");
                return false;
            }
        }
        MainFile.Logger.Warn("[WorkshopSelfUpdate] Timed out waiting for the Workshop download to finish.");
        return false;
    }

    /// <summary>
    /// Tells the player the update landed and needs a restart, once the UI is up. Uses the
    /// base game's generic confirmation popup (same vehicle as the quit-confirm dialog).
    /// </summary>
    private static async Task ShowRestartPopup()
    {
        // LocManager initializes AFTER mods (its Initialize doc: "mods must be initialized
        // before this"), and NModalContainer can exist before that — waiting on the container
        // alone lets SetText run against an unloaded loc system (NRE, popup stuck on the
        // scene's placeholder text). Wait for both.
        while (NModalContainer.Instance == null || LocManager.Instance == null)
        {
            await Task.Delay(s_popupRetryInterval, SteamInitializer.DisconnectToken);
        }
        NGenericPopup? popup = NGenericPopup.Create();
        if (popup == null)
        {
            return; // Test mode.
        }
        NModalContainer.Instance.Add(popup);
        bool quitNow = await popup.WaitForConfirmation(
            new LocString("settings_ui", "THEWITCH-WORKSHOP_UPDATE.body"),
            new LocString("settings_ui", "THEWITCH-WORKSHOP_UPDATE.header"),
            new LocString("settings_ui", "THEWITCH-WORKSHOP_UPDATE.later"),
            new LocString("settings_ui", "THEWITCH-WORKSHOP_UPDATE.quit"));
        if (quitNow)
        {
            MainFile.Logger.Info("[WorkshopSelfUpdate] Player chose to quit and apply the update.");
            NGame.Instance?.Quit();
        }
    }
}
