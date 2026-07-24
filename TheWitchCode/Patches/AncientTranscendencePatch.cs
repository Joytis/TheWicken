using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using TheWitch.TheWitchCode.Cards;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Registers the Witch's starter-transcendence pair in Archaic Tooth's map (base game: Bash→Break, etc.):
/// <see cref="Harvest" /> → <see cref="RipSoul" />. The property getter rebuilds the dictionary on every
/// access, so a postfix covers all readers — including <c>ArchaicTooth.TranscendenceCards</c>, which
/// DustyTome uses to EXCLUDE transcendence cards from its unique-ancient roll.
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
public static class AncientTranscendencePatch
{
    private static void Postfix(ref Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<Harvest>().Id] = ModelDb.Card<RipSoul>();
    }
}
