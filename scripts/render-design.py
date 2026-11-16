#!/usr/bin/env python3
"""Render every interface design page in design/interface to a PNG beside it.

The HTML pages are the source of truth for the interface contract (design/SPEC.md). The PNGs are
their 1280x800 reference renders: what a built screen is compared against.

    python scripts/render-design.py            render every page
    python scripts/render-design.py daylight   render one look

Needs Chrome or Edge. Fonts load from assets/fonts, so no network is used.
"""
from __future__ import annotations

import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DESIGN = ROOT / "design" / "interface"
WIDTH, HEIGHT = 1280, 800


def find_browser() -> Path:
    # WORKAROUND: Edge's headless mode writes the screenshot but can then fail to exit, so Chrome is tried first.
    candidates = [
        Path(r"C:\Program Files\Google\Chrome\Application\chrome.exe"),
        Path(r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
        Path(r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
    ]
    candidates += [Path(found) for name in ("google-chrome", "chromium", "microsoft-edge") if (found := shutil.which(name))]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    sys.exit("No Chrome or Edge found to render the design pages.")


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as handle:
        header = handle.read(24)
    return struct.unpack(">II", header[16:24])


def render(browser: Path, page: Path) -> None:
    target = page.with_suffix(".png")
    # The browser's helpers can hold profile files for a moment after it exits, so a file that
    # cannot be removed yet is left for the system's temp cleanup.
    with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as profile:
        command = [
            str(browser), "--headless=new", "--disable-gpu", "--hide-scrollbars",
            "--force-device-scale-factor=1", "--allow-file-access-from-files",
            f"--user-data-dir={profile}", f"--window-size={WIDTH},{HEIGHT}",
            "--virtual-time-budget=4000", f"--screenshot={target}", page.as_uri(),
        ]
        process = subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            process.wait(timeout=90)
        except subprocess.TimeoutExpired:
            # The browser runs helper processes; end the whole tree, not only the parent.
            if sys.platform == "win32":
                subprocess.run(["taskkill", "/F", "/T", "/PID", str(process.pid)], capture_output=True)
            else:
                process.kill()
            raise
    if png_size(target) != (WIDTH, HEIGHT):
        sys.exit(f"{target}: expected {WIDTH}x{HEIGHT}, the browser produced {png_size(target)}.")


def main() -> None:
    looks = sys.argv[1:] or sorted(p.name for p in DESIGN.iterdir() if p.is_dir())
    browser = find_browser()
    count = 0
    for look in looks:
        pages = sorted((DESIGN / look).glob("*.html"))
        if not pages:
            sys.exit(f"No design pages in {DESIGN / look}.")
        for page in pages:
            render(browser, page)
            count += 1
    print(f"rendered {count} pages with {browser.name}")


if __name__ == "__main__":
    main()
