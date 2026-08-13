using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Hexblast: blast every enemy, then lay Hex on all of them.</summary>
public sealed class Hexblast : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HexPower>(2m),
        new DamageVar(8m, ValueProp.Move)
    ];

    public Hexblast()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(2)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            // Purple occult flame under the target (preloaded via Witch.ExtraAssetPaths) + heavy sting.
            .WithHitVfxNode(t => NGroundFireVfx.Create(t, VfxColor.Purple))
            .WithHitFx(null, null, "heavy_attack.mp3")
            .Execute(choiceContext);

        await PowerCmd.Apply<HexPower>(choiceContext, CombatState!.HittableEnemies, DynamicVars.Hex().BaseValue, Owner.Creature, this);

        // Detonation punch on top of the hit shake.
        NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
    }

    protected override void OnUpgrade() 
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Hex().UpgradeValueBy(1m);
    }
}
