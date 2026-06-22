using BaseLib.Abstracts;
using TheWicken.TheWickenCode.Extensions;
using Godot;

namespace TheWicken.TheWickenCode.Character;

public class WickenPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Wicken.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}