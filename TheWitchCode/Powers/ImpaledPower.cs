using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Impale debuff: this creature takes double damage from Brambles until the end of its side's turn.
/// The doubling itself lives in <see cref="BramblesPower" /> (the only place Brambles damage is dealt);
/// this power is the marker. Removed at the end of the OWNER's side turn so it survives the enemy's
/// attacks (Brambles retaliation fires during the monster turn, after the player turn ends).
/// </summary>
public sealed class ImpaledPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
