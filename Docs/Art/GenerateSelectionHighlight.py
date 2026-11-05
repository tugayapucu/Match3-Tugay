"""Original four-corner selection brackets; Python standard library only.

Run from any directory: python Docs/Art/GenerateSelectionHighlight.py
Coordinates are pixel edges. No reference images or external artwork are used.
"""
from pathlib import Path
import struct
import zlib


SIZE = 512
INSET = 10             # 492 px outer extent = 0.9609375 units at 512 PPU
ARM = 90
STROKE = 22            # 4.30% of a cell; white core is 14 px
BORDER = 4
DARK = (24, 29, 39, 255)
WHITE = (255, 255, 255, 255)


def generate():
    pixels = bytearray(SIZE * SIZE * 4)

    def paint(x, y, color):
        offset = (y * SIZE + x) * 4
        pixels[offset:offset + 4] = bytes(color)

    # Define one L, then mirror it into the four corners. The white inset is the
    # same L eroded by BORDER pixels, leaving a dark border around both arms.
    for y in range(ARM):
        for x in range(ARM):
            if x >= STROKE and y >= STROKE:
                continue
            white = (BORDER <= x < ARM - BORDER and BORDER <= y < ARM - BORDER
                     and (x < STROKE - BORDER or y < STROKE - BORDER))
            for mirror_x in (False, True):
                for mirror_y in (False, True):
                    px, py = INSET + x, INSET + y
                    paint(SIZE - 1 - px if mirror_x else px,
                          SIZE - 1 - py if mirror_y else py, WHITE if white else DARK)

    def chunk(kind, data):
        return (struct.pack('!I', len(data)) + kind + data
                + struct.pack('!I', zlib.crc32(kind + data)))

    rows = b''.join(b'\0' + pixels[y * SIZE * 4:(y + 1) * SIZE * 4] for y in range(SIZE))
    png = (b'\x89PNG\r\n\x1a\n'
           + chunk(b'IHDR', struct.pack('!2I5B', SIZE, SIZE, 8, 6, 0, 0, 0))
           + chunk(b'IDAT', zlib.compress(rows, 9)) + chunk(b'IEND', b''))
    target = Path(__file__).resolve().parents[2] / 'Assets/Art/Tiles/PrismaticEnergy/SelectionHighlight.png'
    target.write_bytes(png)
    print(f'{target}: {len(png)} bytes')


if __name__ == '__main__':
    generate()
