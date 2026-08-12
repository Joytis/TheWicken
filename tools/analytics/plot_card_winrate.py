"""Pull runs from Supabase and scatter-plot per-card win rate vs. pick rate.

Usually invoked via the dispatcher: tools/analytics/analytics.ps1 -Mode win-rate

Usage:
    py tools/analytics/plot_card_winrate.py [--key sb_secret_...] [--mod-version 1.0.0]
                                            [--game-version 1.2.3] [--days-back 30]
                                            [--min-runs 3] [--out card_winrate.png]
"""

import argparse
import sys
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd

import common


def card_stats(runs: pd.DataFrame, min_runs: int) -> pd.DataFrame:
    per_card = common.card_stats(runs)
    return per_card[per_card["runs"] >= min_runs]


def main() -> None:
    parser = argparse.ArgumentParser()
    common.add_common_args(parser)
    parser.add_argument("--min-runs", type=int, default=1,
                        help="only plot cards appearing in at least N runs (default: all)")
    parser.add_argument("--out", default=None,
                        help="output path (default: build/analytics/card_winrate_<mod-version>.png)")
    args = parser.parse_args()
    if not args.key:
        sys.exit(common.missing_key_message())
    filters = common.describe_filters(args)

    runs = common.fetch_runs(args.key, args.mod_version, args.game_version, args.days_back)
    if runs.empty:
        sys.exit(f"No runs found ({filters}).")
    overall = runs["victory"].mean()
    stats = card_stats(runs, args.min_runs)
    print(f"{len(runs)} runs, overall winrate {overall:.0%}, {len(stats)} cards with >= {args.min_runs} runs")
    print(stats.sort_values("winrate", ascending=False).head(10).to_string(index=False))

    common.apply_theme()
    fig, ax = plt.subplots(figsize=(12.5, 9.1))
    ax.grid(True, linewidth=0.5, alpha=0.6)
    ax.set_axisbelow(True)
    ax.scatter(stats["pick_rate"], stats["winrate"], color=common.SAGE, alpha=1.0,
               edgecolors=common.SAGE_RIM, linewidths=0.8)
    ax.axhline(overall, color=common.ROSE, linestyle="--", linewidth=1,
               label=f"overall winrate {overall:.0%}")
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
                    color=common.PARCHMENT if bright else common.MUTED,
                    xytext=(6, 2 + level * 9), textcoords="offset points")
    ax.set_xlabel("pick rate (runs containing card / runs observed)")
    ax.set_ylabel("win rate of those runs")
    ax.xaxis.set_major_formatter(lambda x, _: f"{x:.0%}")
    ax.yaxis.set_major_formatter(lambda y, _: f"{y:.0%}")
    ax.set_title(f"Win rate per card ({filters}, {len(runs)} runs)")
    ax.legend()
    fig.tight_layout()

    out = Path(args.out) if args.out else common.OUT_DIR / f"card_winrate_{args.mod_version or 'all'}.png"
    common.save(fig, out)


if __name__ == "__main__":
    main()
