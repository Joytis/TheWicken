using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using TheWitch.TheWitchCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using TheWitch.TheWitchCode.Cards;
using TheWitch.TheWitchCode.Relics;

namespace TheWitch.TheWitchCode.Character;

public class Witch : PlaceholderCharacterModel
{
    public const string CharacterId = "Witch";
    
    // Colors
    public static readonly Color Color = new("BC8F8F");
    public static readonly Color DarkColor = new("3D1714FF");
    public override Color NameColor => Color;
    public override Color MapDrawingColor => Color;                            
    public override Color RemoteTargetingLineColor => Color;
    public override Color RemoteTargetingLineOutline => DarkColor;
    public override Color EnergyLabelOutlineColor => DarkColor;

    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<StrikeWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<DefendWitch>(),
        ModelDb.Card<Harvest>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<LargePockets>()
    ];
    
    // Preload the non-globally-preloaded vfx scenes used by WitchFx signatures (powers have no
    // ExtraRunAssetPaths hook, so power-spawned vfx like the Bramble slice must preload here).
    protected override IEnumerable<string> ExtraAssetPaths => [
        .. MegaCrit.Sts2.Core.Nodes.Vfx.NPowerUpVfx.AssetPaths,
        .. MegaCrit.Sts2.Core.Nodes.Vfx.NThinSliceVfx.AssetPaths,
        .. MegaCrit.Sts2.Core.Nodes.Vfx.NGroundFireVfx.AssetPaths,
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<WitchCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<WitchRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<WitchPotionPool>();
    
    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets. 
        These are just some of the simplest assets, given some placeholders to differentiate your character with. 
        You don't have to, but you're suggested to rename these images. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();

    public override string CustomEnergyCounterPath => "witch_energy_counter.tscn".CharacterScenePath();
    public override string CustomVisualPath => "witch_visuals.tscn".CharacterScenePath();
    public override string CustomCharacterSelectBg => "char_select_bg_witch.tscn".CharacterScenePath();
    public override string CustomTrailPath => "card_trail_witch.tscn".CharacterScenePath();
    public override string CustomRestSiteAnimPath => "witch_rest_site.tscn".CharacterScenePath();
    public override string CustomMerchantAnimPath => "witch_merchant.tscn".CharacterScenePath();

    // Arms (events / rock-paper-scissors)
    public override string CustomArmPointingTexturePath => "multiplayer_hand_witch_point.png".CharacterUiPath();
    public override string CustomArmRockTexturePath => "multiplayer_hand_witch_rock.png".CharacterUiPath();
    public override string CustomArmPaperTexturePath => "multiplayer_hand_witch_paper.png".CharacterUiPath();
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_witch_scissors.png".CharacterUiPath();


    /*  CustomCharacterModel virtuals not yet overridden — fill these in when replacing
        PlaceholderCharacterModel with CustomCharacterModel.
        (Skipped: CustomEnergyCounter — legacy API, we use CustomEnergyCounterPath.)

    // Behavior / flags
    // public override List<(string, string)> Localization => ...;
    // public override bool HideFromVanillaCharacterSelect => ...;
    // public override bool AllowInVanillaRandomCharacterSelect => ...;   // defaults to !HideFromVanillaCharacterSelect
    // public override bool HideInCompendium => ...;

    // Icons
    // public override string CustomIconOutlineTexturePath => ...;
    // public override string CustomIconPath => ...;

    // Anims / visuals
    // public override float DeathAnimTime => ...;
    // public override NCreatureVisuals CreateCustomVisuals() => ...;
    // public override CreatureAnimator SetupCustomAnimationStates(MegaSprite controller) => ...;
    // public override void RegisterSceneConversions() => ...;

    // public override RelicIconData CustomYummyCookie => ...;

    // Character select
    // public override string CustomCharacterSelectTransitionPath => ...;

    // Audio
    // public override string CustomAttackSfx => ...;
    // public override string CustomCastSfx => ...;
    // public override string CustomDeathSfx => ...;

    // Currently provided by PlaceholderCharacterModel — must supply once off the placeholder base:
    // public override string CharacterSelectSfx => ...;
    // public override string CharacterTransitionSfx => ...;
    // public override List<string> GetArchitectAttackVfx() => ...;
    */
}