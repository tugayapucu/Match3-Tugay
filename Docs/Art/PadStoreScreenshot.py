"""Pad a raw 1080 x 2340 S23 screenshot to a 1350 x 2400 (9:16) Play Store screenshot.

The capture is pasted unchanged (no scaling, cropping or retouching) at offset
(135, 30); only the surrounding border is added. The border extends the
capture's own edge pixels outward (each row's edge colour to the sides, then
the top and bottom rows up and down). The script refuses an image unless every
edge consists of long flat colour runs, so no UI touches an edge and the
padding cannot hide, cut or invent anything. For a screen with one flat
background this is the same as filling with that colour.

Run from the repository root with Pillow 11.3.0 installed outside the repository:

    python Docs/Art/PadStoreScreenshot.py RAW.png OUT.png

Output: 24-bit RGB PNG without alpha, as Play requires.
"""

import sys

from PIL import Image

SOURCE = (1080, 2340)
TARGET = (1350, 2400)
TOLERANCE = 2  # per-channel difference still treated as the same flat colour
MIN_RUN = 40  # shortest flat edge run accepted; anything shorter suggests UI at the edge


def check_edge(name, pixels):
    start, ref = 0, pixels[0]
    for i, c in enumerate(pixels + [None]):
        if c is None or max(abs(a - b) for a, b in zip(c, ref)) > TOLERANCE:
            if i - start < MIN_RUN:
                raise SystemExit(f"{name} edge has a {i - start} px run at {start}; UI may touch the edge. Not padding.")
            if c is not None:
                start, ref = i, c


def main():
    if len(sys.argv) != 3:
        raise SystemExit("usage: PadStoreScreenshot.py RAW.png OUT.png")
    img = Image.open(sys.argv[1]).convert("RGB")
    w, h = img.size
    if (w, h) != SOURCE:
        raise SystemExit(f"Expected a {SOURCE[0]} x {SOURCE[1]} capture, got {w} x {h}.")
    px = img.load()
    check_edge("left", [px[0, y] for y in range(h)])
    check_edge("right", [px[w - 1, y] for y in range(h)])
    check_edge("top", [px[x, 0] for x in range(w)])
    check_edge("bottom", [px[x, h - 1] for x in range(w)])

    pad_x, pad_y = (TARGET[0] - w) // 2, (TARGET[1] - h) // 2
    out = Image.new("RGB", TARGET)
    out.paste(img, (pad_x, pad_y))
    # Sides: repeat each row's edge pixel. Then top and bottom: repeat the full padded edge rows.
    out.paste(img.crop((0, 0, 1, h)).resize((pad_x, h), Image.NEAREST), (0, pad_y))
    out.paste(img.crop((w - 1, 0, w, h)).resize((pad_x, h), Image.NEAREST), (pad_x + w, pad_y))
    out.paste(out.crop((0, pad_y, TARGET[0], pad_y + 1)).resize((TARGET[0], pad_y), Image.NEAREST), (0, 0))
    out.paste(out.crop((0, pad_y + h - 1, TARGET[0], pad_y + h)).resize((TARGET[0], pad_y), Image.NEAREST), (0, pad_y + h))
    out.save(sys.argv[2])


if __name__ == "__main__":
    main()
