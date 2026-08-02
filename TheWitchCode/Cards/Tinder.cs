using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Tinder: burn a bramble as kindling for Energy.</summary>
public sealed class Tinder : WitchCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BramblesPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1)
    ];

    protected override bool IsPlayable => Owner.Creature.GetPower<BramblesPower>() is { Amount: > 0 };

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public Tinder()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        BramblesPower? brambles = Owner.Creature.GetPower<BramblesPower>();
        if (brambles == null)
        {
            return; // IsPlayable gates this; guard for forced plays
        }
        await PowerCmd.Decrement(brambles);

        // Fire Vfx
        SfxCmd.Play("event:/sfx/characters/attack_fire");
        WitchFx.PlayFlipbook("vfx/fire_impact/vfx_fire_burst_center_flipbook", Owner.Creature, null, 0.7f);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
