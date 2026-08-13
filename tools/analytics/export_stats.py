"""Export additive aggregate stats for the web analytics dashboard (pages/analytics.html).

Publishes COUNT tables (runs, wins, offered, picked, ...) keyed by every filter dimension
so the page can recompose any rate client-side by summing. No raw rows, decks, or player
hashes leave this script. Run by .github/workflows/analytics.yml (SUPABASE_READ_KEY secret)
and locally via ./tools/analytics/analytics.ps1 -Mode export.
"""

import argparse
import json
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

import pandas as pd

import common

OUT_DIR = Path(__file__).resolve().parents[2] / "pages" / "analytics-data"

# Dominant-archetype rules: a run commits to the mechanic with the most witch cards in
# deck (duplicates count); fewer than COMMIT_THRESHOLD such cards = "Unfocused".
# Ties break in this fixed order.
MECHANIC_ORDER = ["Hex", "Potions", "Familiars", "Brambles"]
COMMIT_THRESHOLD = 3


def base_keys(run, day: str, data: dict) -> dict:
    return {"day": day, "mod": run.mod_version, "game": run.game_version,
            "asc": int(run.ascension),
            "mp": 1 if data.get("numPlayers", 1) > 1 else 0}


def aggregate(rows: list[dict], keys: list[str], sums: list[str]) -> list[dict]:
    if not rows:
        return []
    return (pd.DataFrame(rows).groupby(keys)[sums].sum().reset_index()
            .to_dict("records"))


def copies_bucket(n: int) -> int:
    return min(n, 3)  # 1, 2, 3+ — enough signal, keeps cardinality flat


def dominant_archetype(deck: list[str], mechanics: dict[str, set[str]]) -> str:
    counts = Counter()
    for card in deck:
        for mech in mechanics.get(card.removeprefix("THEWITCH-"), []):
            if card.startswith("THEWITCH-"):
                counts[mech] += 1
    best = max(MECHANIC_ORDER, key=lambda m: (counts.get(m, 0), -MECHANIC_ORDER.index(m)),
               default="Unfocused")
    return best if counts.get(best, 0) >= COMMIT_THRESHOLD else "Unfocused"


def build_tables(runs: pd.DataFrame) -> dict[str, list[dict]]:
    rarities = common.card_rarities()
    mechanics = common.card_mechanics()
    run_rows, card_rows, choice_rows = [], [], []
    death_rows, floor_rows, enc_rows, arch_rows = [], [], [], []

    for run in runs.itertuples():
        day = pd.to_datetime(run.created_at).strftime("%Y-%m-%d")
        win = int(run.victory)
        data = run.data or {}
        keys = base_keys(run, day, data)
        deck = data.get("deck", [])

        run_rows.append(keys | {"runs": 1, "wins": win})
        arch_rows.append(keys | {"arch": dominant_archetype(deck, mechanics),
                                 "runs": 1, "wins": win})

        for card, copies in Counter(deck).items():
            if rarities.get(card) != "Starter":  # forced picks carry no signal
                card_rows.append(keys | {"card": card, "copies": copies_bucket(copies),
                                         "runs": 1, "wins": win})

        for screen in data.get("cardChoices", []):
            for card in screen.get("picked", []):
                choice_rows.append(keys | {"card": card, "offered": 1, "picked": 1})
            for card in screen.get("skipped", []):
                choice_rows.append(keys | {"card": card, "offered": 1, "picked": 0})

        killed_by = data.get("killedByEncounter")
        if not win and killed_by and killed_by != "NONE":
            death_rows.append(keys | {"enc": killed_by, "deaths": 1})
            floor_rows.append(keys | {"floor": int(run.floor), "deaths": 1})

        for enc in data.get("encounters", []):
            enc_rows.append(keys | {"enc": enc["id"], "fights": 1,
                                    "dmg": int(enc.get("damage", 0)),
                                    "turns": int(enc.get("turns", 0))})

    keys = ["day", "mod", "game", "asc", "mp"]
    return {
        "runs_daily": aggregate(run_rows, keys, ["runs", "wins"]),
        "cards_daily": aggregate(card_rows, keys + ["card", "copies"], ["runs", "wins"]),
        "choices_daily": aggregate(choice_rows, keys + ["card"], ["offered", "picked"]),
        "deaths_daily": aggregate(death_rows, keys + ["enc"], ["deaths"]),
        "death_floors_daily": aggregate(floor_rows, keys + ["floor"], ["deaths"]),
        "encounters_daily": aggregate(enc_rows, keys + ["enc"], ["fights", "dmg", "turns"]),
        "archetypes_daily": aggregate(arch_rows, keys + ["arch"], ["runs", "wins"]),
    }


def build_cards_meta() -> dict[str, dict]:
    """Card entry -> {rarity, mechanics[], witch} for every entry the dashboard may see.
    Witch entries appear under their uploaded THEWITCH- prefixed id."""
    mechanics = common.card_mechanics()
    meta: dict[str, dict] = {}
    for entry, rarity in common.card_rarities().items():
        if entry.startswith("THEWITCH-"):
            bare = entry.removeprefix("THEWITCH-")
            meta[entry] = {"rarity": rarity, "witch": True,
                           "mechanics": sorted(mechanics.get(bare, []))}
        elif entry not in mechanics:  # bare duplicates of witch entries stay out
            meta[entry] = {"rarity": rarity, "witch": False}
    return meta


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    common.add_common_args(parser)
    parser.add_argument("--include-seed", action="store_true",
                        help="keep fabricated mod_version='seed-test' rows (local testing)")
    args = parser.parse_args()
    if not args.key:
        print(common.missing_key_message(), file=sys.stderr)
        return 1

    runs = common.fetch_runs(args.key, args.mod_version, args.game_version, args.days_back)
    if not args.include_seed and not runs.empty:
        runs = runs[runs["mod_version"] != "seed-test"]
    if runs.empty:
        print("No runs to export (after seed filter) — leaving existing data untouched.",
              file=sys.stderr)
        return 1

    tables = build_tables(runs)
    days = sorted({r["day"] for r in tables["runs_daily"]})
    outputs = {f"{name}.json": rows for name, rows in tables.items()}
    outputs["meta.json"] = {
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "total_runs": int(len(runs)),
        "mod_versions": sorted(runs["mod_version"].unique()),
        "game_versions": sorted(runs["game_version"].unique()),
        "first_day": days[0],
        "last_day": days[-1],
    }
    outputs["cards_meta.json"] = build_cards_meta()
    # Base-game encounter whitelist (generated from gamedata/ — see base_encounters.json);
    # the page hides other mods' encounters unless "include other mods" is on.
    outputs["encounters_meta.json"] = json.loads(
        (Path(__file__).with_name("base_encounters.json")).read_text(encoding="utf-8"))

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for name, payload in outputs.items():
        path = OUT_DIR / name
        path.write_text(json.dumps(payload, separators=(",", ":"), sort_keys=True) + "\n",
                        encoding="utf-8")
        print(f"Wrote {path} ({len(payload)} rows)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
