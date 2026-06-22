using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.Cards;

public sealed class StoneSkin : WickenCard
{
    public StoneSkin()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var potionModel = PotionCatalog.Query(PotionTrait.Defensive, false, PotionRarity.Common).FirstOrDefault();
        ArgumentNullException.ThrowIfNull(potionModel, "potionModel");

        await PotionCmd.TryToProcure(potionModel, Owner);
    }
}
