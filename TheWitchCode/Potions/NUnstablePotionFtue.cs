using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// One-time tip shown the first time the local player creates an Unstable potion: belt potions
/// can be right-clicked to use them instantly (PotionQuickUsePatch). Reuses the base-game
/// obtain_potion_ftue scene (its NObtainPotionFtue script wires the confirm button and lifts the
/// belt above the backstop) and swaps the text. Gated by the game's progress-save FTUE flags, so
/// the Tutorials toggle/reset in settings applies.
/// </summary>
public static class NUnstablePotionFtue
{
    public const string Id = "thewitch_unstable_potion_ftue";

    private static readonly string _scenePath = SceneHelper.GetScenePath("ftue/obtain_potion_ftue");

    /// <summary>Called from UnstablePotions.Mark — no-op unless this is the first local sighting.</summary>
    public static void TryShow(PotionModel potion)
    {
        if (TestMode.IsOn || !LocalContext.IsMine(potion) || SaveManager.Instance.SeenFtue(Id))
        {
            return;
        }
        NModalContainer? modal = NModalContainer.Instance;
        if (modal == null || modal.OpenModal != null)
        {
            return; // another modal is up; try again on the next Unstable potion
        }
        NObtainPotionFtue ftue = PreloadManager.Cache.GetScene(_scenePath)
            .Instantiate<NObtainPotionFtue>(PackedScene.GenEditState.Disabled);
        modal.Add(ftue);
        // _Ready (run by Add) filled in the base-game potion text; overwrite with ours.
        ftue.GetNode<MegaLabel>("FtuePopup/Header")
            .SetTextAutoSize(new LocString("ftues", "THEWITCH-UNSTABLE_FTUE_TITLE").GetFormattedText());
        ftue.GetNode<MegaRichTextLabel>("FtuePopup/DescriptionContainer/Description").Text =
            new LocString("ftues", "THEWITCH-UNSTABLE_FTUE_DESCRIPTION").GetFormattedText();
        SaveManager.Instance.MarkFtueAsComplete(Id);
    }
}
