using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>Cursed Bottle: throwable Common potion that applies Hex to an enemy (base-game WeakPotion shape).</summary>
public abstract class WitchCardPotionBase : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected abstract CardModel CreatedCard { get; }

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard(CreatedCard)];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if(Owner.Creature.CombatState is not ICombatState combat)
        {
            return;
        }

        CardModel card = combat.CreateCard(CreatedCard, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}
