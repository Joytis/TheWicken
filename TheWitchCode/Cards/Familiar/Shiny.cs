using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using TheWitch.TheWitchCode.Character;
using TheWitch.TheWitchCode.Potions.Treasures;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>Crow familiar token: the Crow drags home a shiny — create a random Treasure potion.</summary>
public sealed class Shiny : WitchFamiliarCard
{
    // Resolved lazily: ModelDb isn't populated when static initializers can run.
    private static List<PotionModel> Treasures => [
        ModelDb.Potion<PolishedBottlecap>(),
        ModelDb.Potion<ShinyHairpin>(),
        ModelDb.Potion<ForeignCoin>()
    ];

    private static List<PotionModel> BigTreasures => [
        ModelDb.Potion<CrackedGemstone>(),
        ModelDb.Potion<FancyComb>(),
        ModelDb.Potion<ImpeccableSilverware>()
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        new HoverTip(
            new LocString("static_hover_tips", IsUpgraded ? "THEWITCH-BIG_TREASURE.title" : "THEWITCH-TREASURE.title"),
            new LocString("static_hover_tips", IsUpgraded ? "THEWITCH-BIG_TREASURE.description" : "THEWITCH-TREASURE.description")),
    ];

    public Shiny()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<PotionModel> pool = IsUpgraded ? BigTreasures : Treasures;
        PotionModel pick = Owner.RunState.Rng.CombatPotionGeneration.NextItem(pool)!;
        await Witch.ProducePotion(pick, Owner);
    }

    protected override void OnUpgrade()
    {
    }
}
