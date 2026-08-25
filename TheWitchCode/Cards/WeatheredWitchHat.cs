using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheWitch.TheWitchCode.Cards;

/// <summary>
/// Weathered Witch Hat: Block, then play a random Skill from your draw pile (free auto-play; nothing happens
/// if the pile holds none). Random pick + AutoPlay follow the base-game Catastrophe pattern; Unplayable
/// skills are excluded.
/// </summary>
public sealed class WeatheredWitchHat : WitchCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(10m, ValueProp.Move)
    ];

    public WeatheredWitchHat()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        CardModel? skill = PileType.Draw.GetPile(Owner).Cards
            .Where(c => c.Type == CardType.Skill && !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Shuffle)
            .FirstOrDefault();
        if (skill != null)
        {
            await CardCmd.AutoPlay(choiceContext, skill, null);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
