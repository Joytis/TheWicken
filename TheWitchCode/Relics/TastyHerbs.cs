using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Tasty Herbs: whenever you use an Unstable potion, 25% chance it's used an additional time.
/// The replay reuses the base-game Fairy in a Bottle shape (OnUseWrapper with a throwing context);
/// the extra use also fires AfterPotionUsed, so a flag keeps it to one bonus use per potion.
/// </summary>
public sealed class TastyHerbs : WitchRelic
{
    private const float ExtraUseChance = 0.25f;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    private bool _replaying;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [UnstablePotions.UnstableHoverTip];

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (_replaying
            || potion.Owner != Owner
            || !CombatManager.Instance.IsInProgress
            || !UnstablePotions.IsUnstable(potion)
            || Owner.RunState.Rng.Niche.NextFloat() >= ExtraUseChance)
        {
            return;
        }
        _replaying = true;
        try
        {
            Flash();
            await potion.OnUseWrapper(new ThrowingPlayerChoiceContext(), target);
        }
        finally
        {
            _replaying = false;
        }
    }
}
