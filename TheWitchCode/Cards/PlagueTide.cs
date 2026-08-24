using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Plague Tide (MP-only): the rats spread to every ship — ALL players summon a Rat Familiar
/// (upgraded: a Rat Familiar+, per-stack like every summon). EnergySurge iteration pattern.
/// </summary>
public sealed class PlagueTide : WitchCard, IFamiliarSummon
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<RatFamiliarPower>(),
        HoverTipFactory.FromCard<Swarm>(IsUpgraded),
        HoverTipFactory.FromCard<Rummage>(IsUpgraded),
    ];

    public PlagueTide()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        IEnumerable<Creature> players = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);
        foreach (Creature player in players)
        {
            await PowerCmd.Apply<RatFamiliarPower>(choiceContext, player, 1m, Owner.Creature, this);
            if (IsUpgraded && player.GetPower<RatFamiliarPower>() is { } power)
            {
                power.UpgradedStacks++;
            }
        }
    }
}
