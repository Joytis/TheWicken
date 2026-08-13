using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Hijacks creature-visual creation for <see cref="WitchPet" />s: the pet reuses the base
/// game's textureless "rocket" host scene (a valid NCreatureVisuals), and this postfix
/// injects our own pet scene under its %Visuals body, populated from the pet's PetConfig
/// .tres.
///
/// Runs right after the scene is instantiated, before it enters the tree / _Ready, so the
/// game's NCreatureVisuals._Ready then picks up our modified %Visuals as its body.
/// CreateVisuals isn't virtual, hence the Harmony patch.
/// </summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.CreateVisuals))]
public static class WitchPetVisualsPatch
{
    private static void Postfix(MonsterModel __instance, NCreatureVisuals __result)
    {
        if (__instance is not WitchPet pet || __result == null)
        {
            return;
        }

        // "Visuals" is a direct child of the host root (rocket.tscn and fallback.tscn both have it).
        Sprite2D sprite = __result.GetNode<Sprite2D>("Visuals");
        sprite.Visible = true;
        sprite.Scale = Vector2.One * pet.SpriteScale;

        // GD.Load<T>/Instantiate<T> throw when the .tres/.tscn script didn't bind — fail loud.
        PetConfig cfg = GD.Load<PetConfig>(pet.ConfigPath);
        PetVisuals visuals = GD.Load<PackedScene>(pet.PetScenePath).Instantiate<PetVisuals>();

        sprite.Texture = null;
        // The rocket host's Visuals sprite sits at an upward offset (body center) inside the
        // creature — zero it so the pet scene's origin lands on the creature origin (the feet),
        // otherwise every pet floats a body-height above its spawn marker.
        sprite.Position = Vector2.Zero;
        sprite.AddChild(visuals);
        visuals.Populate(cfg, pet);
    }
}
