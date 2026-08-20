using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Ash: inert Unplayable status left behind by Bonfire / Tinder (base-game Soot pattern). Lives in the base-game
/// StatusCardPool (own [Pool] overrides WitchCard's) so it renders with the default grey status styling instead of
/// the witch frame; nothing rolls random cards from that pool, so membership has no drop-rate side effects.
/// </summary>
[Pool(typeof(StatusCardPool))]
public sealed class Ash : WitchCard
{
    public override bool CanBeGeneratedInCombat => false;

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];

    public Ash()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.None)
    {
    }
}
