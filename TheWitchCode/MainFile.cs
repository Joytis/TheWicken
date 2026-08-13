using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace TheWitch.TheWitchCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "TheWitch"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // Register this assembly's [ScriptPath] classes with Godot so scenes/resources in our .pck
        // can bind mod C# scripts (PetVisuals, PetConfig, ...) by res:// path.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Config.WitchConfig config = new();
        BaseLib.Config.ModConfigRegistry.Register(ModId, config);
        // If the host changes settings mid-run, rebroadcast so clients stay in lockstep.
        config.ConfigChanged += (_, _) =>
            Config.TurboWitcherySyncPatch.SendIfHost(MegaCrit.Sts2.Core.Runs.RunManager.Instance.NetService);

        // Bake Bottled Message's contents into the run save (and its net serialization) as a
        // SerializableCard riding on the potion's SerializablePotion entry.
        BaseLib.Patches.Saves.ExtendedSaveTypes.RegisterSavedValue<Potions.BottledMessage, MegaCrit.Sts2.Core.Saves.Runs.SerializableCard>(
            "thewitch_bottled_message",
            potion => potion.SaveState,
            (potion, state) => potion.RestoreState(state),
            (state, writer) => state.Serialize(writer),
            reader =>
            {
                MegaCrit.Sts2.Core.Saves.Runs.SerializableCard state = new();
                state.Deserialize(reader);
                return state;
            });

        // Unstable potions: combat-end sweeper rides the run's hook iteration. No save persistence
        // needed — run saves are written at room entry / post-victory (after the sweep), so a save
        // can never contain a marked potion.
        Potions.UnstablePotionSweeperModel.Register();

        // Witch run analytics: rides the game's official OnMetricsUpload hook (release builds,
        // user's Upload Data setting on) + our own AnalyticsEnabled config toggle.
        CommonCode.Data.WitchMetrics.Initialize();

        // Workshop self-update: if our installed Workshop item is stale (the Steam client's
        // manifest can desync and skip updates), force a re-download and prompt for a restart.
        CommonCode.Steam.WorkshopSelfUpdate.Initialize();

        Harmony harmony = new(ModId);

        harmony.PatchAll();

        Debug.WitchDebug.ApplyPatches(harmony);
    }
}
