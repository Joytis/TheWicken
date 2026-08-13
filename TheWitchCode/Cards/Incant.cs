using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Incant (starter): block, plus Hex if you have already drunk a potion this turn — the second half of the
/// starting deck's potion loop (Harvest generates, Incant pays off). The condition reads combat history
/// (<see cref="PotionUsedEntry" /> + <c>HappenedThisTurn</c>) rather than instance state, so nothing needs
/// serializing. Potion-first sequencing is the point: the Hex is the reward for leading with the potion.
/// </summary>
public sealed class Incant : WitchCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<HexPower>(1m)
    ];

    private bool UsedPotionThisTurn => CombatManager.Instance.History.Entries
        .OfType<PotionUsedEntry>()
        .Any(e => e.Actor == Owner.Creature && e.HappenedThisTurn(CombatState));

    protected override bool ShouldGlowGoldInternal => UsedPotionThisTurn;

    public Incant()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        if (UsedPotionThisTurn && cardPlay.Target != null)
        {
            await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, DynamicVars.Hex().BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() 
    {
        DynamicVars.Hex().UpgradeValueBy(1m);
    }
}
