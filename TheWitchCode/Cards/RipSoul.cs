using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;
using TheWitch.TheWitchCode.Powers;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Rip Soul: the Ancient (transcended) form of Extract Essence — tear the soul out of an enemy: heavy damage,
/// 3 Hex, and ONE potent potion from a hard-coded loot table (Extract Essence pattern). Granted by the
/// Archaic Tooth transcendence map (see <see cref="Patches.AncientTranscendencePatch" />).
/// </summary>
public sealed class RipSoul : WitchCard
{
    // Curated "good one" pool — all Rare, all from the Witch+shared randomizable set. Trim/add freely.
    private static IEnumerable<PotionModel> LootTable => [
        ModelDb.Potion<EntropicBrew>(),
        ModelDb.Potion<GigantificationPotion>(),
        ModelDb.Potion<MazalethsGift>(),
        ModelDb.Potion<DistilledChaos>(),
        ModelDb.Potion<ShipInABottle>(),
        ModelDb.Potion<BuddyInABottle>(),
        ModelDb.Potion<TouchOfInsanity>(),
        ModelDb.Potion<LiquidMemories>(),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<HexPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(15m, ValueProp.Move),
        new PowerVar<HexPower>(3m),
    ];

    public RipSoul()
        : base(2, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_scream", null, "heavy_attack.mp3")
            .Execute(choiceContext);

        await PowerCmd.Apply<HexPower>(choiceContext, cardPlay.Target, DynamicVars.Hex().BaseValue, Owner.Creature, this);

        PotionModel? created = PotionCatalog.Random(LootTable, Owner.RunState.Rng.CombatPotionGeneration);
        if (created != null)
        {
            await PotionCmd.TryToProcure(created.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
