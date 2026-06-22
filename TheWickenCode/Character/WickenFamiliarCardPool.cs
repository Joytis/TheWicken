using BaseLib.Abstracts;
using TheWicken.TheWickenCode.Extensions;
using Godot;

namespace TheWicken.TheWickenCode.Character;

public class WickenFamiliarCardPool : CustomCardPoolModel
{
    public override string Title => Wicken.CharacterId; //This is not a display name.
    
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();


    /* These HSV values will determine the color of your card back.
    They are applied as a shader onto an already colored image,
    so it may take some experimentation to find a color you like.
    Generally they should be values between 0 and 1. */
    public override float H => 0.4f; //Hue; changes the color.
    public override float S => 0.2f; //Saturation
    public override float V => 0.9f; //Brightness
    
    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load TheWicken/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("ffffff");
    
    public override bool IsColorless => true;

    /* Familiar cards (e.g. Wisdom) are Token-rarity spawned cards not tied to the character's
       random reward pool. This pool is referenced by no character's CardPool, so it must be a
       shared pool to land in ModelDb.AllCardPools. Without this, CardModel.Pool can't find the
       familiar's pool, falls back to MockCardPool, and crashes ("You monster!") the moment a
       familiar card is rendered/hovered (e.g. OwlFamiliar's HoverTipFactory.FromCard<Wisdom>). */
    public override bool IsShared => true;
}