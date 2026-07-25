using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Potions.Brewing;

using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Distill (renamed from Brew): replace a random belt potion (non-Rares prioritized) with a random RARE
/// non-healing potion of the same orientation (see <see cref="PotionUpgrade" />). Upgraded, it distills two.
/// Does nothing if the belt is empty.
/// </summary>
public sealed class Distill : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];


    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Potions", 1m)
    ];

    public Distill()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        if (Owner.Potions.Any())
        {
            WitchFx.EnchantShimmer();
        }
        await PotionUpgrade.UpgradeRandomPotions(Owner, Owner.RunState.Rng.CombatPotionGeneration, DynamicVars["Potions"].IntValue);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
