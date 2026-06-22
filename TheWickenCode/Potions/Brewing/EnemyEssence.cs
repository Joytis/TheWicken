using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace TheWicken.TheWickenCode.Potions.Brewing;

/// <summary>
/// Classifies an enemy by what it DOES — across its whole moveset — into a <see cref="PotionOrientation" />, and
/// rolls a thematic Common potion of that orientation. Used by the ExtractEssence card.
///
/// Mapping (from the card's design):
///   * aggressive enemies (Attack / Buff) -> Offensive potion
///   * block/heal enemies (Defend / Heal) -> Defensive potion
///   * debuffers (Debuff / status / summon) -> Utility potion
/// </summary>
public static class EnemyEssence
{
    /// <summary>Classify an enemy into an orientation by tallying the intent types across its moveset.</summary>
    public static PotionOrientation Classify(Creature enemy)
    {
        var votes = new Dictionary<PotionOrientation, int>
        {
            [PotionOrientation.Offensive] = 0,
            [PotionOrientation.Defensive] = 0,
            [PotionOrientation.Utility] = 0,
        };

        foreach (IntentType type in CollectIntentTypes(enemy))
        {
            PotionOrientation orientation = OrientationForIntent(type);
            if (orientation != PotionOrientation.Neutral)
            {
                votes[orientation]++;
            }
        }

        // No classifiable intents (or not a monster) -> default Offensive; this is an attack-triggered card.
        if (votes.Values.All(v => v == 0))
        {
            return PotionOrientation.Offensive;
        }

        return votes
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Priority(kv.Key))
            .First().Key;
    }

    /// <summary>
    /// Pick a random Common potion thematically matching the enemy's orientation (canonical model), or null if the
    /// catalog has nothing usable. Only potions that may be generated in combat are considered.
    /// </summary>
    public static PotionModel? RollThematicPotion(Creature enemy, Rng rng)
    {
        PotionOrientation orientation = Classify(enemy);

        List<PotionModel> candidates = CombatCommons()
            .Where(p => PotionTraits.OrientationOf(p) == orientation)
            .ToList();

        // Fallback: any combat-generatable Common if the themed bucket is empty.
        if (candidates.Count == 0)
        {
            candidates = CombatCommons().ToList();
        }

        return PotionCatalog.Random(candidates, rng);
    }

    private static IEnumerable<PotionModel> CombatCommons() =>
        PotionCatalog.Query(rarity: PotionRarity.Common);

    private static IEnumerable<IntentType> CollectIntentTypes(Creature enemy)
    {
        var monster = enemy.Monster;
        if (monster == null)
        {
            yield break;
        }

        // Prefer the whole moveset (the enemy's "character"); fall back to the telegraphed move.
        var stateMachine = monster.MoveStateMachine;
        if (stateMachine != null)
        {
            foreach (var state in stateMachine.States.Values)
            {
                if (state is MoveState move)
                {
                    foreach (AbstractIntent intent in move.Intents)
                    {
                        yield return intent.IntentType;
                    }
                }
            }
        }
        else
        {
            foreach (AbstractIntent intent in monster.NextMove.Intents)
            {
                yield return intent.IntentType;
            }
        }
    }

    private static PotionOrientation OrientationForIntent(IntentType type) => type switch
    {
        IntentType.Attack or IntentType.DeathBlow or IntentType.Buff => PotionOrientation.Offensive,
        IntentType.Defend or IntentType.Heal => PotionOrientation.Defensive,
        IntentType.Debuff or IntentType.DebuffStrong or IntentType.CardDebuff
            or IntentType.StatusCard or IntentType.Summon => PotionOrientation.Utility,
        _ => PotionOrientation.Neutral,
    };

    // Tie-break preference when two orientations are equally represented.
    private static int Priority(PotionOrientation orientation) => orientation switch
    {
        PotionOrientation.Offensive => 0,
        PotionOrientation.Defensive => 1,
        PotionOrientation.Utility => 2,
        _ => 3,
    };
}
