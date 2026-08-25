"""Shared plumbing for the analytics scripts: Supabase fetch, read-key discovery, and the
card-metadata lookups. Dispatched via tools/analytics/analytics.ps1. The interactive
charts live in pages/analytics.html, fed by export_stats.py."""

import json
import os
from datetime import datetime, timedelta, timezone
from pathlib import Path

import pandas as pd
import requests

SUPABASE_URL = "https://bjwqinohtgsvnbscnvmb.supabase.co/rest/v1/runs"
KEY_FILE = Path(__file__).with_name("supabase-service-key.local.txt")  # gitignored


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


# Supabase caps a single response at 1000 rows regardless of the requested limit, and with no
# explicit order the truncated tail was the NEWEST runs (the 2026-08-21/22 dashboard gap). Page
# through everything instead.
PAGE_SIZE = 1000


def fetch_runs(key: str, mod_version: str | None, game_version: str | None,
               days_back: int | None) -> pd.DataFrame:
    params = {"select": "victory,ascension,floor,data,mod_version,game_version,created_at",
              "order": "created_at.asc", "limit": str(PAGE_SIZE)}
    if mod_version:
        params["mod_version"] = f"eq.{mod_version}"
    if game_version:
        params["game_version"] = f"eq.{game_version}"
    if days_back:
        since = datetime.now(timezone.utc) - timedelta(days=days_back)
        params["created_at"] = f"gte.{since.isoformat()}"
    rows: list[dict] = []
    offset = 0
    while True:
        resp = requests.get(
            SUPABASE_URL,
            params=params | {"offset": str(offset)},
            headers={"apikey": key, "Authorization": f"Bearer {key}"},
            timeout=30,
        )
        resp.raise_for_status()
        page = resp.json()
        rows.extend(page)
        if len(page) < PAGE_SIZE:
            return pd.DataFrame(rows)
        offset += PAGE_SIZE


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


def card_mechanics() -> dict[str, set[str]]:
    """Bare Witch card entry -> mechanics tags (Potions/Hex/Familiars/Brambles/None)."""
    repo = Path(__file__).resolve().parents[2]
    cards = json.loads((repo / "Docs/card-data/cards.json").read_text(encoding="utf-8"))["cards"]
    return {c["entry"]: set(c.get("mechanics", [])) for c in cards}
