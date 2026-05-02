#!/usr/bin/env python3
"""
Generate high-resolution PNG previews for SE Overclock upgrade module icons.

Same *role* shares one identical chip silhouette (body, strip, slot, glow); only
the level meter and LV label change per level — avoids per-image AI drift.

Output: tools/_tmp_module_icons_preview/png/{role}-{level:02d}.png (512x512)
        tools/_tmp_module_icons_preview/contact_sheet.png

Game still uses DDS under SE_Overclock_mod/Textures/...; this is preview only.
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

MODULE_PALETTES: list[tuple[str, tuple[int, int, int]]] = [
    ("defense", (100, 210, 255)),
    ("attack", (240, 85, 95)),
    ("power", (255, 215, 75)),
    ("berserker", (175, 115, 255)),
    ("speed", (85, 220, 130)),
    ("fortress", (255, 145, 55)),
]

# Fixed 3-letter HUD tag (role[:3] is wrong for e.g. "attack" -> "ATT" ok, "berserker" -> "ber")
ROLE_ABBR: dict[str, str] = {
    "defense": "DEF",
    "attack": "ATK",
    "power": "PWR",
    "berserker": "BRK",
    "speed": "SPD",
    "fortress": "FOR",
}

SIZE = 512


def _lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def _try_font(size: int):
    for name in ("arial.ttf", "SegoeUI.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


@dataclass(frozen=True)
class ChipLayout:
    """Pixel geometry shared by all levels of one role."""

    meter_x0: int
    meter_x1: int
    meter_y0: int
    meter_y1: int
    label_bg: tuple[int, int, int, int, int, int]  # x0,y0,x1,y1
    label_line1_y: int
    label_line2_y: int
    label_text_x: int


def draw_role_base(role: str, accent: tuple[int, int, int], out_size: int) -> tuple[Image.Image, ChipLayout]:
    """Chip body + glow + strip + slot. No level-specific pixels."""
    img = Image.new("RGBA", (out_size, out_size), (18, 20, 24, 255))
    w, h = out_size, out_size
    cx, cy = w // 2, h // 2
    chip_w = int(w * 0.42)
    chip_h = int(h * 0.72)
    x0, y0 = cx - chip_w // 2, cy - chip_h // 2
    x1, y1 = x0 + chip_w, y0 + chip_h
    r = 22

    glow = Image.new("RGBA", (out_size, out_size), (0, 0, 0, 0))
    gdr = ImageDraw.Draw(glow)
    for i in range(18, 0, -1):
        alpha = int(14 + i * 5)
        inset = i
        gdr.rounded_rectangle(
            (x0 - inset, y0 - inset, x1 + inset, y1 + inset),
            radius=r + inset,
            outline=(*accent, alpha),
            width=3,
        )
    img = Image.alpha_composite(img, glow)
    dr = ImageDraw.Draw(img)

    for yy in range(y0, y1):
        t = (yy - y0) / max(1, chip_h - 1)
        base = _lerp(42, 28, t)
        dr.line([(x0, yy), (x1, yy)], fill=(int(base), int(base + 2), int(base + 6), 255))

    strip_w = max(10, chip_w // 7)
    dr.rounded_rectangle(
        (x0 + 6, y0 + 18, x0 + 6 + strip_w, y1 - 18),
        radius=8,
        fill=(*accent, 255),
    )

    slot_margin_x = strip_w + 22
    dr.rounded_rectangle(
        (x0 + slot_margin_x, cy - chip_h // 6, x1 - 18, cy + chip_h // 6),
        radius=10,
        fill=(10, 11, 14, 255),
        outline=(70, 75, 88, 255),
        width=2,
    )

    meter_y1 = y1 - 22
    meter_y0 = meter_y1 - 12
    meter_x0 = x0 + 16
    meter_x1 = x1 - 16

    # Reserved label area (fixed size so LV01 / LV10 never shifts layout)
    lb_x0, lb_y0 = x0 + 10, y0 + 10
    lb_x1, lb_y1 = lb_x0 + 118, lb_y0 + 52
    layout = ChipLayout(
        meter_x0=meter_x0,
        meter_x1=meter_x1,
        meter_y0=meter_y0,
        meter_y1=meter_y1,
        label_bg=(lb_x0, lb_y0, lb_x1, lb_y1),
        label_line1_y=lb_y0 + 4,
        label_line2_y=lb_y0 + 26,
        label_text_x=lb_x0 + 8,
    )

    dr.rounded_rectangle(
        (lb_x0, lb_y0, lb_x1, lb_y1),
        radius=8,
        fill=(0, 0, 0, 140),
        outline=(80, 86, 98, 200),
        width=1,
    )

    return img, layout


def composite_level(base: Image.Image, layout: ChipLayout, role: str, accent: tuple[int, int, int], level: int) -> Image.Image:
    """Copy base, draw only meter + fixed-slot LV text."""
    im = base.copy()
    dr = ImageDraw.Draw(im)
    abbr = ROLE_ABBR.get(role, role[:3].upper()).upper()

    seg = (layout.meter_x1 - layout.meter_x0) / 10
    for i in range(10):
        sx0 = int(layout.meter_x0 + i * seg + 1)
        sx1 = int(layout.meter_x0 + (i + 1) * seg - 2)
        lit = i < level
        fill = accent if lit else (55, 58, 66)
        alpha = 255 if lit else 110
        dr.rounded_rectangle((sx0, layout.meter_y0, sx1, layout.meter_y1), radius=3, fill=(*fill[:3], alpha))

    font_sm = _try_font(max(17, SIZE // 28))
    font_lg = _try_font(max(22, SIZE // 22))
    dr.text((layout.label_text_x, layout.label_line1_y), abbr, fill=(210, 215, 225, 255), font=font_sm)
    # Fixed width mental model: always two digits in the number part
    dr.text((layout.label_text_x, layout.label_line2_y), f"LV{level:02d}", fill=(235, 240, 250, 255), font=font_lg)
    return im


def write_contact_sheet(png_dir: Path, out_path: Path, thumb: int = 96) -> None:
    cols, rows = 10, 6
    pad = 2
    sheet = Image.new("RGBA", (pad * 2 + cols * thumb, pad * 2 + rows * thumb), (12, 14, 18, 255))
    for row, (role, _) in enumerate(MODULE_PALETTES):
        for col in range(1, 11):
            src = png_dir / f"{role}-{col:02d}.png"
            im_r = Image.open(src).convert("RGBA").resize((thumb, thumb), Image.Resampling.LANCZOS)
            px = pad + (col - 1) * thumb
            py = pad + row * thumb
            sheet.paste(im_r, (px, py), im_r)
    sheet.save(out_path, format="PNG", optimize=True)


def main() -> None:
    root = Path(__file__).resolve().parent
    out_root = root / "_tmp_module_icons_preview"
    png_dir = out_root / "png"
    png_dir.mkdir(parents=True, exist_ok=True)

    for role, accent in MODULE_PALETTES:
        base, layout = draw_role_base(role, accent, SIZE)
        for level in range(1, 11):
            im = composite_level(base, layout, role, accent, level)
            path = png_dir / f"{role}-{level:02d}.png"
            im.save(path, format="PNG", optimize=True)
            print(path.relative_to(root))

    sheet = out_root / "contact_sheet.png"
    write_contact_sheet(png_dir, sheet)
    print(sheet.relative_to(root))
    print(f"Done: {len(MODULE_PALETTES) * 10} PNG + contact sheet -> {out_root}")


if __name__ == "__main__":
    main()
