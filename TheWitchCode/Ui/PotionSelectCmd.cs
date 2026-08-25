using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace TheWitch.TheWitchCode.Ui;

/// <summary>
/// The potion analog of the base game's RelicSelectCmd: MP-synced pick-one over
/// <see cref="NChoosePotionScreen" />. The choice syncs as a plain list index
/// (PlayerChoiceSynchronizer), so <paramref name="potions" /> must be built in the same order on every
/// client (ModelDb registration order is — never shuffle it). Wrapped in the
/// SignalPlayerChoiceBegun/Ended bracket like CardSelectCmd's in-combat screens.
/// </summary>
public static class PotionSelectCmd
{
    private static bool ShouldSelectLocal(Player player) =>
        LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;

    public static async Task<PotionModel?> FromChoosePotionScreen(
        PlayerChoiceContext context, IReadOnlyList<PotionModel> potions, Player player, LocString header)
    {
        if (potions.Count == 0)
        {
            return null;
        }
        if (potions.Count == 1)
        {
            // Only one option — auto-select without opening the overlay (no sync needed: every
            // client short-circuits identically).
            return potions[0];
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
        PotionModel? potion;
        if (ShouldSelectLocal(player))
        {
            NChoosePotionScreen? screen = NChoosePotionScreen.ShowScreen(potions, header);
            // TestMode has no UI — resolve deterministically to the first option.
            potion = screen == null ? potions[0] : await screen.PotionSelected();
            int index = potion == null ? -1 : potions.ToList().IndexOf(potion);
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndex(index));
        }
        else
        {
            int index = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndex();
            potion = index < 0 ? null : potions[index];
        }
        await context.SignalPlayerChoiceEnded();
        return potion;
    }
}
