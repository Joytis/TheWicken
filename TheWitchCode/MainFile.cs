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

        // Unstable potions: combat-end sweeper rides the run's hook iteration, and each player's
        // marked slot indices are baked into the run save / MP sync so save-quit can't defuse them.
        Potions.UnstablePotionSweeperModel.Register();
        BaseLib.Patches.Saves.ExtendedSaveTypes.RegisterSavedValue<MegaCrit.Sts2.Core.Entities.Players.Player, string>(
            "thewitch_unstable_potions",
            Potions.UnstablePotions.SaveState,
            (player, csv) => Potions.UnstablePotions.RestoreState(player, csv ?? string.Empty),
            (csv, writer) => writer.WriteString(csv),
            reader => reader.ReadString());

        Harmony harmony = new(ModId);

        harmony.PatchAll();

        Debug.WitchDebug.ApplyPatches(harmony);
    }
}
