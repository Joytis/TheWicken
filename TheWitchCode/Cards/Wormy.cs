using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Wormy: a playable nuisance status (from Wormy Apple). Pay 1 energy to clear one, but it bites — lose 1 life.
/// Retain keeps it clinging to your hand; Exhaust removes it once played. Lives in the base-game StatusCardPool
/// (own [Pool] overrides WitchCard's, same as Ash) so it renders with the default grey status styling.
/// </summary>
[Pool(typeof(StatusCardPool))]
public sealed class Wormy : WitchCard
{
    public override int MaxUpgradeLevel => 0;

    // Kept for safety even though StatusCardPool membership already keeps it out of in-combat
    // generation (Discovery, Attack Potion pull from the character's pool).
    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HpLossVar(1m)
    ];

    public Wormy()
        : base(1, CardType.Status, CardRarity.Status, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Base-game wormy squirm on the player (globally preloaded).
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NWormyImpactVfx.Create(Owner.Creature));
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
    }
}
