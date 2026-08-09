using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Potions.Brewing;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Witchcraft: spend X Energy, create X random potions. Each roll uses the game's own drop weights
/// (10% Rare / 25% Uncommon / 65% Common, the <c>PotionFactory</c> thresholds) over the Randomizable
/// pool (Witch + Shared, no healing).
/// </summary>
public sealed class Witchcraft : WitchCard
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Witchcraft()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        Rng rng = Owner.RunState.Rng.CombatPotionGeneration;
        int times = ResolveEnergyXValue();
        if (IsUpgraded)
		{
			times++;
		}
        for (int i = 0; i < times; i++)
        {
            // Base-game PotionFactory rarity thresholds: <=0.1 Rare, <=0.35 Uncommon, else Common.
            float roll = rng.NextFloat();
            PotionRarity rarity = roll <= 0.1f ? PotionRarity.Rare
                : roll <= 0.35f ? PotionRarity.Uncommon
                : PotionRarity.Common;
            List<PotionModel> pool = PotionCatalog.Query(rarity: rarity).ToList();
            if (pool.Count == 0)
            {
                pool = PotionCatalog.Query().ToList();
            }
            PotionModel? potion = await PotionCatalog.Pick(pool, choiceContext, Owner, rng);
            if (potion != null)
            {
                WitchFx.Splash(Owner.Creature, new Godot.Color("ac54b3")); // conjured brew: purple splash
                await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
            }
        }
    }
}
