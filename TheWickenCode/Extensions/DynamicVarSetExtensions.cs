using Godot;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheWicken.TheWickenCode.Extensions;

//Mostly utilities to get asset paths.
public static class DynamicVarSetExtensions
{
    public static PowerVar<BramblesPower> Brambles(this DynamicVarSet set) => (PowerVar<BramblesPower>)set["BramblesPower"];
}
