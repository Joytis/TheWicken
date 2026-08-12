using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Ui;

/// <summary>
/// One clickable potion in the <see cref="NChoosePotionScreen" /> grid: the game's own
/// <see cref="NPotion" /> visual (real potion art + outline; BaseLib routes modded art through it)
/// wrapped in an <see cref="NButton" />, with the base-game corner-bracket selection reticle on
/// hover/focus. Authored in potion_choice_cell.tscn (this script is its root); the potion child is
/// still created in code because <see cref="NPotion" /> needs its model before _Ready. Hover/focus =
/// reticle + scale tween + the potion's real hover tips (the NRelicBasicHolder shape); clicks are
/// handled externally via the Released signal.
/// </summary>
[GlobalClass]
public partial class NPotionChoiceCell : NButton
{
    private const float _cellSize = 120f;
    // Art fills ~70% of the tile; the rest is breathing room inside the cell.
    private const float _potionScale = 1.4f;

    public static readonly string scenePath = "ui/potion_choice_cell.tscn".CharacterScenePath();

    private PotionModel _model = null!;
    private NPotion? _potionNode;
    private NSelectionReticle? _reticle;
    private Tween? _hoverTween;

    public PotionModel Model => _model;

    public static NPotionChoiceCell? Create(PotionModel potion)
    {
        if (TestMode.IsOn)
        {
            return null;
        }
        // Pattern cast, not Instantiate<T>: a .tscn whose script didn't bind instantiates as a
        // plain Control and the generic form would throw.
        if (PreloadManager.Cache.GetScene(scenePath)
                .Instantiate(PackedScene.GenEditState.Disabled) is not NPotionChoiceCell cell)
        {
            MainFile.Logger.Warn("potion_choice_cell.tscn didn't bind its mod script; skipping cell.");
            return null;
        }
        cell.Name = $"NPotionChoiceCell-{potion.Id}";
        cell._model = potion;
        return cell;
    }

    public override void _Ready()
    {
        ConnectSignals();
        // Scene-instanced base-game reticle (needs the src/ junction so the editor resolves its script).
        // Fallback: if the instanced node arrived script-less, spawn the game's scene directly.
        _reticle = GetNodeOrNull<NSelectionReticle>("%SelectionReticle");
        if (_reticle == null)
        {
            GetNodeOrNull("%SelectionReticle")?.QueueFreeSafely();
            if (ResourceLoader.Load<PackedScene>("res://scenes/ui/selection_reticle.tscn")
                    .Instantiate(PackedScene.GenEditState.Disabled) is NSelectionReticle direct)
            {
                _reticle = direct;
                _reticle.Position = Vector2.Zero;
                _reticle.Size = Vector2.One * _cellSize;
                this.AddChildSafely(_reticle);
            }
        }
        _potionNode = NPotion.Create(_model);
        if (_potionNode != null)
        {
            this.AddChildSafely(_potionNode);
            // Control.Position places the 60x60 rect's TOP-LEFT relative to the cell's origin, so
            // centering means (cell - 60) / 2. The scene's pivot is the rect center, so scaling
            // stays centered too.
            _potionNode.Position = Vector2.One * ((_cellSize - 60f) * 0.5f);
            _potionNode.Scale = Vector2.One * _potionScale;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _hoverTween?.Kill();
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        _reticle?.OnSelect();
        if (_potionNode != null)
        {
            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(_potionNode, "scale", Vector2.One * _potionScale * 1.15f, 0.05);
        }
        NHoverTipSet.CreateAndShow(this, _model.HoverTips, HoverTipAlignment.Center)
            ?.SetGlobalPosition(GlobalPosition + Vector2.Down * Size.Y * 1.2f);
    }

    protected override void OnUnfocus()
    {
        _reticle?.OnDeselect();
        if (_potionNode != null)
        {
            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(_potionNode, "scale", Vector2.One * _potionScale, 1.0)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        }
        NHoverTipSet.Remove(this);
    }
}
