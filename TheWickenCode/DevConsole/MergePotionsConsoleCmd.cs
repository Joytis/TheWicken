using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.DevConsole;

/// <summary>
/// Dev console: <c>mergepotions</c> — brews the first two potions in the issuing player's belt into a
/// higher-rarity result, consuming both. With exactly one potion, it is replaced with a Wicked Brew; an empty
/// belt errors. Delegates to <see cref="PotionMerge" />, the same behavior the Brew card uses.
/// </summary>
public sealed class MergePotionsConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "mergepotions";

    public override string Args => "";

    public override string Description =>
        "Brews the first two belt potions into a higher-rarity one. One potion -> Wicked Brew.";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer == null)
        {
            return new CmdResult(success: false, "No issuing player.");
        }
        if (!RunManager.Instance.IsInProgress)
        {
            return new CmdResult(success: false, "A run is not in progress.");
        }

        MergeOutcome outcome = PotionMerge.Plan(issuingPlayer, issuingPlayer.RunState.Rng.CombatPotionGeneration);
        if (!outcome.WillChangeBelt)
        {
            return new CmdResult(success: false, outcome.Description);
        }

        return new CmdResult(PotionMerge.Apply(issuingPlayer, outcome), success: true, outcome.Description);
    }
}
