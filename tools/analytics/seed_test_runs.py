"""Seed the Supabase runs table with fabricated Witch runs for testing analytics scripts.

All seeded rows use mod_version = "seed-test" so they can be excluded/purged:
    delete from runs where mod_version = 'seed-test';

Usage: py tools/analytics/seed_test_runs.py [--runs 20]
"""

import argparse
import hashlib
import json
import random
from pathlib import Path

import requests

SUPABASE_URL = "https://bjwqinohtgsvnbscnvmb.supabase.co/rest/v1/runs"
ANON_KEY = "sb_publishable_W9jQ_6zyHkSvUMofbfk6RQ_Lj_NrScl"
REPO = Path(__file__).resolve().parents[2]

STARTERS = ["STRIKE_WITCH", "DEFEND_WITCH"]
DECK_SIZE = 40


def load_card_pool() -> list[str]:
    cards = json.loads((REPO / "Docs/card-data/cards.json").read_text(encoding="utf-8"))["cards"]
    return [c["entry"] for c in cards if c.get("rarity") not in ("Starter", "Token")]


def fabricate_run(rng: random.Random, pool: list[str], card_power: dict[str, float]) -> dict:
    # Deck: 4x Strike, 4x Defend, rest random picks (duplicates allowed, like a real run).
    deck = [STARTERS[0]] * 4 + [STARTERS[1]] * 4
    while len(deck) < DECK_SIZE:
        deck.append(rng.choice(pool))

    # Hidden per-card "power" drives the win roll so the scatter plot shows real signal.
    strength = sum(card_power[c] for c in deck if c in card_power) / DECK_SIZE
    victory = rng.random() < min(0.9, max(0.1, 0.45 + strength))

    ascension = rng.randint(0, 5)
    floor = rng.randint(45, 57) if victory else rng.randint(6, 44)
    return {
        "mod_version": "seed-test",
        "game_version": "1.0.0-seed",
        "victory": victory,
        "ascension": ascension,
        "floor": floor,
        "playtime": rng.randint(1200, 7200),
        "player_hash": hashlib.sha256(f"seed-player-{rng.randint(0, 5)}".encode()).hexdigest()[:16],
        "data": {
            "character": "WITCH",
            "win": victory,
            "ascension": ascension,
            "floorReached": floor,
            "deck": deck,
            "relics": ["BROOM"],
        },
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--runs", type=int, default=20)
    parser.add_argument("--seed", type=int, default=1234)
    args = parser.parse_args()

    rng = random.Random(args.seed)
    pool = load_card_pool()
    # Fixed hidden power per card, in [-0.35, 0.35].
    card_power = {c: rng.uniform(-0.35, 0.35) for c in pool}

    rows = [fabricate_run(rng, pool, card_power) for _ in range(args.runs)]
    resp = requests.post(
        SUPABASE_URL,
        json=rows,
        headers={
            "apikey": ANON_KEY,
            "Authorization": f"Bearer {ANON_KEY}",
            "Content-Type": "application/json",
            "Prefer": "return=minimal",
        },
        timeout=30,
    )
    resp.raise_for_status()
    wins = sum(r["victory"] for r in rows)
    print(f"Inserted {len(rows)} seed runs ({wins} wins, {len(rows) - wins} losses).")


if __name__ == "__main__":
    main()
