using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Wormy's Apple: on pickup, gain 10 Max HP (per base-game BigMushroom). The catch — every combat opens with a
/// Wormy clinging to your hand (a Wormy is added on the first turn's draw, per base-game Toolbox).
/// </summary>
public sealed class WormysApple : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MaxHpVar(10m)
    ];

    public override async Task AfterObtained()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner || Owner.PlayerCombatState is not { TurnNumber: 1 })
        {
            return;
        }
        Flash();

        NWormyImpactVfx? wormyImpact = NWormyImpactVfx.Create(Owner.Creature);
        if (wormyImpact != null)
        {
            Owner.Creature.GetVfxContainer()?.AddChildSafely(wormyImpact);
        }

        var card = Owner.Creature.CombatState!.CreateCard<Wormy>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Top);
    }
}
