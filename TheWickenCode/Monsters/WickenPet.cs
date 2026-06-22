using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace TheWicken.TheWickenCode.Monsters;

/// <summary>
/// Base for the Wicken's cosmetic familiar "pets" — small sprites that hang around the
/// player like the Byrd summoned by Byrdpip. They never take damage and do nothing on
/// their turn; the familiar token-cards (Wisdom/Ferocity) carry all of the mechanics.
///
/// Registration is automatic: MonsterModel : AbstractModel, so ModelDb.Monster&lt;T&gt;()
/// resolves any subclass via reflection. No pool / Act membership / manual register needed.
/// Pets are combat-scoped, so they vanish at combat end on their own — nothing to clean up.
///
/// PLACEHOLDER VISUAL: we reuse the game's existing Byrd creature scene rather than ship our
/// own. A mod-shipped creature_visuals .tscn would need its root to bind the game's
/// NCreatureVisuals C# script (and, for a spine body, the MegaSpine GDExtension) — both live
/// in the game project, not this standalone mod project, so an exported mod scene can't
/// reliably bind them. Reusing the game scene gives the exact Byrd look with zero asset work
/// and no redistribution. When we have real familiar art, swap VisualsPath to a custom scene
/// (and build/bind its NCreatureVisuals root via the Godot editor against the game project).
/// </summary>
public abstract class WickenPet : MonsterModel
{
    // Pets are never meant to take damage or die; huge HP + hidden bar mirrors Byrdpip.
    public override int MinInitialHp => 9999;
    public override int MaxInitialHp => 9999;
    public override bool IsHealthBarVisible => false;

    // Reuse the game's Byrd scene as the shared placeholder. SceneHelper prepends res://scenes/.
    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/byrdpip");

    // Base SetupSkins is a no-op; the Byrd skeleton renders blank without a concrete skin, so
    // pick one (Byrdpip.SkinOptions[0]). Mirrors the Byrd monster model.
    public override void SetupSkins(MegaSprite spine, MegaSkeleton skeleton)
    {
        MegaSkeletonDataResource data = skeleton.GetData();
        skeleton.SetSkin(data.FindSkin("version1"));
        skeleton.SetSlotsToSetupPose();
    }

    // Cosmetic only: a single self-looping no-op move, mirroring Byrdpip's monster model.
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
    }
}
