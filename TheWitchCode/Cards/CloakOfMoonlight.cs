using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

public sealed class CloakOfMoonlight : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<CloakOfMoonlightPower>(2m)
    ];

    public CloakOfMoonlight()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<CloakOfMoonlightPower>(choiceContext, Owner.Creature, DynamicVars["CloakOfMoonlightPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["CloakOfMoonlightPower"].UpgradeValueBy(1m);
}
