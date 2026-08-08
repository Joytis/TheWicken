using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Hex (enemy debuff): the hexed creature takes flat bonus damage on every hit of an incoming attack
/// (per stack), then loses 1 stack once per attack — a multi-hit attack gets the full bonus on each
/// hit but only burns one Hex. Defender-side mirror of the base-game Vigor shape
/// (ModifyDamageAdditive per hit + AfterAttack consume).
/// Witch-only: in multiplayer, only attacks from a Witch-character player trigger the bonus or burn
/// a stack — other characters' attacks leave Hex untouched, so the Witch can build critical mass.
/// </summary>
public sealed class HexPower : WitchPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private const decimal DamagePerStack = 2m;

    // Loc token {TotalDamage}: the live total bonus damage (per-stack × stacks), kept in sync
    // with the stack count below so the tooltip shows what the enemy will actually take.
    private const string TotalDamageKey = "TotalDamage";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(TotalDamageKey, DamagePerStack)];

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            DynamicVars[TotalDamageKey].BaseValue = DamagePerStack * Math.Max(Amount, 0);
        }

        await Task.CompletedTask;
    }

    /// <summary>Hex signature on every application: occult gaze + doom sting on the hexed creature.</summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        WitchFx.HexGaze(Owner);
        await Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0 || !props.IsPoweredAttack() || !TriggeredBy(dealer))
        {
            return 0m;
        }

        return DamagePerStack * Amount;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Amount <= 0 || !Owner.IsAlive || !command.DamageProps.IsPoweredAttack() || !TriggeredBy(command.Attacker))
        {
            return;
        }

        if (!command.Results.SelectMany(hit => hit).Any(result => result.Receiver == Owner))
        {
            return;
        }

        Flash();
        WitchFx.PurpleFlame(Owner);

        // Torment-style attacks milk the Hex without burning it.
        if (command.ModelSource is IHexPreserving)
        {
            return;
        }

        await PowerCmd.Decrement(this);
    }

    /// <summary>
    /// On an enemy, Hex only responds to a Witch-character player's attacks (any Witch in MP).
    /// On a player, enemy attacks trigger it — intents pick the bonus up automatically since
    /// intent damage runs through the ModifyDamage hook pipeline.
    /// </summary>
    private bool TriggeredBy(Creature? attacker) =>
        Owner.IsPlayer ? attacker?.IsMonster == true : IsWitch(attacker);

    private static bool IsWitch(Creature? attacker) =>
        attacker?.Player?.Character is Witch;
}
