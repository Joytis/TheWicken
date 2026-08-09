using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Eye of Newt: a Power that makes your potions hit every enemy, plus a Noxious Brew to throw.</summary>
public sealed class EyeOfNewt : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion(ModelDb.Potion<NoxiousBrew>()),
        UnstablePotions.UnstableHoverTip,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<EyeOfNewtPower>(1m)
    ];

    public EyeOfNewt()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<EyeOfNewtPower>(choiceContext, Owner.Creature, DynamicVars["EyeOfNewtPower"].BaseValue, Owner.Creature, this);
        await Witch.ProducePotion<NoxiousBrew>(Owner, Witch.PotionMode.Unstable);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
