using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Tinder: burn a card as kindling — the Energy arrives next turn (base-game
/// <see cref="EnergyNextTurnPower" />).</summary>
public sealed class Tinder : WitchCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar("EnergyNextTurnPower", 2)
    ];

    public Tinder()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? kindling = (await CardSelectCmd.FromHand(
            choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), null, this))
            .FirstOrDefault();
        if (kindling != null)
        {
            await CardCmd.Exhaust(choiceContext, kindling);
        }

        // Fire Vfx
        SfxCmd.Play("event:/sfx/characters/attack_fire");
        WitchFx.PlayFlipbook("vfx/fire_impact/vfx_fire_burst_center_flipbook", Owner.Creature, null, 0.7f);
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext, Owner.Creature, DynamicVars["EnergyNextTurnPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["EnergyNextTurnPower"].UpgradeValueBy(1m);
}
