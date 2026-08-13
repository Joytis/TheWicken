"""Export additive aggregate stats for the web analytics dashboard (pages/analytics.html).

Publishes COUNT tables (runs, wins) keyed by every filter dimension so the page can
recompose any rate client-side by summing. No raw rows, decks, or player hashes leave
this script. Run by .github/workflows/analytics.yml (SUPABASE_READ_KEY secret) and
locally via ./tools/analytics/analytics.ps1 -Mode export.
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

import pandas as pd

import common

OUT_DIR = Path(__file__).resolve().parents[2] / "pages" / "analytics-data"


def build_tables(runs: pd.DataFrame) -> tuple[list[dict], list[dict]]:
    """runs_daily rows at grain (day, mod, game, asc) and cards_daily rows at
    grain (day, mod, game, asc, card) — card runs deduped per run via set(deck)."""
    base = pd.DataFrame({
        "day": pd.to_datetime(runs["created_at"]).dt.strftime("%Y-%m-%d"),
        "mod": runs["mod_version"],
        "game": runs["game_version"],
        "asc": runs["ascension"].astype(int),
        "wins": runs["victory"].astype(int),
    })
    keys = ["day", "mod", "game", "asc"]
    runs_daily = (base.groupby(keys).agg(runs=("wins", "size"), wins=("wins", "sum"))
                  .reset_index())

    rarities = common.card_rarities()
    card_rows = [
        row | {"card": card}
        for row, deck in zip(base.to_dict("records"),
                             (set(run.data.get("deck", [])) for run in runs.itertuples()))
        for card in deck
        if rarities.get(card) != "Starter"  # forced picks carry no signal
    ]
    cards_daily = (pd.DataFrame(card_rows)
                   .groupby(keys + ["card"]).agg(runs=("wins", "size"), wins=("wins", "sum"))
                   .reset_index())
    return runs_daily.to_dict("records"), cards_daily.to_dict("records")


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

    runs_daily, cards_daily = build_tables(runs)
    days = sorted({r["day"] for r in runs_daily})
    outputs = {
        "meta.json": {
            "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "total_runs": int(len(runs)),
            "mod_versions": sorted(runs["mod_version"].unique()),
            "game_versions": sorted(runs["game_version"].unique()),
            "first_day": days[0],
            "last_day": days[-1],
        },
        "runs_daily.json": runs_daily,
        "cards_daily.json": cards_daily,
        "cards_meta.json": build_cards_meta(),
    }
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for name, payload in outputs.items():
        path = OUT_DIR / name
        path.write_text(json.dumps(payload, separators=(",", ":"), sort_keys=True) + "\n",
                        encoding="utf-8")
        count = len(payload) if isinstance(payload, (list, dict)) else 1
        print(f"Wrote {path} ({count} rows)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
