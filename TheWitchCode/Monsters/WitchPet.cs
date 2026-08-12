using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Base for the Witch's cosmetic familiar "pets" — small sprites that hang around the
/// player like the Byrd summoned by Byrdpip. They never take damage and do nothing on
/// their turn; the familiar token-cards (Wisdom/Ferocity) carry all of the mechanics.
///
/// Registration is automatic: MonsterModel : AbstractModel, so ModelDb.Monster&lt;T&gt;()
/// resolves any subclass via reflection. Pets are combat-scoped, so they vanish at combat
/// end on their own — nothing to clean up.
///
/// VISUAL: we can't ship our own creature_visuals .tscn (the mod's Godot editor/export can't
/// bind the game's NCreatureVisuals C# script). Instead we reuse the base game's "rocket"
/// scene — a valid NCreatureVisuals whose %Visuals body is an empty, textureless Sprite2D —
/// and swap our own texture onto it at spawn via <see cref="WitchPetVisualsPatch" />. So we
/// ship only PNGs (which pack/load fine), no scene authoring required.
/// </summary>
public abstract class WitchPet : MonsterModel
{
    // Pets are never meant to take damage or die; huge HP + hidden bar mirrors Byrdpip.
    public override int MinInitialHp => 9999;
    public override int MaxInitialHp => 9999;
    public override bool IsHealthBarVisible => false;

    public virtual string PetScenePath => "familiar_visuals.tscn".CharacterScenePath();

    // Base-game host: Node2D + NCreatureVisuals script + empty Sprite2D "%Visuals" + markers.
    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/rocket");

    /// <summary>res:// path to the sprite texture swapped onto the host body at spawn.</summary>
    public abstract string PetFileName { get; }
    public string TexturePath => (PetFileName + ".png").PetImagePath();
    public string ConfigPath => (PetFileName + ".tres").PetConfigPath();

    /// <summary>
    /// Which familiar power stack this pet represents, stamped on the mutable clone by
    /// FamiliarPower.SyncPets. PetVisuals matches animation events against this pair.
    /// Plain C# state — cosmetic only, and mid-combat state is never restored, so it needs no serialization.
    /// </summary>
    public Powers.FamiliarPower? SourcePower { get; set; }
    public int StackIndex { get; set; }

    /// <summary>Sprite scale + offset so the pet sits nicely at the player's feet. Tune per-pet.</summary>
    public virtual float SpriteScale => 0.4f;
    public virtual Vector2 SpriteOffset => new(0f, -40f);

    // Cosmetic only: a single self-looping no-op move, mirroring Byrdpip's monster model.
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
    }
}
