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

        Harmony harmony = new(ModId);

        harmony.PatchAll();

        Debug.WitchDebug.ApplyPatches(harmony);
    }
}
