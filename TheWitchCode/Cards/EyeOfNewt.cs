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

/// <summary>Eye of Newt: a Power that fans your potions out to one extra random target (upgraded: ALL
/// valid targets), plus an unstable Ember Jar to throw. Upgrade is behavior-only — it flips the applied
/// power's HitsAll mode, no stat change.</summary>
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
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<EyeOfNewtPower>(choiceContext, Owner.Creature, DynamicVars["EyeOfNewtPower"].BaseValue, Owner.Creature, this);
        if (IsUpgraded)
        {
            Owner.Creature.GetPower<EyeOfNewtPower>()?.EnableHitsAll();
        }
        await Witch.ProducePotion<NoxiousBrew>(Owner, Witch.PotionMode.Unstable);
    }
}
