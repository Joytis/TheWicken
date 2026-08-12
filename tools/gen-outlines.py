#!/usr/bin/env py
"""Generate the white silhouette "outline" images relics and potions need.

An outline is NOT a stroke around the art -- it is a solid, opaque WHITE
silhouette of the icon, grown a few pixels. The game draws it behind the small
icon (NRelic / NPotion), and tints it flat (SelfModulate) to render an
undiscovered relic in the relic collection, an unbrewed potion in the potion
lab, and the starting relic on the character select screen. Base-game proof:
the yummy_cookie_silent icon atlas region is 74x76 and its outline region
82x83 -- same shape, grown ~4px, filled white. Big (256x256) art never uses an
outline; NRelic hides the layer in IconSize.Large.

Outlines live in an ``outlines/`` subfolder next to the art, keeping the same
filename (mirroring WitchRelic.PackedIconOutlinePath / WitchPotion):

    TheWitch/images/relics/foo.png          ->  TheWitch/images/relics/outlines/foo.png
    TheWitch/images/potions/foo.png         ->  TheWitch/images/potions/outlines/foo.png

Method: threshold the source alpha, dilate by --radius px, soften the edge with
a --blur px gaussian that is kept as a gradient, fill white, keep the source
canvas size. Art that already bleeds to the canvas edge simply clips there,
which is what the base-game sprites do too.

Enclosed holes are deliberately NOT filled -- 47 of the 341 base-game relic
outlines keep interior holes, so a plain dilation is what the game ships.
Dilation closes small holes on its own (snecko_skull: 245 hole px in the icon,
0 in its outline) and can enclose new ones where nearby shapes merge
(blessed_antler: 1 -> 181); both behaviours match the base-game sprites.

Usage:
    py tools/gen-outlines.py                       # bulk: all stale/missing outlines
    py tools/gen-outlines.py relics/foo.png        # just these sources
    py tools/gen-outlines.py --force               # rebuild every outline
    py tools/gen-outlines.py --category potions
    py tools/gen-outlines.py --radius 4 --blur 2 --dry-run
    py tools/gen-outlines.py --sheet               # contact sheet to eyeball them all

Bulk mode regenerates an outline when it is missing or older than its source;
--force rebuilds unconditionally. Files that are themselves outlines are
skipped, so the pass is idempotent.

gen-image-sizes.py calls into this script for every small image it writes, so
outlines stay in sync with regenerated art automatically.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFilter
except ImportError:
    sys.exit("Pillow is required. Install it with:  py -m pip install Pillow")

REPO_ROOT = Path(__file__).resolve().parent.parent
IMAGES_ROOT = REPO_ROOT / "TheWitch" / "images"

# Category -> source dir + default dilation radius / edge blur, tuned by eye
# against the base-game sprites on each canvas (94x94 relic icons, 256x256
# potions). Fractional radii are fine — see dilate().
CATEGORIES = {
    "relics": {"dir": IMAGES_ROOT / "relics", "radius": 1.5, "blur": 0.5},
    "potions": {"dir": IMAGES_ROOT / "potions", "radius": 4, "blur": 1.25},
}

OUTLINE_DIR = "outlines"
ALPHA_THRESHOLD = 8  # source alpha at/below this counts as empty

# --sheet default output (gitignored scratch area next to the other art tools).
DEFAULT_SHEET = REPO_ROOT / "Docs" / "art-tracker" / "outline-sheet.png"
# How the game renders an undiscovered relic / unbrewed potion: the icon is
# hidden and the outline is SelfModulate'd to a flat colour.
SHEET_TINT = (58, 54, 66)
SHEET_BG = (176, 176, 184)
SHEET_CELL = 128


def outline_path_for(src: Path) -> Path:
    """The outline counterpart of a source image: relics/foo.png ->
    relics/outlines/foo.png (same filename, one folder down)."""
    return src.parent / OUTLINE_DIR / src.name


def is_outline(path: Path) -> bool:
    return path.parent.name == OUTLINE_DIR


def dilate(mask: Image.Image, radius: float) -> Image.Image:
    """Grow a 0/255 mask by radius px. MaxFilter only takes whole-pixel kernels,
    so the dilation runs at 2x scale and is halved back down — that is what
    makes a half-pixel radius (the relic default) possible."""
    if radius <= 0:
        return mask
    size = mask.size
    big = mask.resize((size[0] * 2, size[1] * 2), Image.NEAREST)
    kernel = max(1, round(radius * 2))
    big = big.filter(ImageFilter.MaxFilter(kernel * 2 + 1))
    return big.resize(size, Image.BILINEAR)


def make_outline(src: Path, radius: float, blur: float, dry_run: bool) -> bool:
    """Write src's white dilated silhouette. Returns True if a file was written."""
    dst = outline_path_for(src)
    rel_src, rel_dst = src.relative_to(REPO_ROOT), dst.relative_to(REPO_ROOT)
    with Image.open(src) as img:
        alpha = img.convert("RGBA").getchannel("A")
        if alpha.getextrema()[1] <= ALPHA_THRESHOLD:
            print(f"  skip      fully transparent source: {rel_src}")
            return False
        mask = alpha.point(lambda a: 255 if a > ALPHA_THRESHOLD else 0)
        mask = dilate(mask, radius)
        # Soft edge: blur the dilated mask and keep the gradient (no
        # re-threshold), so the silhouette antialiases instead of stairstepping.
        if blur > 0:
            mask = mask.filter(ImageFilter.GaussianBlur(blur))
        out = Image.new("RGBA", img.size, (255, 255, 255, 0))
        out.putalpha(mask)
        print(f"  outline   r{radius:g} b{blur:g}  {rel_src}  ->  {rel_dst}")
        if dry_run:
            return True
        dst.parent.mkdir(parents=True, exist_ok=True)
        out.save(dst)
    return True


def sources_in(cfg: dict) -> list[Path]:
    d = cfg["dir"]
    if not d.exists():
        return []
    return sorted(p for p in d.glob("*.png") if not is_outline(p))


def stale(src: Path) -> bool:
    dst = outline_path_for(src)
    return not dst.exists() or dst.stat().st_mtime < src.stat().st_mtime


def category_for(src: Path) -> dict | None:
    for cfg in CATEGORIES.values():
        if src.parent == cfg["dir"]:
            return cfg
    return None


def resolve_source(arg: str) -> Path | None:
    """Resolve a user/caller-supplied source to an actual file under a
    category dir. Accepts absolute, repo-relative, images-root-relative
    ('relics/foo.png') or a bare filename that is unique across categories."""
    for cand in (Path(arg), REPO_ROOT / arg, IMAGES_ROOT / arg):
        if cand.is_file():
            return cand.resolve()
    base = Path(arg).name
    matches = [cfg["dir"] / base for cfg in CATEGORIES.values()
               if (cfg["dir"] / base).is_file()]
    if len(matches) == 1:
        return matches[0].resolve()
    if len(matches) > 1:
        rels = ", ".join(str(m.relative_to(REPO_ROOT)) for m in matches)
        sys.exit(f"Ambiguous source '{arg}' — matches: {rels}. Pass a fuller path.")
    return None


def generate_for_paths(paths, radius: float | None = None,
                       blur: float | None = None,
                       dry_run: bool = False, quiet: bool = False) -> int:
    """Build outlines for specific source images. Paths outside a category dir
    (card portraits, powers, charui, big/ art) are ignored -- only relics and
    potions have an outline layer. Used by gen-image-sizes.py."""
    written = 0
    for p in paths:
        src = Path(p)
        cfg = category_for(src)
        if cfg is None or is_outline(src) or not src.is_file():
            continue
        if make_outline(src,
                        cfg["radius"] if radius is None else radius,
                        cfg["blur"] if blur is None else blur,
                        dry_run):
            written += 1
    if not quiet and written == 0:
        print("  nothing to do")
    return written


def fit(img: Image.Image, box: int) -> Image.Image:
    """Letterbox an image into a box x box transparent tile."""
    scaled = img.copy()
    scaled.thumbnail((box, box), Image.LANCZOS)
    tile = Image.new("RGBA", (box, box), (0, 0, 0, 0))
    tile.paste(scaled, ((box - scaled.width) // 2, (box - scaled.height) // 2))
    return tile


def build_sheet(out_path: Path, categories: dict) -> int:
    """Contact sheet: one row per asset — source art, its raw white outline,
    and the outline tinted the way the game draws it when undiscovered.
    Missing outlines show as an empty cell, which is the point of the sheet."""
    rows = [(name, src) for name, cfg in categories.items()
            for src in sources_in(cfg)]
    if not rows:
        print("No source images found.")
        return 0

    cell, pad, label_h = SHEET_CELL, 8, 16
    cols = 3
    row_h = cell + label_h + pad
    sheet = Image.new("RGBA", (cols * (cell + pad) + pad,
                               len(rows) * row_h + pad), SHEET_BG + (255,))
    draw = ImageDraw.Draw(sheet)
    checker = Image.new("RGBA", (cell, cell), (150, 150, 158, 255))
    for cy in range(0, cell, 16):
        for cx in range(0, cell, 16):
            if (cx // 16 + cy // 16) % 2:
                ImageDraw.Draw(checker).rectangle(
                    [cx, cy, cx + 15, cy + 15], fill=(166, 166, 174, 255))

    missing = 0
    for i, (cat, src) in enumerate(rows):
        y = pad + i * row_h
        outline_file = outline_path_for(src)
        with Image.open(src) as im:
            cells = [fit(im.convert("RGBA"), cell)]
        if outline_file.exists():
            with Image.open(outline_file) as om:
                om = om.convert("RGBA")
                tinted = Image.new("RGBA", om.size, SHEET_TINT + (0,))
                tinted.putalpha(om.getchannel("A"))
                cells += [fit(om, cell), fit(tinted, cell)]
        else:
            missing += 1
            cells += [None, None]

        for c, tile in enumerate(cells):
            x = pad + c * (cell + pad)
            sheet.paste(checker, (x, y))
            if tile is None:
                draw.text((x + 6, y + cell // 2), "no outline", fill=(90, 30, 30))
            else:
                sheet.alpha_composite(tile, (x, y))
        draw.text((pad, y + cell + 2), f"{cat}/{src.stem}", fill=(20, 20, 24))

    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(out_path)
    print(f"Contact sheet: {len(rows)} asset(s), {missing} without an outline")
    print(f"  wrote {out_path}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("sources", nargs="*",
                        help="Source images to build outlines for (e.g. "
                             "'relics/foo.png'). Without any, runs the bulk pass.")
    parser.add_argument("--category", choices=sorted(CATEGORIES),
                        help="Only process this category (default: all).")
    parser.add_argument("--radius", type=float,
                        help="Dilation radius in px, fractional allowed "
                             "(default: per-category).")
    parser.add_argument("--blur", type=float,
                        help="Edge softening in px (default: per-category). "
                             "0 gives a hard aliased edge.")
    parser.add_argument("--sheet", nargs="?", const=str(DEFAULT_SHEET),
                        metavar="OUT.PNG",
                        help="Skip generation; render a contact sheet showing "
                             "every source next to its outline, both plain and "
                             "tinted the way the game draws an undiscovered "
                             f"relic. Default out: {DEFAULT_SHEET.name}")
    parser.add_argument("--force", action="store_true",
                        help="Rebuild outlines even when newer than their source.")
    parser.add_argument("--dry-run", action="store_true",
                        help="Print actions without writing files.")
    args = parser.parse_args()

    if args.radius is not None and args.radius < 0:
        sys.exit("--radius must be >= 0")

    cats_selected = ({args.category: CATEGORIES[args.category]} if args.category
                     else CATEGORIES)

    if args.sheet:
        return build_sheet(Path(args.sheet), cats_selected)

    if args.sources:
        resolved = []
        for a in args.sources:
            src = resolve_source(a)
            if src is None:
                print(f"  source not found: {a} (skipped)")
                continue
            resolved.append(src)
        total = generate_for_paths(resolved, args.radius, args.blur,
                                   args.dry_run)
        print()
        print(f"{'Dry run: ' if args.dry_run else ''}{total} outline(s)"
              f"{' would be' if args.dry_run else ''} generated.")
        return 0

    total = 0
    for name, cfg in cats_selected.items():
        srcs = sources_in(cfg)
        todo = srcs if args.force else [s for s in srcs if stale(s)]
        print(f"[{name}] sources={len(srcs)} stale={len(todo)}")
        for src in todo:
            if make_outline(src,
                            cfg["radius"] if args.radius is None else args.radius,
                            cfg["blur"] if args.blur is None else args.blur,
                            args.dry_run):
                total += 1
        if not todo:
            print("  nothing to do")

    print()
    print(f"{'Dry run: ' if args.dry_run else ''}{total} outline(s)"
          f"{' would be' if args.dry_run else ''} generated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
