using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Character;

/// <summary>
/// Builds the Witch's card trail from the script-less card_trail_witch.tscn. The scene cannot
/// ship with the game's NCardTrailVfx/NCardTrail scripts attached: the Godot export strips
/// script references it can't resolve (res://src/... exists only in the game's own tree), so
/// the packed scene instantiates as plain nodes and Instantiate&lt;NCardTrailVfx&gt; would throw.
/// BaseLib has NodeFactory conversions for energy counters / creature visuals but none for
/// trails — this prefix does the same job in code: instantiate the plain scene, rebuild the
/// root as a real NCardTrailVfx, and swap every Line2D for an NCardTrail with its properties.
/// </summary>
[HarmonyPatch(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create))]
public static class WitchCardTrailPatch
{
    private static readonly string TrailPath = "card_trail_witch.tscn".CharacterScenePath();

    private static readonly FieldInfo? NodeToFollowField =
        AccessTools.Field(typeof(NCardTrailVfx), "_nodeToFollow");

    private static bool Prefix(Control card, string characterTrailPath, ref NCardTrailVfx? __result)
    {
        if (characterTrailPath != TrailPath || NodeToFollowField == null)
        {
            return true;
        }

        if (TestMode.IsOn)
        {
            __result = null;
            return false;
        }

        try
        {
            Node source = PreloadManager.Cache.GetScene(characterTrailPath)
                .Instantiate(PackedScene.GenEditState.Disabled);

            // If the scripts ever survive the export (or BaseLib grows a trail factory),
            // the scene is already the real thing — just wire it up.
            if (source is not NCardTrailVfx vfx)
            {
                vfx = new NCardTrailVfx();
                CopyStorageProperties(source, vfx);
                foreach (Node child in source.GetChildren())
                {
                    source.RemoveChild(child);
                    vfx.AddChild(child);
                }
                source.QueueFree();
                // Owners still point at the freed source scene root; ReplaceBy copies them
                // onto the new nodes and Godot rejects a non-ancestor owner (error spam).
                ClearOwners(vfx);
                ConvertLinesToTrails(vfx);
            }

            NodeToFollowField.SetValue(vfx, card);
            __result = vfx;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Witch card trail construction failed: {e.Message}");
            __result = null; // callers already handle the TestMode null
        }

        return false;
    }

    private static void ClearOwners(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            child.Owner = null;
            ClearOwners(child);
        }
    }

    /// <summary>Replaces every plain Line2D in the subtree with an NCardTrail clone.</summary>
    private static void ConvertLinesToTrails(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is Line2D line and not NCardTrail)
            {
                var trail = new NCardTrail();
                CopyStorageProperties(line, trail);
                line.ReplaceBy(trail);
                line.QueueFree();
            }
            else
            {
                ConvertLinesToTrails(child);
            }
        }
    }

    /// <summary>
    /// Copies every serialized (storage) property except the script binding — the generic
    /// equivalent of BaseLib's per-type property copies, so scene edits (widths, gradients,
    /// new emitters) never require touching this patch.
    /// </summary>
    private static void CopyStorageProperties(GodotObject from, GodotObject to)
    {
        foreach (var prop in from.GetPropertyList())
        {
            var usage = (PropertyUsageFlags)prop["usage"].As<long>();
            if (!usage.HasFlag(PropertyUsageFlags.Storage))
            {
                continue;
            }

            string name = prop["name"].As<string>();
            if (name == "script")
            {
                continue;
            }

            to.Set(name, from.Get(name));
        }
    }
}
