using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Extract Life: an Attack that also enchants a chosen card in your hand with Replay and Exhaust — one big
/// extra payoff this combat, then gone. Replay enchant mirrors the base-game <c>HiddenGem</c> pattern
/// (<c>BaseReplayCount += Replay</c>, then <c>CardCmd.Preview</c>); Exhaust is added via <c>AddKeyword</c>.
/// </summary>
public sealed class ExtractLife : WitchCard
{
    public override Artists.Artist? ArtBy => Artists.Artist.Joytis;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move),
        new IntVar("Replay", 1m)
    ];

    public ExtractLife()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(VfxCmd.bloodyImpactPath)
            .Execute(choiceContext);

        CardModel? chosen = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: Owner,
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            filter: c => !c.Keywords.Contains(CardKeyword.Unplayable),
            source: this)).FirstOrDefault();
        if (chosen != null)
        {
            chosen.BaseReplayCount += DynamicVars["Replay"].IntValue;
            chosen.AddKeyword(CardKeyword.Exhaust);
            CardCmd.Preview(chosen);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Replay"].UpgradeValueBy(1m);
}
