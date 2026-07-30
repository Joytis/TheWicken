#!/usr/bin/env py
"""Downsize an image to the big card-art size (1000x760).

Overwrites the image in place (or writes to --out). Errors out if the source
is smaller than 1000x760 on either side — upscaling is never allowed.

A bare filename is searched for under any ``big/`` folder in the images tree,
so ``defend_witch.png`` resolves to
``TheWitch/images/card_portraits/big/defend_witch.png``.

Usage:
    py tools/downsize-to-big.py defend_witch.png
    py tools/downsize-to-big.py path/to/image.png --out TheWitch/images/card_portraits/big/foo.png
    py tools/downsize-to-big.py path/to/image.png --nearest
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required. Install it with:  py -m pip install Pillow")

TARGET = (1000, 760)

REPO_ROOT = Path(__file__).resolve().parent.parent
IMAGES_ROOT = REPO_ROOT / "TheWitch" / "images"


def resolve_image(arg: str) -> Path:
    """Resolve the image argument: an existing path wins; otherwise glob for a
    unique match under any 'big/' folder in the images tree (same behavior as
    gen-image-sizes.py's resolve_big)."""
    for cand in (Path(arg), REPO_ROOT / arg, IMAGES_ROOT / arg):
        if cand.is_file():
            return cand
    base = Path(arg).name
    matches = sorted({p.resolve() for p in IMAGES_ROOT.glob(f"**/big/**/{base}")})
    if len(matches) == 1:
        return matches[0]
    if len(matches) > 1:
        rels = ", ".join(str(m.relative_to(REPO_ROOT)) for m in matches)
        sys.exit(f"Ambiguous image '{arg}' — matches: {rels}. Pass a fuller path.")
    sys.exit(f"Image not found: {arg}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("image",
                        help="Image to downsize to 1000x760. A bare filename is "
                             "searched for under any big/ folder in the images tree.")
    parser.add_argument("--out", help="Output path (default: overwrite in place).")
    parser.add_argument("--nearest", action="store_true",
                        help="Nearest-neighbour resampling (crisp pixel art) "
                             "instead of Lanczos.")
    args = parser.parse_args()

    src = resolve_image(args.image)
    dst = Path(args.out) if args.out else src

    resample = Image.NEAREST if args.nearest else Image.LANCZOS

    with Image.open(src) as img:
        w, h = img.size
        if w < TARGET[0] or h < TARGET[1]:
            sys.exit(f"Image too small: {w}x{h} is under {TARGET[0]}x{TARGET[1]} "
                     f"— refusing to upscale: {src}")
        if (w, h) == TARGET:
            print(f"Already {TARGET[0]}x{TARGET[1]}, nothing to do: {src}")
            return 0
        out = img.resize(TARGET, resample)
        dst.parent.mkdir(parents=True, exist_ok=True)
        out.save(dst)
        print(f"downscale {w}x{h} -> {TARGET[0]}x{TARGET[1]}  {src}"
              + (f"  ->  {dst}" if dst != src else "  (in place)"))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
