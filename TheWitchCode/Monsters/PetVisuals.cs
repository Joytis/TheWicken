using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Node script on the root of the pet scene (witch_pet_scene.tscn), instantiated by
/// <see cref="WitchPetVisualsPatch" /> and populated from a <see cref="PetConfig" />.
///
/// Populate is called right after Instantiate, BEFORE the node enters the tree — _Ready
/// has not run yet, so child nodes are resolved here, not cached in _Ready.
/// </summary>
[GlobalClass]
public partial class PetVisuals : Node2D
{
    private AnimationPlayer _animationPlayer = null!;
    private WitchPet? _pet;
    private Node2D _visualsRoot = null!;
    private Node2D _shadow = null!;
    private Node2D _skillVfx = null!;
    private Node2D _attackVfx = null!;

    // Static-event lifecycle: subscribe/unsubscribe must mirror tree membership, or the
    // event ends up holding a delegate to a freed node (ObjectDisposedException on fire).
    public override void _EnterTree() => FamiliarPower.AnimationRequested += OnAnimationRequested;
    public override void _ExitTree() => FamiliarPower.AnimationRequested -= OnAnimationRequested;

    private void OnAnimationRequested(FamiliarPower power, int stackIndex, FamiliarPetAnim anim)
    {
        if (_pet != null && ReferenceEquals(_pet.SourcePower, power) && _pet.StackIndex == stackIndex)
        {
            PlayAnimation(anim);
        }
    }

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        // Non-looping one-shots (attack/skill) fall back to idle when they finish.
        _animationPlayer.AnimationFinished += _ => PlayAnimation("idle");

        // Slight per-pet playback-speed variance so a crowd of pets doesn't bob in unison.
        // Cosmetic + local-only, so plain engine randomness is fine (no game RNG involved).
        _animationPlayer.SpeedScale = 0.95f + GD.Randf() * 0.1f;

        _visualsRoot = GetNode<Node2D>("VisualsRoot");
        _shadow = GetNode<Node2D>("Shadow");
        _skillVfx = GetNode<Node2D>("Vfx/Skill");
        _attackVfx = GetNode<Node2D>("Vfx/Attack");

        PlayAnimation("idle");
    }

    // The shadow is a sibling of VisualsRoot, so it inherits none of the animation's
    // scale/rotation/hop — it only tracks the lunge's horizontal movement.
    public override void _Process(double delta)
    {
        _shadow.Position = new Vector2(_visualsRoot.Position.X, _shadow.Position.Y);
    }

    /// <summary>Play a pet reaction: attack/skill restart their vfx; create is the card-production flourish.</summary>
    public void PlayAnimation(FamiliarPetAnim anim)
    {
        switch (anim)
        {
            case FamiliarPetAnim.Attack:
                PlayAnimation("attack");
                RestartParticles(_attackVfx);
                break;
            case FamiliarPetAnim.Create:
                PlayAnimation("create");
                break;
            default:
                PlayAnimation("skill");
                RestartParticles(_skillVfx);
                break;
        }
    }

    private void PlayAnimation(string name)
    {
        // Play() is a no-op when the same animation is already running (per-hit re-requests
        // during a multi-hit) — rewind instead so every request restarts the reaction.
        if (_animationPlayer.CurrentAnimation == name)
        {
            _animationPlayer.Seek(0.0, update: true);
        }
        else
        {
            _animationPlayer.Play(name);
        }
    }

    private static void RestartParticles(Node2D container)
    {
        foreach (GpuParticles2D particles in container.GetChildren().OfType<GpuParticles2D>())
        {
            particles.Restart();
        }
    }

    public void Populate(PetConfig config, WitchPet pet)
    {
        _pet = pet;
        Sprite2D sprite = GetNode<Sprite2D>("VisualsRoot/Visuals");
        sprite.Texture = config.Texture;

        // Pivot at the sprite's bottom-center: with Centered on, shifting the draw rect up by
        // half the texture height puts the node origin at the feet — simpler animations.
        if (config.Texture is { } tex)
        {
            sprite.Offset = new Vector2(0f, -tex.GetHeight() / 2f);
        }

        // Scale the sprite only — VisualsRoot belongs to the animations.
        sprite.Scale = Vector2.One * config.VisualsScale;

        // Vfx match the sprite's footprint. Resolved directly — Populate runs before _Ready caches them.
        foreach (string vfxPath in new[] { "Vfx/Skill", "Vfx/Attack" })
        {
            GetNode<Node2D>(vfxPath).Scale = Vector2.One * config.VisualsScale;
        }

        GetNode<Node2D>("Shadow").Visible = config.HasShadow;

        Position = config.Offset;
    }
}
