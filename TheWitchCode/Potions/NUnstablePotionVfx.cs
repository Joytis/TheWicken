using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Nodes.Potions;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Ambient belt overlay for Unstable potions: green pulse on the bottle sprite, rising fizz
/// bubbles, and an occasional angry rattle. The particles are authored in
/// unstable_potion_vfx.tscn (this script is its root) and unstable_potion_burst.tscn — tune them
/// in the Godot editor, including their framing — nothing here repositions authored nodes. Both
/// scenes are rooted on a full-rect Control holding a centre-anchored "Center" node, so they size
/// themselves to whatever Control they're dropped onto and the emitters sit on the bottle without
/// any hardcoded coordinates. Only the bottle tint/rattle stays in code, since it drives the
/// game's own NPotion sprite rather than a node we own.
/// Attached/removed by UnstablePotionPatches based on the potion's Unstable mark.
/// </summary>
[GlobalClass]
public partial class NUnstablePotionVfx : Control
{
    public const string NodeName = "UnstableAmbientVfx";

    /// <summary>How long the bottle rattles before it blows. Callers wait this out.</summary>
    public const float ShakeDuration = 0.2f;

    private static readonly string AmbientScenePath = "unstable_potion_vfx.tscn".VfxScenePath();
    private static readonly string BurstScenePath = "unstable_potion_burst.tscn".VfxScenePath();

    private NPotion _potion = null!;
    private double _t;
    private float _shakeLeft;

    /// <summary>Speed of the green simmer on the bottle sprite.</summary>
    [Export]
    public float PulseSpeed { get; set; } = 2.4f;

    /// <summary>Peak strength of the green tint, 0-1.</summary>
    [Export]
    public float MaxTint { get; set; } = 0.45f;

    /// <summary>Peak tilt, in degrees, of the wind-up shake before the explosion.</summary>
    [Export]
    public float ShakeAngle { get; set; } = 14f;

    /// <summary>Instantiates the ambient scene for a belt potion, or null if the scene didn't bind.</summary>
    public static NUnstablePotionVfx? Create(NPotion potion)
    {
        // Pattern cast, not Instantiate<T>: a .tscn whose script didn't bind instantiates as a
        // plain Control and the generic form would throw (same as the pet scenes).
        if (PreloadManager.Cache.GetScene(AmbientScenePath)
                .Instantiate(PackedScene.GenEditState.Disabled) is not NUnstablePotionVfx vfx)
        {
            MainFile.Logger.Warn("unstable_potion_vfx.tscn didn't bind its mod script; skipping ambient vfx.");
            return null;
        }

        vfx.Name = NodeName;
        vfx._potion = potion;
        return vfx;
    }

    public override void _Process(double delta)
    {
        _t += delta;
        if (!IsInstanceValid(_potion) || _potion.Image == null)
        {
            return;
        }
        // Slow green simmer on the bottle.
        float pulse = 0.5f + 0.5f * Mathf.Sin((float)_t * PulseSpeed);

        _potion.Image.Modulate = Colors.White.Lerp(WitchFx.WitchGreen, MaxTint * pulse);
        // Occasional rattle: a sharp gate opens for a beat every few seconds.
        float gate = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((float)_t * 0.9f)), 12f);
        float amplitude = gate * 4f;
        float frequency = 30f;

        // Wind-up: once the blast is queued the idle rattle gives way to a hard shake that
        // builds right up to the moment of the explosion.
        if (_shakeLeft > 0f)
        {
            _shakeLeft -= (float)delta;
            float ramp = 1f - Mathf.Clamp(_shakeLeft / ShakeDuration, 0f, 1f);
            amplitude = ShakeAngle * Mathf.Lerp(0.35f, 1f, ramp);
            frequency = 70f;
        }

        _potion.Image.PivotOffset = _potion.Image.Size * 0.5f;
        _potion.Image.RotationDegrees = amplitude * Mathf.Sin((float)_t * frequency);
    }

    /// <summary>Starts the pre-explosion wind-up shake. Runs for <see cref="ShakeDuration" /> seconds.</summary>
    public void Shake() => _shakeLeft = ShakeDuration;

    public override void _ExitTree()
    {
        if (IsInstanceValid(_potion) && _potion.Image != null)
        {
            _potion.Image.Modulate = Colors.White;
            _potion.Image.RotationDegrees = 0f;
        }
    }

    /// <summary>
    /// One-shot explosion burst + glass-shatter sting, spawned on the potion's belt holder.
    /// The scene root is added as-is — whatever node type it is — and anchors itself to the
    /// holder, so framing is the scene's business, not this method's.
    /// </summary>
    public static void PlayExplosion(Node host)
    {
        NDebugAudioManager.Instance?.Play("glass_orb_evoke.mp3", 1f, PitchVariance.Medium);

        Node burst = PreloadManager.Cache.GetScene(BurstScenePath).Instantiate(PackedScene.GenEditState.Disabled);
        host.AddChild(burst);
        StartEmitters(burst);
        
        host.GetTree().CreateTimer(1.5f).Timeout += () =>
        {
            if (IsInstanceValid(burst))
            {
                burst.QueueFree();
            }
        };
    }

    /// <summary>
    /// Plays every emitter in an instantiated scene. These are one-shot emitters, which the editor
    /// saves with emitting off, so adding the scene alone shows nothing — each one has to be told
    /// to fire. Duck-typed on "restart" rather than a node-type check, so GPU emitters, CPU
    /// emitters and nested sub-scenes all work without this method knowing what's in the scene.
    /// </summary>
    private static void StartEmitters(Node node)
    {
        if (node.HasMethod("restart"))
        {
            node.Set("emitting", true);
            node.Call("restart");
        }
        foreach (Node child in node.GetChildren())
        {
            StartEmitters(child);
        }
    }
}
