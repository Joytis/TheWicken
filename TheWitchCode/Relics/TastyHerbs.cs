using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Tasty Herbs: a little garnish in every bottle — heal after combat if you drank a potion during it
/// (base-game BurningBlood victory-heal shape).
/// </summary>
public sealed class TastyHerbs : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // Combat history can't be read in AfterCombatVictory (EndCombatInternal clears it before the
    // hook fires), and PotionUsedEntry is skipped entirely when the potion lands the killing blow
    // (only recorded while IsInProgress) — so track potion use ourselves. Reset at combat start
    // keeps between-fight map drinks from counting.
    private bool _usedPotionThisCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(3m)
    ];

    public override Task BeforeCombatStart()
    {
        _usedPotionThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner)
        {
            _usedPotionThisCombat = true;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (_usedPotionThisCombat && !Owner.Creature.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        }
    }
}
