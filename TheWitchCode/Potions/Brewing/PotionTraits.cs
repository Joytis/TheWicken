using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWitch.TheWitchCode.Potions.Brewing;

/// <summary>Broad orientation of a potion — the single axis the brewing / "make a potion" system works on.</summary>
public enum PotionOrientation
{
    /// <summary>No classifiable effect.</summary>
    Neutral,
    Offensive,
    Defensive,
    Utility,
}

/// <summary>
/// Classifies any <see cref="PotionModel" /> (base-game or modded) into a single <see cref="PotionOrientation" />.
///
/// We deliberately work at the orientation level only — fine-grained effect tags were collapsed away because
/// they made the brew/upgrade pools too narrow (a Heal potion only ever upgraded into the one Uncommon healer).
///
/// Strategy is a MANUAL TABLE with an inference fallback:
///   1. <see cref="Manual" /> is the authoritative table, keyed by concrete model Type (a rename in the game
///      assembly surfaces as a compile error). Hand-curated — edit freely to tune the potion pools.
///   2. Any potion NOT in the table falls back to <see cref="Derive" />, which infers an orientation from the
///      potion's typed <see cref="DynamicVarSet" /> + <see cref="TargetType" />, so new/DLC potions classify
///      automatically.
///
/// **Every potion we ship must have a <see cref="Manual" /> entry.** A Manual entry fully REPLACES inference.
/// </summary>
public static class PotionTraits
{
    private static readonly Dictionary<Type, PotionOrientation> _cache = new();

    /// <summary>Authoritative, hand-curated orientation table. Keyed by concrete model Type. Tune freely.</summary>
    public static readonly IReadOnlyDictionary<Type, PotionOrientation> Manual = new Dictionary<Type, PotionOrientation>
    {
        // ---------------- Offensive (damage / debuff / poison) ----------------
        [typeof(FirePotion)] = PotionOrientation.Offensive,             // deal damage to one enemy
        [typeof(PotionShapedRock)] = PotionOrientation.Offensive,       // deal damage (card-only rock)
        [typeof(ExplosiveAmpoule)] = PotionOrientation.Offensive,       // deal damage to ALL enemies
        [typeof(FoulPotion)] = PotionOrientation.Offensive,             // deal damage to all players & enemies
        [typeof(VulnerablePotion)] = PotionOrientation.Offensive,       // apply Vulnerable
        [typeof(BeetleJuice)] = PotionOrientation.Offensive,            // enemy's attacks deal less damage
        [typeof(PowderedDemise)] = PotionOrientation.Offensive,         // enemy loses HP at end of each turn
        [typeof(PotionOfDoom)] = PotionOrientation.Offensive,           // apply Doom
        [typeof(PotionOfBinding)] = PotionOrientation.Offensive,        // apply Weak + Vulnerable to ALL enemies
        [typeof(PoisonPotion)] = PotionOrientation.Offensive,           // apply Poison
        [typeof(StrengthPotion)] = PotionOrientation.Offensive,         // gain Strength
        [typeof(FlexPotion)] = PotionOrientation.Offensive,             // gain Strength, lose it at end of turn
        [typeof(GigantificationPotion)] = PotionOrientation.Offensive,  // next Attack deals triple damage
        [typeof(FyshOil)] = PotionOrientation.Offensive,                // gain Strength and Dexterity — leans offensive
        [typeof(MazalethsGift)] = PotionOrientation.Offensive,          // gain Ritual
        [typeof(SoldiersStew)] = PotionOrientation.Offensive,           // Strike cards gain Replay this combat

        // ---------------- Defensive (block / heal / max HP) ----------------
        [typeof(WeakPotion)] = PotionOrientation.Defensive,             // apply Weak
        [typeof(ShacklingPotion)] = PotionOrientation.Defensive,        // ALL enemies lose Strength this turn
        [typeof(BlockPotion)] = PotionOrientation.Defensive,            // gain Block
        [typeof(ShipInABottle)] = PotionOrientation.Defensive,          // gain Block now and again next turn
        [typeof(Fortifier)] = PotionOrientation.Defensive,              // triple your Block
        [typeof(HeartOfIron)] = PotionOrientation.Defensive,            // gain Plating
        [typeof(DexterityPotion)] = PotionOrientation.Defensive,        // gain Dexterity
        [typeof(SpeedPotion)] = PotionOrientation.Defensive,            // gain Dexterity, lose it at end of turn
        [typeof(RegenPotion)] = PotionOrientation.Defensive,            // gain Regen
        [typeof(BloodPotion)] = PotionOrientation.Defensive,            // heal a % of Max HP
        [typeof(FairyInABottle)] = PotionOrientation.Defensive,         // revive: heal to 30% when you'd hit 0 HP
        [typeof(FruitJuice)] = PotionOrientation.Defensive,             // gain Max HP
        [typeof(LiquidBronze)] = PotionOrientation.Defensive,           // gain Thorns
        [typeof(LuckyTonic)] = PotionOrientation.Defensive,             // gain Buffer
        [typeof(GhostInAJar)] = PotionOrientation.Defensive,             // gain Intangible
        [typeof(BoneBrew)] = PotionOrientation.Defensive,               // Summon
        [typeof(SkillPotion)] = PotionOrientation.Defensive,            // add a free random Skill card

        // ---------------- Utility (buff / energy / draw / card & potion gen / upgrade) ----------------
        [typeof(Duplicator)] = PotionOrientation.Utility,               // next card is played an extra time
        [typeof(OrobicAcid)] = PotionOrientation.Utility,               // add a free Attack, Skill and Power
        [typeof(EssenceOfDarkness)] = PotionOrientation.Utility,        // channel a Dark orb per orb slot
        [typeof(StableSerum)] = PotionOrientation.Utility,              // Retain your Hand
        [typeof(PotionOfCapacity)] = PotionOrientation.Utility,         // gain Orb slots
        [typeof(EnergyPotion)] = PotionOrientation.Utility,             // gain Energy
        [typeof(RadiantTincture)] = PotionOrientation.Utility,          // gain Energy now and next turns
        [typeof(StarPotion)] = PotionOrientation.Utility,               // gain Stars
        [typeof(SwiftPotion)] = PotionOrientation.Utility,              // draw cards
        [typeof(BottledPotential)] = PotionOrientation.Utility,         // shuffle hand into Draw, then draw
        [typeof(Clarity)] = PotionOrientation.Utility,                  // draw now and extra at next turns' start
        [typeof(DropletOfPrecognition)] = PotionOrientation.Utility,    // pull a card from Draw into Hand
        [typeof(GamblersBrew)] = PotionOrientation.Utility,             // discard any number, draw that many
        [typeof(LiquidMemories)] = PotionOrientation.Utility,           // pull a card from Discard into Hand (free)
        [typeof(CureAll)] = PotionOrientation.Utility,                  // gain Energy and draw
        [typeof(AttackPotion)] = PotionOrientation.Offensive,           // add a free random Attack card
        [typeof(PowerPotion)] = PotionOrientation.Utility,              // add a free random Power card
        [typeof(ColorlessPotion)] = PotionOrientation.Utility,          // add a free random Colorless card
        [typeof(CosmicConcoction)] = PotionOrientation.Utility,         // add Upgraded Colorless cards
        [typeof(CunningPotion)] = PotionOrientation.Offensive,          // add Upgraded Shivs
        [typeof(PotOfGhouls)] = PotionOrientation.Utility,              // add Souls into Hand
        [typeof(Ashwater)] = PotionOrientation.Utility,                 // exhaust any number of cards in Hand
        [typeof(TouchOfInsanity)] = PotionOrientation.Utility,          // make a card free to play this combat
        [typeof(DistilledChaos)] = PotionOrientation.Utility,           // play the top cards of your Draw Pile
        [typeof(SneckoOil)] = PotionOrientation.Utility,                // draw, then randomize hand card costs
        [typeof(GlowwaterPotion)] = PotionOrientation.Utility,          // exhaust Hand, then draw
        [typeof(EntropicBrew)] = PotionOrientation.Utility,             // fill empty potion slots with random potions
        [typeof(BlessingOfTheForge)] = PotionOrientation.Utility,       // upgrade all cards in Hand for combat
        [typeof(KingsCourage)] = PotionOrientation.Utility,             // Forge (upgrade)

        // ---------------- Modded (TheWitch) ----------------
        [typeof(NoxiousBrew)] = PotionOrientation.Offensive,            // card-only offensive brew (deal damage)
        [typeof(CursedBottle)] = PotionOrientation.Offensive,           // apply Hex to an enemy
        [typeof(Fertilizer)] = PotionOrientation.Offensive,             // gains Brambles (retaliation damage) — offensive by design, not strictly defensive
        [typeof(BuddyInABottle)] = PotionOrientation.Utility,           // adds a random Familiar summon card
        [typeof(VialOfSmoke)] = PotionOrientation.Defensive,            // card-only Block potion (Light the Candle)
        [typeof(MushroomExtract)] = PotionOrientation.Utility,          // discard hand, draw 6 free gibberish cards
        [typeof(BottledMessage)] = PotionOrientation.Utility,           // card-only: returns the bottled card to hand
    };

    /// <summary>
    /// Manual table of potions that can heal the player (direct heal, regen, revive, or max-HP gain — which
    /// raises current HP too). Same manual-first philosophy as <see cref="Manual" />; potions absent from this
    /// set fall back to var inference in <see cref="IsHealing" />.
    /// </summary>
    public static readonly IReadOnlySet<Type> HealingManual = new HashSet<Type>
    {
        typeof(BloodPotion),      // heal a % of Max HP
        typeof(RegenPotion),      // Regen heals over turns
        typeof(FairyInABottle),   // revive: heal to 30% when you'd hit 0 HP
        typeof(FruitJuice),       // gain Max HP (current HP rises with it)
    };

    /// <summary>
    /// True if the potion can heal the player. Manual set first, then var inference
    /// (<see cref="HealVar" />/<see cref="MaxHpVar" /> or a <c>PowerVar&lt;RegenPower&gt;</c>).
    /// </summary>
    public static bool IsHealing(PotionModel potion)
    {
        if (HealingManual.Contains(potion.GetType()))
        {
            return true;
        }

        foreach (DynamicVar var in potion.DynamicVars.Values)
        {
            if (var is HealVar or MaxHpVar)
            {
                return true;
            }
            if (IsPowerVar(var, out Type? powerType) && powerType == typeof(RegenPower))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Orientation of a potion (manual table first, inference fallback). Cached per Type.</summary>
    public static PotionOrientation OrientationOf(PotionModel potion)
    {
        Type type = potion.GetType();
        if (_cache.TryGetValue(type, out PotionOrientation cached))
        {
            return cached;
        }

        PotionOrientation orientation = Manual.TryGetValue(type, out PotionOrientation manual)
            ? manual
            : Derive(potion);

        _cache[type] = orientation;
        return orientation;
    }

    /// <summary>
    /// Inference fallback for potions absent from <see cref="Manual" />: read an orientation from the potion's
    /// typed <see cref="DynamicVarSet" /> + <see cref="TargetType" />. Priority Offensive &gt; Defensive &gt; Utility.
    /// </summary>
    private static PotionOrientation Derive(PotionModel potion)
    {
        bool targetsEnemy = TargetsEnemy(potion.TargetType);
        bool defensive = false;
        bool utility = false;

        foreach (DynamicVar var in potion.DynamicVars.Values)
        {
            switch (var)
            {
                case DamageVar:
                case CalculatedDamageVar:
                case ExtraDamageVar:
                    return PotionOrientation.Offensive;
                case BlockVar:
                case CalculatedBlockVar:
                case HealVar:
                case MaxHpVar:
                    defensive = true;
                    break;
                case EnergyVar:
                case CardsVar:
                    utility = true;
                    break;
                default:
                    if (IsPowerVar(var, out Type? powerType))
                    {
                        if (powerType == typeof(PoisonPower))
                        {
                            return PotionOrientation.Offensive;
                        }
                        if (targetsEnemy)
                        {
                            return PotionOrientation.Offensive; // a power on an enemy is a debuff
                        }
                        utility = true; // a power on self/ally is a buff
                    }
                    break;
            }
        }

        if (targetsEnemy)
        {
            return PotionOrientation.Offensive;
        }
        if (defensive)
        {
            return PotionOrientation.Defensive;
        }
        if (utility)
        {
            return PotionOrientation.Utility;
        }
        return PotionOrientation.Neutral;
    }

    private static bool TargetsEnemy(TargetType target) =>
        target is TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy;

    /// <summary>True if <paramref name="var" /> is any closed <c>PowerVar&lt;T&gt;</c>, yielding the power type.</summary>
    private static bool IsPowerVar(DynamicVar var, out Type? powerType)
    {
        for (Type? t = var.GetType(); t != null && t != typeof(object); t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PowerVar<>))
            {
                powerType = t.GetGenericArguments()[0];
                return true;
            }
        }

        powerType = null;
        return false;
    }
}
