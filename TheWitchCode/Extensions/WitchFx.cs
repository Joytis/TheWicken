using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace TheWitch.TheWitchCode.Extensions;

/// <summary>
/// Shared Witch sfx/vfx mechanic signatures. See Docs/sfx-vfx-catalog.md for the
/// base-game asset catalog and Docs/sfx-vfx-proposal.md for the house palette.
/// </summary>
public static class WitchFx
{
    public const string SummonSfx = "event:/sfx/characters/necrobinder/necrobinder_summon";
    public const string HexSfx = "doom_apply.mp3";

    /// <summary>Poison-green tint shared by brew/bramble effects (base-game Noxious Fumes green).</summary>
    public static readonly Color WitchGreen = new("83eb85");
    public static readonly Color Purple = new("ac54b3");

    /// <summary>Duplicate of the Attack state in witch_visuals.tscn — same swing animation, but the
    /// trigger name dodges CreatureCmd.TriggerAnim's "Attack" case, so CustomAttackSfx never plays.</summary>
    public const string SilentAttackTrigger = "AttackSilent";

    /// <summary>Play the attack swing without the character's default attack SFX, so a card can
    /// supply its own via WithAttackerFx/WithHitFx. Call after FromCard.</summary>
    public static AttackCommand WithSilentAttack(this AttackCommand cmd) =>
        cmd.WithAttackerAnim(SilentAttackTrigger, cmd.Attacker!.Player.Character.AttackAnimDelay);

    private static readonly System.Reflection.FieldInfo AttackerAnimNameField =
        HarmonyLib.AccessTools.Field(typeof(AttackCommand), "_attackerAnimName");

    /// <summary>Set just the attacker anim delay — the per-hit pacing knob (each hit awaits this
    /// before damage) — keeping whatever anim trigger is already set (Attack, AttackSilent, ...).
    /// Call after FromCard; combine with OnlyPlayAnimOnce for the fastest multi-hit rattle.</summary>
    public static AttackCommand WithAttackDelay(this AttackCommand cmd, float time) =>
        cmd.WithAttackerAnim((string?)AttackerAnimNameField.GetValue(cmd), time);

    private static void Attach(Node2D? vfx) => NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);

    /// <summary>Familiar summon signature: ghostly aura + summon sting. NPowerUpVfx self-attaches.
    /// Cards using this must include NPowerUpVfx.AssetPaths in ExtraRunAssetPaths.</summary>
    public static void SummonFlourish(Creature owner)
    {
        NPowerUpVfx.CreateGhostly(owner);
        SfxCmd.Play(SummonSfx);
    }

    /// <summary>Bramble-gain signature: green spore burst on the gainer (globally preloaded).</summary>
    public static void SporePuff(Creature owner) => Attach(NSporeImpactVfx.Create(owner, WitchGreen));

    /// <summary>Bramble retaliation/thorn hit: swamp-green thin slice on the target.
    /// NThinSliceVfx is NOT globally preloaded — every Bramble-granting card registers it.</summary>
    public static void BrambleSlice(Creature target) => Attach(NThinSliceVfx.Create(target, VfxColor.Swamp));

    /// <summary>Hex signature: occult gaze + doom sting on the target (globally preloaded).</summary>
    public static void HexGaze(Creature target)
    {
        VfxCmd.PlayOnCreatureCenter(target, VfxCmd.gazePath);
        NDebugAudioManager.Instance?.Play(HexSfx);
    }

    /// <summary>Hex explosion signature: purple ground fire on the target (globally preloaded via Witch.ExtraAssetPaths).</summary>
    public static void PurpleFlame(Creature target) => Attach(NGroundFireVfx.Create(target, VfxColor.Purple));

    /// <summary>Green gas burst (globally preloaded) — plague/rot effects.</summary>
    public static void GreenGas(Creature target) => Attach(NGaseousImpactVfx.Create(target, WitchGreen));

    /// <summary>Sparkle sting for in-combat card/potion upgrades (base NCardUpgradeVfx is silent).</summary>
    public static void EnchantShimmer() => SfxCmd.Play("event:/sfx/ui/enchant_shimmer");

    /// <summary>Potion-splash visual with custom tint.</summary>
    public static void Splash(Creature target, Color tint) => NCombatRoom.Instance?.PlaySplashVfx(target, tint);
}
