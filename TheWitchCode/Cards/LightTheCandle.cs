using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Potions;

using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Artists;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Light the Candle: upgrade random cards in your hand (via <c>CardCmd.Upgrade</c> so any on-upgrade listeners
/// fire), then create a Vial of Smoke.
/// </summary>
public sealed class LightTheCandle : WitchCard
{
    public override Artist? ArtBy => Artist.Joytis;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPotion<VialOfSmoke>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public LightTheCandle()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        List<CardModel> upgradable = PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList();
        int count = DynamicVars.Cards.IntValue;
        for (int i = 0; i < count && upgradable.Count > 0; i++)
        {
            CardModel pick = Owner.RunState.Rng.CombatCardGeneration.NextItem(upgradable)!;
            upgradable.Remove(pick);
            WitchFx.EnchantShimmer();
            CardCmd.Upgrade(pick);
        }

        await Witch.ProducePotion<VialOfSmoke>(Owner, Witch.PotionMode.Unstable);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}
