using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Smolder: at the start of each of the owner's next <see cref="PowerModel.Amount" /> turns, create an
/// Unstable Ember Jar. One stack is spent per turn (the power auto-removes at zero) — the
/// <c>StrengthNextTurnPower</c> shape, but paying out once per stack instead of all at once.
/// </summary>
public sealed class SmolderPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
        HoverTipFactory.FromPotion<EmberJar>(),
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        await Witch.ProducePotion<EmberJar>(player, Witch.PotionMode.Unstable);
        await PowerCmd.Decrement(this);
    }
}
