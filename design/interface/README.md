# Interface design pages

One HTML page and one 1280 x 800 PNG per screen, for each of Merchant's three looks. The pages are the exact source for layout, size, colour, type and copy; [`../SPEC.md`](../SPEC.md) is the source for behaviour.

| Folder | Look |
| --- | --- |
| `daylight` | Light and high contrast, top tabs. The default. |
| `counter` | Dark, side rail, product tiles and a cart. |
| `tape` | Dark, a paper column on the left, keys on the right, navigation along the bottom. |

Each folder holds the same nine screens: `welcome`, `sell`, `payment`, `sale-complete`, `products`, `sales`, `day-end`, `tills`, `settings`.

- Open a page in a browser to inspect it. Fonts load from `assets/fonts` through `fonts.css`, so no network is needed.
- After changing a page, run `python scripts/render-design.py` (or `python scripts/render-design.py daylight` for one look) to refresh its PNG.
- All pages show the same shop at the same moment. That data is described in section 9 of the contract.
