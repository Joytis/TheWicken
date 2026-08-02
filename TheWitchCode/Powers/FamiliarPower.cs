using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Monsters;
using TheWitch.TheWitchCode.Relics;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>
/// Marker base for "familiar" counter powers. Each familiar type (Owl, Cat, …) has its own
/// <see cref="FamiliarPower" /> subclass; playing that familiar applies one stack
/// (see <c>WitchCard.GainFamiliar</c>). The player's total familiar count is the sum of all
/// <see cref="FamiliarPower" /> stacks across the creature (see <see cref="Familiars" />).
///
/// Payoff: just BEFORE the owner's turn hand-draw, the familiar adds one random card it can produce to your
/// hand PER STACK (see <see cref="CreateTurnStartCard" /> — each stack rolls independently), so tokens sit in
/// front of the drawn cards. This replaces the old
/// "shuffle N token cards into your deck on summon" — ongoing, immediate value, and sacrificing the power
/// (<c>PowerCmd.Decrement</c> to zero, which auto-removes the <see cref="PowerStackType.Counter" />) actually
/// costs you those recurring cards.
/// </summary>
public abstract class FamiliarPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// When true, the token cards this familiar produces come out Upgraded. Set by <c>WitchCard.GainFamiliar</c>
    /// when an upgraded familiar summon card is played — sticky once any upgraded summon has applied this power.
    /// </summary>
    public bool GrantsUpgradedCards { get; set; }

    /// <summary>Create one real combat card this familiar can produce (Upgraded per <see cref="GrantsUpgradedCards" />), chosen at random if it has several.</summary>
    protected abstract CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng);

    /// <summary>
    /// Every card this familiar can produce, one of each (Sack of Treats path). Single-card familiars
    /// produce their one card; loot-table familiars override to yield one of EACH table entry.
    /// </summary>
    protected virtual IEnumerable<CardModel> CreateAllTurnStartCards(Player owner, ICombatState combat, Rng rng) =>
        [CreateTurnStartCard(owner, combat, rng)];

    /// <summary>
    /// Canonical cosmetic pet shown at the player's feet — ONE PET PER STACK, so the board shows how many of
    /// which familiar are out without reading the buff bar. The pet count is synced to <see cref="Amount" />
    /// on every stack change (see <see cref="AfterPowerAmountChanged" />); same-type pets cluster together via
    /// <c>WitchPetClusterPatch</c>. Every familiar declares one.
    /// </summary>
    protected abstract WitchPet Pet { get; }

    // BeforeHandDraw (base-game SentryModePower pattern) so tokens enter the hand BEFORE the turn's
    // draw — they sit in front of the drawn cards. Top guards the retained-cards case.
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        await GenerateCards(player, combatState);
    }

    /// <summary>A single roll of this familiar's card production, ignoring stack count (Command's mid-turn order).</summary>
    public async Task GenerateOneCard(Player player, ICombatState combatState)
    {
        Flash();
        Rng rng = player.RunState.Rng.CombatCardGeneration;
        CardModel card = CreateTurnStartCard(player, combatState, rng);
        TagSource(card, 0);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
    }

    /// <summary>
    /// One round of this familiar's card production: one card per stack (all of its cards per stack with
    /// Sack of Treats). Runs at turn start via <see cref="BeforeHandDraw" />.
    /// </summary>
    public async Task GenerateCards(Player player, ICombatState combatState)
    {
        Flash();

        Rng rng = player.RunState.Rng.CombatCardGeneration;
        SackOfTreats? sack = player.GetRelic<SackOfTreats>();
        sack?.Flash();

        for (int i = 0; i < Amount; i++)
        {
            IEnumerable<CardModel> cards = sack != null
                ? CreateAllTurnStartCards(player, combatState, rng)
                : [CreateTurnStartCard(player, combatState, rng)];

            foreach (CardModel card in cards)
            {
                TagSource(card, i);
                
                // Use the "generated" path (not a plain Add) so the card counts as created — records combat
                // history and fires AfterCardGeneratedForCombat, which card-creation payoffs like Cloak of Moonlight listen to.
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
            }
        }
    }

    /// <summary>Summon signature on every familiar gained (assets preloaded via Witch.ExtraAssetPaths). Pet spawning lives in <see cref="AfterPowerAmountChanged" />, which also fires for the initial application.</summary>
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        WitchFx.SummonFlourish(Owner);
        return Task.CompletedTask;
    }

    /// <summary>Keep the cosmetic pet count in lockstep with the stack count (one pet per stack).</summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            await SyncPets();
        }
    }

    /// <summary>Despawn any remaining pets when the power is removed outright (decrement-to-zero already synced to 0 via <see cref="AfterPowerAmountChanged" />).</summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player is not { PlayerCombatState: not null } player)
        {
            return;
        }

        /* Owner death runs this from inside CreatureCmd.KillWithoutCheckingWinCondition's
           RemoveAllPowersAfterDeath loop. Killing a pet there removes its NCreature node, and any
           caller still iterating a snapshot that includes the pets then dereferences a null node —
           The Insatiable's SandpitPower.AfterRemoved does exactly that (player + Target.Pets array
           captured up front, Visuals.Visible set with no null check), so the NRE aborts the enemy
           turn and the surviving co-op player's turn never starts. Hide the pets instead; the
           combat's pet list is torn down with the combat anyway. */
        bool ownerDied = oldOwner.IsDead;

        foreach (Creature pet in FindPets(player))
        {
            if (ownerDied)
            {
                NCreature? petNode = NCombatRoom.Instance?.GetCreatureNode(pet);
                if (petNode != null)
                {
                    petNode.Visuals.Visible = false;
                }
                continue;
            }

            await CreatureCmd.Kill(pet, force: true);
        }
    }

    private async Task SyncPets()
    {
        if (Owner.Player is not { PlayerCombatState: not null } player || Owner.CombatState is not { } combat || Owner.IsDead)
        {
            return;
        }

        List<Creature> pets = FindPets(player);
        int want = Math.Max(0, Amount);
        for (int i = pets.Count; i < want; i++)
        {
            WitchPet petModel = (WitchPet)Pet.ToMutable();
            petModel.SourcePower = this;
            petModel.StackIndex = i;
            Creature pet = combat.CreateCreature(petModel, Owner.Side, null);
            await PlayerCmd.AddPet(pet, player);
        }
        for (int i = pets.Count - 1; i >= want; i--)
        {
            await CreatureCmd.Kill(pets[i], force: true);
        }
    }

    /// <summary>Stamp the generated token with its origin so playing it can animate the matching pet (stack i ↔ pet i, spawn order).</summary>
    private void TagSource(CardModel card, int stackIndex)
    {
        if (card is WitchFamiliarCard familiarCard)
        {
            familiarCard.SourceFamiliar = this;
            familiarCard.SourceStackIndex = stackIndex;
        }
    }

    /// <summary>
    /// Raised when a familiar token is played: (source power, stack index, played card's type).
    /// Each PetVisuals node listens and reacts only to its own (power, index) pair —
    /// purely cosmetic, never touches game state, so firing on every MP client is fine.
    /// </summary>
    public static event Action<FamiliarPower, int, CardType>? AnimationRequested;

    /// <summary>
    /// Announce the played card type for tokens THIS power generated; PetVisuals maps it to an animation.
    /// BeforeCardPlayed (not After) so the pet reacts at the start of the card's visual effects.
    /// </summary>
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card is WitchFamiliarCard familiarCard && ReferenceEquals(familiarCard.SourceFamiliar, this))
        {
            RequestPetAnimation(familiarCard.SourceStackIndex, cardPlay.Card.Type);
        }

        return Task.CompletedTask;
    }

    /// <summary>Ask this power's pet at <paramref name="stackIndex" /> to play the animation PetVisuals maps to <paramref name="cardType" />. Cosmetic only.</summary>
    protected void RequestPetAnimation(int stackIndex, CardType cardType) =>
        AnimationRequested?.Invoke(this, stackIndex, cardType);

    /// <summary>
    /// Cosmetic only: when a token THIS familiar produced deals attack damage, the pet that produced it
    /// plays its attack animation on each hit (fires once per hit of a multi-hit — the per-hit
    /// counterpart of AfterAttack). Never touches game state, so firing on every MP client is fine.
    /// </summary>
    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner
            && props.IsPoweredAttack()
            && cardSource is WitchFamiliarCard familiarCard
            && ReferenceEquals(familiarCard.SourceFamiliar, this))
        {
            RequestPetAnimation(familiarCard.SourceStackIndex, CardType.Attack);
        }

        return Task.CompletedTask;
    }

    protected List<Creature> FindPets(Player player) =>
        player.PlayerCombatState!.Pets.Where(p => p.Monster?.GetType() == Pet.GetType()).ToList();
}

/// <summary>
/// Convenience base for the common case: a familiar that always produces the same single token card
/// <typeparamref name="TCard" />. Single-type familiar powers just declare <c>: FamiliarPower&lt;TCard&gt;</c>.
/// Familiars that produce several different cards (Bear) extend <see cref="FamiliarPower" /> directly
/// and override <see cref="CreateTurnStartCard" />.
/// </summary>
public abstract class FamiliarPower<TCard> : FamiliarPower where TCard : WitchFamiliarCard
{
    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        FamiliarCardRegistry.CreateFamiliarCards<TCard>(owner, 1, combat, GrantsUpgradedCards).First();
}

/// <summary>
/// A weighted "loot table" of familiar token-cards. Each <c>Add&lt;TCard&gt;(weight)</c> registers a card type
/// the familiar can produce; <see cref="Roll" /> picks one with probability proportional to its weight
/// (default weight 1 = uniform). Built once per power via <see cref="LootTableFamiliarPower.BuildLootTable" />.
/// </summary>
public sealed class FamiliarLootTable
{
    private readonly List<(int Weight, Func<Player, ICombatState, bool, CardModel> Create)> _entries = [];
    private int _totalWeight;

    /// <summary>Register a card type the familiar can roll, with optional relative <paramref name="weight" /> (default 1).</summary>
    public FamiliarLootTable Add<TCard>(int weight = 1) where TCard : WitchFamiliarCard
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Loot-table weight must be positive.");
        }

        _entries.Add((weight, (owner, combat, upgraded) => FamiliarCardRegistry.CreateFamiliarCards<TCard>(owner, 1, combat, upgraded).First()));
        _totalWeight += weight;
        return this;
    }

    /// <summary>One card of EACH entry, table order (Sack of Treats: "create ALL of their cards").</summary>
    public IEnumerable<CardModel> CreateAll(Player owner, ICombatState combat, bool upgraded) =>
        _entries.Select(e => e.Create(owner, combat, upgraded));

    /// <summary>Roll one real combat card from the table, weighted by each entry's weight (Upgraded if <paramref name="upgraded" />).</summary>
    public CardModel Roll(Player owner, ICombatState combat, Rng rng, bool upgraded)
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("Familiar loot table is empty.");
        }

        int roll = rng.NextInt(_totalWeight);
        foreach ((int weight, Func<Player, ICombatState, bool, CardModel> create) in _entries)
        {
            if (roll < weight)
            {
                return create(owner, combat, upgraded);
            }

            roll -= weight;
        }

        return _entries[^1].Create(owner, combat, upgraded); // unreachable; satisfies the compiler
    }
}

/// <summary>
/// Convenience base for a familiar that can produce one of several token-cards (a "loot table"), e.g. Bear.
/// Declare the cards (and optional weights) by overriding <see cref="BuildLootTable" />; the table is built
/// once and rolled at the start of each owner turn. Single-card familiars should use <see cref="FamiliarPower{TCard}" />.
/// </summary>
public abstract class LootTableFamiliarPower : FamiliarPower
{
    private FamiliarLootTable? _lootTable;

    /// <summary>Declare the cards this familiar can produce. Called once, lazily.</summary>
    protected abstract FamiliarLootTable BuildLootTable();

    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, Rng rng) =>
        (_lootTable ??= BuildLootTable()).Roll(owner, combat, rng, GrantsUpgradedCards);

    protected override IEnumerable<CardModel> CreateAllTurnStartCards(Player owner, ICombatState combat, Rng rng) =>
        (_lootTable ??= BuildLootTable()).CreateAll(owner, combat, GrantsUpgradedCards);
}
