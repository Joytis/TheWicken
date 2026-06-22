using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Potions;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

/// <summary>
/// Brew: attempts to merge the first two potions in the player's belt into a higher-rarity one
/// (see <see cref="PotionMerge" />). With a single potion held, it becomes a <see cref="WickedBrew" />.
/// </summary>
public sealed class Brew : WickenCard
{
    public Brew()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PotionMerge.MergeBeltPotions(Owner, Owner.RunState.Rng.CombatPotionGeneration);
    }
}
