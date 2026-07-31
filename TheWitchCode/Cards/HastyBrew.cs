using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Hasty Brew: fast mana — brew an Energy Potion. Exhausts; upgrade removes Exhaust.</summary>
public sealed class HastyBrew : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
        HoverTipFactory.FromPotion<EnergyPotion>(),
    ];

    public HastyBrew()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await Witch.ProducePotion<EnergyPotion>(Owner, Witch.PotionMode.Unstable);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
