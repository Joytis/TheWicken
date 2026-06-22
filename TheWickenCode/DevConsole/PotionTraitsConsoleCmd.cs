using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Logging;
using TheWicken.TheWickenCode.Potions.Brewing;

namespace TheWicken.TheWickenCode.DevConsole;

/// <summary>
/// Dev console: <c>potiontraits</c> — dumps every registered potion with its classified
/// <see cref="PotionTrait" /> and <see cref="PotionOrientation" />, grouped by rarity.
/// Useful for eyeballing the auto-derive + override classification (no test suite; validation is manual).
/// </summary>
public sealed class PotionTraitsConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "potiontraits";

    public override string Args => "";

    public override string Description => "Dumps every potion's traits & orientation, grouped by rarity.";

    public override bool IsNetworked => false;

    // Atomic flags only — skip the Offensive/Defensive/Utility masks so combined values list their parts.
    private static readonly PotionTrait[] _atomic =
    {
        PotionTrait.Damage, PotionTrait.Debuff, PotionTrait.Poison,
        PotionTrait.Block, PotionTrait.Buff, PotionTrait.Heal, PotionTrait.MaxHp,
        PotionTrait.Energy, PotionTrait.Draw, PotionTrait.CardGen, PotionTrait.CardManip,
        PotionTrait.PotionGen, PotionTrait.Upgrade, PotionTrait.Aoe,
    };

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        var byRarity = PotionCatalog.All
            .OrderBy(p => p.Rarity)
            .ThenBy(p => p.Id.Entry)
            .GroupBy(p => p.Rarity);

        StringBuilder sb = new StringBuilder().AppendLine("Potion traits:");
        int count = 0;

        foreach (var group in byRarity)
        {
            sb.AppendLine($"== {group.Key} ==");
            foreach (var potion in group)
            {
                PotionTrait traits = PotionTraits.Of(potion);
                PotionOrientation orientation = PotionTraits.OrientationOf(traits);
                sb.AppendLine($"  {potion.Id.Entry,-24} {orientation,-10} {TraitNames(traits)}");
                count++;
            }
        }

        string report = sb.ToString();
        Log.Info(report);
        return new CmdResult(success: true, $"Dumped traits for {count} potions to console & logs.\n{report}");
    }

    private static string TraitNames(PotionTrait traits)
    {
        IEnumerable<string> names = _atomic.Where(a => (traits & a) != 0).Select(a => a.ToString());
        string joined = string.Join("|", names);
        return joined.Length == 0 ? "None" : joined;
    }
}
