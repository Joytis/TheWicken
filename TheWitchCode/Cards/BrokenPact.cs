using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Broken Pact: end every familiar's service at once and take their strength for your own.</summary>
public sealed class BrokenPact : WitchCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(4m)
    ];

    public BrokenPact()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int sacrificed = await Familiars.RemoveAll(Owner.Creature);
        if (sacrificed > 0)
        {
            VfxCmd.PlayOnCreatureCenter(Owner.Creature, VfxCmd.spookyScreamVfx);
            await PowerCmd.Apply<StrengthPower>(
                choiceContext, Owner.Creature, DynamicVars.Strength.BaseValue * sacrificed, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Strength.UpgradeValueBy(2m);
}
