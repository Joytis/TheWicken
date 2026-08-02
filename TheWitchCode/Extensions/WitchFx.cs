using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Extensions;

public static class WitchFx
{
    public const string SummonSfx = "event:/sfx/characters/necrobinder/necrobinder_summon";
    public const string HexSfx = "doom_apply.mp3";

    public static readonly Color WitchGreen = new("83eb85");

    public static readonly Color Purple = new("ac54b3");
    public static readonly Color FireOrange = new("ff8b57");
    public static readonly Color RummageBrown = new ("743323");

    public const string SilentAttackTrigger = "AttackSilent";

    public static void EnchantShimmer() => SfxCmd.Play("event:/sfx/ui/enchant_shimmer");


    public static AttackCommand WithSilentAttack(this AttackCommand cmd) =>
        cmd.WithAttackerAnim(SilentAttackTrigger, cmd.Attacker!.Player.Character.AttackAnimDelay);

    private static readonly System.Reflection.FieldInfo AttackerAnimNameField =
        HarmonyLib.AccessTools.Field(typeof(AttackCommand), "_attackerAnimName");

    public static AttackCommand WithAttackDelay(this AttackCommand cmd, float time) =>
        cmd.WithAttackerAnim((string?)AttackerAnimNameField.GetValue(cmd), time);

    private static void Attach(Node2D? vfx) => NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);

    public static void SummonFlourish(Creature owner)
    {
        NPowerUpVfx.CreateGhostly(owner);
        SfxCmd.Play(SummonSfx);
    }


    public static void SporePuff(Creature owner) => Attach(SporePuffNode(owner));
    public static Node2D? SporePuffNode(Creature target) => NSporeImpactVfx.Create(target, WitchGreen);

    public static void BrambleSlice(Creature target) => Attach(BrambleSliceNode(target));
    public static Node2D? BrambleSliceNode(Creature target) => NThinSliceVfx.Create(target, VfxColor.Swamp);

    public static void HexGaze(Creature target)
    {
        VfxCmd.PlayOnCreatureCenter(target, VfxCmd.gazePath);
        NDebugAudioManager.Instance?.Play(HexSfx);
    }


    public static void PurpleFlame(Creature target) => Attach(PurpleFlameNode(target));
    public static Node2D? PurpleFlameNode(Creature target) => NGroundFireVfx.Create(target, VfxColor.Purple);

    public static void RedFlame(Creature target) => Attach(RedFlameNode(target));
    public static Node2D? RedFlameNode(Creature target) => NGroundFireVfx.Create(target);

    public static void GreenGas(Creature target) => Attach(GreenGasNode(target));
    public static Node2D? GreenGasNode(Creature target) => NGaseousImpactVfx.Create(target, WitchGreen);


    public static void Splash(Creature target, Color tint) => NCombatRoom.Instance?.PlaySplashVfx(target, tint);
    public static Node2D? SplashNode(Creature target, Color tint)
    {
        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(target);
        return nCreature == null ? null : NSplashVfx.Create(nCreature.GetBottomOfHitbox(), tint);
    }

    public static void PlayFlipbook(string innerPath, Vector2 globalPosition, Color? tint = null, float scale = 1f) =>
        Attach(FlipbookNode(innerPath, globalPosition, tint, scale));

    public static void PlayFlipbook(string innerPath, Creature target, Color? tint = null, float scale = 1f) =>
        Attach(FlipbookNode(innerPath, target, tint, scale));


    public static void RipSoulImpact(Creature target) => Attach(RipSoulImpactNode(target));
    public static Node2D? RipSoulImpactNode(Creature target)
    {
        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(target);
        var vfx = NStarryImpactVfx.Create(nCreature!.VfxSpawnPosition);
        if (vfx != null)
        {
            vfx.Modulate = new Color(0.284f, 1.353f, 1.253f);
        }
        return vfx;
        
    }

    public static GpuParticles2D FlipbookNode(string innerPath, Vector2 globalPosition, Color? tint = null, float scale = 1f)
    {
        GpuParticles2D particles = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache
            .GetScene(SceneHelper.GetScenePath(innerPath))
            .Instantiate<GpuParticles2D>(PackedScene.GenEditState.Disabled);
        particles.GlobalPosition = globalPosition;
        particles.Scale *= scale;
        if (tint.HasValue)
        {
            particles.Modulate = tint.Value;
        }
        particles.Finished += particles.QueueFree;
        particles.Ready += particles.Restart;
        return particles;
    }

    public static GpuParticles2D? FlipbookNode(string innerPath, Creature target, Color? tint = null, float scale = 1f)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(target);
        return node == null ? null : FlipbookNode(innerPath, node.VfxSpawnPosition, tint, scale);
    }

    public static void FlameBurst(Creature target, float scale, Color? tint = null) => Attach(FlameBurstNode(target, scale, tint));
    public static Node2D? FlameBurstNode(Creature target, float scale, Color? tint = null)
    {
        NCreature? nCreature = NCombatRoom.Instance?.GetCreatureNode(target);
        return nCreature == null
            ? null
            : NFireBurstVfx.Create(nCreature.GetBottomOfHitbox(), scale, tint ?? FireOrange);
    }

}
