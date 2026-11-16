# Merchant brand

Everything in this folder is built by `python scripts/build-brand.py` from one description of the mark, colours and type. Change the script and rebuild; do not edit these files by hand.

## The mark

One amber line draws an M that is also two pop-up tent peaks, under a coin, on a night-blue tile.

| File | Use |
| --- | --- |
| `merchant-icon.svg` | The app icon: the mark on its tile |
| `merchant-mark.svg`, `merchant-mark-dark.svg` | The mark alone, for light and dark grounds |
| `merchant-logo.svg`, `merchant-logo-dark.svg` | Icon and name side by side |
| `merchant-wordmark.svg`, `merchant-wordmark-dark.svg` | The name alone |
| `merchant-readme-banner.svg`, `.png` | The banner at the top of the README (1600 x 400) |
| `merchant-github-social.svg`, `.png` | The repository's social preview (1280 x 640) |
| `merchant-social.svg`, `.png` | Link previews elsewhere (1200 x 630) |
| `icon/merchant-<size>.png`, `icon/merchant.ico` | The icon from 16 to 1024 px, and the Windows icon |

Text in the SVGs is converted to outlines, so they look the same on any machine.

## Colours

| Name | Value | Use |
| --- | --- | --- |
| Night | `#101635` | Tile, dark grounds, text on light |
| Night raised | `#1E2757` | Tile on a dark ground, raised surfaces |
| Amber | `#FFB000` | The mark's line, primary actions, accents |
| Indigo | `#3A43A8` | Secondary line in banners, links on light |
| Mist | `#F5F6FB` | The coin, light grounds, text on dark |

## Type

Sora 700 for the name and headings, Inter for text, JetBrains Mono for receipts. The fonts are in `assets/fonts` under the SIL Open Font License.

## Rules

- Keep clear space around the icon of at least a quarter of its width.
- Do not recolour the mark, stretch it, or place the amber line on amber or the coin on white.
- On a dark ground use the `-dark` files.
