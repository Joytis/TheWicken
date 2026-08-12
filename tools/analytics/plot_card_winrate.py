"""Pull runs from Supabase and scatter-plot per-card win rate vs. how often the card shows up.

Reading requires a key with SELECT rights (the shipped anon key is insert-only):
pass the service_role/secret key via --key or the SUPABASE_READ_KEY env var. Keep it
out of the repo.

Usually invoked via the dispatcher: tools/analytics/analytics.ps1 -Mode win-rate

Usage:
    py tools/analytics/plot_card_winrate.py [--key sb_secret_...] [--mod-version 1.0.0]
                                            [--game-version 1.2.3] [--days-back 30]
                                            [--min-runs 3] [--out card_winrate.png]
"""

import argparse
import os
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd
import requests

SUPABASE_URL = "https://bjwqinohtgsvnbscnvmb.supabase.co/rest/v1/runs"
KEY_FILE = Path(__file__).with_name("supabase-service-key.local.txt")  # gitignored
OUT_DIR = Path(__file__).resolve().parents[2] / "build" / "analytics"  # gitignored


def default_key() -> str | None:
    if key := os.environ.get("SUPABASE_READ_KEY"):
        return key
    if KEY_FILE.exists():
        return KEY_FILE.read_text(encoding="utf-8").strip()
    return None


def fetch_runs(key: str, mod_version: str | None, game_version: str | None,
               days_back: int | None) -> pd.DataFrame:
    params = {"select": "victory,ascension,floor,data", "limit": "10000"}
    if mod_version:
        params["mod_version"] = f"eq.{mod_version}"
    if game_version:
        params["game_version"] = f"eq.{game_version}"
    if days_back:
        since = datetime.now(timezone.utc) - timedelta(days=days_back)
        params["created_at"] = f"gte.{since.isoformat()}"
    resp = requests.get(
        SUPABASE_URL,
        params=params,
        headers={"apikey": key, "Authorization": f"Bearer {key}"},
        timeout=30,
    )
    resp.raise_for_status()
    return pd.DataFrame(resp.json())


def card_stats(runs: pd.DataFrame, min_runs: int) -> pd.DataFrame:
    # One row per (run, unique card in deck): a card's win rate = win rate of runs playing it,
    # its pick rate = share of observed runs whose deck contains it.
    rows = [
        {"card": card, "victory": run.victory}
        for run in runs.itertuples()
        for card in set(run.data.get("deck", []))
    ]
    per_card = (
        pd.DataFrame(rows)
        .groupby("card")
        .agg(runs=("victory", "size"), winrate=("victory", "mean"))
        .reset_index()
    )
    per_card["pick_rate"] = per_card["runs"] / len(runs)
    return per_card[per_card["runs"] >= min_runs]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--key", default=default_key())
    parser.add_argument("--mod-version", default=None, help="filter to one mod release (default: all)")
    parser.add_argument("--game-version", default=None, help="filter to one StS2 build (default: all)")
    parser.add_argument("--days-back", type=int, default=None, help="only runs from the last N days")
    parser.add_argument("--min-runs", type=int, default=3)
    parser.add_argument("--out", default=None,
                        help="output path (default: build/analytics/card_winrate_<mod-version>.png)")
    args = parser.parse_args()
    if not args.key:
        sys.exit("No read key: pass --key, set SUPABASE_READ_KEY, or create "
                 f"{KEY_FILE.name} next to this script (anon key is insert-only).")

    filters = ", ".join(f"{name}={val}" for name, val in [
        ("mod_version", args.mod_version), ("game_version", args.game_version),
        ("last_days", args.days_back)] if val) or "all runs"

    runs = fetch_runs(args.key, args.mod_version, args.game_version, args.days_back)
    if runs.empty:
        sys.exit(f"No runs found ({filters}).")
    overall = runs["victory"].mean()
    stats = card_stats(runs, args.min_runs)
    print(f"{len(runs)} runs, overall winrate {overall:.0%}, {len(stats)} cards with >= {args.min_runs} runs")
    print(stats.sort_values("winrate", ascending=False).head(10).to_string(index=False))

    # Witch-house palette: rose-brown ground, sage-green marks, parchment text.
    plt.rcParams.update({
        "figure.facecolor": "#2e2226",
        "axes.facecolor": "#382a2c",
        "axes.edgecolor": "#8a6f66",
        "axes.labelcolor": "#e8dcc8",
        "text.color": "#e8dcc8",
        "xtick.color": "#cbb8a4",
        "ytick.color": "#cbb8a4",
        "grid.color": "#4d3b3a",
        "legend.facecolor": "#382a2c",
        "legend.edgecolor": "#8a6f66",
    })
    fig, ax = plt.subplots(figsize=(12.5, 9.1))
    ax.grid(True, linewidth=0.5, alpha=0.6)
    ax.set_axisbelow(True)
    ax.scatter(stats["pick_rate"], stats["winrate"], color="#b8d48f", alpha=1.0,
               edgecolors="#e6f2c8", linewidths=0.8)
    ax.axhline(overall, color="#c99a90", linestyle="--", linewidth=1, label=f"overall winrate {overall:.0%}")
    # Label every card: the strongest outliers in bright parchment, the mid-pack muted toward
    # the background so it reads as texture. Cards with identical (pick_rate, winrate) share
    # one pip — stack their labels instead of overtyping each other (outliers stack lowest).
    by_deviation = stats.reindex((stats["winrate"] - overall).abs().sort_values(ascending=False).index)
    outlier_cards = set(by_deviation.head(12)["card"])
    stacked: dict[tuple[float, float], int] = {}
    for row in by_deviation.itertuples():
        coord = (row.pick_rate, row.winrate)
        level = stacked.get(coord, 0)
        stacked[coord] = level + 1
        bright = row.card in outlier_cards
        ax.annotate(row.card, coord, fontsize=7,
                    color="#e8dcc8" if bright else "#6d5a55",
                    xytext=(6, 2 + level * 9), textcoords="offset points")
    ax.set_xlabel("pick rate (runs containing card / runs observed)")
    ax.set_ylabel("win rate of those runs")
    ax.xaxis.set_major_formatter(lambda x, _: f"{x:.0%}")
    ax.yaxis.set_major_formatter(lambda y, _: f"{y:.0%}")
    ax.set_title(f"Win rate per card ({filters}, {len(runs)} runs)")
    ax.legend()
    fig.tight_layout()

    out = Path(args.out) if args.out else OUT_DIR / f"card_winrate_{args.mod_version or 'all'}.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(out, dpi=150)
    print(f"Wrote {out}")


if __name__ == "__main__":
    main()
