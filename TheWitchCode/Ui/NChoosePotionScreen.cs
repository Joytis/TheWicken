using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Ui;

/// <summary>
/// Mid-combat "choose a potion" overlay: a 4-column grid of <see cref="NPotionChoiceCell" />s under the
/// base-game common banner, pushed on <see cref="NOverlayStack" /> (shared dim backstop). Structure cloned
/// from NChooseARelicSelection; layout authored in choose_potion_screen.tscn (this script is its root):
/// banner + a full-width band from 60% viewport height down, with the grid shrink-centered inside it so
/// rows sit top-center under the title and grow downward. Force-pick: there is no Skip button — the
/// screen resolves only by choosing. Await <see cref="PotionSelected" />. Show via
/// <see cref="PotionSelectCmd.FromChoosePotionScreen" /> for the MP-synced flow, not directly.
/// </summary>
[GlobalClass]
public partial class NChoosePotionScreen : Control, IOverlayScreen
{
    private const int _columns = 4;

    public static readonly string scenePath = "ui/choose_potion_screen.tscn".CharacterScenePath();

    private readonly TaskCompletionSource<PotionModel?> _completionSource = new();
    private IReadOnlyList<PotionModel> _potions = null!;
    private LocString _header = null!;
    private GridContainer _grid = null!;
    private Tween? _fadeTween;

    public NetScreenType ScreenType => NetScreenType.CardSelection;

    public bool UseSharedBackstop => true;

    public Control? DefaultFocusedControl => _grid.GetChildOrNull<Control>(0);

    public static NChoosePotionScreen? ShowScreen(IReadOnlyList<PotionModel> potions, LocString header)
    {
        if (TestMode.IsOn || NOverlayStack.Instance == null)
        {
            return null;
        }
        // Pattern cast, not Instantiate<T>: a .tscn whose script didn't bind instantiates as a
        // plain Control and the generic form would throw.
        if (PreloadManager.Cache.GetScene(scenePath)
                .Instantiate(PackedScene.GenEditState.Disabled) is not NChoosePotionScreen screen)
        {
            MainFile.Logger.Warn("choose_potion_screen.tscn didn't bind its mod script; skipping selection screen.");
            return null;
        }
        screen.Name = "NChoosePotionScreen";
        screen._potions = potions;
        screen._header = header;
        NOverlayStack.Instance.Push(screen);
        return screen;
    }

    public override void _Ready()
    {
        // Scene-instanced base-game banner (requires the src/ junction at export). DIAGNOSTIC in
        // progress: the instanced node has arrived script-less at runtime even with the junction —
        // log what actually got instantiated, and fall back to a direct runtime load so the screen
        // stays functional.
        Node? bannerNode = GetNodeOrNull("%Banner");
        NCommonBanner? banner = bannerNode as NCommonBanner;
        MainFile.Logger.Info(
            $"[NChoosePotionScreen] instanced Banner: clrType={bannerNode?.GetType().FullName ?? "null"} " +
            $"script={(bannerNode?.GetScript().Obj as Script)?.ResourcePath ?? "none"}");
        if (banner == null)
        {
            Node direct = ResourceLoader.Load<PackedScene>("res://scenes/ui/common_banner.tscn")
                .Instantiate(PackedScene.GenEditState.Disabled);
            MainFile.Logger.Info($"[NChoosePotionScreen] direct-loaded banner: clrType={direct.GetType().FullName}");
            bannerNode?.QueueFreeSafely();
            banner = direct as NCommonBanner;
            if (banner != null)
            {
                this.AddChildSafely(banner);
            }
            else
            {
                direct.QueueFreeSafely();
            }
        }
        if (banner != null)
        {
            banner.label.SetTextAutoSize(_header.GetRawText());
            banner.AnimateIn();
        }

        _grid = GetNode<GridContainer>("%Grid");
        _grid.Columns = _columns;

        foreach (PotionModel potion in _potions)
        {
            NPotionChoiceCell? cell = NPotionChoiceCell.Create(potion);
            _grid.AddChild(cell);
            cell!.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => SelectCell(cell)));
            CreateTween()
                .TweenProperty(cell, "modulate", Colors.White, 1.0)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic)
                .From(Colors.Black);
        }

        WireControllerFocus();
    }

    /// <summary>Non-wrapping 4-column grid navigation; a partial last row is reachable from anywhere above it.</summary>
    private void WireControllerFocus()
    {
        List<NPotionChoiceCell> cells = _grid.GetChildren().OfType<NPotionChoiceCell>().ToList();
        int last = cells.Count - 1;
        for (int i = 0; i < cells.Count; i++)
        {
            Control cell = cells[i];
            int col = i % _columns;
            Control below = i + _columns <= last
                ? cells[i + _columns]
                : (i / _columns < last / _columns ? cells[last] : cell);
            cell.FocusNeighborLeft = (col > 0 ? cells[i - 1] : cell).GetPath();
            cell.FocusNeighborRight = (col < _columns - 1 && i < last ? cells[i + 1] : cell).GetPath();
            cell.FocusNeighborTop = (i - _columns >= 0 ? cells[i - _columns] : cell).GetPath();
            cell.FocusNeighborBottom = below.GetPath();
        }
    }

    private void SelectCell(NPotionChoiceCell cell)
    {
        if (!_completionSource.Task.IsCompleted)
        {
            _completionSource.SetResult(cell.Model);
        }
    }

    public async Task<PotionModel?> PotionSelected()
    {
        PotionModel? result = await _completionSource.Task;
        NOverlayStack.Instance?.Remove(this);
        return result;
    }

    public override void _ExitTree()
    {
        if (!_completionSource.Task.IsCompleted)
        {
            _completionSource.SetCanceled();
        }
    }

    public void AfterOverlayOpened()
    {
        Modulate = Colors.Transparent;
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1f, 0.2);
        // Explicit default-focus grab on open (the rewards screen does the same). No-op on mouse+keyboard.
        DefaultFocusedControl?.TryGrabFocus();
    }

    public void AfterOverlayClosed()
    {
        _fadeTween?.Kill();
        this.QueueFreeSafely();
    }

    public void AfterOverlayShown()
    {
        Visible = true;
    }

    public void AfterOverlayHidden()
    {
        Visible = false;
    }
}
