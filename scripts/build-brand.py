#!/usr/bin/env python3
"""Build Merchant's brand assets from one description of the mark, colours and type.

Writes SVGs (text converted to outlines, so they render the same everywhere) and their PNG and
ICO exports into assets/brand, and the till's application icon into src/Merchant.Till/Assets.

    python scripts/build-brand.py            build everything
    python scripts/build-brand.py --svg-only  skip the PNG and ICO exports (no browser needed)

Needs fontTools and Pillow, and Edge or Chrome to turn the SVGs into PNGs.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from functools import lru_cache
from pathlib import Path

from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

ROOT = Path(__file__).resolve().parent.parent
FONTS = ROOT / "assets" / "fonts"
OUT = ROOT / "assets" / "brand"
TILL_ASSETS = ROOT / "src" / "Merchant.Till" / "Assets"

NIGHT = "#101635"
NIGHT_RAISED = "#1E2757"
AMBER = "#FFB000"
INDIGO = "#3A43A8"
MIST = "#F5F6FB"
MIST_DIM = "#C9CDE4"

NAME = "merchant"
TAGLINE = "The till that packs up with you."
DESCRIPTOR = "The offline point of sale for pop-up shops and food trucks"
PITCH = ["The no-cloud point of sale for pop-up", "shops and food trucks."]
PROOF = "Works fully offline. Tills sync with each other over local Wi-Fi."
CHIPS = ["Offline first", "Encrypted SQLite", "Local sync", "C# and Avalonia", "AGPL-3.0"]
REPOSITORY = "rizonesoft/Merchant"

# The mark on a 512 grid: an M drawn as two pop-up tent peaks, under a coin.
MARK_PEAKS = "M104 386L175 202L256 328L337 202L408 386"
MARK_STROKE = 58
MARK_COIN = (256, 126, 31)
TILE_RADIUS = 112

ICON_SIZES = [16, 24, 32, 48, 64, 128, 256, 512, 1024]
ICO_SIZES = [16, 24, 32, 48, 64, 128, 256]


class Face:
    """One static cut of a variable font, able to measure text and turn it into SVG outlines."""

    def __init__(self, file: str, **axes: float) -> None:
        self.font = instantiateVariableFont(TTFont(FONTS / file), axes, inplace=False)
        self.glyphs = self.font.getGlyphSet()
        self.cmap = self.font.getBestCmap()
        self.upm = self.font["head"].unitsPerEm

    def _names(self, text: str) -> list[str]:
        return [self.cmap[ord(ch)] for ch in text]

    def width(self, text: str, size: float, tracking: float = 0.0) -> float:
        """Advance width in px. `tracking` is letter-spacing in em, applied between letters."""
        units = sum(self.glyphs[name].width for name in self._names(text))
        return units * size / self.upm + tracking * size * (len(text) - 1)

    def extent(self, text: str, size: float) -> tuple[float, float]:
        """How far the ink reaches above and below the baseline, in px (both positive up)."""
        low, high = 0.0, 0.0
        for name in self._names(text):
            pen = BoundsPen(self.glyphs)
            self.glyphs[name].draw(pen)
            if pen.bounds:
                low = min(low, pen.bounds[1])
                high = max(high, pen.bounds[3])
        return high * size / self.upm, low * size / self.upm

    def outline(self, text: str, x: float, baseline: float, size: float, fill: str, tracking: float = 0.0) -> str:
        scale = size / self.upm
        pen = SVGPathPen(self.glyphs, ntos=lambda value: f"{value:.2f}".rstrip("0").rstrip("."))
        for name in self._names(text):
            self.glyphs[name].draw(TransformPen(pen, (scale, 0, 0, -scale, x, baseline)))
            x += self.glyphs[name].width * scale + tracking * size
        return f'<path d="{pen.getCommands()}" fill="{fill}"/>'


@lru_cache(maxsize=None)
def sora() -> Face:
    return Face("Sora[wght].ttf", wght=700)


@lru_cache(maxsize=None)
def inter(weight: int, size: int) -> Face:
    return Face("Inter[opsz,wght].ttf", wght=weight, opsz=max(14, min(32, size)))


def svg(width: int, height: int, body: str, label: str) -> str:
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" '
        f'viewBox="0 0 {width} {height}" role="img" aria-label="{label}">\n{body}\n</svg>\n'
    )


def mark(coin: str = MIST) -> str:
    cx, cy, radius = MARK_COIN
    return (
        f'<path d="{MARK_PEAKS}" fill="none" stroke="{AMBER}" stroke-width="{MARK_STROKE}" '
        f'stroke-linecap="round" stroke-linejoin="round"/>\n'
        f'<circle cx="{cx}" cy="{cy}" r="{radius}" fill="{coin}"/>'
    )


def tile(x: float, y: float, size: float, fill: str = NIGHT) -> str:
    scale = size / 512
    return (
        f'<g transform="translate({x:g} {y:g}) scale({scale:g})">\n'
        f'<rect width="512" height="512" rx="{TILE_RADIUS}" fill="{fill}"/>\n{mark()}\n</g>'
    )


def wordmark(x: float, centre: float, size: float, fill: str) -> tuple[str, float]:
    """The name set in Sora, its ink centred on `centre`. Returns the path and its width."""
    face = sora()
    above, below = face.extent(NAME, size)
    baseline = centre + (above + below) / 2
    tracking = -0.05
    return face.outline(NAME, x, baseline, size, fill, tracking), face.width(NAME, size, tracking)


def peaks(width: int, top: float, colour: str, step: int = 100) -> str:
    """A row of tent peaks running off both edges, the mark's own line repeated."""
    points = [(-40, top + 140)]
    x, up = 60, True
    while x <= width + step:
        points.append((x, top if up else top + 90))
        x, up = x + step, not up
    path = "M" + "L".join(f"{px:g} {py:g}" for px, py in points)
    return (
        f'<path d="{path}" fill="none" stroke="{colour}" stroke-width="30" '
        f'stroke-linecap="round" stroke-linejoin="round"/>'
    )


def chips(x: float, y: float, labels: list[str]) -> str:
    face, size, height, pad = inter(500, 18), 18, 40, 16
    parts = []
    for label in labels:
        width = face.width(label, size) + pad * 2
        parts.append(f'<rect x="{x:.1f}" y="{y}" width="{width:.1f}" height="{height}" rx="{height / 2}" fill="{NIGHT_RAISED}"/>')
        parts.append(face.outline(label, x + pad, y + 26.5, size, MIST))
        x += width + 10
    return "\n".join(parts)


def icon_svg() -> str:
    return svg(512, 512, tile(0, 0, 512), "Merchant")


def mark_svg(coin: str) -> str:
    return svg(512, 512, mark(coin), "Merchant")


def logo_svg(dark: bool) -> str:
    size, gap, height = 300, 96, 512
    text, width = wordmark(512 + gap, height / 2, size, MIST if dark else NIGHT)
    body = tile(0, 0, 512, NIGHT_RAISED if dark else NIGHT) + "\n" + text
    return svg(round(512 + gap + width + 8), height, body, "Merchant")


def wordmark_svg(dark: bool) -> str:
    size = 300
    above, below = sora().extent(NAME, size)
    height = round(above - below + 40)
    text, width = wordmark(8, height / 2, size, MIST if dark else NIGHT)
    return svg(round(width + 16), height, text, "Merchant")


def social_svg() -> str:
    text, _ = wordmark(232, 135, 124, MIST)
    body = "\n".join([
        f'<rect width="1200" height="630" fill="{NIGHT}"/>',
        peaks(1200, 580, INDIGO),
        peaks(1200, 520, AMBER),
        tile(88, 76, 118, NIGHT_RAISED),
        text,
        sora().outline(TAGLINE, 88, 268, 42, AMBER, -0.03),
        inter(500, 24).outline(DESCRIPTOR, 88, 326, 24, MIST_DIM),
    ])
    return svg(1200, 630, body, f"Merchant. {TAGLINE} {DESCRIPTOR}.")


def github_svg() -> str:
    text, _ = wordmark(88, 122, 112, MIST)
    body = "\n".join([
        f'<rect width="1280" height="640" fill="{NIGHT}"/>',
        text,
        sora().outline(PITCH[0], 88, 290, 40, MIST, -0.03),
        sora().outline(PITCH[1], 88, 338, 40, MIST, -0.03),
        inter(400, 23).outline(PROOF, 88, 388, 23, MIST_DIM),
        chips(88, 482, CHIPS),
        inter(600, 20).outline(REPOSITORY, 88, 566, 20, AMBER),
        tile(900, 150, 340, NIGHT_RAISED),
    ])
    return svg(1280, 640, body, f"Merchant. {' '.join(PITCH)} {PROOF}")


def readme_svg() -> str:
    text, width = wordmark(300, 150, 132, MIST)
    body = "\n".join([
        f'<rect width="1600" height="400" rx="28" fill="{NIGHT}"/>',
        '<clipPath id="card"><rect width="1600" height="400" rx="28"/></clipPath>',
        f'<g clip-path="url(#card)">{peaks(1600, 356, INDIGO)}{peaks(1600, 316, AMBER)}</g>',
        tile(96, 64, 172, NIGHT_RAISED),
        text,
        sora().outline(TAGLINE, 96 + 172 + 32 + width + 44, 162, 34, AMBER, -0.03),
        inter(500, 22).outline(DESCRIPTOR, 300, 252, 22, MIST_DIM),
    ])
    return svg(1600, 400, body, f"Merchant. {TAGLINE}")


def find_browser() -> Path:
    # Chrome first: Edge's headless mode writes the screenshot but can then fail to exit.
    candidates = [
        Path(r"C:\Program Files\Google\Chrome\Application\chrome.exe"),
        Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
        Path(r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
    ]
    candidates += [Path(found) for name in ("google-chrome", "chromium", "microsoft-edge") if (found := shutil.which(name))]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    sys.exit("No Edge or Chrome found to export PNGs. Run with --svg-only, or install one.")


def render(browser: Path, source: Path, target: Path, width: int, height: int) -> None:
    """Rasterises an SVG at its own pixel size with a transparent background."""
    from PIL import Image

    # The browser's helpers can hold profile files for a moment after it exits, so a file that
    # cannot be removed yet is left for the system's temp cleanup.
    with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as profile:
        command = [
            str(browser), "--headless=new", "--disable-gpu", "--hide-scrollbars",
            "--force-device-scale-factor=1", "--default-background-color=00000000",
            f"--user-data-dir={profile}", f"--window-size={width},{height}",
            f"--screenshot={target}", source.as_uri(),
        ]
        process = subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            process.wait(timeout=60)
        except subprocess.TimeoutExpired:
            # The browser runs helper processes; end the whole tree, not only the parent.
            if sys.platform == "win32":
                subprocess.run(["taskkill", "/F", "/T", "/PID", str(process.pid)], capture_output=True)
            else:
                process.kill()
            raise
    with Image.open(target) as image:
        if image.size != (width, height):
            sys.exit(f"{target.name}: expected {width}x{height}, the browser produced {image.size}.")


def export_icons(browser: Path) -> None:
    from PIL import Image

    large = OUT / "icon" / "merchant-1024.svg"
    large.write_text(svg(1024, 1024, tile(0, 0, 1024), "Merchant"), encoding="utf-8", newline="\n")
    master = OUT / "icon" / "merchant-1024.png"
    render(browser, large, master, 1024, 1024)
    large.unlink()

    with Image.open(master) as opened:
        source = opened.convert("RGBA")
    for size in ICON_SIZES[:-1]:
        source.resize((size, size), Image.Resampling.LANCZOS).save(OUT / "icon" / f"merchant-{size}.png")
    source.save(OUT / "icon" / "merchant.ico", sizes=[(size, size) for size in ICO_SIZES])
    TILL_ASSETS.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(OUT / "icon" / "merchant.ico", TILL_ASSETS / "merchant.ico")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--svg-only", action="store_true", help="write the SVGs and skip the PNG and ICO exports")
    arguments = parser.parse_args()

    (OUT / "icon").mkdir(parents=True, exist_ok=True)
    vectors = {
        "merchant-icon.svg": icon_svg(),
        "merchant-mark.svg": mark_svg(NIGHT),
        "merchant-mark-dark.svg": mark_svg(MIST),
        "merchant-logo.svg": logo_svg(dark=False),
        "merchant-logo-dark.svg": logo_svg(dark=True),
        "merchant-wordmark.svg": wordmark_svg(dark=False),
        "merchant-wordmark-dark.svg": wordmark_svg(dark=True),
        "merchant-social.svg": social_svg(),
        "merchant-github-social.svg": github_svg(),
        "merchant-readme-banner.svg": readme_svg(),
    }
    for name, content in vectors.items():
        (OUT / name).write_text(content, encoding="utf-8", newline="\n")
    print(f"wrote {len(vectors)} SVGs to {OUT.relative_to(ROOT)}")

    if arguments.svg_only:
        return

    browser = find_browser()
    for name, (width, height) in {
        "merchant-social": (1200, 630),
        "merchant-github-social": (1280, 640),
        "merchant-readme-banner": (1600, 400),
    }.items():
        render(browser, OUT / f"{name}.svg", OUT / f"{name}.png", width, height)
    export_icons(browser)
    print(f"exported PNGs and merchant.ico with {browser.name}")


if __name__ == "__main__":
    main()
