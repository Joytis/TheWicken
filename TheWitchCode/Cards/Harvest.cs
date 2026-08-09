using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Harvest: deal damage and create small potions from a HARD-CODED loot table (hand-tuned —
/// trim/add freely). These are the raw ingredients the Orientation Brew cards transform.
/// </summary>
public sealed class Harvest : WitchCard
{
    // Small "ingredient" potions only. Trim/add freely — this list IS the card's roll pool.
    private static List<PotionModel> LootTable => [
        ModelDb.Potion<PuffOfSmoke>(),
        ModelDb.Potion<PricklyVial>(),
        ModelDb.Potion<OminousFlask>(),
        ModelDb.Potion<EmberJar>(),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [ UnstablePotions.UnstableHoverTip ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3m, ValueProp.Move),
        new IntVar("Potions", 1m)
    ];

    public Harvest()
        : base(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx(VfxCmd.slashPath)
            .Execute(choiceContext);

        for (int i = 0; i < DynamicVars["Potions"].IntValue; i++)
        {
            PotionModel? potion = await PotionCatalog.Pick(
                LootTable, choiceContext, Owner, Owner.RunState.Rng.CombatPotionGeneration);
            if (potion != null)
            {
                await Witch.ProducePotion(potion, Owner, Witch.PotionMode.Unstable);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars["Potions"].UpgradeValueBy(1m);
}
