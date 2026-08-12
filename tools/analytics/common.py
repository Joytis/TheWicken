"""Shared plumbing for the analytics scripts: Supabase fetch, read-key discovery, and the
witch-house chart theme. Dispatched via tools/analytics/analytics.ps1."""

import json
import os
from datetime import datetime, timedelta, timezone
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd
import requests

SUPABASE_URL = "https://bjwqinohtgsvnbscnvmb.supabase.co/rest/v1/runs"
KEY_FILE = Path(__file__).with_name("supabase-service-key.local.txt")  # gitignored
OUT_DIR = Path(__file__).resolve().parents[2] / "build" / "analytics"  # gitignored

# Witch-house palette: rose-brown ground, sage-green marks, parchment text.
GROUND = "#2e2226"
PANEL = "#382a2c"
FRAME = "#8a6f66"
PARCHMENT = "#e8dcc8"
PARCHMENT_DIM = "#cbb8a4"
MUTED = "#6d5a55"
GRID = "#4d3b3a"
SAGE = "#b8d48f"
SAGE_RIM = "#e6f2c8"
ROSE = "#c99a90"


def default_key() -> str | None:
    if key := os.environ.get("SUPABASE_READ_KEY"):
        return key
    if KEY_FILE.exists():
        return KEY_FILE.read_text(encoding="utf-8").strip()
    return None


def missing_key_message() -> str:
    return ("No read key: pass --key, set SUPABASE_READ_KEY, or create "
            f"{KEY_FILE.name} next to this script (anon key is insert-only).")


def add_common_args(parser) -> None:
    parser.add_argument("--key", default=default_key())
    parser.add_argument("--mod-version", default=None, help="filter to one mod release (default: all)")
    parser.add_argument("--game-version", default=None, help="filter to one StS2 build (default: all)")
    parser.add_argument("--days-back", type=int, default=None, help="only runs from the last N days")
    parser.add_argument("--witch-only", action="store_true",
                        help="only chart THEWITCH- cards (players mix in other mods' cards)")


def describe_filters(args) -> str:
    return ", ".join(f"{name}={val}" for name, val in [
        ("mod_version", args.mod_version), ("game_version", args.game_version),
        ("last_days", args.days_back),
        ("cards", "witch-only" if getattr(args, "witch_only", False) else None)] if val) or "all runs"


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


def card_stats(runs: pd.DataFrame, witch_only: bool = False) -> pd.DataFrame:
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
    if witch_only:
        # Every remaining label carries the mod prefix — strip it for readability.
        per_card = per_card[per_card["card"].str.startswith("THEWITCH-")]
        per_card = per_card.assign(card=per_card["card"].str.removeprefix("THEWITCH-"))
    return per_card


def card_rarities() -> dict[str, str]:
    """Entry -> rarity, from the Witch card db plus the base-game class dumps. Witch entries
    are mapped both bare and with the THEWITCH- prefix (uploads carry the prefix; witch-only
    charts strip it)."""
    repo = Path(__file__).resolve().parents[2]
    rarities: dict[str, str] = {}
    for name in ("silent", "necrobinder", "ironclad", "cards"):
        data = json.loads((repo / f"Docs/card-data/{name}.json").read_text(encoding="utf-8"))
        for card in data["cards"] if isinstance(data, dict) else data:
            entry, rarity = card.get("entry"), card.get("rarity")
            if not entry or not rarity:
                continue
            if name == "cards":
                rarities[entry] = rarities[f"THEWITCH-{entry}"] = rarity
            else:
                rarities.setdefault(entry, rarity)
    return rarities


def apply_theme() -> None:
    plt.rcParams.update({
        "figure.facecolor": GROUND,
        "axes.facecolor": PANEL,
        "axes.edgecolor": FRAME,
        "axes.labelcolor": PARCHMENT,
        "text.color": PARCHMENT,
        "xtick.color": PARCHMENT_DIM,
        "ytick.color": PARCHMENT_DIM,
        "grid.color": GRID,
        "legend.facecolor": PANEL,
        "legend.edgecolor": FRAME,
    })


def save(fig, out_path: Path) -> None:
    out_path.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(out_path, dpi=150)
    print(f"Wrote {out_path}")
