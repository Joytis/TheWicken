using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Hidden in Smoke: at the start of the owner's turn, brew a <see cref="PuffOfSmoke" /> potion. Passive toggle
/// (Single stack) — playing the card again doesn't make more per turn.
/// </summary>
public sealed class HiddenInSmokePower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        Flash();
        await PotionCmd.TryToProcure<PuffOfSmoke>(player);
    }
}
