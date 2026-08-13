#!/usr/bin/env python3
"""Pack ALL mod art into atlas pages + .tres AtlasTexture slices (cards, relics, powers, potions, pets).

Mirrors the base game's atlas structure (card_atlas / relic_atlas / power_atlas / potion_atlas
+ the relic/potion outline atlases) and Downfall's ImageGen approach: the shipped pck contains
only the packed pages + .tres slices; source art stays in the repo and never ships.

Category         source (repo)                    slice        atlas base           notes
--------         -------------                    -----        ----------           -----
cards            card_portraits/**/*.png          250x190      card_atlas           RGB on black; tall art
                 (.gdignore'd; familiar/, beta/)  250x351      card_ancient_atlas   (h > w) goes to ancient
relics           relics/*.png (256, ships)        94x94        relic_atlas          + white outline slices ->
                                                                                    relic_outline_atlas
powers           powers/*.png (256, ships)        64x64        power_atlas          base-game power slice size
potions          potions/*.png (256, .gdignore'd) 80x80        potion_atlas         + outline slices ->
                                                                                    potion_outline_atlas
pets             pets/*.png (512, .gdignore'd)    512 native   pets_atlas

The relic/power source pngs (256) also ship loose — the game's BigIcon views load them directly.
Outline slices are generated here (dilate + soft blur, ported from the retired gen-outlines.py,
radii tuned per category at slice scale) — outlines can never go stale.

Outputs land in TheWitch/images/atlases/ (generated, gitignored):
  <base>_<i>.png                       atlas pages (page 4096 max, cropped to used extent)
  <base>.sprites/<key>.tres            AtlasTexture slice per source image
  relic_atlas.sprites/<key>_outline.tres, potion_atlas.sprites/<key>_outline.tres

Runtime resolution: the model bases (WitchCard/WitchRelic/WitchPower/WitchPotion, WitchPet)
point their path overrides at the .tres via the *AtlasPath helpers in StringExtensions.cs.
Missing art falls back to the packed placeholder slice (card/relic/power/potion.tres).

Run with no args; invoked automatically by `dotnet publish` (PackAtlases target).
"""

import hashlib
import io
import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
IMAGES = ROOT / "TheWitch" / "images"
OUT_DIR = IMAGES / "atlases"
RES_BASE = "res://TheWitch/images/atlases"

MAX_ATLAS = 4096
PAD = 1

UID_CHARS = "abcdefghijklmnopqrstuvwxyz0123456789"


def deterministic_uid(name: str, length: int = 13) -> str:
    h = int.from_bytes(hashlib.md5(name.encode("utf-8")).digest()[:8], "big")
    out = []
    for _ in range(length):
        out.append(UID_CHARS[h % len(UID_CHARS)])
        h //= len(UID_CHARS)
    return "".join(out)


def write_if_changed(path: Path, data: bytes) -> bool:
    if path.exists() and path.read_bytes() == data:
        return False
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    return True


def save_image_if_changed(img: Image.Image, path: Path) -> bool:
    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=True)
    return write_if_changed(path, buf.getvalue())


def dilate(mask: Image.Image, radius: float) -> Image.Image:
    """Grow a 0/255 mask by radius px. MaxFilter only takes whole-pixel kernels, so the
    dilation runs at 2x scale and is halved back down — that allows fractional radii."""
    if radius <= 0:
        return mask
    size = mask.size
    big = mask.resize((size[0] * 2, size[1] * 2), Image.NEAREST)
    kernel = max(1, round(radius * 2)) * 2 + 1
    big = big.filter(ImageFilter.MaxFilter(kernel))
    return big.resize(size, Image.BILINEAR)


def white_outline(icon: Image.Image, radius: float, blur: float) -> Image.Image:
    """Solid-white silhouette of icon's alpha, dilated + soft-edged — what the game tints
    to render undiscovered relics / unbrewed potions behind the small icon."""
    mask = icon.getchannel("A").point(lambda a: 255 if a >= 8 else 0)
    mask = dilate(mask, radius)
    if blur > 0:
        mask = mask.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", icon.size, (255, 255, 255, 0))
    out.putalpha(mask)
    return out


def slot_to_pos(slot: int, w: int, h: int) -> tuple[int, int, int]:
    cols = MAX_ATLAS // (w + PAD)
    rows = MAX_ATLAS // (h + PAD)
    per_page = cols * rows
    page, loc = divmod(slot, per_page)
    return page, (loc % cols) * (w + PAD), (loc // cols) * (h + PAD)


def write_tres(rel: str, page_res_path: str, x: int, y: int, w: int, h: int) -> bool:
    uid = deterministic_uid(f"thewitch_{rel}")
    content = (
        f'[gd_resource type="AtlasTexture" load_steps=2 format=3 uid="uid://{uid}"]\n'
        f'[ext_resource type="Texture2D" path="{page_res_path}" id="1"]\n'
        f'[resource]\natlas = ExtResource("1")\nregion = Rect2({x}, {y}, {w}, {h})\n'
    )
    return write_if_changed(OUT_DIR / rel, content.encode("utf-8"))


def pack_group(atlas_base: str, sprites_dir: str, size: tuple[int, int],
               entries: list[tuple[str, Image.Image]], generated: set[str],
               rgb: bool = False) -> int:
    """Grid-pack uniform-size images; writes pages + slices, records outputs in `generated`.
    Returns the number of .tres files (re)written. `entries` = (key, slice-sized image).
    rgb=True saves pages without an alpha channel (opaque card portraits — smaller pages)."""
    if not entries:
        return 0
    w, h = size
    placements = {key: slot_to_pos(i, w, h) for i, (key, _) in enumerate(entries)}
    page_count = max(p for p, _, _ in placements.values()) + 1
    pages = [Image.new("RGBA", (MAX_ATLAS, MAX_ATLAS), (0, 0, 0, 0)) for _ in range(page_count)]
    for key, img in entries:
        page, x, y = placements[key]
        pages[page].paste(img, (x, y))

    page_res_paths = []
    for i, page_img in enumerate(pages):
        used = [(x, y) for p, x, y in placements.values() if p == i]
        used_w = max(x for x, _ in used) + w
        used_h = max(y for _, y in used) + h
        fname = f"{atlas_base}_{i}.png"
        page_res_paths.append(f"{RES_BASE}/{fname}")
        cropped = page_img.crop((0, 0, used_w, used_h))
        if rgb:
            cropped = cropped.convert("RGB")
        if save_image_if_changed(cropped, OUT_DIR / fname):
            print(f"  wrote: {fname}")
        generated.add(fname)

    tres_written = 0
    for key, _ in entries:
        page, x, y = placements[key]
        rel = f"{sprites_dir}/{key}.tres"
        if write_tres(rel, page_res_paths[page], x, y, w, h):
            tres_written += 1
        generated.add(rel)
    return tres_written


def flatten_resize(src: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Card portraits: resize onto an opaque black RGB canvas (frames are opaque)."""
    resized = src.convert("RGBA").resize(size, Image.LANCZOS)
    dst = Image.new("RGBA", size, (0, 0, 0, 255))
    dst.paste(resized, (0, 0), resized)
    return dst


CARD_NORMAL = (250, 190)
CARD_ANCIENT = (250, 351)


def pack_cards(generated: set[str]) -> None:
    src_dir = IMAGES / "card_portraits"
    normal: list[tuple[str, Image.Image]] = []
    ancient: list[tuple[str, Image.Image]] = []
    for png in sorted(src_dir.rglob("*.png")):
        key = png.relative_to(src_dir).with_suffix("").as_posix()
        with Image.open(png) as raw:
            tall = raw.height > raw.width
            (ancient if tall else normal).append(
                (key, flatten_resize(raw, CARD_ANCIENT if tall else CARD_NORMAL)))
    n = pack_group("card_atlas", "card_atlas.sprites", CARD_NORMAL, normal, generated, rgb=True)
    n += pack_group("card_ancient_atlas", "card_atlas.sprites", CARD_ANCIENT, ancient, generated, rgb=True)
    print(f"  cards: {len(normal) + len(ancient)} packed, {n} .tres updated")


def resize_icon(png: Path, size: int) -> Image.Image:
    with Image.open(png) as raw:
        return raw.convert("RGBA").resize((size, size), Image.LANCZOS)


def pack_icons(label: str, src_glob: list[Path], size: int, atlas_base: str,
               generated: set[str], outline: tuple[str, float, float] | None = None) -> None:
    """Flat icon category: uniform square slices, optional companion outline atlas.
    outline = (outline_atlas_base, dilate_radius, blur) at slice scale."""
    sprites_dir = f"{atlas_base}.sprites"
    entries = [(p.stem, resize_icon(p, size)) for p in sorted(src_glob)]
    n = pack_group(atlas_base, sprites_dir, (size, size), entries, generated)
    if outline:
        outline_base, radius, blur = outline
        outline_entries = [(f"{key}_outline", white_outline(img, radius, blur))
                           for key, img in entries]
        n += pack_group(outline_base, sprites_dir, (size, size), outline_entries, generated)
    print(f"  {label}: {len(entries)} packed, {n} .tres updated")


def pack_pets(generated: set[str]) -> None:
    pngs = sorted((IMAGES / "pets").glob("*.png"))
    if not pngs:
        return
    entries = [(p.stem, Image.open(p).convert("RGBA")) for p in pngs]
    w = max(img.width for _, img in entries)
    h = max(img.height for _, img in entries)
    n = pack_group("pets_atlas", "pets_atlas.sprites", (w, h), entries, generated)
    print(f"  pets: {len(entries)} packed, {n} .tres updated")


def main() -> int:
    generated: set[str] = set()

    pack_cards(generated)
    # Outline radii/blur ported from the retired gen-outlines.py, scaled to slice size:
    # relics were tuned r1.5/b0.5 at 94px; potions r4/b1.25 at 256px -> r1.25/b0.4 at 80px.
    pack_icons("relics", list((IMAGES / "relics").glob("*.png")), 94,
               "relic_atlas", generated, outline=("relic_outline_atlas", 1.5, 0.5))
    pack_icons("powers", list((IMAGES / "powers").glob("*.png")), 64,
               "power_atlas", generated)
    pack_icons("potions", list((IMAGES / "potions").glob("*.png")), 80,
               "potion_atlas", generated, outline=("potion_outline_atlas", 1.25, 0.4))
    pack_pets(generated)

    if not generated:
        print("pack-atlases: no source art found", file=sys.stderr)
        return 1

    # Prune stale output (removed/renamed art, shrunk page counts). .import sidecars are
    # Godot's; delete them only alongside their source file.
    for f in OUT_DIR.rglob("*"):
        if f.is_dir() or f.suffix == ".import":
            continue
        rel = f.relative_to(OUT_DIR).as_posix()
        if rel not in generated:
            f.unlink()
            imp = f.with_name(f.name + ".import")
            if imp.exists():
                imp.unlink()
            print(f"  removed: {rel}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
