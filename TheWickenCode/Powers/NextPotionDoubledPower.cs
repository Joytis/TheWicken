using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheWicken.TheWickenCode.Powers;

/// <summary>
/// Double, Double: the next potion the player uses this turn is duplicated — when it's used, a fresh copy is
/// procured straight back into the belt, so the player gets to use it again. Granting a copy (rather than
/// replaying the effect) sidesteps re-running protected potion code, headless player-choice replays, and any
/// recursion: procurement fires <c>AfterPotionProcured</c>, not a potion-USE hook, so this can't re-enter.
/// Self-removes at the end of the player's turn ("this turn").
/// </summary>
public sealed class NextPotionDoubledPower : WickenPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner.Player)
        {
            return;
        }
        PotionModel? canonical = ModelDb.AllPotions.FirstOrDefault(p => p.GetType() == potion.GetType());
        if (canonical == null)
        {
            return;
        }
        await PowerCmd.Decrement(this);
        Flash();
        await PotionCmd.TryToProcure(canonical.ToMutable(), Owner.Player);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}
