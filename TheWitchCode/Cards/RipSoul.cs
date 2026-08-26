using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Rip Soul: the Ancient (transcended) form of Extract Essence — tear the soul out of an enemy: heavy damage,
/// Hex, and one random Unstable potion (any non-healing potion the Witch can roll, any rarity). Granted by the
/// Archaic Tooth transcendence map (see <see cref="Patches.AncientTranscendencePatch" />).
/// </summary>
public sealed class RipSoul : WitchCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
        UnstablePotions.UnstableHoverTip,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10m, ValueProp.Move),
        new PowerVar<HexPower>(2m),
    ];

    public RipSoul()
        : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(null, null, "heavy_attack.mp3")
            .WithHitVfxNode(WitchFx.RipSoulImpactNode)
            .Execute(choiceContext);

        // Color(0.505, 1.196, 1.353)

        await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, DynamicVars.Hex().BaseValue, Owner.Creature, this);

        // Any non-healing potion the Witch can roll (Query defaults: randomizable pool, healing excluded).
        PotionModel? created = await PotionCatalog.Pick(
            PotionCatalog.Query(), choiceContext, Owner, Owner.RunState.Rng.CombatPotionGeneration);
        if (created != null)
        {
            await Witch.ProducePotion(created, Owner, Witch.PotionMode.Unstable);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
