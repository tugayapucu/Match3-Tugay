"""Compact HUD sprites from the approved B4-A Final Polish v3 design; Python standard library only.

Run from any directory: python Docs/Art/GenerateHudSprites.py [--out DIR]
Default output: Assets/Art/Hud/Compact. Every run writes byte-identical files.

The values below are the Compact tier's CSS from the v3 design (sizes in Canvas units, u, of the
1080-wide reference). Sprites are drawn at 2 pixels per unit and imported at 200 pixels per unit,
so they stay sharp where the Canvas scales up (1440-wide screens draw 1.33 pixels per unit).

Shapes are anti-aliased with 4 x 4 samples per pixel. Colours are mixed like the browser that renders
the design: premultiplied, in sRGB. Blurs use the three box blurs the CSS filter specification
describes for a Gaussian. Only additions, multiplications, divisions and square roots are used, so
the output does not depend on the platform's maths library. The fill glow (0 0 8 u @ 45%) is left
out until it has been reviewed.
"""
import argparse
import math
from pathlib import Path
import struct
import zlib


SCALE = 2          # pixels per unit
SAMPLES = 4        # samples per axis and pixel

# HUD shell: 112 u tall, radius 34, 2 u stroke, vertical gradient, 2 u inner top highlight.
SHELL_HEIGHT = 112
SHELL_RADIUS = 34
SHELL_STROKE = 2
SHELL_WIDTH = 2 * SHELL_RADIUS + 4          # 4 u straight centre column, stretched by the 9-slice
SHELL_TOP = (40, 50, 132, 0.84)
SHELL_BOTTOM = (22, 29, 84, 0.88)
SHELL_STROKE_COLOR = (150, 165, 255, 0.20)
SHELL_HIGHLIGHT = (255, 255, 255, 0.09)

# Drop shadow: 0 18 u offset, 40 u blur, rgba(3, 5, 25, .5); drawn only outside the shell, like CSS.
SHADOW_OFFSET = 18
SHADOW_BLUR = 40                            # CSS blur radius; the Gaussian's sigma is half of it
SHADOW_COLOR = (3, 5, 25, 0.5)
SHADOW_MARGIN = 60                          # covers the blur's full reach (about 1.5 box widths)

# Goal halo: 80 u circle, radial rgba(80, 160, 255, .28) to transparent at 70% of the corner distance.
HALO_SIZE = 80
HALO_COLOR = (80, 160, 255, 0.28)

# Goal bar: 14 u tall, radius 7. The track adds a 2 u ring outside the bar and an inset shadow
# (0 2 u offset, 4 u blur, rgba(0, 0, 0, .6)) inside it.
BAR_HEIGHT = 14
BAR_RADIUS = 7
TRACK_RING = 2
TRACK_COLOR = (7, 11, 42, 1.0)
TRACK_RING_COLOR = (150, 165, 255, 0.12)
TRACK_SHADOW_OFFSET = 2
TRACK_SHADOW_BLUR = 4
TRACK_SHADOW_COLOR = (0, 0, 0, 0.6)
TRACK_WIDTH = 2 * (BAR_RADIUS + TRACK_RING) + 16    # long enough that the centre column is uniform
FILL_WIDTH = 2 * BAR_RADIUS + 4
FILL_STOPS = ((0.0, (127, 228, 255, 1.0)), (0.45, (58, 168, 255, 1.0)), (1.0, (43, 107, 255, 1.0)))


def rounded_rect(x0, y0, x1, y1, radius):
    def inside(x, y):
        if x < x0 or x > x1 or y < y0 or y > y1:
            return False
        dx = x - min(max(x, x0 + radius), x1 - radius)
        dy = y - min(max(y, y0 + radius), y1 - radius)
        return dx * dx + dy * dy <= radius * radius
    return inside


def coverage(width, height, test):
    """Fraction of each pixel's samples for which test(x, y) holds; x and y in units."""
    pixels_w, pixels_h = width * SCALE, height * SCALE
    offsets = [(k + 0.5) / SAMPLES for k in range(SAMPLES)]
    result = []
    for py in range(pixels_h):
        for px in range(pixels_w):
            hits = 0
            for oy in offsets:
                y = (py + oy) / SCALE
                for ox in offsets:
                    if test((px + ox) / SCALE, y):
                        hits += 1
            result.append(hits / (SAMPLES * SAMPLES))
    return result


def box_sizes(blur_radius):
    """The CSS filter specification's three box blurs for sigma = blur_radius / 2, in pixels."""
    sigma = blur_radius / 2 * SCALE
    d = int(sigma * 3 * math.sqrt(2 * math.pi) / 4 + 0.5)
    if d % 2:
        return [(d, 0), (d, 0), (d, 0)]
    return [(d, -1), (d, 0), (d + 1, 0)]     # two even boxes shifted half a pixel each way, then d + 1


def box_blur_1d(values, size, shift):
    """Mean over a window of `size` samples; values outside are 0.

    An odd window is centred on the sample. An even window cannot be: shift -1 puts the extra sample
    on the left, shift 0 on the right.
    """
    count = len(values)
    low = -(size // 2)
    if size % 2 == 0 and shift == 0:
        low += 1
    prefix = [0.0]
    for value in values:
        prefix.append(prefix[-1] + value)
    out = []
    for i in range(count):
        start = min(max(i + low, 0), count)
        end = min(max(i + low + size, 0), count)
        out.append((prefix[end] - prefix[start]) / size)
    return out


def blur(mask, width, height, blur_radius):
    pixels_w, pixels_h = width * SCALE, height * SCALE
    data = list(mask)
    for size, shift in box_sizes(blur_radius):
        for row in range(pixels_h):
            data[row * pixels_w:(row + 1) * pixels_w] = box_blur_1d(
                data[row * pixels_w:(row + 1) * pixels_w], size, shift)
    for size, shift in box_sizes(blur_radius):
        for col in range(pixels_w):
            column = box_blur_1d(data[col::pixels_w], size, shift)
            for row in range(pixels_h):
                data[row * pixels_w + col] = column[row]
    return data


def mix(stops, t):
    """CSS gradient colour at t: stops interpolated premultiplied, returned premultiplied."""
    if t <= stops[0][0]:
        return premultiply(stops[0][1])
    for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
        if t <= t1:
            f = (t - t0) / (t1 - t0)
            p0, p1 = premultiply(c0), premultiply(c1)
            return tuple(a + (b - a) * f for a, b in zip(p0, p1))
    return premultiply(stops[-1][1])


def premultiply(color):
    r, g, b, a = color
    return (r * a, g * a, b * a, a)


class Canvas:
    def __init__(self, width, height):
        self.width, self.height = width, height
        self.pixels = [(0.0, 0.0, 0.0, 0.0)] * (width * SCALE * height * SCALE)

    def paint(self, amounts, color_at_row):
        """Composite a premultiplied colour (per pixel row) over the canvas, scaled by amounts."""
        pixels_w = self.width * SCALE
        for i, amount in enumerate(amounts):
            if amount <= 0:
                continue
            r, g, b, a = color_at_row(i // pixels_w)
            r, g, b, a = r * amount, g * amount, b * amount, a * amount
            dr, dg, db, da = self.pixels[i]
            keep = 1 - a
            self.pixels[i] = (r + dr * keep, g + dg * keep, b + db * keep, a + da * keep)

    def png(self):
        pixels_w, pixels_h = self.width * SCALE, self.height * SCALE
        raw = bytearray()
        for row in range(pixels_h):
            raw.append(0)
            for r, g, b, a in self.pixels[row * pixels_w:(row + 1) * pixels_w]:
                alpha = round(a * 255)
                if alpha == 0:
                    raw += b'\0\0\0\0'
                else:
                    raw += bytes((min(255, round(r / a)), min(255, round(g / a)), min(255, round(b / a)), alpha))

        def chunk(kind, data):
            return struct.pack('!I', len(data)) + kind + data + struct.pack('!I', zlib.crc32(kind + data))

        return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('!2I5B', pixels_w, pixels_h, 8, 6, 0, 0, 0))
                + chunk(b'IDAT', zlib.compress(bytes(raw), 9)) + chunk(b'IEND', b''))


def constant(color):
    value = premultiply(color)
    return lambda row: value


def shell():
    canvas = Canvas(SHELL_WIDTH, SHELL_HEIGHT)
    outer = rounded_rect(0, 0, SHELL_WIDTH, SHELL_HEIGHT, SHELL_RADIUS)
    s = SHELL_STROKE
    inner = rounded_rect(s, s, SHELL_WIDTH - s, SHELL_HEIGHT - s, SHELL_RADIUS - s)
    lowered = rounded_rect(s, 2 * s, SHELL_WIDTH - s, SHELL_HEIGHT, SHELL_RADIUS - s)
    stops = ((0.0, SHELL_TOP), (1.0, SHELL_BOTTOM))
    canvas.paint(coverage(SHELL_WIDTH, SHELL_HEIGHT, outer),
                 lambda row: mix(stops, (row + 0.5) / (SHELL_HEIGHT * SCALE)))
    # Inset 0 2 u highlight: the padding box minus itself moved down 2 u, under the stroke.
    canvas.paint(coverage(SHELL_WIDTH, SHELL_HEIGHT, lambda x, y: inner(x, y) and not lowered(x, y)),
                 constant(SHELL_HIGHLIGHT))
    canvas.paint(coverage(SHELL_WIDTH, SHELL_HEIGHT, lambda x, y: outer(x, y) and not inner(x, y)),
                 constant(SHELL_STROKE_COLOR))
    return canvas


def shadow():
    """The shell's drop shadow. The shell sits SHADOW_MARGIN u from the left and
    SHADOW_MARGIN - SHADOW_OFFSET u from the top; the shadow is cut away under it."""
    width = SHELL_WIDTH + 2 * SHADOW_MARGIN
    top = SHADOW_MARGIN - SHADOW_OFFSET
    height = top + SHELL_HEIGHT + SHADOW_OFFSET + SHADOW_MARGIN
    left = SHADOW_MARGIN
    cast = rounded_rect(left, top + SHADOW_OFFSET, left + SHELL_WIDTH, top + SHADOW_OFFSET + SHELL_HEIGHT, SHELL_RADIUS)
    body = rounded_rect(left, top, left + SHELL_WIDTH, top + SHELL_HEIGHT, SHELL_RADIUS)
    blurred = blur(coverage(width, height, cast), width, height, SHADOW_BLUR)
    under = coverage(width, height, body)
    canvas = Canvas(width, height)
    canvas.paint([b * (1 - u) for b, u in zip(blurred, under)], constant(SHADOW_COLOR))
    return canvas


def halo():
    canvas = Canvas(HALO_SIZE, HALO_SIZE)
    centre = HALO_SIZE / 2
    end = 0.7 * math.sqrt(2) * centre        # 70% of the distance to the farthest corner
    pixels = HALO_SIZE * SCALE
    amounts = []
    for py in range(pixels):
        for px in range(pixels):
            dx, dy = (px + 0.5) / SCALE - centre, (py + 0.5) / SCALE - centre
            distance = math.sqrt(dx * dx + dy * dy)
            amounts.append(max(0.0, 1 - distance / end))
    canvas.paint(amounts, constant(HALO_COLOR))
    return canvas


def track():
    width, height = TRACK_WIDTH, BAR_HEIGHT + 2 * TRACK_RING
    ring = TRACK_RING
    outer = rounded_rect(0, 0, width, height, BAR_RADIUS + ring)
    bar = rounded_rect(ring, ring, width - ring, height - ring, BAR_RADIUS)
    lowered = rounded_rect(ring, ring + TRACK_SHADOW_OFFSET, width - ring, height - ring + TRACK_SHADOW_OFFSET, BAR_RADIUS)
    bar_cover = coverage(width, height, bar)
    canvas = Canvas(width, height)
    # The 2 u ring is an outer box-shadow spread: drawn outside the bar only.
    canvas.paint(coverage(width, height, lambda x, y: outer(x, y) and not bar(x, y)), constant(TRACK_RING_COLOR))
    canvas.paint(bar_cover, constant(TRACK_COLOR))
    # Inset shadow: the blurred area outside the lowered bar, kept inside the bar.
    lit = blur(coverage(width, height, lowered), width, height, TRACK_SHADOW_BLUR)
    canvas.paint([(1 - l) * c for l, c in zip(lit, bar_cover)], constant(TRACK_SHADOW_COLOR))
    return canvas


def fill():
    canvas = Canvas(FILL_WIDTH, BAR_HEIGHT)
    shape = rounded_rect(0, 0, FILL_WIDTH, BAR_HEIGHT, BAR_RADIUS)
    canvas.paint(coverage(FILL_WIDTH, BAR_HEIGHT, shape),
                 lambda row: mix(FILL_STOPS, (row + 0.5) / (BAR_HEIGHT * SCALE)))
    return canvas


SPRITES = {
    'HudShell.png': shell,
    'HudShellShadow.png': shadow,
    'GoalHalo.png': halo,
    'GoalTrack.png': track,
    'GoalFill.png': fill,
}


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', type=Path,
                        default=Path(__file__).resolve().parents[2] / 'Assets/Art/Hud/Compact')
    target = parser.parse_args().out
    target.mkdir(parents=True, exist_ok=True)
    for name, draw in SPRITES.items():
        data = draw().png()
        (target / name).write_bytes(data)
        print(f'{target / name}: {len(data)} bytes')


if __name__ == '__main__':
    main()
