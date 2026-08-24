using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
        // Iterate the combat's Players directly — unambiguously includes the caster.
        foreach (Player player in CombatState!.Players)
        {
            if (!player.Creature.IsAlive)
            {
                continue;
            }
            await PowerCmd.Apply<RatFamiliarPower>(choiceContext, player.Creature, 1m, Owner.Creature, this);
            if (IsUpgraded && player.Creature.GetPower<RatFamiliarPower>() is { } power)
            {
                power.UpgradedStacks++;
            }
        }
    }
}
