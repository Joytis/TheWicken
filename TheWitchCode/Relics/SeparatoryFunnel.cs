using MegaCrit.Sts2.Core.Entities.Relics;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Separatory Funnel: whenever an effect would create a RANDOM potion, you choose it instead. Passive —
/// <see cref="Potions.Brewing.PotionCatalog.Pick" /> is the chokepoint every in-combat potion-creation
/// effect routes through; it checks for this relic and opens the potion-select overlay over the same pool.
/// </summary>
public sealed class SeparatoryFunnel : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
}
