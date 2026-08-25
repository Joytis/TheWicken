using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Models.Cards;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>Wormy Apple: a big heal that comes with a catch — 3 Wormy status cards wriggle into your hand.</summary>
public sealed class WormyApple : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Wormy>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(15m),
        new CardsVar(3)
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/wriggler/wriggler_attack");
        NWormyImpactVfx? wormyImpact = NWormyImpactVfx.Create(Owner.Creature);
        if (wormyImpact != null)
        {
            Owner.Creature.GetVfxContainer()?.AddChildSafely(wormyImpact);
        }
        
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue);

        int wormyCount = DynamicVars.Cards.IntValue;
        for (int i = 0; i < wormyCount; i++)
        {
            var card = Owner.Creature.CombatState!.CreateCard<Wormy>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Top);
            await Cmd.Wait(0.1f);
        }
    }
}
