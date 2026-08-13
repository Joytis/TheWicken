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
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Vfx;

/// <summary>
/// Rats projectile throw + impact, duplicated from the base game's NShivThrowVfx.
/// Authored in vfx_rats_throw.tscn (this script is its root).
/// Not globally preloaded — cards spawning it must list <see cref="scenePath"/> in ExtraRunAssetPaths.
/// </summary>
[GlobalClass]
public partial class NRatsThrowVfx : Node2D
{
    private static readonly StringName _color = new StringName("color");

    public static readonly string scenePath = "vfx_rats_throw.tscn".VfxScenePath();

    // Hard-coded against vfx_rats_throw.tscn (exported fields don't bind in this editor setup):
    // throw/impact = every emitter under the matching container; modulate = everything except
    // the rat-textured emitters, which keep their own colors.
    private readonly List<GpuParticles2D> _throwParticles = [];
    private readonly List<GpuParticles2D> _impactParticles = [];
    private readonly List<GpuParticles2D> _modulateParticles = [];

    private Color? _pendingTint;

    private CancellationTokenSource? _cts;

    public static NRatsThrowVfx? Create(Creature owner, Creature? target, Color tint)
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

    public static NRatsThrowVfx? Create(Vector2 throwerCenterPosition, Vector2 targetCenterPosition, Color tint)
    {
        if (TestMode.IsOn)
        {
            return null;
        }
        // Instantiate<T> throws when the .tscn script didn't bind — a no-bind scene is a
        // malformed asset; fail loud.
        NRatsThrowVfx vfx = PreloadManager.Cache.GetScene(scenePath)
            .Instantiate<NRatsThrowVfx>(PackedScene.GenEditState.Disabled);
        vfx.GlobalPosition = targetCenterPosition;
        vfx.ApplyRotation(throwerCenterPosition, targetCenterPosition);
        vfx.ApplyTint(tint);
        return vfx;
    }

    // Nodes aren't in the tree yet when Create() calls this, so stash the tint; _Ready applies it
    // once the particle lists are resolved.
    public void ApplyTint(Color tint)
    {
        _pendingTint = tint;
    }

    private void ResolveParticles()
    {
        // This is gross, but my Godot isn't showing my nodes for some reason. Oh well. 
        foreach (Node child in GetNode("throw_container").GetChildren())
        {
            if (child is GpuParticles2D p)
            {
                _throwParticles.Add(p);
            }
        }
        foreach (Node child in GetNode("impact_container").GetChildren())
        {
            if (child is GpuParticles2D p)
            {
                _impactParticles.Add(p);
            }
        }

        // Rat-textured emitters keep their authored colors; everything else takes the tint.
        _modulateParticles.AddRange(
            _throwParticles.Concat(_impactParticles).Where(p => !p.Name.ToString().Contains("rat")));
    }

    public void ApplyRotation(Vector2 throwerPosition, Vector2 targetPosition)
    {
        Vector2 delta = targetPosition - throwerPosition;
        RotationDegrees = Mathf.RadToDeg(Mathf.Atan2(delta.Y, delta.X));
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
        for (int i = 0; i < _throwParticles.Count; i++)
        {
            _throwParticles[i].Restart();
        }
        await Cmd.Wait(0.15f, _cts.Token);
        for (int i = 0; i < _impactParticles.Count; i++)
        {
            _impactParticles[i].Restart();
        }
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        await Cmd.Wait(2f, _cts.Token);
        this.QueueFreeSafely();
    }
}
