using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Runs.Metrics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.GameInfo;
using TheWitch.TheWitchCode;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.CommonCode.Data;

/// <summary>
/// Uploads Witch run analytics to our Supabase backend via the game's official
/// <see cref="ModManager.OnMetricsUpload"/> hook. The game only fires that hook on release
/// builds, when the user's "Upload Data" setting is on, full console is off, the run wasn't
/// abandoned, and it isn't their first run — so consent and dev-noise filtering are handled
/// upstream. We additionally gate on our own Analytics Enabled mod config toggle.
/// </summary>
internal static class WitchMetrics
{
    // Supabase project config. The anon key is a publishable key restricted by RLS to
    // insert-only on the runs table. Empty values disable uploads entirely.
    private const string SupabaseEndpoint = "https://bjwqinohtgsvnbscnvmb.supabase.co/rest/v1/runs";
    private const string SupabaseAnonKey = "sb_publishable_W9jQ_6zyHkSvUMofbfk6RQ_Lj_NrScl";

    /// <summary>Mirrors the base game's minimum-floor threshold for a run to count.</summary>
    private const int RunLengthThreshold = 5;

    private static readonly RunMetricsUploader s_uploader = new(SupabaseEndpoint, SupabaseAnonKey);

    // Same shape the vanilla metrics serializer uses: camelCase, fields included (the metric
    // structs are field-based), ModelIds flattened to their entry string.
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        Converters = { new ModelIdMetricsConverter() },
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Initialize()
    {
        ModManager.OnMetricsUpload += OnMetricsUpload;
    }

    private static void OnMetricsUpload(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        try
        {
            OnMetricsUploadInternal(run, isVictory, localPlayerId);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to build/upload Witch run analytics: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void OnMetricsUploadInternal(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        if (!TheWitchCode.Config.WitchConfig.AnalyticsEnabled)
        {
            MainFile.Logger.Info("Skipping Witch analytics upload, disabled in mod config.");
            return;
        }
        if (run.GameMode != GameMode.Standard)
        {
            return;
        }
        SerializablePlayer? localPlayer = run.Players.FirstOrDefault(p => p.NetId == localPlayerId);
        if (localPlayer?.CharacterId == null || localPlayer.CharacterId != ModelDb.Character<Witch>().Id)
        {
            return; // Only Witch runs.
        }
        List<MapPointHistoryEntry> allPoints = run.MapPointHistory.SelectMany(logs => logs).ToList();
        if (allPoints.Count < RunLengthThreshold)
        {
            return;
        }

        LocManager.Instance.StartOverridingLanguageAsEnglish();
        try
        {
            RunMetrics metrics = BuildRunMetrics(run, isVictory, localPlayerId, localPlayer, allPoints);
            Upload(run, isVictory, metrics);
        }
        finally
        {
            LocManager.Instance.StopOverridingLanguageAsEnglish();
        }
    }

    /// <summary>Mirrors the vanilla payload built in MetricUtilities.UploadRunMetricsInternal.</summary>
    private static RunMetrics BuildRunMetrics(SerializableRun run, bool isVictory, ulong localPlayerId,
        SerializablePlayer localPlayer, List<MapPointHistoryEntry> allPoints)
    {
        ModelId killedByEncounter = ModelId.none;
        MapPointHistoryEntry? lastPoint = run.MapPointHistory.LastOrDefault()?.LastOrDefault();
        if (!isVictory && lastPoint != null && lastPoint.Rooms.Last().RoomType.IsCombatRoom())
        {
            killedByEncounter = lastPoint.Rooms.Last().ModelId!;
        }

        List<EncounterMetric> encounters = (from e in allPoints
            where e.Rooms.Last().RoomType.IsCombatRoom()
            select new EncounterMetric(e.Rooms.Last().ModelId!.Entry,
                int.Min(e.GetEntry(localPlayerId).DamageTaken, localPlayer.MaxHp),
                e.Rooms.Last().TurnsTaken + 1)).ToList();
        List<CardChoiceMetric> cardChoices = (from e in allPoints
            where e.GetEntry(localPlayerId).CardChoices.Count > 0
            select new CardChoiceMetric(e.GetEntry(localPlayerId).CardChoices)).ToList();
        List<AncientMetric> ancientChoices = (from e in allPoints
            where e.MapPointType == MapPointType.Ancient
            where e.GetEntry(localPlayerId).AncientChoices.Count > 0
            select new AncientMetric(e, e.GetEntry(localPlayerId))).ToList();

        List<ActWinMetric> actWins = new();
        List<EventChoiceMetric> eventChoices = new();
        for (int actIndex = 0; actIndex < run.MapPointHistory.Count; actIndex++)
        {
            foreach (MapPointHistoryEntry entry in run.MapPointHistory[actIndex])
            {
                if (entry.Rooms.First().RoomType == RoomType.Event
                    && entry.GetEntry(localPlayerId).EventChoices.Count != 0
                    && entry.MapPointType != MapPointType.Ancient)
                {
                    eventChoices.Add(new EventChoiceMetric(entry, localPlayerId, run.Acts[actIndex]));
                }
            }
            bool win = actIndex < run.MapPointHistory.Count - 1 || isVictory;
            actWins.Add(new ActWinMetric(run.Acts[actIndex].Id!.Entry, win));
        }

        ProgressState progress = SaveManager.Instance.Progress;
        return new RunMetrics
        {
            Ascension = run.Ascension,
            TotalPlaytime = progress.TotalPlaytime,
            TotalWinRate = progress.NumberOfRuns > 0 ? (float)progress.Wins / progress.NumberOfRuns : 0f,
            NumReloads = run.NumReloads,
            BuildId = ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "NON-RELEASE-VERSION",
            BuildType = PlatformUtil.GetPlatformBranch().ToName(),
            PlayerId = HashPlayerId(progress.UniqueId),
            Character = localPlayer.CharacterId!,
            NumPlayers = run.Players.Count,
            Team = run.Players.Count > 1
                ? run.Players.Select(p => p.CharacterId).OfType<ModelId>().ToList()
                : new List<ModelId>(),
            Win = isVictory,
            FloorReached = allPoints.Count,
            KilledByEncounter = killedByEncounter,
            Deck = localPlayer.Deck.Select(c => c.Id).OfType<ModelId>(),
            Relics = localPlayer.Relics.Select(r => r.Id).OfType<ModelId>(),
            RunPlaytime = run.WinTime > 0 ? run.WinTime : run.RunTime,
            Encounters = encounters,
            CardChoices = cardChoices,
            EventChoices = eventChoices,
            AncientChoices = ancientChoices,
            ActWins = actWins,
            CampfireUpgrades = (from c in allPoints.Where(e => e.MapPointType == MapPointType.RestSite)
                    .SelectMany(e => e.GetEntry(localPlayerId).UpgradedCards)
                select c.Entry).ToList(),
            RelicBuys = (from r in allPoints.SelectMany(e => e.GetEntry(localPlayerId).BoughtRelics)
                select r.Entry).ToList(),
            PotionBuys = (from p in allPoints.SelectMany(e => e.GetEntry(localPlayerId).BoughtPotions)
                select p.Entry).ToList(),
            ColorlessBuys = (from c in allPoints.SelectMany(e => e.GetEntry(localPlayerId).BoughtColorless)
                select c.Entry).ToList(),
            PotionDiscards = (from p in allPoints.SelectMany(e => e.GetEntry(localPlayerId).PotionDiscarded)
                select p.Entry).ToList(),
        };
    }

    private static void Upload(SerializableRun run, bool isVictory, RunMetrics metrics)
    {
        // Supabase row: promoted columns for cheap SQL, full vanilla-shaped payload as jsonb.
        JsonObject row = new()
        {
            ["mod_version"] = GetModVersion(),
            ["game_version"] = metrics.BuildId,
            ["victory"] = isVictory,
            ["ascension"] = run.Ascension,
            ["floor"] = metrics.FloorReached,
            ["playtime"] = (int)metrics.RunPlaytime,
            ["player_hash"] = metrics.PlayerId,
            ["data"] = JsonNode.Parse(JsonSerializer.Serialize(metrics, s_jsonOptions)),
        };
        MainFile.Logger.Info("Uploading Witch run analytics...");
        s_uploader.Upload(row.ToJsonString(), "Witch run analytics");
    }

    private static string GetModVersion() =>
        ModManager.GetLoadedMods().FirstOrDefault(m => m.manifest?.id == MainFile.ModId)?.manifest?.version ?? "unknown";

    /// <summary>
    /// The game's UniqueId is already an anonymous install id (never the Steam id), but we
    /// still only ship a truncated SHA-256 of it — enough for dedupe/per-player aggregation.
    /// </summary>
    private static string HashPlayerId(string uniqueId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(uniqueId));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
