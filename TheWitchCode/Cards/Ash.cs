using MegaCrit.Sts2.Core.Entities.Cards;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Ash: inert Unplayable status left behind by Bonfire / Tinder (base-game Soot pattern). Status rarity keeps it
/// out of card rewards; in-combat generation opts out explicitly because it lives in WitchCardPool rather than the
/// shared StatusCardPool (see Wormy).
/// </summary>
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
