using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Impale buff (on the PLAYER): the owner's Brambles deal 2^stacks damage this turn — each
/// Impale played doubles again. The multiplication itself lives in <see cref="BramblesPower" />
/// (the only place Brambles damage is dealt); this power is the counter. "This turn" must
/// survive the enemy turn — Brambles retaliation fires while monsters attack, after the player
/// turn ends — so it expires at the start of the owner's NEXT turn (the *NextTurn power shape),
/// not at player turn end.
/// </summary>
public sealed class ImpaledPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Loc token {Multiplier}: the live Brambles damage multiplier (2^stacks), kept in sync
    // with the stack count so the tooltip shows what Brambles will actually deal.
    private const string MultiplierKey = "Multiplier";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(MultiplierKey, 2m)];

    /// <summary>2^stacks, clamped so a degenerate stack count can't overflow decimal.</summary>
    public static decimal MultiplierFor(int stacks) => 1L << Math.Clamp(stacks, 0, 30);

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            DynamicVars[MultiplierKey].BaseValue = MultiplierFor(Amount);
        }

        await Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            await PowerCmd.Remove(this);
        }
    }
}
