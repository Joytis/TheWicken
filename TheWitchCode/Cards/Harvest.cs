using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Harvest: deal damage; on unblocked damage, create a random potion from a HARD-CODED
/// loot table (Herbal Brew pattern — not a live catalog query), so the pool is tuned per card.
/// </summary>
public sealed class Harvest : WitchCard
{
    // First pass: every Common the card could previously roll (shared pool + Witch pool).
    // Trim/add freely — this list IS the card's roll pool.
    private static List<PotionModel> LootTable => [
        // Common
        ModelDb.Potion<PuffOfSmoke>(),
        ModelDb.Potion<PricklyVial>(),
        ModelDb.Potion<OminousFlask>(),
        ModelDb.Potion<EmberJar>(),

    ];

    private static List<PotionModel> SecondaryLootTable => [
        ModelDb.Potion<CatWhisker>(),
        ModelDb.Potion<CrowTalon>(),
        ModelDb.Potion<OwlFeather>(),
        ModelDb.Potion<WolfFang>(),
        ModelDb.Potion<RatTail>(),
        ModelDb.Potion<BearFur>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move)
    ];

    public Harvest()
        : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);

        var rng = Owner.RunState.Rng.CombatPotionGeneration;
        var roll = rng.NextInt(LootTable.Count + 1);

        PotionModel potion = roll == LootTable.Count ? 
            SecondaryLootTable[rng.NextInt(SecondaryLootTable.Count)] :
            LootTable[roll];

        if (potion != null)
        {
            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
