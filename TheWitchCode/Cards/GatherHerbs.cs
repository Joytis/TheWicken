using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Copy the next potion you create this combat. Exhausts.</summary>
public sealed class GatherHerbs : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<NextPotionCopiedPower>(1m)
    ];

    public GatherHerbs()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<NextPotionCopiedPower>(choiceContext, Owner.Creature, DynamicVars["NextPotionCopiedPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["NextPotionCopiedPower"].UpgradeValueBy(1m);
}
