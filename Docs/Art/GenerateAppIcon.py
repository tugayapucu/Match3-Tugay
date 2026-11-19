"""Generate Galaxplode's launcher icon layers, Play Store icon and feature graphic.

Design "A": the Spectrum Core special gem with a soft glow on a navy space
background (chosen by the human on 2026-11-20).

Reproducible: run from the repository root with Pillow 11.3.0 installed
outside the repository (for example in a throwaway virtual environment):

    python Docs/Art/GenerateAppIcon.py --store-out Docs/Art/Store
    python Docs/Art/GenerateAppIcon.py --launcher-out Assets/Art/AppIcon

Inputs are project-owned files only: the Prismatic Energy Spectrum Core sprite
and the LiberationSans font shipped with TextMesh Pro (SIL Open Font License).
The star field uses fixed seeds, so the same inputs give the same images.
"""

import argparse
import math
import os
import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

GEM = "Assets/Art/Tiles/PrismaticEnergy/Specials/SpectrumCore.png"
FONT = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf"

# Colours sampled from the game: Main Menu background and gameplay background.
NAVY = (22, 34, 61)
BLUE = (49, 76, 120)

ADAPTIVE = 432  # 108 dp at xxxhdpi
SAFE_DIAMETER = 264  # 66 dp safe zone: never clipped by any launcher mask


def gem(size):
    """Spectrum Core scaled so its visible body (alpha >= 128) spans `size` px."""
    im = Image.open(GEM).convert("RGBA")
    im = im.crop(im.getchannel("A").point(lambda a: 255 if a >= 128 else 0).getbbox())
    scale = size / max(im.size)
    w, h = max(1, round(im.width * scale)), max(1, round(im.height * scale))
    # Resize premultiplied so transparent edges get no dark fringe.
    return im.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")


def background(w, h, seed=7):
    """Radial blue-to-navy gradient with a fixed, sparse star field."""
    cx, cy = w / 2, h / 2
    rmax = math.hypot(cx, cy)
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):
        for x in range(w):
            t = min(1.0, math.hypot(x - cx, y - cy) / rmax)
            t = t * t * (3 - 2 * t)
            px[x, y] = tuple(round(BLUE[i] + (NAVY[i] - BLUE[i]) * t) for i in range(3))
    stars = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(stars)
    rng = random.Random(seed)
    for _ in range(int(w * h / 2600)):
        x, y = rng.uniform(0, w), rng.uniform(0, h)
        r = rng.choice((0.8, 1.0, 1.2, 1.6))
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, rng.randint(70, 190)))
    img = img.convert("RGBA")
    img.alpha_composite(stars)
    return img


def foreground(size, body):
    """Transparent layer: glow plus gem, all inside a circle of diameter `body`."""
    fg = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    c = size / 2
    radius = body * 0.52
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((c - radius, c - radius, c + radius, c + radius), fill=(255, 236, 190, 70))
    fg.alpha_composite(glow.filter(ImageFilter.GaussianBlur(radius * 0.35)))
    g = gem(body * 0.92)
    fg.alpha_composite(g, (round(c - g.width / 2), round(c - g.height / 2)))
    return fg


def composite(size, body_ratio):
    img = background(size, size)
    img.alpha_composite(foreground(size, size * body_ratio))
    return img


def circle(img):
    """Anti-aliased circular crop (supersampled mask)."""
    s = img.width
    m = Image.new("L", (s * 4, s * 4), 0)
    ImageDraw.Draw(m).ellipse((0, 0, s * 4 - 1, s * 4 - 1), fill=255)
    m = m.resize((s, s), Image.LANCZOS)
    out = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    out.paste(img, (0, 0), ImageChops.multiply(m, img.getchannel("A")))
    return out


def feature_graphic():
    w, h = 1024, 500
    img = background(w, h, seed=11)
    side = 420  # canvas larger than the art so the soft glow fades out fully
    art = foreground(side, 280)
    img.alpha_composite(art, (round(800 - side / 2), round(h / 2 - side / 2)))
    font = ImageFont.truetype(FONT, 78)
    d = ImageDraw.Draw(img)
    text = "GALAXPLODE"
    box = d.textbbox((0, 0), text, font=font, stroke_width=3)
    tx = 350 - (box[2] - box[0]) / 2
    ty = h / 2 - (box[3] - box[1]) / 2 - box[1]
    d.text((tx, ty), text, font=font, fill=(255, 255, 255), stroke_width=3, stroke_fill=(255, 255, 255))
    return img.convert("RGB")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--launcher-out", help="folder for Android launcher icon sources")
    p.add_argument("--store-out", help="folder for Play Store listing graphics")
    args = p.parse_args()
    if not (args.launcher_out or args.store_out):
        p.error("give --launcher-out and/or --store-out")

    if args.launcher_out:
        out = args.launcher_out
        os.makedirs(out, exist_ok=True)
        # Adaptive layers: 432 px = 108 dp at xxxhdpi; artwork inside the 66 dp safe zone.
        background(ADAPTIVE, ADAPTIVE).convert("RGB").save(os.path.join(out, "AdaptiveBackground.png"))
        foreground(ADAPTIVE, SAFE_DIAMETER).save(os.path.join(out, "AdaptiveForeground.png"))
        # Legacy (full square) and round icons, 192 px source.
        legacy = composite(192, 0.80)
        legacy.convert("RGB").save(os.path.join(out, "LegacyIcon.png"))
        circle(legacy).save(os.path.join(out, "RoundIcon.png"))

    if args.store_out:
        out = args.store_out
        os.makedirs(out, exist_ok=True)
        # Play listing icon: 512 x 512 full square, fully opaque 32-bit PNG; Google applies the mask.
        composite(512, 0.74).save(os.path.join(out, "PlayStoreIcon-512.png"))
        # Feature graphic: 1024 x 500, 24-bit PNG without alpha.
        feature_graphic().save(os.path.join(out, "FeatureGraphic-1024x500.png"))


if __name__ == "__main__":
    main()
