using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Tasty Herbs: a little garnish in every bottle — heal after combat if you drank a potion during it
/// (base-game BurningBlood victory-heal shape; potion check via combat history, no instance state).
/// </summary>
public sealed class TastyHerbs : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(3m)
    ];

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        bool usedPotion = CombatManager.Instance.History.Entries
            .OfType<PotionUsedEntry>()
            .Any(e => e.Actor == Owner.Creature);
        if (usedPotion && !Owner.Creature.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        }
    }
}
