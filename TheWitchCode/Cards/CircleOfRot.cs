using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Co-op rot ritual (MP-only): you and every ally hunker down (Block) but soak up the rot (Weak),
/// while all enemies choke on it too. Leans into the Witch's "having debuffs pays off" theme.
/// </summary>
public sealed class CircleOfRot : WitchCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(20m, ValueProp.Move),
        new PowerVar<WeakPower>(2m)
    ];

    public CircleOfRot()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal weak = DynamicVars.Weak.BaseValue;
        IEnumerable<Creature> allies = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);
        foreach (Creature ally in allies)
        {
            await CreatureCmd.GainBlock(ally, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
            await PowerCmd.Apply<WeakPower>(choiceContext, ally, weak, Owner.Creature, this);
        }
        await PowerCmd.Apply<WeakPower>(choiceContext, CombatState.HittableEnemies, weak, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}
