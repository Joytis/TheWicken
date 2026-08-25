using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Crystal Bottle: a Power — the next potion you use becomes a Buff that repeats its effect each turn.</summary>
public sealed class CrystalBottle : WitchCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    // Surfaces the power tooltip, which carries the "Healing Potions cannot be bottled" note.
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<CrystalBottlePower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<CrystalBottlePower>(1m)
    ];

    public CrystalBottle()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<CrystalBottlePower>(choiceContext, Owner.Creature, DynamicVars["CrystalBottlePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
