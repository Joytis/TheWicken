using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Potions.Brewing;

/// <summary>
/// Classifies any <see cref="PotionModel" /> (base-game or modded) into a set of <see cref="PotionTrait" />.
///
/// Strategy is a MANUAL TABLE with an inference fallback:
///   1. <see cref="Manual" /> is the authoritative classification table, keyed by concrete model Type. It is
///      the primary source of truth — hand-curated best guesses, edit it freely to tune the potion loot
///      table (Something Wicked = Offensive, Toil and Trouble = Utility, Stone Skin = Defensive, brewing).
///      A potion can carry traits across orientations (e.g. <c>Damage | Aoe</c>, <c>Draw | CardManip</c>).
///   2. Any potion NOT in the table falls back to <see cref="Derive" />, which infers traits from the potion's
///      typed <see cref="DynamicVarSet" /> + <see cref="TargetType" />. This keeps new/DLC potions classified
///      automatically until someone adds them to the table.
///
/// **Every potion we ship must have a <see cref="Manual" /> entry** (Wicked Brew, Villainous Brew, The Cauldron).
/// A <see cref="Manual" /> entry fully REPLACES the inferred result for that potion.
/// </summary>
public static class PotionTraits
{
    private static readonly Dictionary<Type, PotionTrait> _cache = new();

    /// <summary>
    /// Authoritative, hand-curated trait table. Keyed by concrete model Type so a rename in the game assembly
    /// surfaces as a compile error. Grouped by primary orientation for readability; the actual orientation is
    /// derived from the fine-grained flags (Offensive &gt; Defensive &gt; Utility). Best-guess values — tune freely.
    /// </summary>
    public static readonly IReadOnlyDictionary<Type, PotionTrait> Manual = new Dictionary<Type, PotionTrait>
    {
        // ---------------- Offensive (Damage / Debuff / Poison) ----------------
        [typeof(FirePotion)] = PotionTrait.Damage,
        [typeof(PotionShapedRock)] = PotionTrait.Damage,
        [typeof(ExplosiveAmpoule)] = PotionTrait.Damage | PotionTrait.Aoe,
        [typeof(FoulPotion)] = PotionTrait.Damage | PotionTrait.Aoe,
        [typeof(WeakPotion)] = PotionTrait.Debuff,
        [typeof(VulnerablePotion)] = PotionTrait.Debuff,
        [typeof(BeetleJuice)] = PotionTrait.Debuff,            // enemy attacks deal less damage
        [typeof(PowderedDemise)] = PotionTrait.Debuff,         // enemy loses HP each turn
        [typeof(PotionOfDoom)] = PotionTrait.Debuff,           // Doom
        [typeof(ShacklingPotion)] = PotionTrait.Debuff | PotionTrait.Aoe,
        [typeof(PotionOfBinding)] = PotionTrait.Debuff | PotionTrait.Aoe,
        [typeof(PoisonPotion)] = PotionTrait.Poison,
        [typeof(StrengthPotion)] = PotionTrait.Damage,
        [typeof(FlexPotion)] = PotionTrait.Damage,
        [typeof(GigantificationPotion)] = PotionTrait.Damage,

        // ---------------- Defensive (Block / Buff / Heal / MaxHp) ----------------
        [typeof(BlockPotion)] = PotionTrait.Block,
        [typeof(ShipInABottle)] = PotionTrait.Block,
        [typeof(Fortifier)] = PotionTrait.Block,               // triples block
        [typeof(HeartOfIron)] = PotionTrait.Block,             // Plating
        [typeof(DexterityPotion)] = PotionTrait.Block,
        [typeof(SpeedPotion)] = PotionTrait.Block,
        
        [typeof(RegenPotion)] = PotionTrait.Heal,
        [typeof(BloodPotion)] = PotionTrait.Heal,
        [typeof(FairyInABottle)] = PotionTrait.Heal,
        [typeof(FruitJuice)] = PotionTrait.MaxHp | PotionTrait.Heal | PotionTrait.Buff,

        // ---------------- Damage, Block
        [typeof(FyshOil)] = PotionTrait.Damage | PotionTrait.Block,

        // ---------------- Utility (Energy / Draw / CardGen / CardManip / PotionGen / Upgrade) ----------------
        [typeof(FocusPotion)] = PotionTrait.Buff,
        [typeof(LiquidBronze)] = PotionTrait.Buff,             // Thorns
        [typeof(LuckyTonic)] = PotionTrait.Buff,               // Buffer
        [typeof(MazalethsGift)] = PotionTrait.Buff,            // Ritual
        [typeof(GhostInAJar)] = PotionTrait.Buff,              // Intangible
        [typeof(Duplicator)] = PotionTrait.Buff,
        [typeof(EssenceOfDarkness)] = PotionTrait.Buff,        // channel Dark orbs
        [typeof(SoldiersStew)] = PotionTrait.Buff,
        [typeof(StableSerum)] = PotionTrait.Buff,              // Retain hand
        [typeof(BoneBrew)] = PotionTrait.Buff,                 // summon
        [typeof(PotionOfCapacity)] = PotionTrait.Buff,         // orb slots
        [typeof(EnergyPotion)] = PotionTrait.Energy,
        [typeof(RadiantTincture)] = PotionTrait.Energy,
        [typeof(StarPotion)] = PotionTrait.Energy,             // gain Stars
        [typeof(SwiftPotion)] = PotionTrait.Draw,
        [typeof(BottledPotential)] = PotionTrait.Draw,
        [typeof(Clarity)] = PotionTrait.Draw,
        [typeof(DropletOfPrecognition)] = PotionTrait.Draw,
        [typeof(GamblersBrew)] = PotionTrait.Draw,
        [typeof(LiquidMemories)] = PotionTrait.Draw,
        [typeof(CureAll)] = PotionTrait.Energy | PotionTrait.Draw,
        [typeof(AttackPotion)] = PotionTrait.CardGen,
        [typeof(SkillPotion)] = PotionTrait.CardGen,
        [typeof(PowerPotion)] = PotionTrait.CardGen,
        [typeof(ColorlessPotion)] = PotionTrait.CardGen,
        [typeof(CosmicConcoction)] = PotionTrait.CardGen,
        [typeof(CunningPotion)] = PotionTrait.CardGen,         // adds Shivs
        [typeof(OrobicAcid)] = PotionTrait.CardGen,
        [typeof(PotOfGhouls)] = PotionTrait.CardGen,           // adds Souls
        [typeof(Ashwater)] = PotionTrait.CardManip,            // exhaust chosen cards
        [typeof(TouchOfInsanity)] = PotionTrait.CardManip,     // make a card free
        [typeof(DistilledChaos)] = PotionTrait.CardManip,      // play top cards
        [typeof(SneckoOil)] = PotionTrait.Draw | PotionTrait.CardManip,
        [typeof(GlowwaterPotion)] = PotionTrait.Draw | PotionTrait.CardManip,
        [typeof(EntropicBrew)] = PotionTrait.PotionGen,
        [typeof(BlessingOfTheForge)] = PotionTrait.Upgrade,
        [typeof(KingsCourage)] = PotionTrait.Upgrade,          // Forge

        // ---------------- Modded (TheWicken) ----------------
        [typeof(WickedBrew)] = PotionTrait.Damage,
        [typeof(VillainousBrew)] = PotionTrait.Damage,
        [typeof(TheCauldron)] = PotionTrait.Damage | PotionTrait.Block | PotionTrait.Buff | PotionTrait.Debuff | PotionTrait.Aoe,
        [typeof(Fertilizer)] = PotionTrait.Damage,             // gains Brambles, tagged offensive by design
        [typeof(BottledRot)] = PotionTrait.Poison | PotionTrait.Aoe,
        [typeof(BuddyInABottle)] = PotionTrait.CardGen,        // adds a random Familiar summon card
    };

    /// <summary>Get the trait set for a potion (manual table first, inference fallback). Cached per Type.</summary>
    public static PotionTrait Of(PotionModel potion)
    {
        Type type = potion.GetType();
        if (_cache.TryGetValue(type, out PotionTrait cached))
        {
            return cached;
        }

        PotionTrait traits = Manual.TryGetValue(type, out PotionTrait manual)
            ? manual
            : Derive(potion);

        _cache[type] = traits;
        return traits;
    }

    /// <summary>Orientation of a potion derived from its traits. Offensive &gt; Defensive &gt; Utility &gt; Neutral.</summary>
    public static PotionOrientation OrientationOf(PotionModel potion) => OrientationOf(Of(potion));

    public static PotionOrientation OrientationOf(PotionTrait traits)
    {
        if ((traits & PotionTrait.Offensive) != 0) return PotionOrientation.Offensive;
        if ((traits & PotionTrait.Defensive) != 0) return PotionOrientation.Defensive;
        if ((traits & PotionTrait.Utility) != 0) return PotionOrientation.Utility;
        return PotionOrientation.Neutral;
    }

    /// <summary>
    /// Inference fallback for potions absent from <see cref="Manual" />: read traits from the potion's typed
    /// <see cref="DynamicVarSet" /> + <see cref="TargetType" />.
    /// </summary>
    private static PotionTrait Derive(PotionModel potion)
    {
        PotionTrait traits = PotionTrait.None;
        bool targetsEnemy = TargetsEnemy(potion.TargetType);

        foreach (DynamicVar var in potion.DynamicVars.Values)
        {
            switch (var)
            {
                case DamageVar:
                case CalculatedDamageVar:
                case ExtraDamageVar:
                    traits |= PotionTrait.Damage;
                    break;
                case BlockVar:
                case CalculatedBlockVar:
                    traits |= PotionTrait.Block;
                    break;
                case EnergyVar:
                    traits |= PotionTrait.Energy;
                    break;
                case MaxHpVar:
                    traits |= PotionTrait.MaxHp;
                    break;
                case HealVar:
                    traits |= PotionTrait.Heal;
                    break;
                case CardsVar:
                    traits |= PotionTrait.Draw;
                    break;
                default:
                    if (IsPowerVar(var, out Type? powerType))
                    {
                        if (powerType == typeof(PoisonPower))
                        {
                            traits |= PotionTrait.Poison;
                        }
                        else
                        {
                            // A power on an enemy is a debuff; on self/ally it's a buff.
                            traits |= targetsEnemy ? PotionTrait.Debuff : PotionTrait.Buff;
                        }
                    }
                    break;
            }
        }

        if (potion.TargetType == TargetType.AllEnemies)
        {
            traits |= PotionTrait.Aoe;
        }

        return traits;
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
