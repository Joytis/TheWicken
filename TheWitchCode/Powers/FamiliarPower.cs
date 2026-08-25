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
using MegaCrit.Sts2.Core.ValueProps;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Extensions;
using TheWitch.TheWitchCode.Monsters;
using TheWitch.TheWitchCode.Relics;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>Cosmetic pet reactions PetVisuals can play (mapped to AnimationPlayer clips in the pet scene).</summary>
public enum FamiliarPetAnim
{
    Attack,
    Skill,
    Create,
}

/// <summary>
/// Marker base for "familiar" counter powers. Each familiar type (Owl, Cat, …) has its own
/// <see cref="FamiliarPower" /> subclass; playing that familiar applies one stack
/// (see <c>WitchCard.GainFamiliar</c>). The player's total familiar count is the sum of all
/// <see cref="FamiliarPower" /> stacks across the creature (see <see cref="Familiars" />).
///
/// Payoff: just BEFORE the owner's turn hand-draw, the familiar adds one card it can produce to your
/// hand PER STACK (see <see cref="CreateTurnStartCard" /> — each stack cycles through the familiar's card
/// list independently), so tokens sit in front of the drawn cards. This replaces the old
/// "shuffle N token cards into your deck on summon" — ongoing, immediate value, and sacrificing the power
/// (<c>PowerCmd.Decrement</c> to zero, which auto-removes the <see cref="PowerStackType.Counter" />) actually
/// costs you those recurring cards.
/// </summary>
public abstract class FamiliarPower : WitchPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// How many of this power's stacks came from an UPGRADED summon card — those stacks produce Upgraded
    /// token cards. Tracked PER STACK (not one sticky flag) so 3 normal + 3 upgraded Crows produce
    /// 3 normal + 3 upgraded tokens. Incremented by <c>WitchCard.GainFamiliar</c>; clamped to
    /// <see cref="Amount" /> when stacks are sacrificed. Upgraded stacks occupy the low indices.
    /// </summary>
    public int UpgradedStacks { get; set; }

    /// <summary>Does the stack at <paramref name="stackIndex" /> produce Upgraded tokens?</summary>
    protected bool IsStackUpgraded(int stackIndex) => stackIndex < UpgradedStacks;

    /// <summary>
    /// Create one real combat card this familiar can produce (Upgraded per <paramref name="upgraded" />).
    /// Multi-card familiars CYCLE deterministically through their card list per stack (first generation →
    /// first card, second → second, …, wrap), keyed by <paramref name="stackIndex" />.
    /// </summary>
    protected abstract CardModel CreateTurnStartCard(Player owner, ICombatState combat, bool upgraded, int stackIndex);

    /// <summary>
    /// Every card this familiar can produce, one of each (Sack of Treats path). Single-card familiars
    /// produce their one card; loot-table familiars override to yield one of EACH table entry.
    /// </summary>
    protected virtual IEnumerable<CardModel> CreateAllTurnStartCards(Player owner, ICombatState combat, bool upgraded, int stackIndex) =>
        [CreateTurnStartCard(owner, combat, upgraded, stackIndex)];

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

    /// <summary>A single roll of this familiar's card production, ignoring stack count (Command's mid-turn order).
    /// <paramref name="stackIndex" /> picks which pet performs the create animation.</summary>
    public async Task GenerateOneCard(Player player, ICombatState combatState, int stackIndex = 0)
    {
        Flash();
        RequestPetAnimation(stackIndex, FamiliarPetAnim.Create);
        CardModel card = CreateTurnStartCard(player, combatState, IsStackUpgraded(stackIndex), stackIndex);
        TagSource(card, stackIndex);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
    }

    /// <summary>
    /// One round of this familiar's card production: one card per stack (all of its cards per stack with
    /// Sack of Treats). Runs at turn start via <see cref="BeforeHandDraw" />.
    /// </summary>
    public async Task GenerateCards(Player player, ICombatState combatState)
    {
        Flash();

        SackOfTreats? sack = player.GetRelic<SackOfTreats>();
        sack?.Flash();

        for (int i = 0; i < Amount; i++)
        {
            RequestPetAnimation(i, FamiliarPetAnim.Create);

            bool upgraded = IsStackUpgraded(i);
            IEnumerable<CardModel> cards = sack != null
                ? CreateAllTurnStartCards(player, combatState, upgraded, i)
                : [CreateTurnStartCard(player, combatState, upgraded, i)];

            foreach (CardModel card in cards)
            {
                TagSource(card, i);
                
                // Use the "generated" path (not a plain Add) so the card counts as created — records combat
                // history and fires AfterCardGeneratedForCombat, which card-creation payoffs like Cloak of Moonlight listen to.
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
                await Cmd.Wait(0.1f);
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
            // Sacrificing stacks can't leave more upgraded stacks than stacks (normal ones are spent first).
            UpgradedStacks = Math.Clamp(UpgradedStacks, 0, Math.Max(0, Amount));
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
    public static event Action<FamiliarPower, int, FamiliarPetAnim>? AnimationRequested;

    /// <summary>
    /// Announce the played card type for tokens THIS power generated; PetVisuals maps it to an animation.
    /// BeforeCardPlayed (not After) so the pet reacts at the start of the card's visual effects.
    /// </summary>
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card is WitchFamiliarCard familiarCard && ReferenceEquals(familiarCard.SourceFamiliar, this))
        {
            RequestPetAnimation(familiarCard.SourceStackIndex,
                cardPlay.Card.Type == CardType.Attack ? FamiliarPetAnim.Attack : FamiliarPetAnim.Skill);
        }

        return Task.CompletedTask;
    }

    /// <summary>Ask this power's pet at <paramref name="stackIndex" /> to play <paramref name="anim" />. Cosmetic only.</summary>
    protected void RequestPetAnimation(int stackIndex, FamiliarPetAnim anim) =>
        AnimationRequested?.Invoke(this, stackIndex, anim);

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
            RequestPetAnimation(familiarCard.SourceStackIndex, FamiliarPetAnim.Attack);
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
    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, bool upgraded, int stackIndex) =>
        FamiliarCardRegistry.CreateFamiliarCards<TCard>(owner, 1, combat, upgraded).First();
}

/// <summary>
/// The ordered card list of a multi-card familiar. Each <c>Add&lt;TCard&gt;()</c> registers a card type the
/// familiar can produce; generation CYCLES through the entries in declaration order (see
/// <see cref="LootTableFamiliarPower" />). Built once per power via <see cref="LootTableFamiliarPower.BuildLootTable" />.
/// </summary>
public sealed class FamiliarLootTable
{
    /// <summary>One producible card type. The delegate exists because card creation is generic
    /// (<c>FamiliarCardRegistry.CreateFamiliarCards&lt;TCard&gt;</c>) — <see cref="Add{TCard}" /> captures the
    /// type in a closure at registration so lookup needs no reflection.</summary>
    private readonly record struct Entry(Func<Player, ICombatState, bool, CardModel> Create);

    private readonly List<Entry> _entries = [];

    /// <summary>Register a card type the familiar can produce; declaration order is the cycle order.</summary>
    public FamiliarLootTable Add<TCard>() where TCard : WitchFamiliarCard
    {
        _entries.Add(new Entry((owner, combat, upgraded) =>
            FamiliarCardRegistry.CreateFamiliarCards<TCard>(owner, 1, combat, upgraded).First()));
        return this;
    }

    /// <summary>One card of EACH entry, table order (Sack of Treats: "create ALL of their cards").</summary>
    public IEnumerable<CardModel> CreateAll(Player owner, ICombatState combat, bool upgraded) =>
        _entries.Select(e => e.Create(owner, combat, upgraded));

    /// <summary>Number of entries in the table (the cycle length).</summary>
    public int Count => _entries.Count;

    /// <summary>The card at <paramref name="position" /> in table order, wrapping past the end (cycling path).</summary>
    public CardModel CardAt(int position, Player owner, ICombatState combat, bool upgraded)
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("Familiar loot table is empty.");
        }

        return _entries[position % _entries.Count].Create(owner, combat, upgraded);
    }
}

/// <summary>
/// Convenience base for a familiar that can produce one of several token-cards (a "loot table"), e.g. Bear.
/// Declare the cards by overriding <see cref="BuildLootTable" />; each stack CYCLES deterministically through
/// the table in declaration order — its first generation makes the first card, the next the second, …, then
/// wraps — tracked per stack in <see cref="_cyclePositionByStack" />. (Plain instance field, not DynamicVars:
/// mid-combat state is never save-restored and MP is lockstep, so a field is safe — see the NeverendingPotion
/// precedent.) Single-card familiars should use <see cref="FamiliarPower{TCard}" />.
/// </summary>
public abstract class LootTableFamiliarPower : FamiliarPower
{
    private FamiliarLootTable? _lootTable;

    // Not readonly: DeepCloneFields must give each mutable clone its own list — MemberwiseClone would share
    // the canonical's list with every clone, leaking cycle positions across combats (Crystal Bottle bug class).
    private List<int> _cyclePositionByStack = [];

    /// <summary>Declare the cards this familiar can produce. Called once, lazily.</summary>
    protected abstract FamiliarLootTable BuildLootTable();

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _cyclePositionByStack = [];
    }

    /// <summary>
    /// Sacrificed stacks drop their cycle state, so a later re-summon starts at the first card again.
    /// Trimming the TAIL mirrors the base <c>UpgradedStacks</c> clamp: normal stacks (high indices) are
    /// spent first, so stack identity below <see cref="PowerModel.Amount" /> is stable.
    /// </summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        int keep = Math.Max(0, Amount);
        if (power == this && _cyclePositionByStack.Count > keep)
        {
            _cyclePositionByStack.RemoveRange(keep, _cyclePositionByStack.Count - keep);
        }
    }

    /// <summary>This stack's current cycle position, post-incremented so its next generation advances.</summary>
    private int NextCyclePosition(int stackIndex)
    {
        while (_cyclePositionByStack.Count <= stackIndex)
        {
            _cyclePositionByStack.Add(0);
        }

        return _cyclePositionByStack[stackIndex]++;
    }

    protected override CardModel CreateTurnStartCard(Player owner, ICombatState combat, bool upgraded, int stackIndex) =>
        (_lootTable ??= BuildLootTable()).CardAt(NextCyclePosition(stackIndex), owner, combat, upgraded);

    protected override IEnumerable<CardModel> CreateAllTurnStartCards(Player owner, ICombatState combat, bool upgraded, int stackIndex) =>
        (_lootTable ??= BuildLootTable()).CreateAll(owner, combat, upgraded);
}
