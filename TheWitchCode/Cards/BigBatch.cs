using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Character;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Big Batch: one cauldron, many bottles — create a batch of Noxious Brews.</summary>
public sealed class BigBatch : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        UnstablePotions.UnstableHoverTip,
        HoverTipFactory.FromPotion<NoxiousBrew>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Brews", 2m)
    ];

    public BigBatch()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        for (int i = 0; i < DynamicVars["Brews"].IntValue; i++)
        {
            await Witch.ProducePotion<NoxiousBrew>(Owner, Witch.PotionMode.Unstable);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Brews"].UpgradeValueBy(1m);
}
