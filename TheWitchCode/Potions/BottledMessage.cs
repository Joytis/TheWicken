using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TheWitch.TheWitchCode.Potions;

/// <summary>
/// Bottled Message: card-only (Token rarity) payload potion created by Message in a Bottle. Stores the
/// bottled card's CANONICAL model + upgrade level + enchantment — never the live combat card (a dead
/// combat's card instance replayed across fights caused an MP desync). Using it creates a fresh copy of
/// that card in your hand via the generated path; hovering previews the held card. State is baked into
/// the run save AND the potion's net serialization via BaseLib's ExtendedSaveTypes (registered in
/// MainFile) as a <see cref="SerializableCard"/> attached to this potion's SerializablePotion entry.
/// </summary>
public sealed class BottledMessage : WitchPotion
{
    public override PotionRarity Rarity => PotionRarity.Token;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    private CardModel? _bottledCanonical;
    private int _bottledUpgrades;
    private EnchantmentModel? _bottledEnchantCanonical;
    private int _bottledEnchantAmount;

    /// <summary>Whether a card is currently bottled.</summary>
    public bool HoldsCard => _bottledCanonical != null;

    /// <summary>Record the bottled card as (canonical model, upgrade level, enchantment) — deterministic, MP-safe data.</summary>
    public void Bottle(CardModel card)
    {
        AssertMutable();
        _bottledCanonical = card.CanonicalInstance;
        _bottledUpgrades = card.CurrentUpgradeLevel;
        _bottledEnchantCanonical = card.Enchantment?.CanonicalInstance;
        _bottledEnchantAmount = card.Enchantment?.Amount ?? 0;
    }

    /// <summary>Extended-save snapshot (registered in MainFile via ExtendedSaveTypes). Null if empty.</summary>
    internal SerializableCard? SaveState =>
        _bottledCanonical is { } canonical
            ? new SerializableCard
            {
                Id = canonical.Id,
                CurrentUpgradeLevel = _bottledUpgrades,
                Enchantment = _bottledEnchantCanonical is { } enchant
                    ? new SerializableEnchantment { Id = enchant.Id, Amount = _bottledEnchantAmount }
                    : null,
            }
            : null;

    /// <summary>Extended-save restore counterpart of <see cref="Bottle"/>. Unknown ids (mod update) leave the bottle empty.</summary>
    internal void RestoreState(SerializableCard? state)
    {
        AssertMutable();
        _bottledCanonical = state?.Id is { } cardId ? ModelDb.GetByIdOrNull<CardModel>(cardId) : null;
        if (_bottledCanonical == null)
        {
            _bottledUpgrades = 0;
            _bottledEnchantCanonical = null;
            _bottledEnchantAmount = 0;
            return;
        }
        _bottledUpgrades = state!.CurrentUpgradeLevel;
        _bottledEnchantCanonical = state.Enchantment?.Id is { } enchantId
            ? ModelDb.GetByIdOrNull<EnchantmentModel>(enchantId)
            : null;
        _bottledEnchantAmount = _bottledEnchantCanonical != null ? state.Enchantment!.Amount : 0;
    }

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        _bottledCanonical is { } canonical ? [HoverTipFactory.FromCard(canonical, _bottledUpgrades > 0)] : [];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (_bottledCanonical is not { } canonical || Owner.Creature.CombatState is not ICombatState combat)
        {
            return;
        }

        CardModel card = combat.CreateCard(canonical, Owner);
        for (int i = 0; i < _bottledUpgrades; i++)
        {
            CardCmd.Upgrade(card);
        }
        if (_bottledEnchantCanonical is { } enchantCanonical)
        {
            CardCmd.Enchant(enchantCanonical.ToMutable(), card, _bottledEnchantAmount);
        }
        _bottledCanonical = null;
        _bottledUpgrades = 0;
        _bottledEnchantCanonical = null;
        _bottledEnchantAmount = 0;

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}
