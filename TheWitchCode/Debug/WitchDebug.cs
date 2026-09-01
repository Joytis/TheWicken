using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Debug;

/// <summary>
/// Launch-option debug helpers. Patches are only applied when the matching argument is present,
/// so a normal launch is untouched.
///   --witch-debug                     patches NGame.IsReleaseGame to false, unlocking dev-only
///                                     features gated on it (notably the -autoslay smoke-test bot).
///                                     With -autoslay also present, the bot is forced onto the Witch.
///   --witch-bootstrap[=ENCOUNTER_ID]  skips the main menu and launches a fresh Witch run straight
///                                     into a combat with 100 max energy. Optional value picks the
///                                     encounter (default CULTISTS_NORMAL); -seed &lt;s&gt; is honored.
///   --witch-fxlab                     skips the main menu and opens the FX Lab (NFxLab):
///                                     searchable SFX/VFX browser with play + copy-path buttons.
///                                     Pair with --witch-debug. Wins over --witch-bootstrap.
///   --witch-iconlab                   skips the main menu and opens the Icon Lab (NIconLab):
///                                     every relic + potion, Witch above base game, drawn in
///                                     each composited state (owned / not seen / undiscovered /
///                                     locked / raw outline) for art-parity checks.
///   --witch-cardtest                  headless smoke test: at main-menu ready, plays every Witch
///                                     card in a throwaway test combat (WitchCardTest) and logs
///                                     failures to the autoslay log. -seed &lt;s&gt; is honored.
///   --witch-potiontest                same harness: procures + uses + discards every Witch potion.
///   --witch-relictest                 same harness: equips EVERY Witch relic, then runs the card
///                                     exercise for every card and the potion exercise for every potion.
///   --witch-testall                   cards, potions, then relics — all three in one process.
///   --witch-test-update-popup         shows the Workshop self-update "restart required" popup
///                                     directly at the main menu (no Steam calls) — popup UI/loc
///                                     iteration. Handled in WorkshopSelfUpdate.Initialize.
///   --witch-force-workshop-download[=ITEMID]
///                                     skips the staleness gate and forces the high-priority
///                                     Workshop download + monitor + popup path. The optional
///                                     ITEMID lets a local mods/-folder build target the live
///                                     Workshop item. Handled in WorkshopSelfUpdate.Initialize.
///
/// Patch-point note: NGame.GameStartup's state machine is already JIT-compiled (and possibly
/// tier-1 promoted with call sites inlined) by the time mods initialize inside it, so methods it
/// calls (e.g. LaunchMainMenu) cannot be reliably detoured from here. We therefore hook
/// NMainMenu._Ready / NCharacterSelectButton.Select — both compile after mod init.
/// </summary>
public static class WitchDebug
{
    private const string DefaultEncounter = "CULTISTS_NORMAL";
    private const decimal BootstrapMaxEnergy = 100m;

    private static bool _bootstrapStarted;
    private static bool _fxLabStarted;
    private static bool _iconLabStarted;
    private static bool _cardTestStarted;
    private static WitchCardTest.Mode _smokeTestMode;

    private static bool TryGetSmokeTestMode(out WitchCardTest.Mode mode)
    {
        if (CommandLineHelper.HasArg("witch-cardtest")) { mode = WitchCardTest.Mode.Cards; return true; }
        if (CommandLineHelper.HasArg("witch-potiontest")) { mode = WitchCardTest.Mode.Potions; return true; }
        if (CommandLineHelper.HasArg("witch-relictest")) { mode = WitchCardTest.Mode.Relics; return true; }
        if (CommandLineHelper.HasArg("witch-testall")) { mode = WitchCardTest.Mode.All; return true; }
        mode = default;
        return false;
    }

    public static void ApplyPatches(Harmony harmony)
    {
        if (CommandLineHelper.HasArg("witch-debug"))
        {
            // DebugSettings.DevSkip latches STS2_DEV_SKIP on first access, which happens after
            // mod init (NGame's skipLogo check) — so setting it here skips the intro logo and
            // the timeline prompt for every -witch-debug launch, no launcher env var needed.
            System.Environment.SetEnvironmentVariable("STS2_DEV_SKIP", "1");

            MainFile.Logger.Info("--witch-debug: patching NGame.IsReleaseGame to false");
            harmony.Patch(
                AccessTools.Method(typeof(NGame), nameof(NGame.IsReleaseGame)),
                prefix: new HarmonyMethod(typeof(WitchDebug), nameof(IsReleaseGamePrefix)));

            if (CommandLineHelper.HasArg("autoslay"))
            {
                MainFile.Logger.Info("--witch-debug + -autoslay: forcing the Witch in character select");
                harmony.Patch(
                    AccessTools.Method(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Select)),
                    prefix: new HarmonyMethod(typeof(WitchDebug), nameof(CharacterSelectPrefix)));
            }
        }

        if (CommandLineHelper.HasArg("witch-fxlab"))
        {
            MainFile.Logger.Info("--witch-fxlab: will skip menu and open the FX Lab");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(WitchDebug), nameof(FxLabMenuReadyPostfix)));
        }
        else if (CommandLineHelper.HasArg("witch-iconlab"))
        {
            MainFile.Logger.Info("--witch-iconlab: will skip menu and open the Icon Lab");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(WitchDebug), nameof(IconLabMenuReadyPostfix)));
        }
        else if (TryGetSmokeTestMode(out WitchCardTest.Mode mode))
        {
            _smokeTestMode = mode;
            MainFile.Logger.Info($"{WitchCardTest.TagFor(mode)}: will run the headless {mode} smoke test at the main menu");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(WitchDebug), nameof(CardTestMenuReadyPostfix)));
        }
        else if (CommandLineHelper.HasArg("witch-bootstrap"))
        {
            MainFile.Logger.Info("--witch-bootstrap: will skip menu and enter combat directly");
            harmony.Patch(
                AccessTools.Method(typeof(NMainMenu), "_Ready"),
                postfix: new HarmonyMethod(typeof(WitchDebug), nameof(MainMenuReadyPostfix)));
            harmony.Patch(
                AccessTools.Method(typeof(Hook), nameof(Hook.ModifyMaxEnergy)),
                postfix: new HarmonyMethod(typeof(WitchDebug), nameof(ModifyMaxEnergyPostfix)));
        }
    }

    private static bool IsReleaseGamePrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    private static void ModifyMaxEnergyPostfix(ref decimal __result)
    {
        __result = BootstrapMaxEnergy;
    }

    // AutoSlay picks a random unlocked character button; redirect its pick to the Witch.
    // Only steers the bot — manual character select is untouched.
    private static bool CharacterSelectPrefix(NCharacterSelectButton __instance)
    {
        if (!AutoSlayer.IsActive || __instance.Character is Witch)
        {
            return true;
        }
        NCharacterSelectButton? witchButton = __instance.GetParent()
            .GetChildren()
            .OfType<NCharacterSelectButton>()
            .FirstOrDefault(b => b.Character is Witch && !b.IsLocked);
        if (witchButton == null)
        {
            MainFile.Logger.Error("autoslay: no unlocked Witch button found; letting the random pick stand");
            return true;
        }
        MainFile.Logger.Info($"autoslay: redirecting character select to the Witch (was {__instance.Character.Id})");
        witchButton.Select();
        return false;
    }

    private static void FxLabMenuReadyPostfix(NMainMenu __instance)
    {
        if (_fxLabStarted)
        {
            return;
        }
        _fxLabStarted = true;
        MainFile.Logger.Info("--witch-fxlab: main menu ready, opening the FX Lab");
        TaskHelper.RunSafely(OpenFxLab(__instance));
    }

    private static async Task OpenFxLab(NMainMenu menu)
    {
        // Same settle-delay as the combat bootstrap: let NGame's startup finish first.
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 5; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            NGame? game = NGame.Instance;
            if (game == null)
            {
                MainFile.Logger.Error("--witch-fxlab failed: NGame.Instance is null");
                return;
            }
            game.RootSceneContainer.SetCurrentScene(NFxLab.Create());
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"--witch-fxlab failed: {e}");
        }
    }

    private static void IconLabMenuReadyPostfix(NMainMenu __instance)
    {
        if (_iconLabStarted)
        {
            return;
        }
        _iconLabStarted = true;
        MainFile.Logger.Info("--witch-iconlab: main menu ready, opening the Icon Lab");
        TaskHelper.RunSafely(OpenIconLab(__instance));
    }

    private static async Task OpenIconLab(NMainMenu menu)
    {
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 5; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            NGame? game = NGame.Instance;
            if (game == null)
            {
                MainFile.Logger.Error("--witch-iconlab failed: NGame.Instance is null");
                return;
            }
            game.RootSceneContainer.SetCurrentScene(NIconLab.Create());
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"--witch-iconlab failed: {e}");
        }
    }

    private static void CardTestMenuReadyPostfix(NMainMenu __instance)
    {
        if (_cardTestStarted)
        {
            return;
        }
        _cardTestStarted = true;
        TaskHelper.RunSafely(RunCardTest(__instance));
    }

    private static async Task RunCardTest(NMainMenu menu)
    {
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 5; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        string? seed = CommandLineHelper.GetValue("seed");
        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = SeedHelper.GetRandomSeed();
        }
        try
        {
            await WitchCardTest.RunAll(seed, _smokeTestMode);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"{WitchCardTest.TagFor(_smokeTestMode)} failed: {e}");
        }
    }

    private static void MainMenuReadyPostfix(NMainMenu __instance)
    {
        if (_bootstrapStarted)
        {
            return;
        }
        _bootstrapStarted = true;
        MainFile.Logger.Info("--witch-bootstrap: main menu ready, starting combat bootstrap");
        TaskHelper.RunSafely(BootstrapFromMenu(__instance));
    }

    private static async Task BootstrapFromMenu(NMainMenu menu)
    {
        // Let NGame's startup step past the menu await before replacing the scene.
        SceneTree tree = menu.GetTree();
        for (int i = 0; i < 2; i++)
        {
            await menu.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            NGame? game = NGame.Instance;
            if (game == null)
            {
                MainFile.Logger.Error("--witch-bootstrap failed: NGame.Instance is null");
                return;
            }
            await StartCombatRun(game);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"--witch-bootstrap failed: {e}");
        }
    }

    // Uses the same path as a real new run (NGame.StartNewSingleplayerRun -> StartRun: run+act
    // assets, starting relics, EnterAct), then jumps to the encounter like the `fight` console cmd.
    private static async Task StartCombatRun(NGame game)
    {
        NAudioManager.Instance?.StopMusic();

        CharacterModel character = ModelDb.Character<Witch>();
        string seed = CommandLineHelper.GetValue("seed") ?? SeedHelper.GetRandomSeed();

        await game.StartNewSingleplayerRun(
            character,
            shouldSave: false,
            ActModel.GetDefaultList().ToList(),
            new List<ModifierModel>(),
            seed,
            GameMode.Standard);

        // GetValue can yield "" (not just null) when the arg has no value — fall back on both.
        string? encounterArg = CommandLineHelper.GetValue("witch-bootstrap");
        string entry = (string.IsNullOrWhiteSpace(encounterArg) ? DefaultEncounter : encounterArg).ToUpperInvariant();
        ModelId modelId = new(ModelId.SlugifyCategory<EncounterModel>(), entry);
        EncounterModel encounter = ModelDb.GetById<EncounterModel>(modelId).ToMutable();
        encounter.DebugRandomizeRng();
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Unassigned, encounter);

        MainFile.Logger.Info($"--witch-bootstrap: entered encounter '{entry}' with seed '{seed}'");
    }
}
