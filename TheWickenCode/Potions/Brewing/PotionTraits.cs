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
/// Strategy is HYBRID:
///   1. Auto-derive from the potion's typed <see cref="DynamicVarSet" /> + <see cref="TargetType" />. This covers
///      ~80% of potions for free and automatically handles future/DLC potions that expose standard vars.
///   2. For potions that express their effect in code with no canonical var (Fortifier, AttackPotion, EntropicBrew,
///      ...), the <see cref="Overrides" /> table supplies an authoritative trait set, keyed by model Type so it is
///      compile-checked against the game assembly.
///
/// An override entry fully REPLACES the auto-derived result for that potion.
/// </summary>
public static class PotionTraits
{
    private static readonly Dictionary<Type, PotionTrait> _cache = new();

    /// <summary>
    /// Authoritative trait sets for potions whose effect is not visible in their <see cref="DynamicVarSet" />.
    /// Keyed by the concrete model Type so a rename in the game assembly surfaces as a compile error.
    /// Add an entry here whenever auto-derivation tags a potion incorrectly or not at all.
    /// </summary>
    public static readonly IReadOnlyDictionary<Type, PotionTrait> Overrides = new Dictionary<Type, PotionTrait>
    {
        // --- card generation ---
        [typeof(AttackPotion)] = PotionTrait.CardGen,
        [typeof(SkillPotion)] = PotionTrait.CardGen,
        [typeof(PowerPotion)] = PotionTrait.CardGen,
        [typeof(ColorlessPotion)] = PotionTrait.CardGen,
        [typeof(OrobicAcid)] = PotionTrait.CardGen,

        // --- card draw / pull into hand ---
        [typeof(DropletOfPrecognition)] = PotionTrait.Draw,
        [typeof(GamblersBrew)] = PotionTrait.Draw,
        [typeof(LiquidMemories)] = PotionTrait.Draw,

        // --- card manipulation ---
        [typeof(Ashwater)] = PotionTrait.CardManip,        // exhaust chosen cards
        [typeof(TouchOfInsanity)] = PotionTrait.CardManip, // make a card free this combat

        // --- buffs expressed in code (no PowerVar) ---
        [typeof(Duplicator)] = PotionTrait.Buff,           // DuplicationPower
        [typeof(EssenceOfDarkness)] = PotionTrait.Buff,    // channel Dark orbs
        [typeof(SoldiersStew)] = PotionTrait.Buff,         // strikes gain Replay

        // --- block / heal expressed in code ---
        [typeof(Fortifier)] = PotionTrait.Block,           // doubles current block
        [typeof(FairyInABottle)] = PotionTrait.Heal,       // heal on lethal

        // --- generators / upgrade ---
        [typeof(EntropicBrew)] = PotionTrait.PotionGen,
        [typeof(BlessingOfTheForge)] = PotionTrait.Upgrade,
    };

    /// <summary>Get the trait set for a potion. Results are cached per Type.</summary>
    public static PotionTrait Of(PotionModel potion)
    {
        Type type = potion.GetType();
        if (_cache.TryGetValue(type, out PotionTrait cached))
        {
            return cached;
        }

        PotionTrait traits = Overrides.TryGetValue(type, out PotionTrait overridden)
            ? overridden
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
