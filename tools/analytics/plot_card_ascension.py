"""Bar graph: win rate per ascension for runs whose deck contains a given card, so we can
see how a card's quality trends as ascension rises. Bars show the card's runs; a dashed
marker per ascension shows the overall win rate at that ascension for comparison.

Usually invoked via the dispatcher: tools/analytics/analytics.ps1 -Mode card-ascension

Usage:
    py tools/analytics/plot_card_ascension.py --card RITUAL_SACRIFICE
        [--key sb_secret_...] [--mod-version 1.0.0] [--game-version 1.2.3] [--days-back 30]
        [--out card_ascension.png]
"""

import argparse
import sys
from pathlib import Path

import matplotlib.pyplot as plt

import common


def main() -> None:
    parser = argparse.ArgumentParser()
    common.add_common_args(parser)
    parser.add_argument("--card", required=True,
                        help="card entry id as uploaded, e.g. RITUAL_SACRIFICE")
    parser.add_argument("--out", default=None,
                        help="output path (default: build/analytics/card_ascension_<card>_<mod-version>.png)")
    args = parser.parse_args()
    if not args.key:
        sys.exit(common.missing_key_message())
    card = args.card.strip().upper()
    filters = common.describe_filters(args)

    runs = common.fetch_runs(args.key, args.mod_version, args.game_version, args.days_back)
    if runs.empty:
        sys.exit(f"No runs found ({filters}).")
    runs["has_card"] = runs["data"].map(lambda d: card in d.get("deck", []))
    card_runs = runs[runs["has_card"]]
    if card_runs.empty:
        sys.exit(f"No runs containing {card} ({filters}). Check the card entry id.")

    per_asc = (card_runs.groupby("ascension")
               .agg(runs=("victory", "size"), winrate=("victory", "mean")))
    overall_per_asc = (runs.groupby("ascension")
                       .agg(all_runs=("victory", "size"), all_winrate=("victory", "mean")))
    table = per_asc.join(overall_per_asc)
    print(f"{len(card_runs)}/{len(runs)} runs contain {card} ({filters})")
    print(table.to_string())

    common.apply_theme()
    fig, ax = plt.subplots(figsize=(12.5, 7))
    ax.grid(True, axis="y", linewidth=0.5, alpha=0.6)
    ax.set_axisbelow(True)
    ax.bar(per_asc.index, per_asc["winrate"], width=0.62, color=common.SAGE,
           edgecolor=common.SAGE_RIM, linewidth=0.8, label=f"runs with {card}")
    # Baseline: overall winrate at each ascension, drawn as short dashes over each bar slot.
    ax.hlines(overall_per_asc["all_winrate"], overall_per_asc.index - 0.31,
              overall_per_asc.index + 0.31, color=common.ROSE, linestyle="--", linewidth=1.2,
              label="all runs at that ascension")
    # Run counts on top of the bars, so thin data is visibly thin.
    for asc, row in per_asc.iterrows():
        ax.annotate(f"{int(row['runs'])} {'run' if row['runs'] == 1 else 'runs'}",
                    (asc, row["winrate"]), ha="center", va="bottom", fontsize=7,
                    color=common.PARCHMENT_DIM, xytext=(0, 3), textcoords="offset points")

    ax.set_xlabel("ascension")
    ax.set_ylabel(f"win rate of runs containing {card}")
    ax.set_ylim(0, 1.05)
    ax.set_xticks(sorted(runs["ascension"].unique()))
    ax.yaxis.set_major_formatter(lambda y, _: f"{y:.0%}")
    ax.set_title(f"{card} win rate by ascension ({filters}, {len(card_runs)} runs)")
    ax.legend()
    fig.tight_layout()

    out = Path(args.out) if args.out else (
        common.OUT_DIR / f"card_ascension_{card}_{args.mod_version or 'all'}.png")
    common.save(fig, out)


if __name__ == "__main__":
    main()
