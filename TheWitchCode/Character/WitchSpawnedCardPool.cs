using BaseLib.Abstracts;
using TheWitch.TheWitchCode.Extensions;
using Godot;

namespace TheWitch.TheWitchCode.Character;

/// <summary>
/// Shared pool for cards that are created by other Witch cards and must look like real Witch cards
/// (e.g. the Wicker Bones / Wicker Consumation chain, which render with the Rare frame) but never appear in
/// card rewards, shops, or random-card rolls. Random rewards only draw from the character's main
/// <see cref="WitchCardPool" />; a shared pool (<see cref="IsShared" />) is registered in ModelDb so the cards
/// render/hover safely, but nothing rolls from it. Frame/colour values mirror <see cref="WitchCardPool" />.
/// </summary>
public class WitchSpawnedCardPool : CustomCardPoolModel
{
    public override string Title => Witch.CharacterId; //This is not a display name.

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    public override float H => 0.0f;
    public override float S => 0.24f;
    public override float V => 1.0f;

    public override Color DeckEntryCardColor => new("BC8F8F");

    public override bool IsColorless => false;

    public override bool IsShared => true;
}
