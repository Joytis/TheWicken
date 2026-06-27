using System;

namespace TheWicken.TheWickenCode.Potions.Brewing;

/// <summary>
/// Effect tags for a potion, used to query and brew potions by what they DO rather than by id.
/// A potion can carry several traits at once (e.g. a potion that deals damage and applies a debuff).
/// Derived automatically from a potion's <see cref="MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVarSet" />
/// and <see cref="MegaCrit.Sts2.Core.Entities.Cards.TargetType" /> where possible; var-less potions are tagged
/// by the override table in <see cref="PotionTraits" />.
/// </summary>
[Flags]
public enum PotionTrait
{
    None = 0,

    // --- Offensive ---
    /// <summary>Deals direct HP damage (FirePotion, PotionShapedRock, ExplosiveAmpoule).</summary>
    Damage = 1 << 0,
    /// <summary>Applies a debuff power to an enemy (WeakPotion, VulnerablePotion).</summary>
    Debuff = 1 << 1,
    /// <summary>Applies Poison specifically (PoisonPotion). Implies offensive.</summary>
    Poison = 1 << 2,

    // --- Defensive ---
    /// <summary>Grants Block (BlockPotion, Fortifier, HeartOfIron).</summary>
    Block = 1 << 3,
    /// <summary>Restores HP (FruitJuice via maxHp, FairyInABottle, RegenPotion).</summary>
    Heal = 1 << 5,
    /// <summary>Raises max HP (FruitJuice, BloodPotion).</summary>
    MaxHp = 1 << 6,

    // --- Utility ---
    /// <summary>Applies a beneficial power to self/ally (StrengthPotion, Duplicator, SoldiersStew). Classified Utility.</summary>
    Buff = 1 << 4,
    /// <summary>Grants energy (EnergyPotion).</summary>
    Energy = 1 << 7,
    /// <summary>Draws / pulls cards into hand (DropletOfPrecognition, GamblersBrew, LiquidMemories).</summary>
    Draw = 1 << 8,
    /// <summary>Generates brand-new cards (AttackPotion, SkillPotion, ColorlessPotion, OrobicAcid).</summary>
    CardGen = 1 << 9,
    /// <summary>Manipulates existing cards in hand/deck (Ashwater exhaust, TouchOfInsanity cost-down).</summary>
    CardManip = 1 << 10,
    /// <summary>Generates potions (EntropicBrew).</summary>
    PotionGen = 1 << 11,
    /// <summary>Upgrades cards (BlessingOfTheForge).</summary>
    Upgrade = 1 << 12,
    /// <summary>Hits all enemies (derived from TargetType.AllEnemies).</summary>
    Aoe = 1 << 13,

    // --- Convenience masks (orientation axes) ---
    Offensive = Damage | Debuff | Poison,
    Defensive = Block | Heal | MaxHp,
    Utility = Buff | Energy | Draw | CardGen | CardManip | PotionGen | Upgrade,
}

/// <summary>Broad orientation of a potion, derived from its <see cref="PotionTrait" /> flags.</summary>
public enum PotionOrientation
{
    /// <summary>No classifiable effect traits.</summary>
    Neutral,
    Offensive,
    Defensive,
    Utility,
}
