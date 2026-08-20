using MegaCrit.Sts2.Core.Models;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Monsters;

namespace TheWitch.TheWitchCode.Powers;

/// <summary>Counter marking how many Wolf familiars the player currently has. See <see cref="FamiliarPower" />.</summary>
public sealed class WolfFamiliarPower : LootTableFamiliarPower
{
    protected override WitchPet Pet => ModelDb.Monster<WolfPet>();

    protected override FamiliarLootTable BuildLootTable() =>
        new FamiliarLootTable()
            .Add<Gnash>()
            .Add<PackTactics>();
}
