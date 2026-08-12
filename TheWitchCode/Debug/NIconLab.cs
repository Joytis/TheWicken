using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Relics;

namespace TheWitch.TheWitchCode.Debug;

/// <summary>
/// Debug-only icon browser (launched via --witch-debug --witch-iconlab, see WitchDebug).
/// Shows every relic and potion — Witch content first, base game underneath — rendered in the
/// same composited states the game uses, so Witch art can be checked for parity against the
/// base-game sprites at a glance:
///
///   Owned       icon over its outline, outline at 50% black. What the top-panel relic tray and
///               the potion belt draw all run long (relic.tscn / potion.tscn defaults).
///   Not seen    icon at 90% black over a 50% white outline (NRelicCollectionEntry /
///               NLabPotionHolder, ModelVisibility.NotSeen).
///   Undiscovered outline flat-tinted with the owning pool's LabOutlineColor at 66% alpha —
///               the silhouette in the relic collection / potion lab. This is the state a
///               missing or misshapen outline shows up in most obviously.
///   Locked      the shared padlock at StsColors.gray; identical for all content, included so
///               the full state set is verifiable.
///   Outline     the raw outline texture, untinted, over the checker — for judging the
///               silhouette itself (dilation radius, edge softness, interior holes).
///
/// Built entirely in code so a plain `dotnet build` ships it (no .pck export needed), matching
/// NFxLab. Cells composite the textures directly rather than instancing NRelic/NPotion: those
/// pull their scene through PreloadManager, which is not populated on the main menu.
/// </summary>
public partial class NIconLab : Control
{
    private const int CellSize = 96;
    private const int Columns = 12;

    private enum IconState
    {
        Owned,
        NotSeen,
        Undiscovered,
        Locked,
        Outline,
    }

    private sealed record Entry(string Name, Texture2D? Icon, Texture2D? Outline, Color PoolTint, bool IsWitch);

    private sealed record Cell(Control Root, TextureRect Icon, TextureRect Outline, string Filter,
        Texture2D? IconTexture, Color PoolTint);

    private readonly List<Cell> _cells = new();

    private IconState _state = IconState.Owned;
    private string _filter = "";
    private Texture2D? _lockedIcon;

    public static NIconLab Create() => new() { Name = "IconLab" };

    public override void _Ready()
    {
        NAudioManager.Instance?.StopMusic();

        // NSceneContainer doesn't force children full-rect; size to the viewport ourselves.
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

        List<Entry> relics = CollectRelics();
        List<Entry> potions = CollectPotions();

        TabContainer tabs = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        Control relicTab = BuildTab(relics);
        relicTab.Name = $"Relics ({relics.Count})";
        tabs.AddChild(relicTab);
        Control potionTab = BuildTab(potions);
        potionTab.Name = $"Potions ({potions.Count})";
        tabs.AddChild(potionTab);
        rootBox.AddChild(tabs);

        ApplyState();

        int witchRelics = relics.Count(e => e.IsWitch);
        int witchPotions = potions.Count(e => e.IsWitch);
        int missing = relics.Concat(potions).Count(e => e.Outline == null);
        MainFile.Logger.Info($"Icon Lab ready: {relics.Count} relics ({witchRelics} witch), "
            + $"{potions.Count} potions ({witchPotions} witch), {missing} without an outline");
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

        Label title = new() { Text = "Witch Icon Lab" };
        title.AddThemeFontSizeOverride("font_size", 26);
        bar.AddChild(title);

        OptionButton state = new();
        foreach (IconState value in Enum.GetValues<IconState>())
        {
            state.AddItem(value.ToString(), (int)value);
        }
        state.Selected = 0;
        state.ItemSelected += index =>
        {
            _state = (IconState)state.GetItemId((int)index);
            ApplyState();
        };
        bar.AddChild(state);

        LineEdit search = new() { PlaceholderText = "filter...", CustomMinimumSize = new Vector2(220, 0) };
        search.TextChanged += text =>
        {
            _filter = text.Trim().ToLowerInvariant();
            ApplyState();
        };
        bar.AddChild(search);

        Control spacer = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bar.AddChild(spacer);

        Button quit = new() { Text = "Quit Game" };
        quit.Pressed += () => GetTree().Quit();
        bar.AddChild(quit);

        return bar;
    }

    private Control BuildTab(List<Entry> entries)
    {
        ScrollContainer scroll = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        VBoxContainer box = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(box);

        AddSection(box, "Witch", entries.Where(e => e.IsWitch).ToList());
        AddSection(box, "Base game", entries.Where(e => !e.IsWitch).ToList());
        return scroll;
    }

    private void AddSection(VBoxContainer parent, string title, List<Entry> entries)
    {
        Label header = new() { Text = $"{title} ({entries.Count})" };
        header.AddThemeFontSizeOverride("font_size", 20);
        parent.AddChild(header);

        GridContainer grid = new() { Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        parent.AddChild(grid);

        foreach (Entry entry in entries)
        {
            grid.AddChild(BuildCell(entry));
        }
    }

    private Control BuildCell(Entry entry)
    {
        VBoxContainer cell = new() { CustomMinimumSize = new Vector2(CellSize, CellSize + 18) };

        // Checkerboard so a white silhouette and an empty cell can't be confused.
        Control frame = new() { CustomMinimumSize = new Vector2(CellSize, CellSize) };
        ColorRect back = new() { Color = new Color(0.62f, 0.62f, 0.66f) };
        back.SetAnchorsPreset(LayoutPreset.FullRect);
        frame.AddChild(back);

        // Outline first so it draws behind the icon, matching show_behind_parent in relic.tscn.
        TextureRect outline = MakeRect(entry.Outline);
        frame.AddChild(outline);
        TextureRect icon = MakeRect(entry.Icon);
        frame.AddChild(icon);
        cell.AddChild(frame);

        Label label = new()
        {
            Text = entry.Name,
            CustomMinimumSize = new Vector2(CellSize, 0),
            ClipText = true,
            TooltipText = entry.Name,
        };
        label.AddThemeFontSizeOverride("font_size", 10);
        cell.AddChild(label);

        _cells.Add(new Cell(cell, icon, outline,
            $"{entry.Name} {(entry.IsWitch ? "witch" : "base")}".ToLowerInvariant(),
            entry.Icon, entry.PoolTint));
        return cell;
    }

    private static TextureRect MakeRect(Texture2D? texture)
    {
        TextureRect rect = new()
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsPreset(LayoutPreset.FullRect);
        return rect;
    }

    // Re-tints every cell for the current state. Colours are lifted from relic.tscn /
    // potion.tscn defaults and from NRelicCollectionEntry / NLabPotionHolder.
    private void ApplyState()
    {
        foreach (Cell cell in _cells)
        {
            cell.Root.Visible = _filter.Length == 0 || cell.Filter.Contains(_filter);
            Color poolTint = cell.PoolTint;
            cell.Icon.Texture = _state == IconState.Locked ? LockedIcon() : cell.IconTexture;

            switch (_state)
            {
                case IconState.Owned:
                    Show(cell, icon: true, outline: true);
                    cell.Icon.SelfModulate = Colors.White;
                    cell.Outline.SelfModulate = StsColors.halfTransparentBlack;
                    break;
                case IconState.NotSeen:
                    Show(cell, icon: true, outline: true);
                    cell.Icon.SelfModulate = StsColors.ninetyPercentBlack;
                    cell.Outline.SelfModulate = StsColors.halfTransparentWhite;
                    break;
                case IconState.Undiscovered:
                    Show(cell, icon: false, outline: true);
                    poolTint.A = 0.66f;
                    cell.Outline.SelfModulate = poolTint;
                    break;
                case IconState.Locked:
                    Show(cell, icon: true, outline: false);
                    cell.Icon.SelfModulate = StsColors.gray;
                    break;
                case IconState.Outline:
                    Show(cell, icon: false, outline: true);
                    cell.Outline.SelfModulate = Colors.White;
                    break;
            }
        }
    }

    private static void Show(Cell cell, bool icon, bool outline)
    {
        cell.Icon.Visible = icon;
        cell.Outline.Visible = outline && cell.Outline.Texture != null;
    }

    private Texture2D? LockedIcon()
    {
        return _lockedIcon ??= ResourceLoader.Load<Texture2D>(
            ImageHelper.GetImagePath("packed/common_ui/locked_model.png"),
            null,
            ResourceLoader.CacheMode.Reuse);
    }

    // ----------------------------------------------------------- collection --

    private static List<Entry> CollectRelics()
    {
        List<Entry> entries = new();
        foreach (RelicModel relic in ModelDb.AllRelics)
        {
            Texture2D? icon = TryLoad(() => relic.Icon, relic.Id.ToString(), "icon");
            Texture2D? outline = TryLoad(() => relic.IconOutline, relic.Id.ToString(), "outline");
            entries.Add(new Entry(relic.Id.Entry.ToLowerInvariant(), icon, outline,
                PoolTint(ModelDb.AllCharacterRelicPools.FirstOrDefault(p => p.AllRelicIds.Contains(relic.Id))?.LabOutlineColor),
                relic is WitchRelic));
        }
        return Sorted(entries);
    }

    private static List<Entry> CollectPotions()
    {
        List<Entry> entries = new();
        foreach (PotionModel potion in ModelDb.AllPotions)
        {
            Texture2D? icon = TryLoad(() => potion.Image, potion.Id.ToString(), "image");
            Texture2D? outline = TryLoad(() => potion.Outline, potion.Id.ToString(), "outline");
            entries.Add(new Entry(potion.Id.Entry.ToLowerInvariant(), icon, outline,
                PoolTint(ModelDb.AllCharacterPotionPools.FirstOrDefault(p => p.AllPotions.Any(x => x.Id == potion.Id))?.LabOutlineColor),
                potion is WitchPotion));
        }
        return Sorted(entries);
    }

    private static List<Entry> Sorted(List<Entry> entries)
    {
        // Missing outlines float to the front of their section — that is the defect this
        // screen exists to catch.
        return entries
            .OrderBy(e => e.Outline != null)
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static Color PoolTint(Color? poolColor) => poolColor ?? StsColors.halfTransparentBlack;

    private static Texture2D? TryLoad(Func<Texture2D?> load, string id, string what)
    {
        try
        {
            return load();
        }
        catch (Exception e)
        {
            MainFile.Logger.Info($"Icon Lab: {id} has no {what} ({e.GetType().Name})");
            return null;
        }
    }
}
