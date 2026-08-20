using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Extensions;

/// <summary>
/// Read-only counters derived from the game's <see cref="CombatHistory" /> (every combat event is logged
/// with an <c>Actor</c> and a <c>HappenedThisTurn</c> check). This avoids needing hidden "tracker" powers
/// for "X this turn / this combat" card scaling — just query the log.
///
/// Pass the playing card's <c>Owner.Creature</c> as <paramref name="player" /> and its <c>CombatState</c>
/// as <paramref name="state" />. All methods return 0 when there is no live combat.
/// </summary>
public static class CombatHistoryQueries
{
    private static CombatHistory? History => CombatManager.Instance?.History;

    public static int PotionsUsedThisTurn(Creature player, ICombatState? state) =>
        History?.Entries.OfType<PotionUsedEntry>()
            .Count(e => e.Actor == player && e.HappenedThisTurn(state)) ?? 0;

    public static int BramblesCreatedThisTurn(Creature player, ICombatState? state) =>
        (int)(History?.Entries.OfType<PowerReceivedEntry>()
            .Where(e => e.Actor == player && e.Power is BramblesPower && e.Amount > 0m && e.HappenedThisTurn(state))
            .Sum(e => e.Amount) ?? 0m);

    public static int CardsPlayedThisCombat<T>(Creature player) where T : CardModel =>
        History?.CardPlaysFinished.Count(e => e.CardPlay.Card is T && e.CardPlay.Card.Owner.Creature == player) ?? 0;

    /// <summary>
    /// Sum of <c>ExtraDamage</c> over every Gnash played this combat — each Gnash raises ALL Gnash by its OWN
    /// bonus (3, or 4 for Gnash+), so the pack total is per-card, not count × this card's bonus.
    /// </summary>
    public static decimal GnashBonusThisCombat(Creature player) =>
        History?.CardPlaysFinished
            .Where(e => e.CardPlay.Card is Gnash && e.CardPlay.Card.Owner.Creature == player)
            .Sum(e => e.CardPlay.Card.DynamicVars.ExtraDamage.BaseValue) ?? 0m;

    public static int RatsPlayedThisCombat(Creature player) => CardsPlayedThisCombat<Rats>(player);
}
