using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace TheWitch.TheWitchCode.Debug;

/// <summary>
/// Debug-only FX browser scene (launched via -witch-debug -witch-fxlab, see WitchDebug).
/// Left list: every FMOD sfx event (parsed out of the shipped *.strings.bank) plus the
/// res://debug_audio mp3s. Right list: every vfx particle scene (res://scenes/vfx plus any
/// mod vfx scenes under res://TheWitch). Each row: Play, and Copy (puts the exact string the
/// code APIs take into the Windows clipboard). Vfx spawn on the stage panel and free
/// themselves; a failsafe frees anything still alive after a few seconds.
/// Built entirely in code so a plain `dotnet build` ships it (no .pck export needed).
/// </summary>
public partial class NFxLab : Control
{
    private const double VfxFailsafeSeconds = 8.0;
    private const double LoopVfxKillSeconds = 3.0;

    // Spawned vfx are scaled down so big effects fit inside the stage panel.
    private const float StageZoom = 0.5f;

    private sealed record FxEntry(string Name, string[] Group, string CopyText, string PlayKey, double MaxLifetime = VfxFailsafeSeconds, string Tag = "");

    private sealed record RowEntry(Control Row, string Filter, string Tag);

    private readonly List<RowEntry> _sfxRows = new();
    private readonly List<RowEntry> _vfxRows = new();

    private Label _sfxCount = null!;
    private Label _vfxCount = null!;
    private Control _stage = null!;

    public static NFxLab Create() => new() { Name = "FxLab" };

    public override void _Ready()
    {
        // Silence the main-menu music that is still playing when we replace the menu scene.
        NAudioManager.Instance?.StopMusic();

        // Size ourselves to the viewport directly instead of relying on parent anchors —
        // NSceneContainer doesn't force its children full-rect, so an anchored-only child
        // ends up with a stale rect.
        UpdateSize();
        GetViewport().SizeChanged += UpdateSize;

        ColorRect bg = new() { Color = new Color(0.09f, 0.09f, 0.12f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        VBoxContainer rootBox = new();
        rootBox.SetAnchorsPreset(LayoutPreset.FullRect);
        rootBox.OffsetLeft = 12;
        rootBox.OffsetTop = 8;
        rootBox.OffsetRight = -12;
        rootBox.OffsetBottom = -8;
        AddChild(rootBox);

        rootBox.AddChild(BuildToolbar());

        HBoxContainer columns = new();
        columns.SizeFlagsVertical = SizeFlags.ExpandFill;
        columns.AddThemeConstantOverride("separation", 12);
        rootBox.AddChild(columns);

        List<FxEntry> sfx = CollectSfxEntries();
        List<FxEntry> vfx = CollectVfxEntries();

        columns.AddChild(BuildListPanel($"SFX ({sfx.Count})", sfx, _sfxRows, PlaySfx, out _sfxCount));

        // The vfx column is tabbed: scene files vs the code-driven N*Vfx node factories.
        TabContainer vfxTabs = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.8f,
            CustomMinimumSize = new Vector2(344, 0),
        };
        Control scenesPanel = BuildListPanel($"VFX ({vfx.Count})", vfx, _vfxRows, PlayVfx, out _vfxCount, withOneShotToggle: true);
        scenesPanel.Name = "VFX Scenes";
        vfxTabs.AddChild(scenesPanel);
        Control nodesPanel = BuildNodeVfxPanel();
        nodesPanel.Name = "VFX Nodes";
        vfxTabs.AddChild(nodesPanel);
        columns.AddChild(vfxTabs);

        columns.AddChild(BuildStage());

        MainFile.Logger.Info($"FX Lab ready: {sfx.Count} sfx, {vfx.Count} vfx");
    }

    private void UpdateSize()
    {
        Position = Vector2.Zero;
        Size = GetViewportRect().Size;
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= UpdateSize;
    }

    // ------------------------------------------------------------------ UI --

    private Control BuildToolbar()
    {
        HBoxContainer bar = new();
        bar.AddThemeConstantOverride("separation", 12);

        Label title = new() { Text = "Witch FX Lab" };
        title.AddThemeFontSizeOverride("font_size", 26);
        bar.AddChild(title);

        Control spacer = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bar.AddChild(spacer);

        Button killVfx = new() { Text = "Kill VFX" };
        killVfx.Pressed += () =>
        {
            foreach (Node child in _stage.GetChildren())
            {
                child.QueueFree();
            }
        };
        bar.AddChild(killVfx);

        Button stop = new() { Text = "Stop Sounds" };
        stop.Pressed += () =>
        {
            NAudioManager.Instance?.StopAllLoops();
            NDebugAudioManager.Instance?.StopAll();
        };
        bar.AddChild(stop);

        Button quit = new() { Text = "Quit Game" };
        quit.Pressed += () => GetTree().Quit();
        bar.AddChild(quit);

        return bar;
    }

    private Control BuildListPanel(
        string header,
        List<FxEntry> entries,
        List<RowEntry> rowStore,
        Action<FxEntry> play,
        out Label countLabel,
        bool withOneShotToggle = false)
    {
        VBoxContainer panel = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.8f, // lists 20% narrower than the stage's share
        };
        panel.CustomMinimumSize = new Vector2(344, 0);

        Label headerLabel = new() { Text = header };
        headerLabel.AddThemeFontSizeOverride("font_size", 20);
        panel.AddChild(headerLabel);

        LineEdit search = new() { PlaceholderText = "filter...", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        CheckBox? oneShotToggle = null;
        if (withOneShotToggle)
        {
            HBoxContainer searchRow = new();
            searchRow.AddChild(search);
            oneShotToggle = new CheckBox { Text = "one-shot only" };
            oneShotToggle.AddThemeFontSizeOverride("font_size", 12);
            searchRow.AddChild(oneShotToggle);
            panel.AddChild(searchRow);
        }
        else
        {
            panel.AddChild(search);
        }

        countLabel = new Label { Text = $"{entries.Count} shown" };
        countLabel.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(countLabel);

        ScrollContainer scroll = new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        panel.AddChild(scroll);

        VBoxContainer list = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(list);

        // Group headers cover the leaf rows added while recursing into them, tracked as a
        // [From, To) range into rowStore; a header hides when none of its leaves are visible.
        List<(Control Header, int From, int To)> headers = new();

        void AddRow(FxEntry entry, int depth)
        {
            HBoxContainer row = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            if (depth > 0)
            {
                row.AddChild(new Control { CustomMinimumSize = new Vector2(depth * 16, 0) });
            }

            Button playBtn = new() { Text = "Play", CustomMinimumSize = new Vector2(52, 0) };
            playBtn.Pressed += () => play(entry);
            row.AddChild(playBtn);

            Button copyBtn = new() { Text = "Copy", CustomMinimumSize = new Vector2(52, 0) };
            copyBtn.Pressed += () =>
            {
                DisplayServer.ClipboardSet(entry.CopyText);
                MainFile.Logger.Info($"FX Lab: copied '{entry.CopyText}'");
            };
            row.AddChild(copyBtn);

            Label pathLabel = new()
            {
                Text = entry.Name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TooltipText = entry.CopyText,
                MouseFilter = MouseFilterEnum.Stop, // so the tooltip shows
            };
            pathLabel.AddThemeFontSizeOverride("font_size", 14);
            row.AddChild(pathLabel);

            if (entry.Tag.Length > 0)
            {
                Label tagLabel = new() { Text = entry.Tag };
                tagLabel.AddThemeFontSizeOverride("font_size", 12);
                tagLabel.AddThemeColorOverride("font_color", entry.Tag switch
                {
                    "[one-shot]" => new Color(0.45f, 0.9f, 0.45f),
                    "[loop]" => new Color(0.95f, 0.75f, 0.35f),
                    "[mp3]" => new Color(0.5f, 0.8f, 0.95f),
                    _ => new Color(0.6f, 0.6f, 0.7f),
                });
                row.AddChild(tagLabel);
            }

            list.AddChild(row);
            rowStore.Add(new RowEntry(row, entry.Name.ToLowerInvariant(), entry.Tag));
        }

        void AddLevel(IEnumerable<FxEntry> subset, int depth)
        {
            List<FxEntry> here = subset.ToList();
            foreach (FxEntry entry in here.Where(e => e.Group.Length == depth))
            {
                AddRow(entry, depth);
            }
            foreach (IGrouping<string, FxEntry> group in here
                         .Where(e => e.Group.Length > depth)
                         .GroupBy(e => e.Group[depth])
                         .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                HBoxContainer headerRow = new();
                headerRow.AddChild(new Control { CustomMinimumSize = new Vector2(depth * 16, 0) });
                Label groupLabel = new() { Text = group.Key };
                groupLabel.AddThemeFontSizeOverride("font_size", 14);
                groupLabel.AddThemeColorOverride("font_color", new Color(0.72f, 0.72f, 0.9f));
                headerRow.AddChild(groupLabel);
                list.AddChild(headerRow);

                int from = rowStore.Count;
                AddLevel(group, depth + 1);
                headers.Add((headerRow, from, rowStore.Count));
            }
        }

        AddLevel(entries, 0);

        Label count = countLabel;
        CheckBox? toggle = oneShotToggle;
        void ApplyFilter()
        {
            string t = search.Text.Trim().ToLowerInvariant();
            bool oneShotOnly = toggle?.ButtonPressed ?? false;
            int shown = 0;
            bool[] vis = new bool[rowStore.Count];
            for (int i = 0; i < rowStore.Count; i++)
            {
                RowEntry r = rowStore[i];
                bool visible = (t.Length == 0 || r.Filter.Contains(t))
                               && (!oneShotOnly || r.Tag == "[one-shot]");
                vis[i] = visible;
                r.Row.Visible = visible;
                if (visible)
                {
                    shown++;
                }
            }
            foreach ((Control headerRow, int from, int to) in headers)
            {
                bool any = false;
                for (int i = from; i < to && !any; i++)
                {
                    any = vis[i];
                }
                headerRow.Visible = any;
            }
            count.Text = $"{shown} shown";
        }
        search.TextChanged += _ => ApplyFilter();
        if (toggle != null)
        {
            toggle.Toggled += _ => ApplyFilter();
        }

        return panel;
    }

    private Control BuildStage()
    {
        PanelContainer stagePanel = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(420, 0),
        };

        ColorRect stageBg = new() { Color = new Color(0.04f, 0.04f, 0.06f) };
        stagePanel.AddChild(stageBg);

        _stage = new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ClipContents = true,
        };
        stagePanel.AddChild(_stage);

        return stagePanel;
    }

    // ------------------------------------------------------- node factories --

    /// <summary>
    /// A code-driven N*Vfx node the lab can spawn. <c>Spawn</c> gets the picked tint, or null when
    /// tinting is off — each lambda falls back to the effect's canonical base-game color.
    /// TintTag legend: [Color] = factory takes a raw Color; [VfxColor] = snapped to the nearest
    /// enum value the node implements; [Modulate] = no tint param, whole node modulated; [fixed].
    /// </summary>
    private sealed record NodeVfxEntry(string Name, string TintTag, Func<NFxLab, Color?, Node?> Spawn, double MaxLifetime = VfxFailsafeSeconds);

    private ColorPickerButton _nodeTint = null!;
    private CheckBox _nodeTintEnabled = null!;

    // Stage-local spawn anchors. Factories set GlobalPosition BEFORE the node is parented, which
    // Godot stores as the local position — so stage-local coordinates land inside the stage panel.
    private Vector2 StageCenter => _stage.Size * 0.5f;
    private Vector2 StageGround => _stage.Size * 0.5f + new Vector2(0f, _stage.Size.Y * 0.2f);

    private static readonly Color WitchPurple = new("A803FF");

    private List<NodeVfxEntry> CollectNodeVfxEntries() => new()
    {
        // -- occult / hex ---------------------------------------------------
        new("NSmallMagicMissileVfx — hex bolt", "[Color]",
            (lab, tint) => NSmallMagicMissileVfx.Create(lab.StageCenter, tint ?? new Color("50b598"))),
        new("NLargeMagicMissileVfx — big ritual bolt", "[Color]",
            (lab, tint) => NLargeMagicMissileVfx.Create(lab.StageGround, tint ?? new Color("8c2447"))),
        new("NSpookyScreamVfx — ghost wail", "[Modulate]",
            (lab, tint) => Modulated(NSpookyScreamVfx.Create(lab.StageCenter), tint)),
        new("NScreamVfx — wail + shake", "[Modulate]",
            (lab, tint) => Modulated(NScreamVfx.Create(lab.StageCenter), tint)),

        // -- brew / poison --------------------------------------------------
        new("NPoisonImpactVfx — toxic burst", "[Modulate]",
            (lab, tint) => Modulated(NPoisonImpactVfx.Create(lab.StageCenter), tint)),
        new("NGoopyImpactVfx — cauldron splat", "[Color]",
            (lab, tint) => NGoopyImpactVfx.Create(lab.StageCenter, tint ?? Colors.Green)),
        new("NSplashVfx — potion splash", "[Color]",
            (lab, tint) => NSplashVfx.Create(lab.StageCenter, tint ?? Colors.Green)),

        // -- nature / bramble -----------------------------------------------
        new("NWormyImpactVfx — ground tendrils", "[Modulate]",
            (lab, tint) => Modulated(NWormyImpactVfx.Create(lab.StageGround, lab.StageCenter), tint)),
        new("NBg/FgGroundSpikeVfx — thorn field", "[VfxColor]",
            (lab, tint) => lab.SpawnSpikeField(tint)),

        // -- celestial ------------------------------------------------------
        new("NStarryImpactVfx — star burst", "[Modulate]",
            (lab, tint) => Modulated(NStarryImpactVfx.Create(lab.StageCenter), tint)),
        new("NBigSlashVfx — crescent arc", "[Color]",
            (lab, tint) => NBigSlashVfx.Create(lab.StageCenter, true, tint ?? new Color("a380ff"))),
        new("NBigSlashImpactVfx — crescent impact", "[Color]",
            (lab, tint) => NBigSlashImpactVfx.Create(lab.StageCenter, 60f, tint ?? new Color("80dbff"))),

        // -- fire / smoke / atmosphere --------------------------------------
        new("NFireBurstVfx — flame pop", "[Color]",
            (lab, tint) => NFireBurstVfx.Create(lab.StageGround, 1f, tint ?? new Color("ff8b57"))),
        new("NFireBurningVfx — lingering flame", "[Color]",
            (lab, tint) => NFireBurningVfx.Create(lab.StageGround, 1f, true, tint ?? new Color("ff8b57"))),
        new("NRestSmokeVfx — ambient smoke (viewport center)", "[Modulate]",
            (lab, tint) => Modulated(NRestSmokeVfx.Create(), tint)),
        new("NRainVfx — rain (3s kill)", "[Modulate]",
            (lab, tint) => Modulated(NRainVfx.Create(), tint), LoopVfxKillSeconds),
        new("NSmokyVignetteVfx — screen-edge fog", "[Color]",
            (lab, tint) =>
            {
                Color c = tint ?? new Color(0.8f, 0.3f, 0.8f);
                return NSmokyVignetteVfx.Create(
                    new Color(c.R, c.G, c.B, 0.66f),
                    new Color(c.R * 4f, c.G * 4f, c.B * 4f, 0.33f));
            }),
        new("NAdditiveOverlayVfx — screen flash", "[VfxColor]",
            (lab, tint) => NAdditiveOverlayVfx.Create(NearestVfxColor(tint ?? WitchPurple,
                (VfxColor.Green, new Color("00ff15")), (VfxColor.Purple, new Color("b300ff")),
                (VfxColor.Blue, Colors.Blue), (VfxColor.White, Colors.White),
                (VfxColor.Cyan, Colors.Cyan), (VfxColor.Gold, new Color("ffd700"))))),
        new("NDoomOverlayVfx — hell flash (singleton)", "[fixed]",
            (lab, tint) => NDoomOverlayVfx.GetOrCreate()),

        // -- creature-targeted factories, mirrored --------------------------
        // These factories only use the Creature to resolve a spawn position (plus private
        // config fields), so the lab instantiates the same scene and sets the same state
        // with stage positions standing in. NLiquidOverlayVfx is the one true exclusion:
        // it paints a shader overlay onto the creature's rig, so there is nothing to show
        // without a real creature.
        new("NSporeImpactVfx — spore poof", "[Color]",
            (lab, tint) => NSporeImpactVfx.Create(lab.StageGround, tint ?? new Color("83eb85"))),
        new("NGaseousImpactVfx — gas burst", "[Color]",
            (lab, tint) => NGaseousImpactVfx.Create(lab.StageCenter, tint ?? new Color("83eb85"))),
        new("NGroundFireVfx — ground fire", "[VfxColor]",
            (lab, tint) => MirrorVfx<NGroundFireVfx>(NGroundFireVfx.AssetPaths.First(), lab.StageGround,
                ("_vfxColor", NearestVfxColor(tint ?? WitchPurple,
                    (VfxColor.Red, new Color("ff4020")), (VfxColor.Green, new Color("2fa800")),
                    (VfxColor.Blue, new Color("0099cd")), (VfxColor.Purple, new Color("7821ff")),
                    (VfxColor.White, Colors.White), (VfxColor.Black, Colors.Black))))),
        new("NSmokePuffVfx — smoke puff", "[VfxColor]",
            (lab, tint) => MirrorVfx<NSmokePuffVfx>(NSmokePuffVfx.AssetPaths.First(), lab.StageCenter,
                ("_color", IsPurplish(tint ?? WitchPurple) ? NSmokePuffVfx.SmokePuffColor.Purple : NSmokePuffVfx.SmokePuffColor.Green))),
        new("NStabVfx — cursed needle", "[VfxColor]",
            (lab, tint) => MirrorVfx<NStabVfx>("res://scenes/vfx/stab_vfx.tscn", null,
                ("_vfxColor", NearestVfxColor(tint ?? WitchPurple,
                    (VfxColor.Red, new Color("ff4020")), (VfxColor.Green, new Color("00a52f")),
                    (VfxColor.Blue, new Color("007bdd")), (VfxColor.Purple, WitchPurple),
                    (VfxColor.White, Colors.White), (VfxColor.Cyan, Colors.Cyan), (VfxColor.Gold, new Color("ffd700")))),
                ("_facingEnemies", true),
                ("_creatureCenter", lab.StageCenter))),
        new("NThinSliceVfx — thin slice", "[VfxColor]",
            (lab, tint) => MirrorVfx<NThinSliceVfx>(NThinSliceVfx.AssetPaths.First(), null,
                ("_vfxColor", NearestVfxColor(tint ?? Colors.Cyan,
                    (VfxColor.Red, new Color("ff9900")), (VfxColor.Green, new Color("00a52f")),
                    (VfxColor.Blue, new Color("007bdd")), (VfxColor.Purple, WitchPurple),
                    (VfxColor.White, Colors.White), (VfxColor.Cyan, Colors.Cyan))),
                ("_creatureCenter", lab.StageCenter))),
        new("NFireSmokePuffVfx — fiery pop", "[Modulate]",
            (lab, tint) => Modulated(MirrorVfx<NFireSmokePuffVfx>(NFireSmokePuffVfx.AssetPaths.First(), lab.StageCenter), tint)),
        new("NPowerUpVfx — power-up aura", "[Modulate]",
            (lab, tint) => Modulated(MirrorVfx<NPowerUpVfx>(NPowerUpVfx.AssetPaths.First(), lab.StageCenter), tint)),
        new("NPowerUpVfx — ghostly aura (familiar summon)", "[Modulate]",
            (lab, tint) => Modulated(MirrorVfx<NPowerUpVfx>(NPowerUpVfx.AssetPaths.Last(), lab.StageCenter), tint)),
    };

    /// <summary>
    /// Lab-only stand-in for the creature-taking factories: instantiate the same scene and set the
    /// same private fields the factory would (reflection is fine in a debug tool). The creature is
    /// only ever used for a spawn position, which <paramref name="position" /> replaces.
    /// </summary>
    private static T MirrorVfx<T>(string scenePath, Vector2? position, params (string Field, object Value)[] privateFields) where T : Node2D
    {
        T node = PreloadManager.Cache.GetScene(scenePath).Instantiate<T>(PackedScene.GenEditState.Disabled);
        foreach ((string field, object value) in privateFields)
        {
            HarmonyLib.AccessTools.Field(typeof(T), field).SetValue(node, value);
        }
        if (position.HasValue)
        {
            node.Position = position.Value;
        }
        return node;
    }

    // Rough purple-vs-green split for NSmokePuffVfx's two-value enum.
    private static bool IsPurplish(Color c) => c.R + c.B > c.G * 1.5f;

    // ------------------------------------------------- copy-to-code snippets --

    // Float-component Color ctor — hex strings clamp to LDR and can't express overblown/HDR values.
    private static string Rgba(Color c) => c.A < 0.999f
        ? FormattableString.Invariant($"new Color({c.R:0.###}f, {c.G:0.###}f, {c.B:0.###}f, {c.A:0.###}f)")
        : FormattableString.Invariant($"new Color({c.R:0.###}f, {c.G:0.###}f, {c.B:0.###}f)");

    private static string Rgba(Color? tint, string fallback) => Rgba(tint ?? new Color(fallback));

    private static string AddLine(string expr) =>
        $"NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely({expr});";

    private static string AddModulated(string expr, Color? tint) => tint.HasValue
        ? $"var vfx = {expr};\nif (vfx != null)\n{{\n    vfx.Modulate = {Rgba(tint.Value)};\n    {AddLine("vfx")}\n}}"
        : AddLine(expr);

    private static string SnappedVfxColor(Color? tint, Color fallback, params (VfxColor Value, Color Approx)[] options) =>
        $"VfxColor.{NearestVfxColor(tint ?? fallback, options)}";

    /// <summary>
    /// Card-ready spawn code per node entry, with the picked tint baked in (null = canonical color).
    /// Placeholders: `target` = Creature, `targetCenter`/`targetGround` = Vector2 (see
    /// NCreature.VfxSpawnPosition / GetBottomOfHitbox). KEYS MUST MATCH the entry names in
    /// CollectNodeVfxEntries, and the VfxColor option sets mirror the spawn lambdas — keep in sync.
    /// </summary>
    private static readonly Dictionary<string, Func<Color?, string>> CopySnippets = new(StringComparer.Ordinal)
    {
        ["NSmallMagicMissileVfx — hex bolt"] = tint =>
            $"var missile = NSmallMagicMissileVfx.Create(targetCenter, {Rgba(tint, "50b598")});\n{AddLine("missile")}\nawait Cmd.Wait(missile?.WaitTime ?? 0f);",
        ["NLargeMagicMissileVfx — big ritual bolt"] = tint =>
            $"var missile = NLargeMagicMissileVfx.Create(targetGround, {Rgba(tint, "8c2447")});\n{AddLine("missile")}\nawait Cmd.Wait(missile?.WaitTime ?? 0f);",
        ["NSpookyScreamVfx — ghost wail"] = tint => AddModulated("NSpookyScreamVfx.Create(targetCenter)", tint),
        ["NScreamVfx — wail + shake"] = tint => AddModulated("NScreamVfx.Create(targetCenter)", tint),
        ["NPoisonImpactVfx — toxic burst"] = tint => AddModulated("NPoisonImpactVfx.Create(target)", tint),
        ["NGoopyImpactVfx — cauldron splat"] = tint => AddLine($"NGoopyImpactVfx.Create(targetCenter, {Rgba(tint, "00ff00")})"),
        ["NSplashVfx — potion splash"] = tint => AddLine($"NSplashVfx.Create(targetCenter, {Rgba(tint, "00ff00")})"),
        ["NWormyImpactVfx — ground tendrils"] = tint => AddModulated("NWormyImpactVfx.Create(target)", tint),
        ["NBg/FgGroundSpikeVfx — thorn field"] = tint =>
        {
            string vc = SnappedVfxColor(tint, WitchPurple,
                (VfxColor.Red, new Color("ff4020")), (VfxColor.Purple, WitchPurple),
                (VfxColor.White, Colors.White), (VfxColor.Cyan, Colors.Cyan), (VfxColor.Gold, new Color("ffd700")));
            return $"for (int i = 0; i < 6; i++)\n{{\n    Vector2 pos = targetGround + new Vector2((GD.Randf() - 0.5f) * 240f, 0f);\n    Node2D? spike = i % 2 == 0\n        ? NBgGroundSpikeVfx.Create(pos, movingRight: true, {vc})\n        : NFgGroundSpikeVfx.Create(pos, movingRight: true, {vc});\n    {AddLine("spike")}\n}}\n// not preloaded — ExtraRunAssetPaths: \"res://scenes/vfx/bg_ground_spike_vfx.tscn\", \"res://scenes/vfx/fg_ground_spike_vfx.tscn\"";
        },
        ["NStarryImpactVfx — star burst"] = tint => AddModulated("NStarryImpactVfx.Create(targetCenter)", tint),
        ["NBigSlashVfx — crescent arc"] = tint => AddLine($"NBigSlashVfx.Create(targetCenter, facingRight: true, {Rgba(tint, "a380ff")})"),
        ["NBigSlashImpactVfx — crescent impact"] = tint => AddLine($"NBigSlashImpactVfx.Create(targetCenter, 60f, {Rgba(tint, "80dbff")})"),
        ["NFireBurstVfx — flame pop"] = tint => AddLine($"NFireBurstVfx.Create(targetGround, 1f, {Rgba(tint, "ff8b57")})"),
        ["NFireBurningVfx — lingering flame"] = tint => AddLine($"NFireBurningVfx.Create(targetGround, 1f, true, {Rgba(tint, "ff8b57")})"),
        ["NRestSmokeVfx — ambient smoke (viewport center)"] = tint =>
            AddModulated("NRestSmokeVfx.Create()", tint) + "\n// not preloaded — ExtraRunAssetPaths: NRestSmokeVfx.AssetPaths",
        ["NRainVfx — rain (3s kill)"] = tint =>
            AddModulated("NRainVfx.Create()", tint) + "\n// NRainVfx never frees itself — QueueFree it when the effect should end.\n// not preloaded — ExtraRunAssetPaths: \"res://scenes/vfx/whole_screen/vfx_rain.tscn\"",
        ["NSmokyVignetteVfx — screen-edge fog"] = tint =>
        {
            Color c = tint ?? new Color(0.8f, 0.3f, 0.8f);
            return FormattableString.Invariant(
                $"NGame.Instance.CurrentRunNode.GlobalUi.AddChildSafely(NSmokyVignetteVfx.Create(\n    new Color({c.R:0.##}f, {c.G:0.##}f, {c.B:0.##}f, 0.66f),\n    new Color({c.R * 4f:0.##}f, {c.G * 4f:0.##}f, {c.B * 4f:0.##}f, 0.33f)));");
        },
        ["NAdditiveOverlayVfx — screen flash"] = tint =>
            AddLine($"NAdditiveOverlayVfx.Create({SnappedVfxColor(tint, WitchPurple, (VfxColor.Green, new Color("00ff15")), (VfxColor.Purple, new Color("b300ff")), (VfxColor.Blue, Colors.Blue), (VfxColor.White, Colors.White), (VfxColor.Cyan, Colors.Cyan), (VfxColor.Gold, new Color("ffd700")))})")
            + "\n// not preloaded — ExtraRunAssetPaths: \"res://scenes/vfx/additive_overlay_vfx.tscn\"",
        ["NDoomOverlayVfx — hell flash (singleton)"] = tint =>
            "NDoomOverlayVfx? overlay = NDoomOverlayVfx.GetOrCreate();\nif (overlay != null && overlay.GetParent() == null)\n{\n    NGame.Instance.CurrentRunNode.GlobalUi.AddChildSafely(overlay);\n}\n// not preloaded — ExtraRunAssetPaths: \"res://scenes/vfx/doom_overlay_vfx.tscn\"",
        ["NSporeImpactVfx — spore poof"] = tint => AddLine($"NSporeImpactVfx.Create(target, {Rgba(tint, "83eb85")})"),
        ["NGaseousImpactVfx — gas burst"] = tint => AddLine($"NGaseousImpactVfx.Create(target, {Rgba(tint, "83eb85")})"),
        ["NGroundFireVfx — ground fire"] = tint =>
            AddLine($"NGroundFireVfx.Create(target, {SnappedVfxColor(tint, WitchPurple, (VfxColor.Red, new Color("ff4020")), (VfxColor.Green, new Color("2fa800")), (VfxColor.Blue, new Color("0099cd")), (VfxColor.Purple, new Color("7821ff")), (VfxColor.White, Colors.White), (VfxColor.Black, Colors.Black))})")
            + "\n// not preloaded — ExtraRunAssetPaths: NGroundFireVfx.AssetPaths (Witch.ExtraAssetPaths already includes it)",
        ["NSmokePuffVfx — smoke puff"] = tint =>
            AddLine($"NSmokePuffVfx.Create(target, NSmokePuffVfx.SmokePuffColor.{(IsPurplish(tint ?? WitchPurple) ? "Purple" : "Green")})")
            + "\n// not preloaded — ExtraRunAssetPaths: NSmokePuffVfx.AssetPaths",
        ["NStabVfx — cursed needle"] = tint =>
            $".WithHitVfxNode(t => NStabVfx.Create(t, facingEnemies: true, {SnappedVfxColor(tint, WitchPurple, (VfxColor.Red, new Color("ff4020")), (VfxColor.Green, new Color("00a52f")), (VfxColor.Blue, new Color("007bdd")), (VfxColor.Purple, WitchPurple), (VfxColor.White, Colors.White), (VfxColor.Cyan, Colors.Cyan), (VfxColor.Gold, new Color("ffd700")))}))\n// not preloaded — ExtraRunAssetPaths: \"res://scenes/vfx/stab_vfx.tscn\"",
        ["NThinSliceVfx — thin slice"] = tint =>
            AddLine($"NThinSliceVfx.Create(target, {SnappedVfxColor(tint, Colors.Cyan, (VfxColor.Red, new Color("ff9900")), (VfxColor.Green, new Color("00a52f")), (VfxColor.Blue, new Color("007bdd")), (VfxColor.Purple, WitchPurple), (VfxColor.White, Colors.White), (VfxColor.Cyan, Colors.Cyan))})"),
        ["NFireSmokePuffVfx — fiery pop"] = tint => AddModulated("NFireSmokePuffVfx.Create(target)", tint),
        ["NPowerUpVfx — power-up aura"] = tint => "NPowerUpVfx.CreateNormal(target); // self-attaches to the combat vfx container",
        ["NPowerUpVfx — ghostly aura (familiar summon)"] = tint => "NPowerUpVfx.CreateGhostly(target); // self-attaches to the combat vfx container",
    };

    private static Node? Modulated(CanvasItem? node, Color? tint)
    {
        if (node != null && tint.HasValue)
        {
            node.Modulate = tint.Value;
        }
        return node;
    }

    // Snap a picked color to the nearest VfxColor value the node actually implements
    // (several nodes throw or no-op on unimplemented enum values).
    private static VfxColor NearestVfxColor(Color c, params (VfxColor Value, Color Approx)[] options)
    {
        VfxColor best = options[0].Value;
        float bestDist = float.MaxValue;
        foreach ((VfxColor value, Color approx) in options)
        {
            float d = (c.R - approx.R) * (c.R - approx.R)
                      + (c.G - approx.G) * (c.G - approx.G)
                      + (c.B - approx.B) * (c.B - approx.B);
            if (d < bestDist)
            {
                bestDist = d;
                best = value;
            }
        }
        return best;
    }

    // Spikes are single tiny sprites; spawn a scattered handful so the preview reads as a field.
    private Node SpawnSpikeField(Color? tint)
    {
        VfxColor color = NearestVfxColor(tint ?? WitchPurple,
            (VfxColor.Red, new Color("ff4020")), (VfxColor.Purple, WitchPurple),
            (VfxColor.White, Colors.White), (VfxColor.Cyan, Colors.Cyan), (VfxColor.Gold, new Color("ffd700")));
        Node2D cluster = new() { Name = "SpikeField" };
        for (int i = 0; i < 6; i++)
        {
            Vector2 pos = StageGround + new Vector2((GD.Randf() - 0.5f) * 240f, (GD.Randf() - 0.5f) * 30f);
            Node2D? spike = i % 2 == 0
                ? NBgGroundSpikeVfx.Create(pos, movingRight: GD.Randf() > 0.5f, color)
                : NFgGroundSpikeVfx.Create(pos, movingRight: GD.Randf() > 0.5f, color);
            if (spike != null)
            {
                cluster.AddChild(spike);
            }
        }
        return cluster;
    }

    private Control BuildNodeVfxPanel()
    {
        VBoxContainer panel = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };

        Label headerLabel = new() { Text = "N*Vfx node factories" };
        headerLabel.AddThemeFontSizeOverride("font_size", 20);
        panel.AddChild(headerLabel);

        HBoxContainer tintRow = new();
        _nodeTintEnabled = new CheckBox { Text = "tint", ButtonPressed = true, TooltipText = "Off = each effect's canonical base-game color" };
        tintRow.AddChild(_nodeTintEnabled);
        _nodeTint = new ColorPickerButton
        {
            Color = WitchPurple,
            CustomMinimumSize = new Vector2(96, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        tintRow.AddChild(_nodeTint);
        panel.AddChild(tintRow);

        Label legend = new() { Text = "[Color] exact tint · [VfxColor] snapped to nearest supported · [Modulate] whole-node tint · [fixed] untintable" };
        legend.AddThemeFontSizeOverride("font_size", 11);
        legend.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        legend.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddChild(legend);

        ScrollContainer scroll = new()
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        panel.AddChild(scroll);

        VBoxContainer list = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(list);

        foreach (NodeVfxEntry entry in CollectNodeVfxEntries())
        {
            HBoxContainer row = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            Button spawnBtn = new() { Text = "Spawn", CustomMinimumSize = new Vector2(60, 0) };
            spawnBtn.Pressed += () => SpawnNodeVfx(entry);
            row.AddChild(spawnBtn);

            Button copyBtn = new()
            {
                Text = "Copy",
                CustomMinimumSize = new Vector2(52, 0),
                TooltipText = "Copy card-ready spawn code using the current tint",
                Disabled = !CopySnippets.ContainsKey(entry.Name),
            };
            copyBtn.Pressed += () =>
            {
                if (CopySnippets.TryGetValue(entry.Name, out Func<Color?, string>? snippet))
                {
                    Color? tint = _nodeTintEnabled.ButtonPressed ? _nodeTint.Color : null;
                    DisplayServer.ClipboardSet(snippet(tint));
                    MainFile.Logger.Info($"FX Lab: copied spawn code for '{entry.Name}'");
                }
            };
            row.AddChild(copyBtn);

            Label nameLabel = new()
            {
                Text = entry.Name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TooltipText = entry.Name,
                MouseFilter = MouseFilterEnum.Stop,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 14);
            row.AddChild(nameLabel);

            Label tagLabel = new() { Text = entry.TintTag };
            tagLabel.AddThemeFontSizeOverride("font_size", 12);
            tagLabel.AddThemeColorOverride("font_color", entry.TintTag switch
            {
                "[Color]" => new Color(0.45f, 0.9f, 0.45f),
                "[VfxColor]" => new Color(0.95f, 0.75f, 0.35f),
                "[Modulate]" => new Color(0.5f, 0.8f, 0.95f),
                _ => new Color(0.6f, 0.6f, 0.7f),
            });
            row.AddChild(tagLabel);

            list.AddChild(row);
        }

        return panel;
    }

    private void SpawnNodeVfx(NodeVfxEntry entry)
    {
        try
        {
            Color? tint = _nodeTintEnabled.ButtonPressed ? _nodeTint.Color : null;
            Node? node = entry.Spawn(this, tint);
            if (node == null)
            {
                MainFile.Logger.Info($"FX Lab: '{entry.Name}' returned null (TestMode guard or missing context)");
                return;
            }
            if (node.GetParent() == null) // NDoomOverlayVfx singleton may already be parented
            {
                _stage.AddChild(node);
            }
            if (node is Control control)
            {
                control.SetAnchorsPreset(LayoutPreset.FullRect); // overlay/vignette style nodes fill the stage
            }
            else if (node is Node2D node2D)
            {
                node2D.Scale *= StageZoom;
            }
            _ = FreeAfterFailsafe(node, entry.MaxLifetime);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"FX Lab: node vfx '{entry.Name}' failed: {e}");
        }
    }

    // ------------------------------------------------------------ playback --

    private static void PlaySfx(FxEntry entry)
    {
        try
        {
            if (entry.PlayKey.StartsWith("event:/", StringComparison.Ordinal))
            {
                NAudioManager.Instance?.PlayOneShot(entry.PlayKey);
            }
            else
            {
                NDebugAudioManager.Instance?.Play(entry.PlayKey);
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"FX Lab: sfx '{entry.PlayKey}' failed: {e}");
        }
    }

    private void PlayVfx(FxEntry entry)
    {
        try
        {
            PackedScene packed = ResourceLoader.Load<PackedScene>(entry.PlayKey);
            Node node = packed.Instantiate<Node>(PackedScene.GenEditState.Disabled);
            _stage.AddChild(node);
            Vector2 center = _stage.GlobalPosition + _stage.Size * 0.5f;
            // Many vfx scenes draw offset from their origin; recenter using their
            // visual bounds (particle visibility rects + sprite rects) when we can.
            // Roots are either Node2D or Control (e.g. hellraiser_sword_vfx) — the two
            // don't share a positioning API, hence the twin branches.
            if (node is Node2D node2D)
            {
                node2D.Scale *= StageZoom;
                node2D.GlobalPosition = center;
                Rect2? bounds = null;
                AccumulateBounds(node2D, Transform2D.Identity, ref bounds, isRoot: true);
                if (bounds.HasValue)
                {
                    node2D.GlobalPosition = center - node2D.GlobalTransform.BasisXform(bounds.Value.GetCenter());
                }
            }
            else if (node is Control control)
            {
                control.Scale *= StageZoom;
                control.GlobalPosition = center;
                Rect2? bounds = null;
                AccumulateBounds(control, Transform2D.Identity, ref bounds, isRoot: true);
                if (bounds.HasValue)
                {
                    control.GlobalPosition = center - control.GetGlobalTransform().BasisXform(bounds.Value.GetCenter());
                }
            }
            KickDormantParticles(node);
            _ = FreeAfterFailsafe(node, entry.MaxLifetime);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"FX Lab: vfx '{entry.PlayKey}' failed: {e}");
        }
    }

    // Building-block sub-scenes (e.g. vfx/common/vfx_common_hit_flare) ship with
    // emitting=false and rely on a parent scene/script calling restart(). If NOTHING in the
    // instanced scene is emitting, kick every emitter so the preview shows something. Scenes
    // where at least one emitter self-plays are left alone — their script/animation drives
    // the rest, and restarting those stages early would double-fire them.
    private static void KickDormantParticles(Node root)
    {
        List<GpuParticles2D> emitters = new();
        CollectEmitters(root, emitters);
        if (emitters.Count == 0 || emitters.Any(p => p.Emitting))
        {
            return;
        }
        foreach (GpuParticles2D p in emitters)
        {
            p.Restart();
        }
    }

    private static void CollectEmitters(Node node, List<GpuParticles2D> sink)
    {
        if (node is GpuParticles2D p)
        {
            sink.Add(p);
        }
        foreach (Node child in node.GetChildren())
        {
            CollectEmitters(child, sink);
        }
    }

    // Estimate a scene's visual bounds in the root node's local space by merging
    // GpuParticles2D visibility rects, sprite rects, and control rects down the tree.
    // Approximate by design — visibility rects are authored, not simulated — but good
    // enough to keep each vfx roughly centered on the stage.
    private static void AccumulateBounds(Node node, Transform2D xform, ref Rect2? acc, bool isRoot = false)
    {
        // Bounds are expressed in the ROOT's local space: the root's own transform is
        // excluded (the caller re-applies it via GlobalTransform when recentering).
        Transform2D t = !isRoot && node is CanvasItem item ? xform * item.GetTransform() : xform;

        Rect2? local = node switch
        {
            GpuParticles2D p => p.VisibilityRect,
            Sprite2D s when s.Texture != null => s.GetRect(),
            Control c => new Rect2(Vector2.Zero, c.Size),
            _ => null,
        };
        if (local.HasValue)
        {
            Rect2 r = local.Value;
            foreach (Vector2 corner in new[]
                     {
                         r.Position,
                         r.Position + new Vector2(r.Size.X, 0),
                         r.Position + new Vector2(0, r.Size.Y),
                         r.End,
                     })
            {
                Vector2 p = t * corner;
                acc = acc.HasValue ? acc.Value.Expand(p) : new Rect2(p, Vector2.Zero);
            }
        }

        foreach (Node child in node.GetChildren())
        {
            AccumulateBounds(child, t, ref acc);
        }
    }

    // Most vfx scenes free themselves when their particles finish; this catches the ones
    // that don't (looping/overlay scenes) so the stage never accumulates stale nodes.
    private async System.Threading.Tasks.Task FreeAfterFailsafe(Node node, double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        if (IsInstanceValid(node) && !node.IsQueuedForDeletion())
        {
            node.QueueFree();
        }
    }

    // ---------------------------------------------------------- enumeration --

    private static List<FxEntry> CollectSfxEntries()
    {
        List<FxEntry> entries = new();

        foreach (string ev in CollectFmodEventPaths())
        {
            // Group by the path segments (the common "event:/sfx/" root is elided so the
            // tree doesn't start with a single all-encompassing "sfx" header).
            string trimmed = ev["event:/".Length..];
            if (trimmed.StartsWith("sfx/", StringComparison.Ordinal))
            {
                trimmed = trimmed["sfx/".Length..];
            }
            string[] segments = trimmed.Split('/');
            entries.Add(new FxEntry(segments[^1], segments[..^1], ev, ev));
        }

        foreach (string file in ListFiles("res://debug_audio").OrderBy(f => f, StringComparer.Ordinal))
        {
            if (file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                // Copy text is the bare filename — the exact string the tmpSfx params take
                // (.WithHitFx(vfx, sfx, tmpSfx) / NDebugAudioManager.Play).
                entries.Add(new FxEntry(file, new[] { "debug_audio" }, file, file, Tag: "[mp3]"));
            }
        }

        return entries;
    }

    // Ask the FMOD GDExtension (the "FmodServer" engine singleton) for every event
    // description in the loaded banks. Note: only loaded banks are visible — the act music
    // banks load on demand during a run, so music events won't appear here; every sfx bank
    // (Master/sfx/temp_sfx/ambience + strings) is loaded at startup. The event-path strings
    // are not recoverable by scanning the bank bytes (the strings bank interns/fragments
    // them), so the live API is the only reliable source.
    private static List<string> CollectFmodEventPaths()
    {
        HashSet<string> events = new(StringComparer.Ordinal);
        try
        {
            GodotObject fmod = Engine.GetSingleton("FmodServer");
            foreach (Variant v in fmod.Call("get_all_event_descriptions").AsGodotArray())
            {
                GodotObject? desc = v.AsGodotObject();
                if (desc == null)
                {
                    continue;
                }
                string path = desc.HasMethod("get_path")
                    ? desc.Call("get_path").AsString()
                    : desc.HasMethod("get_event_path")
                        ? desc.Call("get_event_path").AsString()
                        : "";
                if (path.StartsWith("event:/", StringComparison.Ordinal))
                {
                    events.Add(path);
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"FX Lab: FmodServer event enumeration failed: {e}");
        }

        if (events.Count == 0)
        {
            MainFile.Logger.Error("FX Lab: no FMOD events found via FmodServer");
        }
        return events.OrderBy(e => e, StringComparer.Ordinal).ToList();
    }

    private static List<FxEntry> CollectVfxEntries()
    {
        List<FxEntry> entries = new();

        // Base-game vfx scenes. Copy text is the "inner path" the code APIs take
        // (VfxCmd.PlayOnCreatureCenter / .WithHitFx): "vfx/vfx_attack_slash".
        List<string> baseScenes = new();
        CollectFilesRecursive("res://scenes/vfx", baseScenes);
        foreach (string scene in NormalizeSceneFiles(baseScenes))
        {
            string inner = scene["res://scenes/".Length..^".tscn".Length];
            entries.Add(MakeVfxEntry(inner, inner, scene));
        }

        // Mod vfx scenes (if any): copy text is the full res:// path, which is what
        // ResourceLoader.Load in mod code takes.
        List<string> modScenes = new();
        CollectFilesRecursive(MainFile.ResPath, modScenes);
        foreach (string scene in NormalizeSceneFiles(modScenes))
        {
            if (scene.Contains("vfx", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(MakeVfxEntry(scene.Replace("res://", ""), scene, scene));
            }
        }

        return entries.OrderBy(e => $"{string.Join('/', e.Group)}/{e.Name}", StringComparer.Ordinal).ToList();
    }

    private static FxEntry MakeVfxEntry(string display, string copyText, string scenePath)
    {
        SceneFxInfo info = AnalyzeScene(scenePath);
        string tag;
        double lifetime = VfxFailsafeSeconds;
        if (info.Scripted)
        {
            // Game C# script drives (and usually frees) the effect; may need combat context.
            tag = "[script]";
        }
        else if (info.Emitters > 0 && info.OneShotEmitters < info.Emitters)
        {
            tag = "[loop]";
            lifetime = LoopVfxKillSeconds;
        }
        else
        {
            tag = "[one-shot]";
        }
        string[] segments = display.Split('/');
        return new FxEntry(segments[^1], segments[..^1], copyText, scenePath, lifetime, tag);
    }

    private sealed record SceneFxInfo(int Emitters, int OneShotEmitters, bool Scripted);

    private static readonly Dictionary<string, SceneFxInfo> _sceneInfoCache = new(StringComparer.Ordinal);

    // Classify a vfx scene by reading its .tscn text (scenes ship as text in the pck):
    // count GPUParticles2D emitters vs one_shot flags, note game-code scripts, and fold in
    // instanced sub-scenes recursively. Heuristic, but drives only the list tag + kill time.
    private static SceneFxInfo AnalyzeScene(string scenePath)
    {
        if (_sceneInfoCache.TryGetValue(scenePath, out SceneFxInfo? cached))
        {
            return cached;
        }
        _sceneInfoCache[scenePath] = new SceneFxInfo(0, 0, false); // cycle guard

        string text = Godot.FileAccess.FileExists(scenePath) ? Godot.FileAccess.GetFileAsString(scenePath) : "";
        int emitters = CountOccurrences(text, "type=\"GPUParticles2D\"");
        int oneShot = CountOccurrences(text, "one_shot = true");
        bool scripted = System.Text.RegularExpressions.Regex.IsMatch(
            text, "\\[ext_resource type=\"Script\"[^\\]]*path=\"res://src/");

        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                     text, "\\[ext_resource type=\"PackedScene\"[^\\]]*path=\"([^\"]+)\""))
        {
            SceneFxInfo sub = AnalyzeScene(m.Groups[1].Value);
            emitters += sub.Emitters;
            oneShot += sub.OneShotEmitters;
            scripted |= sub.Scripted;
        }

        SceneFxInfo info = new(emitters, oneShot, scripted);
        _sceneInfoCache[scenePath] = info;
        return info;
    }

    private static int CountOccurrences(string text, string needle)
    {
        int count = 0;
        int i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }

    // In an exported .pck, .tscn files may be listed as ".tscn.remap"; the original path
    // still loads through the remap. Strip the suffix and dedupe.
    private static IEnumerable<string> NormalizeSceneFiles(IEnumerable<string> files)
    {
        return files
            .Select(f => f.EndsWith(".remap", StringComparison.Ordinal) ? f[..^".remap".Length] : f)
            .Where(f => f.EndsWith(".tscn", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(f => f, StringComparer.Ordinal);
    }

    private static IEnumerable<string> ListFiles(string dirPath)
    {
        using DirAccess? dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            return Array.Empty<string>();
        }
        return dir.GetFiles()
            .Select(f => f.EndsWith(".import", StringComparison.Ordinal) || f.EndsWith(".remap", StringComparison.Ordinal)
                ? f[..f.LastIndexOf('.')]
                : f)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static void CollectFilesRecursive(string dirPath, List<string> sink)
    {
        using DirAccess? dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            return;
        }
        foreach (string file in dir.GetFiles())
        {
            sink.Add($"{dirPath}/{file}");
        }
        foreach (string sub in dir.GetDirectories())
        {
            CollectFilesRecursive($"{dirPath}/{sub}", sink);
        }
    }
}
