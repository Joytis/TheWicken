"""Bar graph of every observed card on the x-axis against one metric on the y-axis:
pick rate (share of runs whose deck contains the card) or win rate (win rate of those
runs), sorted descending.

Usually invoked via the dispatcher:
    tools/analytics/analytics.ps1 -Mode card-pick-rate
    tools/analytics/analytics.ps1 -Mode card-win-rate

Usage:
    py tools/analytics/plot_card_bars.py --metric pick-rate|win-rate
        [--key sb_secret_...] [--mod-version 1.0.0] [--game-version 1.2.3] [--days-back 30]
        [--out card_bars.png]
"""

import argparse
import sys
from pathlib import Path

import matplotlib.pyplot as plt

import common

METRICS = {
    "pick-rate": ("pick_rate", "pick rate (runs containing card / runs observed)", "Card by pick rate"),
    "win-rate": ("winrate", "win rate of runs containing card", "Card by win rate"),
}


def main() -> None:
    parser = argparse.ArgumentParser()
    common.add_common_args(parser)
    parser.add_argument("--metric", required=True, choices=sorted(METRICS))
    parser.add_argument("--out", default=None,
                        help="output path (default: build/analytics/card_<metric>_<mod-version>.png)")
    args = parser.parse_args()
    if not args.key:
        sys.exit(common.missing_key_message())
    column, ylabel, title = METRICS[args.metric]
    filters = common.describe_filters(args)

    runs = common.fetch_runs(args.key, args.mod_version, args.game_version, args.days_back)
    if runs.empty:
        sys.exit(f"No runs found ({filters}).")
    stats = common.card_stats(runs).sort_values(column, ascending=False)
    print(f"{len(runs)} runs, {len(stats)} cards ({filters})")
    print(stats.head(10).to_string(index=False))

    common.apply_theme()
    fig, ax = plt.subplots(figsize=(max(12.5, 0.17 * len(stats)), 7))
    ax.grid(True, axis="y", linewidth=0.5, alpha=0.6)
    ax.set_axisbelow(True)
    ax.bar(stats["card"], stats[column], width=0.72, color=common.SAGE,
           edgecolor=common.SAGE_RIM, linewidth=0.6)
    if args.metric == "win-rate":
        overall = runs["victory"].mean()
        ax.axhline(overall, color=common.ROSE, linestyle="--", linewidth=1,
                   label=f"overall winrate {overall:.0%}")
        ax.legend()

    ax.set_xlabel("card")
    ax.set_ylabel(ylabel)
    ax.set_ylim(0, 1.05)
    ax.margins(x=0.01)
    ax.tick_params(axis="x", labelrotation=90, labelsize=7)
    ax.yaxis.set_major_formatter(lambda y, _: f"{y:.0%}")
    ax.set_title(f"{title} ({filters}, {len(runs)} runs)")
    fig.tight_layout()

    # "bars" in the name keeps the win-rate variant from clobbering the scatter's output.
    out = Path(args.out) if args.out else (
        common.OUT_DIR / f"card_bars_{args.metric}_{args.mod_version or 'all'}.png")
    common.save(fig, out)


if __name__ == "__main__":
    main()
