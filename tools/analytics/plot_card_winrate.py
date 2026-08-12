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
from adjustText import adjust_text

import common


def card_stats(runs: pd.DataFrame, min_runs: int, witch_only: bool) -> pd.DataFrame:
    per_card = common.card_stats(runs, witch_only)
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
    stats = card_stats(runs, args.min_runs, args.witch_only)
    print(f"{len(runs)} runs, overall winrate {overall:.0%}, {len(stats)} cards with >= {args.min_runs} runs")
    print(stats.sort_values("winrate", ascending=False).head(10).to_string(index=False))

    common.apply_theme()
    fig, ax = plt.subplots(figsize=(16.25, 11.8))
    ax.grid(True, linewidth=0.5, alpha=0.6)
    ax.set_axisbelow(True)
    # Tint by rarity — distinct hues (lightness steps were too subtle): amber = Rare,
    # sage = Uncommon, dusty rose = Common, grey-brown = everything else.
    rarity_tint = {"Rare": "#e6c15c", "Uncommon": common.SAGE, "Common": "#cf9a8f"}
    default_tint = "#8a7f74"
    rarities = common.card_rarities()
    colors = [rarity_tint.get(rarities.get(card, ""), default_tint) for card in stats["card"]]
    ax.scatter(stats["pick_rate"], stats["winrate"], color=colors, alpha=1.0,
               edgecolors=common.PARCHMENT, linewidths=0.6)
    for label, tint in [("Rare", rarity_tint["Rare"]), ("Uncommon", rarity_tint["Uncommon"]),
                        ("Common", rarity_tint["Common"]), ("other", default_tint)]:
        ax.scatter([], [], color=tint, edgecolors=common.PARCHMENT, linewidths=0.6, label=label)
    ax.axhline(overall, color=common.ROSE, linestyle="--", linewidth=1,
               label=f"overall winrate {overall:.0%}")
    # Label every card: the strongest outliers in bright parchment, the mid-pack muted toward
    # the background. adjustText nudges overlapping labels apart and draws leader lines back
    # to their pips.
    # Headroom above/below the data so edge-pinned labels (100%/0% cards) have room to fan out.
    ax.set_ylim(-0.08, 1.14)
    by_deviation = stats.reindex((stats["winrate"] - overall).abs().sort_values(ascending=False).index)
    outlier_cards = set(by_deviation.head(12)["card"])
    # Co-located pips get their labels seeded at spread-out starting positions (the solver
    # separates identical starts poorly); leader lines still target the pip itself.
    texts, stacked = [], {}
    for row in by_deviation.itertuples():
        coord = (row.pick_rate, row.winrate)
        level = stacked.get(coord, 0)
        stacked[coord] = level + 1
        step = (level + 1) // 2 * (0.035 if level % 2 else -0.035)
        texts.append(ax.text(row.pick_rate, row.winrate + step, row.card, fontsize=7,
                             color=common.PARCHMENT if row.card in outlier_cards else common.MUTED))
    adjust_text(texts, ax=ax, expand=(1.25, 1.6), force_text=(0.4, 0.9),
                time_lim=10,
                target_x=by_deviation["pick_rate"].to_numpy(),
                target_y=by_deviation["winrate"].to_numpy(),
                arrowprops=dict(arrowstyle="-", color=common.MUTED, lw=0.5))
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
