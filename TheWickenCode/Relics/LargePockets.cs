using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TheWicken.TheWickenCode.Relics;

/// <summary>
/// Large Pockets: the Wicken's starting relic. On pickup, gain one potion slot — leaning into the character's
/// potion identity from turn one. Mirrors base-game <c>PotionBelt</c> via <see cref="PlayerCmd.GainMaxPotionCount" />.
/// </summary>
public sealed class LargePockets : WickenRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("PotionSlots", 1m)
    ];

    public override async Task AfterObtained()
    {
        await PlayerCmd.GainMaxPotionCount(DynamicVars["PotionSlots"].IntValue, Owner);
    }
}
