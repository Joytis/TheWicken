#!/usr/bin/env python3
"""Poll Steam Workshop stats for the mod's Main + Beta items into Docs/workshop-stats.json.

Steam exposes only a CURRENT snapshot per Workshop item (there is no historical API),
so history is built by running this repeatedly and accumulating samples.

Endpoint: POST https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/
No API key required -- which is why this works from a public CI runner.

Output shape:
    {
      "items":   {"main": "<id>", "beta": "<id>"},
      "updated": "<iso8601 utc>",
      "samples": [{"time": ..., "branch": "main", "subscriptions": N, ...}, ...]
    }
"""

import argparse
import json
import sys
import urllib.parse
import urllib.request
from datetime import datetime, timedelta, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
API = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/"

# Beta is a separate published item with no id file in the repo; main's id is the
# versioned workshop/mod_id.txt written by bundle-workshop.ps1 on first upload.
BETA_ID = "3768831080"


def load_items():
    main_id_file = REPO / "workshop" / "mod_id.txt"
    if not main_id_file.exists():
        sys.exit(f"Missing {main_id_file} -- main Workshop item not published yet.")
    return {"main": main_id_file.read_text(encoding="utf-8").strip(), "beta": BETA_ID}


def fetch(ids):
    form = {"itemcount": str(len(ids))}
    for i, item_id in enumerate(ids):
        form[f"publishedfileids[{i}]"] = item_id
    req = urllib.request.Request(
        API,
        data=urllib.parse.urlencode(form).encode(),
        headers={"User-Agent": "TheWicken-workshop-stats"},
    )
    with urllib.request.urlopen(req, timeout=30) as resp:
        payload = json.load(resp)
    return payload.get("response", {}).get("publishedfiledetails", [])


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out", type=Path, default=REPO / "Docs" / "workshop-stats.json",
                    help="accumulated stats file (default Docs/workshop-stats.json)")
    ap.add_argument("--min-interval-hours", type=float, default=0.0,
                    help="skip a branch sampled within this many hours (0 = always sample)")
    args = ap.parse_args()

    items = load_items()

    data = {}
    if args.out.exists():
        data = json.loads(args.out.read_text(encoding="utf-8"))
    samples = data.get("samples", [])

    now = datetime.now(timezone.utc)
    stamp = now.strftime("%Y-%m-%dT%H:%M:%SZ")

    details = {d.get("publishedfileid"): d for d in fetch(list(items.values()))}

    added = 0
    for branch, item_id in items.items():
        d = details.get(item_id)
        if d is None:
            print(f"WARN [{branch}] {item_id} -- no details returned", file=sys.stderr)
            continue
        if d.get("result") != 1:
            print(f"WARN [{branch}] {item_id} -- Steam result={d.get('result')} "
                  "(item missing/private?)", file=sys.stderr)
            continue

        if args.min_interval_hours > 0:
            prior = [s for s in samples if s.get("branch") == branch]
            if prior:
                last = datetime.strptime(prior[-1]["time"], "%Y-%m-%dT%H:%M:%SZ") \
                    .replace(tzinfo=timezone.utc)
                age = (now - last) / timedelta(hours=1)
                if age < args.min_interval_hours:
                    print(f"[{branch}] skipped -- last sample {age:.1f}h ago "
                          f"(< {args.min_interval_hours}h)")
                    continue

        samples.append({
            "time": stamp,
            "branch": branch,
            "publishedfileid": item_id,
            "subscriptions": int(d.get("subscriptions", 0)),
            "lifetimeSubscriptions": int(d.get("lifetime_subscriptions", 0)),
            "favorited": int(d.get("favorited", 0)),
            "lifetimeFavorited": int(d.get("lifetime_favorited", 0)),
            "followers": int(d.get("followers", 0)),
            "views": int(d.get("views", 0)),
            "timeUpdated": int(d.get("time_updated", 0)),
        })
        added += 1
        print(f"[{branch:<4}] subs {d.get('subscriptions'):>6}  "
              f"lifetime {d.get('lifetime_subscriptions'):>6}  "
              f"favs {d.get('favorited'):>5}  views {d.get('views'):>7}")

    if not added:
        print("No new samples written.")
        return

    out = {"items": items, "updated": stamp, "samples": samples}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {added} sample(s) -> {args.out} ({len(samples)} total)")


if __name__ == "__main__":
    main()
