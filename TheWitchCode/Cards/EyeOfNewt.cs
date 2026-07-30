using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Eye of Newt (was Experiment): a Power that makes your potions hit harder.</summary>
public sealed class EyeOfNewt : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<EyeOfNewtPower>(1m)
    ];

    public EyeOfNewt()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<EyeOfNewtPower>(choiceContext, Owner.Creature, DynamicVars["EyeOfNewtPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["EyeOfNewtPower"].UpgradeValueBy(1m);
}
