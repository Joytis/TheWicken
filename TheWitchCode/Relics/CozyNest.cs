using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Powers;

namespace TheWitch.TheWitchCode.Relics;

/// <summary>
/// Cozy Nest (shop): at the start of combat, summon a Crow Familiar — one stack of
/// <see cref="CrowFamiliarPower" />, same as playing an unupgraded Crow Familiar (the power
/// spawns the pet and produces the turn-start token cards).
/// </summary>
public sealed class CozyNest : WitchRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<CrowFamiliarPower>(),
        HoverTipFactory.FromCard<DarkOmen>(),
        HoverTipFactory.FromCard<Shiny>(),
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<CrowFamiliarPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
    }
}
