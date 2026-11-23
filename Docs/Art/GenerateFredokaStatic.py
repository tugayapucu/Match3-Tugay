"""Static Fredoka Medium and SemiBold for TextMeshPro, from the official Google Fonts variable font.

TextMeshPro reads only a variable font's default instance (Fredoka Light), so the two weights the HUD
uses are cut here as ordinary static fonts. Source, licence and tool are pinned; the output is byte-identical
on every run.

Run from the repository root in an isolated environment created outside the repository
(fontTools is not a project dependency):
    python -m venv <outside-repo>/fredoka-venv
    <outside-repo>/fredoka-venv/Scripts/python -m pip install --require-hashes -r Docs/Art/requirements-fonts.txt
    <outside-repo>/fredoka-venv/Scripts/python -I Docs/Art/GenerateFredokaStatic.py
Writes Fredoka-Medium.ttf, Fredoka-SemiBold.ttf and OFL.txt to Assets/Fonts/Fredoka/ (or the folder given
as the first argument) and prints their SHA-256 hashes.
"""
from hashlib import sha256
from pathlib import Path
import sys
import urllib.request

from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

# google/fonts, the official Google Fonts repository; latest commit touching ofl/fredoka when pinned.
COMMIT = "a60a77e14f28abd4ef243a1b5dfc48df0cec5205"
BASE = f"https://raw.githubusercontent.com/google/fonts/{COMMIT}/ofl/fredoka/"
SOURCES = {
    "Fredoka[wdth,wght].ttf": ("Fredoka%5Bwdth%2Cwght%5D.ttf",
                               "2ba02e68b152868aef9ba28e24b3648c7d457fe6f25c761f2c2c53fb61a73fc8"),
    "OFL.txt": ("OFL.txt", "5c9e7eee5c6b25f4b05b8d53b2e470ea4962f9ced742d044a98f7d95d1375bab"),
}
# The variable font's own named instances; width 100 is the normal width.
WEIGHTS = {"Fredoka-Medium.ttf": 500, "Fredoka-SemiBold.ttf": 600}
FONTTOOLS_VERSION = "4.68.0"


def fetch(name):
    remote, expected = SOURCES[name]
    data = urllib.request.urlopen(BASE + remote, timeout=60).read()
    actual = sha256(data).hexdigest()
    if actual != expected:
        raise SystemExit(f"{name}: SHA-256 {actual} does not match the pinned {expected}")
    return data


def main():
    import fontTools
    if fontTools.version != FONTTOOLS_VERSION:
        raise SystemExit(f"fontTools {fontTools.version} found; install the pinned {FONTTOOLS_VERSION}.")

    root = Path(__file__).resolve().parents[2]
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "Assets" / "Fonts" / "Fredoka"
    out.mkdir(parents=True, exist_ok=True)

    variable_path = out / "_source.ttf"
    variable_path.write_bytes(fetch("Fredoka[wdth,wght].ttf"))
    try:
        for file_name, weight in WEIGHTS.items():
            font = TTFont(variable_path)
            static = instancer.instantiateVariableFont(font, {"wght": weight, "wdth": 100},
                                                       updateFontNames=True, static=True)
            static.recalcTimestamp = False  # keep the source's date, so repeated runs match byte for byte
            static.save(out / file_name)
    finally:
        variable_path.unlink()
    (out / "OFL.txt").write_bytes(fetch("OFL.txt"))

    for file_name in [*WEIGHTS, "OFL.txt"]:
        print(sha256((out / file_name).read_bytes()).hexdigest(), file_name)


if __name__ == "__main__":
    main()
