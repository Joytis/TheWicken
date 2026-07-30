using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Potions;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Ambient belt overlay for Unstable potions: green pulse on the bottle sprite, rising fizz
/// bubbles, and an occasional angry rattle. Built procedurally — no scene/.pck assets needed.
/// Attached/removed by UnstablePotionPatches based on the potion's Unstable mark.
/// </summary>
public partial class NUnstablePotionVfx : Control
{
    public const string NodeName = "UnstableAmbientVfx";

    private const float _pulseSpeed = 2.4f;
    private const float _maxTint = 0.45f;

    private static Texture2D? _dot;

    private NPotion _potion = null!;
    private double _t;

    /// <summary>Soft radial dot shared by the fizz + burst particles.</summary>
    private static Texture2D Dot
    {
        get
        {
            if (_dot == null)
            {
                Gradient gradient = new()
                {
                    Offsets = [0f, 1f],
                    Colors = [Colors.White, new Color(1f, 1f, 1f, 0f)],
                };
                _dot = new GradientTexture2D
                {
                    Width = 16,
                    Height = 16,
                    Fill = GradientTexture2D.FillEnum.Radial,
                    FillFrom = new Vector2(0.5f, 0.5f),
                    FillTo = new Vector2(0.5f, 0f),
                    Gradient = gradient,
                };
            }
            return _dot;
        }
    }

    public static NUnstablePotionVfx Create(NPotion potion) => new()
    {
        Name = NodeName,
        MouseFilter = MouseFilterEnum.Ignore,
        _potion = potion,
    };

    public override void _Ready()
    {
        GpuParticles2D fizz = new()
        {
            Amount = 5,
            Lifetime = 1.1f,
            Preprocess = 1.1f,
            LocalCoords = true,
            Texture = Dot,
            Position = _potion.Size * 0.5f,
            ProcessMaterial = new ParticleProcessMaterial
            {
                EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
                EmissionSphereRadius = 20f,
                Direction = new Vector3(0f, -1f, 0f),
                Spread = 25f,
                InitialVelocityMin = 8f,
                InitialVelocityMax = 20f,
                Gravity = new Vector3(0f, -25f, 0f),
                ScaleMin = 0.35f,
                ScaleMax = 0.7f,
                Color = new Color(WitchFx.WitchGreen, 0.8f),
                LifetimeRandomness = 0.4f,
            },
        };
        AddChild(fizz);
    }

    public override void _Process(double delta)
    {
        _t += delta;
        if (!IsInstanceValid(_potion) || _potion.Image == null)
        {
            return;
        }
        // Slow green simmer on the bottle.
        float pulse = 0.5f + 0.5f * Mathf.Sin((float)_t * _pulseSpeed);
        _potion.Image.Modulate = Colors.White.Lerp(WitchFx.WitchGreen, _maxTint * pulse);
        // Occasional rattle: a sharp gate opens for a beat every few seconds.
        float gate = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((float)_t * 0.9f)), 12f);
        _potion.Image.PivotOffset = _potion.Image.Size * 0.5f;
        _potion.Image.RotationDegrees = gate * Mathf.Sin((float)_t * 30f) * 4f;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_potion) && _potion.Image != null)
        {
            _potion.Image.Modulate = Colors.White;
            _potion.Image.RotationDegrees = 0f;
        }
    }

    /// <summary>One-shot explosion burst + glass-shatter sting on a belt holder.</summary>
    public static void PlayExplosion(Control host)
    {
        SfxCmd.Play("event:/sfx/characters/defect/defect_glass");
        GpuParticles2D burst = new()
        {
            Amount = 24,
            Lifetime = 0.7f,
            OneShot = true,
            Explosiveness = 1f,
            Emitting = true,
            LocalCoords = true,
            Texture = Dot,
            Position = host.Size * 0.5f,
            ProcessMaterial = new ParticleProcessMaterial
            {
                Direction = new Vector3(0f, -1f, 0f),
                Spread = 180f,
                InitialVelocityMin = 60f,
                InitialVelocityMax = 140f,
                Gravity = new Vector3(0f, 160f, 0f),
                ScaleMin = 0.2f,
                ScaleMax = 0.5f,
                Color = new Color(WitchFx.WitchGreen, 0.9f),
                LifetimeRandomness = 0.3f,
            },
        };
        host.AddChild(burst);
        host.GetTree().CreateTimer(1.5f).Timeout += () =>
        {
            if (IsInstanceValid(burst))
            {
                burst.QueueFree();
            }
        };
    }
}
