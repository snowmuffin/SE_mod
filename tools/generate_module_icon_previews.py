#!/usr/bin/env python3
"""
Generate high-resolution PNG previews for SE Overclock upgrade module icons.

Output: tools/_tmp_module_icons_preview/png/{role}-{level:02d}.png (512x512)
        tools/_tmp_module_icons_preview/contact_sheet.png (quick overview)

These are previews only; game uses DDS under SE_Overclock_mod/Textures/...
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

# Role name -> accent RGB (matches prior mod color mapping)
MODULE_PALETTES: list[tuple[str, tuple[int, int, int]]] = [
    ("defense", (100, 210, 255)),
    ("attack", (240, 85, 95)),
    ("power", (255, 215, 75)),
    ("berserker", (175, 115, 255)),
    ("speed", (85, 220, 130)),
    ("fortress", (255, 145, 55)),
]

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


def draw_module_icon(role: str, accent: tuple[int, int, int], level: int, out_size: int) -> Image.Image:
    """Single module chip: dark body, accent rim / glow, level meter."""
    img = Image.new("RGBA", (out_size, out_size), (18, 20, 24, 255))
    dr = ImageDraw.Draw(img)
    w, h = out_size, out_size
    cx, cy = w // 2, h // 2
    chip_w = int(w * 0.42)
    chip_h = int(h * 0.72)
    x0, y0 = cx - chip_w // 2, cy - chip_h // 2
    x1, y1 = x0 + chip_w, y0 + chip_h
    r = 22

    # Outer glow (accent)
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

    # Body gradient (vertical)
    for yy in range(y0, y1):
        t = (yy - y0) / max(1, chip_h - 1)
        base = _lerp(42, 28, t)
        dr.line([(x0, yy), (x1, yy)], fill=(int(base), int(base + 2), int(base + 6), 255))

    # Accent vertical strip (left)
    strip_w = max(10, chip_w // 7)
    dr.rounded_rectangle(
        (x0 + 6, y0 + 18, x0 + 6 + strip_w, y1 - 18),
        radius=8,
        fill=(*accent, 255),
    )

    # Inner slot
    slot_margin_x = strip_w + 22
    dr.rounded_rectangle(
        (x0 + slot_margin_x, cy - chip_h // 6, x1 - 18, cy + chip_h // 6),
        radius=10,
        fill=(10, 11, 14, 255),
        outline=(70, 75, 88, 255),
        width=2,
    )

    # Level meter: 10 horizontal ticks along bottom inside chip
    meter_y1 = y1 - 22
    meter_y0 = meter_y1 - 10
    meter_x0 = x0 + 16
    meter_x1 = x1 - 16
    total_w = meter_x1 - meter_x0
    seg = total_w / 10
    for i in range(10):
        sx0 = int(meter_x0 + i * seg + 1)
        sx1 = int(meter_x0 + (i + 1) * seg - 2)
        lit = i < level
        fill = accent if lit else (55, 58, 66)
        alpha = 255 if lit else 130
        dr.rounded_rectangle((sx0, meter_y0, sx1, meter_y1), radius=3, fill=(*fill[:3], alpha))

    # Role + level label
    font_sm = _try_font(max(18, out_size // 22))
    font_lg = _try_font(max(26, out_size // 14))
    label = role[:3].upper()
    dr.text((x0 + 14, y0 + 10), label, fill=(200, 205, 215, 255), font=font_sm)
    dr.text((x0 + 14, y0 + 10 + 22), f"LV {level}", fill=(220, 225, 235, 255), font=font_lg)

    return img


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
        for level in range(1, 11):
            im = draw_module_icon(role, accent, level, SIZE)
            path = png_dir / f"{role}-{level:02d}.png"
            im.save(path, format="PNG", optimize=True)
            print(path.relative_to(root))

    sheet = out_root / "contact_sheet.png"
    write_contact_sheet(png_dir, sheet)
    print(sheet.relative_to(root))
    print(f"Done: {len(MODULE_PALETTES) * 10} PNG + contact sheet -> {out_root}")


if __name__ == "__main__":
    main()
