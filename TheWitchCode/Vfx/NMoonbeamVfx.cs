using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Vfx;

/// <summary>
/// Moonbeam hit vfx: an impact at the target, rotated to face away from the caster.
/// Same shape as <see cref="NRatsThrowVfx" /> — authored in vfx_moonbeam.tscn (this script is its
/// root), spawned through <see cref="Create(Creature, Creature?, Color)" /> and usually reached via
/// <c>WitchFx.Moonbeam</c> / <c>WitchFx.MoonbeamNode</c>.
///
/// Every GpuParticles2D under "impact_container" fires together — add emitters there and they are
/// picked up automatically; no code change and no exported fields needed. Emitters named with
/// "moon" keep their authored colors, everything else takes the tint (mirrors the rat-textured
/// emitters).
///
/// Not globally preloaded — cards spawning it must list <see cref="scenePath" /> in
/// ExtraRunAssetPaths.
/// </summary>
[GlobalClass]
public partial class NMoonbeamVfx : Node2D
{
    private static readonly StringName _color = new StringName("color");

    /// <summary>Seconds the node lives after the impact before freeing itself.</summary>
    private const float _lifetime = 2f;

    /// <summary>Seconds for a beam line to collapse from its authored width to nothing — the flash.</summary>
    private const float _beamDecaySeconds = 0.25f;

    public static readonly string scenePath = "vfx_moonbeam.tscn".VfxScenePath();

    // Hard-coded against vfx_moonbeam.tscn (exported fields don't bind in this editor setup):
    // every emitter under impact_container.
    private readonly List<GpuParticles2D> _impactParticles = [];
    private readonly List<GpuParticles2D> _modulateParticles = [];

    // Line2D beams under impact_container. Their width_curve shapes the beam ALONG its length;
    // this collapses the overall width over TIME, which the curve can't express.
    private readonly List<Line2D> _beamLines = [];

    private Color? _pendingTint;

    private CancellationTokenSource? _cts;

    public static NMoonbeamVfx? Create(Creature owner, Creature? target, Color tint)
    {
        if (TestMode.IsOn)
        {
            return null;
        }
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null)
        {
            return null;
        }
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (targetNode == null)
        {
            return null;
        }
        return Create(ownerNode.VfxSpawnPosition, targetNode.VfxSpawnPosition, tint);
    }

    public static NMoonbeamVfx? Create(Vector2 casterCenterPosition, Vector2 targetCenterPosition, Color tint)
    {
        if (TestMode.IsOn)
        {
            return null;
        }
        // Instantiate<T> throws when the .tscn script didn't bind — a no-bind scene is a
        // malformed asset; fail loud.
        NMoonbeamVfx vfx = PreloadManager.Cache.GetScene(scenePath)
            .Instantiate<NMoonbeamVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = targetCenterPosition;
        vfx.ApplyRotation(casterCenterPosition, targetCenterPosition);
        vfx.ApplyTint(tint);
        return vfx;
    }

    // Nodes aren't in the tree yet when Create() calls this, so stash the tint; _Ready applies it
    // once the particle lists are resolved.
    public void ApplyTint(Color tint)
    {
        _pendingTint = tint;
    }

    public void ApplyRotation(Vector2 casterPosition, Vector2 targetPosition)
    {
        Vector2 delta = targetPosition - casterPosition;
        RotationDegrees = Mathf.RadToDeg(Mathf.Atan2(delta.Y, delta.X));
    }

    private void ResolveParticles()
    {
        Collect("impact_container", _impactParticles);

        // Moon-textured emitters keep their authored colors; everything else takes the tint.
        _modulateParticles.AddRange(
            _impactParticles.Where(p => !p.Name.ToString().Contains("moon")));
    }

    private void Collect(string containerName, List<GpuParticles2D> into)
    {
        Node container = GetNode(containerName);
        foreach (Node child in container.GetChildren())
        {
            if (child is GpuParticles2D p)
            {
                into.Add(p);
            }
            else if (child is Line2D line)
            {
                _beamLines.Add(line);
            }
        }
    }

    /// <summary>Collapses each beam line's width to zero over <see cref="_beamDecaySeconds" />, so the
    /// laser reads as a flash rather than a sustained beam. Exponential ease-out: most of the width is
    /// gone almost immediately, with a brief thin tail.</summary>
    private void DecayBeams()
    {
        foreach (Line2D line in _beamLines)
        {
            CreateTween()
                .TweenProperty(line, "width", 0f, _beamDecaySeconds)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
        }
    }

    public override void _Ready()
    {
        ResolveParticles();
        if (_pendingTint is { } tint)
        {
            foreach (GpuParticles2D p in _modulateParticles)
            {
                p.ProcessMaterial = (ParticleProcessMaterial)p.ProcessMaterial.Duplicate();
                p.ProcessMaterial.Set(_color, tint);
            }
        }
        TaskHelper.RunSafely(PlaySequence());
    }

    public override void _ExitTree()
    {
        _cts?.Cancel();
    }

    private async Task PlaySequence()
    {
        _cts = new CancellationTokenSource();
        for (int i = 0; i < _impactParticles.Count; i++)
        {
            _impactParticles[i].Restart();
        }
        DecayBeams();
        await Cmd.Wait(_lifetime, _cts.Token);
        this.QueueFreeSafely();
    }
}
