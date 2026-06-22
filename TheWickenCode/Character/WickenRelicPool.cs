using BaseLib.Abstracts;
using TheWicken.TheWickenCode.Extensions;
using Godot;

namespace TheWicken.TheWickenCode.Character;

public class TheWickenRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => TheWicken.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}