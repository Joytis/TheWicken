using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Random;
using TheWitch.TheWitchCode.Extensions;

namespace TheWitch.TheWitchCode.Patches;

/// <summary>
/// Registry + Harmony patches behind the Mushroom Extract potion. A "mushroomed" card shows random-letter
/// gibberish for its name and description and uses the <c>mystery.png</c> art — only that specific card
/// instance, only visually (it stays fully playable). The gibberish is generated once per card and stored,
/// so it's stable for the rest of the combat. The store is keyed by card instance via a
/// <see cref="ConditionalWeakTable{TKey,TValue}" />, so it's collected automatically when the combat's card
/// clones are discarded — no explicit combat-end cleanup needed.
///
/// Card cost-0 is handled separately by the potion via <c>CardModel.SetToFreeThisCombat()</c>; this file only
/// owns the cosmetic override.
/// </summary>
public static class MushroomedCards
{
    public sealed record Gibberish(string Title, string Description);

    private static readonly ConditionalWeakTable<CardModel, Gibberish> Store = new();

    private static readonly string[] Syllables =
    [
        "bo", "gru", "mish", "fa", "lor", "nak", "wim", "ble", "zu", "thra", "glor", "pfo", "oo", "ka", "tz",
        "ip", "mun", "dle", "sev", "qua", "rin", "tho", "blu", "grik", "sna", "fenn", "wob", "plo", "dox", "yib",
    ];

    /// <summary>Tag <paramref name="card" /> as mushroomed, generating + storing its gibberish text once.</summary>
    public static void Mark(CardModel card, Rng rng)
    {
        if (Store.TryGetValue(card, out _))
        {
            return;
        }
        Store.Add(card, Generate(rng));
    }

    public static bool TryGet(CardModel card, out Gibberish gibberish) => Store.TryGetValue(card, out gibberish!);

    private static Texture2D? _mystery;

    /// <summary>The shared "mystery" portrait used for every mushroomed card (falls back to card.png if missing).</summary>
    public static Texture2D? MysteryPortrait =>
        _mystery ??= ResourceLoader.Load<Texture2D>("mystery.tres".CardAtlasPath(), null, ResourceLoader.CacheMode.Reuse);

    private static Gibberish Generate(Rng rng)
    {
        int titleWords = 1 + rng.NextInt(2); // 1–2 words
        string title = string.Join(' ', Range(titleWords, () => Word(rng, 2 + rng.NextInt(3))));

        int sentences = 2 + rng.NextInt(3); // 2–4 "sentences"
        string description = string.Join(' ', Range(sentences, () =>
        {
            int words = 3 + rng.NextInt(4); // 3–6 words
            return string.Join(' ', Range(words, () => Word(rng, 1 + rng.NextInt(3)))) + ".";
        }));

        return new Gibberish(title, description);
    }

    private static IEnumerable<string> Range(int count, Func<string> make)
    {
        for (int i = 0; i < count; i++)
        {
            yield return make();
        }
    }

    private static string Word(Rng rng, int syllables)
    {
        string word = string.Concat(Range(syllables, () => Syllables[rng.NextInt(Syllables.Length)]));
        return char.ToUpperInvariant(word[0]) + word[1..];
    }
}

[HarmonyPatch(typeof(CardModel), "get_Title")]
internal static class MushroomedTitlePatch
{
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (MushroomedCards.TryGet(__instance, out MushroomedCards.Gibberish g))
        {
            __result = g.Title;
        }
    }
}

/// <summary>
/// Targets the PRIVATE 3-arg <c>GetDescriptionForPile(PileType, DescriptionPreviewType, Creature)</c> — the real
/// body that every description path (hand, hover previews, upgrade preview) funnels into. The public 2-arg overload
/// is a one-line forwarder that the JIT inlines into <c>NCard</c> once it goes hot (tier-1 rejit), which silently
/// bypasses a patch placed on it — the "Mushroom Extract sometimes shows the real text" bug.
/// </summary>
[HarmonyPatch]
internal static class MushroomedDescriptionPatch
{
    private static MethodBase TargetMethod()
    {
        Type previewType = AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType")
            ?? throw new MissingMemberException("CardModel.DescriptionPreviewType not found");
        return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", [typeof(PileType), previewType, typeof(Creature)])
            ?? throw new MissingMethodException("CardModel.GetDescriptionForPile(PileType, DescriptionPreviewType, Creature) not found");
    }

    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (MushroomedCards.TryGet(__instance, out MushroomedCards.Gibberish g))
        {
            __result = g.Description;
        }
    }
}

[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
internal static class MushroomedHoverTipsPatch
{
    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        // Keyword/status hover tips would leak the real card's identity through the gibberish.
        if (MushroomedCards.TryGet(__instance, out _))
        {
            __result = [];
        }
    }
}

[HarmonyPatch(typeof(CardModel), "get_Portrait")]
internal static class MushroomedPortraitPatch
{
    private static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (MushroomedCards.TryGet(__instance, out _) && MushroomedCards.MysteryPortrait is { } mystery)
        {
            __result = mystery;
        }
    }
}

/// <summary>
/// The model-level <c>get_Portrait</c> patch can be bypassed when the JIT inlines that trivial getter at
/// <c>NCard.Reload</c>'s call site, leaving the rendered card showing its real art. This forces the swap on the
/// view itself: after a card view reloads, if its model is mushroomed, overwrite the portrait TextureRect.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
internal static class MushroomedNCardPortraitPatch
{
    private static readonly AccessTools.FieldRef<NCard, TextureRect> PortraitField =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_portrait");
    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientPortraitField =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_ancientPortrait");

    private static void Postfix(NCard __instance)
    {
        // Reload() itself early-returns when the node isn't ready (Model can be assigned before _Ready
        // runs, e.g. by CardCmd.Preview spawns) — the portrait TextureRects are still null then, so this
        // postfix must bail out too or it NREs and kills the whole draw action.
        if (!__instance.IsNodeReady()
            || __instance.Model is not { } model
            || !MushroomedCards.TryGet(model, out _)
            || MushroomedCards.MysteryPortrait is not { } mystery)
        {
            return;
        }
        PortraitField(__instance).Texture = mystery;
        AncientPortraitField(__instance).Texture = mystery;
    }
}

/// <summary>
/// Belt-and-suspenders for the text, same reasoning as <see cref="MushroomedNCardPortraitPatch" />: after the card
/// view rebuilds its labels, force the gibberish onto the title + description labels directly, so even an inlined
/// model getter (or a PGO-devirtualized <c>Title</c>) can't leak the real text.
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals), new[] { typeof(PileType), typeof(CardPreviewMode) })]
internal static class MushroomedNCardTextPatch
{
    private static readonly AccessTools.FieldRef<NCard, MegaLabel> TitleLabelField =
        AccessTools.FieldRefAccess<NCard, MegaLabel>("_titleLabel");
    private static readonly AccessTools.FieldRef<NCard, MegaRichTextLabel> DescriptionLabelField =
        AccessTools.FieldRefAccess<NCard, MegaRichTextLabel>("_descriptionLabel");

    private static void Postfix(NCard __instance)
    {
        // UpdateVisuals early-returns before touching labels when the node isn't ready; mirror that.
        if (!__instance.IsNodeReady()
            || __instance.Model is not { } model
            || __instance.Visibility != ModelVisibility.Visible
            || !MushroomedCards.TryGet(model, out MushroomedCards.Gibberish g))
        {
            return;
        }
        TitleLabelField(__instance).SetTextAutoSize(g.Title);
        DescriptionLabelField(__instance).SetTextAutoSize("[center]" + g.Description + "[/center]");
    }
}
