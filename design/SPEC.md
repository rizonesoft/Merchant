# Merchant interface contract

This document is the contract for Merchant's interface. The till is finished when every item here works as written, in all three looks, and each screen matches its reference page. Reviewed 2026-11-17.

## 1. Sources and precedence

- The design pages in `design/interface/<look>/<screen>.html` are the exact source for layout, size, colour, type and copy. Each has a 1280 x 800 reference render beside it (`<screen>.png`), made by `python scripts/render-design.py`.
- This document is the source for behaviour: what each control does, its states, its refusals, and everything the pages do not draw (section 7).
- Where a page and this document disagree on appearance, the page wins. Where they disagree on behaviour, this document wins. A disagreement found while building is filed in `todo/findings.md`, never settled silently.
- The brand (mark, colours, typefaces) comes from `assets/brand` and `assets/fonts`, built by `python scripts/build-brand.py`.

## 2. Three looks, one till

Merchant ships three looks. They are three sets of views over the same view models, so every behaviour in sections 5 to 7 is identical in each.

- `daylight` is the default: light and high contrast for direct sun, with top tabs and large rows.
- `counter`: dark, with a side rail, product tiles and a cart panel.
- `tape`: dark, with a paper column on the left (the live receipt, the day summary, the pairing slip) and controls on the right, navigation along the bottom.
- The look is a setting of the till, not of the shop: two tills in one shop may differ. It is chosen in Settings (`SET-19`), applies at once without a restart, and is kept across restarts. A till that has never chosen uses `daylight`.
- No behaviour, text meaning, validation rule or data differs between looks. Where a look draws a control another look lacks (for example the Tape quantity keys), sections 5 and 6 say so by name.

## 3. What "1:1" means

- The reference window is 1280 x 800. At that size each screen's layout matches its page: every position, size, gap, radius, border, colour, typeface, weight and letter spacing is the value in the page's CSS.
- All copy is verbatim from the pages, including capitalisation. Sample values (shop name, products, amounts, times) are data, shown by the fixture in section 9.
- Type: Sora for headings and totals, Inter for everything else, JetBrains Mono for paper columns and Tape amounts. The three font files in `assets/fonts` are embedded in the till, so no screen depends on fonts installed on the machine.
- Every tappable control is at least 44 x 44 px. Text contrast is at least 4.5:1, or 3:1 at 24 px and above.
- Above 1280 x 800 the content columns grow and fixed-width panels (cart, paper column, side panels) keep their width. The minimum window is 1024 x 700; below 1280 wide the product grids drop one column and tables keep their columns with the Items column truncating.
- Each screen in each look has a headless render test at 1280 x 800 with the section 9 fixture, written to `build/screens/<look>/<screen>.png`. The test compares the render with the reference through the test helper `ScreenComparer`: after a 2 px blur, at most 6% of pixels may differ by more than 40 of 255 in any channel. A screen that fails is not done, and the limits are constants no screen may loosen.
- The independent review of every surface section looks at the render and the reference side by side.

## 4. Tokens

Each look's colours are named resources, so no view holds a literal colour. The values:

| Token | Daylight | Counter | Tape |
| --- | --- | --- | --- |
| Ground | `#F5F6FB` | `#0B1028` | `#101635` |
| Surface | `#FFFFFF` | `#141B3F` | `#0B1028` |
| Raised | `#FFFFFF` | `#1E2757` | `#1E2757` |
| Line | `#D6D9E8` | `#232C5C` | `#232C5C` |
| Control border | `#8E94B5` | `#2F3A75` | `#3A478C` |
| Text | `#101635` | `#F5F6FB` | `#F5F6FB` |
| Dim text | `#4A5072` | `#AEB4D6` | `#AEB4D6` |
| Primary action fill | `#101635` | `#FFB000` | `#FFB000` |
| Primary action text | `#FFFFFF` | `#101635` | `#101635` |
| Accent | `#FFB000` | `#FFB000` | `#FFB000` |
| Selected row | `#FFF3D1` | `#1E2757` | `#FFDB85` (on paper) |
| Good | `#0B8A58` | `#3DDC97` | `#3DDC97` |
| Danger | `#B3261E` | `#FF8A8A` | `#FF8A8A` |
| Paper | not used | `#F5F6FB` | `#F5F6FB` |
| Paper ink | not used | `#101635` | `#101635` |

- Tape key colours, also the default per category in creation order: `#FFB000` amber, `#8FD0FF` blue, `#FFA98A` coral, `#CDBEFF` lilac. Key text is `#101635`.
- Radii: Daylight controls 10 px and cards 14 px; Counter controls 12 px and cards 16 px; Tape controls 12 px, panels 16 px and keys 14 px.
- Money is shown as the currency symbol, a space, and the amount with two decimals and a space between thousands (`R 3 864.00`). Paper columns and Tape keys show the amount without the symbol, except the receipt total line in Settings.

## 5. Screens and controls

Each control has an ID. TODO sections and tests cite these IDs.

### 5.1 Shell (every screen except Welcome, Payment and Sale complete)

- `SH-01` Navigation with six destinations in this order: Sell, Sales, Products, Day end, Tills, Settings. The current one is marked. Counter: side rail with icon above label, Settings at the bottom. Daylight: top tabs with an amber underline. Tape: bottom bar with icon beside label.
- `SH-02` Shop name and this till's name. Counter: top bar of Sell. Daylight: right of the tab bar. Tape: the paper header on Sell.
- `SH-03` Sync status with a dot and text. States: synced (`2 tills in range, synced 12:41`; Daylight shows `Synced 12:41`), nobody in range (`No other tills in range`, hollow dot), syncing (`Syncing...`), problem (`Sync problem`, accent colour, with the reason on the Tills screen). Tapping it opens Tills.
- `SH-04` Today's takings pill, Counter Sell only: `Today R 3 864.00 in 42 sales`, all tills.
- `SH-05` The Merchant mark. Decorative, not a button.
- `SH-06` Notice bar: a one-line message across the top of the content area for results and refusals (for example `Added Flat white.`). It shows for 5 seconds or until the next action, and a refusal stays until dismissed or corrected. Looks: a full-width strip, 48 px high, Surface fill, Line border, text in Text colour, or Danger for a refusal.

### 5.2 Welcome (`welcome`), shown only while the till belongs to no shop

- `WEL-01` Shop name field and `Create shop`. The name is required and at most 40 characters. Creating makes a new shop key, records the shop settings, and opens Sell.
- `WEL-02` Pairing code field and `Join shop`. The code is 32 hexadecimal digits; spaces, dashes and case are ignored. An incomplete code is refused with `That pairing code is not complete. It has 32 letters and digits.` Joining stores the key, starts sync and opens Sell; the shop's name and products arrive with the first sync.
- `WEL-03` Restore. Counter: `Restore from a backup file` opens the file picker, then the passphrase dialog `X-07`. Daylight and Tape: a passphrase field and `Choose backup file`. A wrong passphrase is refused with `Wrong passphrase, or the file is not a Merchant backup.` A good restore opens Sell.
- `WEL-04` Brand panel: mark, name, strapline and version. Tape draws it as a paper slip.

### 5.3 Sell (`sell`)

- `SELL-01` Search field. Filters products by name as you type, ignoring case. No match shows `No products match "<text>".`
- `SELL-02` Category filter. Counter: chips, `All` first. Daylight: a side list with a count per category. Tape has no category filter; its keys are coloured by category with the legend under the grid.
- `SELL-03` Product button (Counter tile, Daylight row, Tape key) showing name and price. A tap adds one to the cart, or raises that product's line by one. On Tape it adds the `SELL-10` quantity.
- `SELL-04` Cart lines: quantity, name, line total. Counter and Daylight: the cart panel. Tape: lines on the paper.
- `SELL-05` Line minus and plus. Counter and Daylight: on every line. Tape: for the selected line (`SELL-09`). Minus at quantity 1 removes the line.
- `SELL-06` `Clear` (Tape: `Clear sale`). Asks `X-01` before emptying a cart that has lines.
- `SELL-07` Totals: item count with the VAT amount included, and the total.
- `SELL-08` Pay buttons, disabled while the cart is empty. Counter and Tape: `Cash` and `Card`, each opening Payment with that method chosen. Daylight: `Take payment`, opening Payment on Cash.
- `SELL-09` Tape only: tapping a paper line selects it; the selected-line strip shows its name with minus, quantity, plus and `Remove line`. With no line selected the strip reads `Tap a line on the receipt to change it.`
- `SELL-10` Tape only: `Next key adds` with `x1` to `x5`. The choice applies to the next product key, then returns to `x1`.
- `SELL-11` Empty cart text in the cart area: `Tap a product to start a sale.`
- `SELL-12` Empty catalogue: the product area shows `No products yet.` and a primary button `Add your first product` that opens Products with the editor empty.
- `SELL-13` A product that counts stock shows nothing extra here while stock is above 3; at 3 or less its button shows `3 left` (or `None left`) in the accent colour. Selling at zero is allowed and the count goes negative, because the till must never block a sale.

### 5.4 Payment (`payment`)

- `PAY-01` `Back to cart` (Daylight and Tape: `Back to the sale`). Returns to Sell with the cart unchanged.
- `PAY-02` The order: lines, VAT included, amount due. Counter: an Order card. Daylight: the amount in the header band. Tape: the paper, which also gains the Cash and CHANGE lines as cash is entered, and reads `Not saved yet` at the foot.
- `PAY-03` Method: `Cash` or `Card on the reader`. Counter and Daylight: a two-part switch. Tape: the screen is the cash screen with `Pay by card instead`.
- `PAY-04` Cash entry. Counter and Tape: a keypad (digits, decimal point, delete) and four quick amounts: the exact amount, the next multiple of 10 above it, the next multiple of 100 above it and the one after (for 155.00: 155, 160, 200, 300; for 200.00: 200, 210, 300, 400; duplicates are dropped). Daylight: note and coin buttons `R 200` to `R 1` that add to the amount received, `Exact amount`, `Type an amount` (opens `X-03`) and `Start again` (back to zero).
- `PAY-05` Received and change. While received is less than due the change box reads `Still to pay` with the shortfall, and `PAY-06` is disabled. Daylight also lists what was tapped (`1 x R 200`).
- `PAY-06` Complete (Counter `Complete sale`, Daylight `Finish sale`, Tape `Save sale, give R 45.00 change`). Records the sale, lowers counted stock, prints the receipt and opens the drawer as settings say, and opens Sale complete. The sale is on disk before anything is printed.
- `PAY-07` Card. Choosing card replaces the cash entry with `Take R 155.00 on your card reader, then confirm.`, a primary button `Card payment approved` and `Back to cash`. Approving records the sale as paid by card. Merchant does not talk to the reader.
- `PAY-08` Sale number and till name.

### 5.5 Sale complete (`sale-complete`)

- `DONE-01` Confirmation with the sale number, method and time, and the sentence that it is stored on this till and shared with other tills when they are in range.
- `DONE-02` Change to give, cash sales only. A card sale shows `Paid by card` with the amount in its place.
- `DONE-03` `New sale` (Daylight: `Next customer`). Opens Sell with an empty cart. It has keyboard focus, so Enter starts the next sale.
- `DONE-04` `Print receipt again` (Tape: `Print again`). A reprint here or from Sales (`SALES-05`) uses the current shop and paper settings and never opens the cash drawer.
- `DONE-05` `Refund this sale`. Opens `X-02`.
- `DONE-06` Printer line: `Receipt printed on <printer address>.` with `The cash drawer is open.` on a cash sale; or `The receipt did not print: <reason>.`; or `Receipt printing is off.`
- `DONE-07` Receipt preview. Counter: a paper receipt on the right. Tape: the paper column, ending in a torn edge. Daylight shows none.

### 5.6 Products (`products`)

- `PROD-01` Search field, as `SELL-01`.
- `PROD-02` `Add product`. Opens the editor empty, titled `New product`.
- `PROD-03` Product list; choosing a product opens it in the editor. Counter: a table of name, category, price and stock (`Not tracked`, a count, or `3 low` in the accent colour at 3 or less). Daylight: one card per category with name, stock left and price. Tape: the paper price list, names with the count in brackets.
- `PROD-04` Editor fields: Name (required, at most 30 characters); Category (a list of the shop's categories, ending with `New category...`, which opens `X-05`); Price including VAT (accepts `45`, `45.5`, `45,50`; refuses negatives, more than two decimals and thousands separators with `Enter a price such as 45 or 45.50.`); `Track stock for this product` (Daylight and Tape: `Count stock for this product`); `In stock now` (a whole number of 0 or more, enabled only while stock is counted). Counter: a side panel. Daylight: a dialog over the list. Tape: the right-hand panel.
- `PROD-05` `Save` and `Cancel`. Save records the product and shows it at once on Sell on every till after sync. Cancel discards the edit.
- `PROD-06` `Remove from the till` (Daylight: `Remove`). Asks `X-04`. The product leaves Sell and Products; past sales keep its name and price.
- `PROD-07` Daylight only: `Add category`, opening `X-05`.
- `PROD-08` Tape only: four key-colour swatches and a preview of the key. Other looks keep the product's colour without showing it. A new product takes its category's default colour.
- `PROD-09` Empty state: `No products yet. Add the first thing you sell.`

### 5.7 Sales and refunds (`sales`)

- `SALES-01` Day filter: `Today`, `Yesterday`, `Pick a day` (opens `X-06`).
- `SALES-02` Till filter. Counter and Tape: `All tills`, a toggle that is on by default. Daylight: `This till only`, off by default. Both mean the same thing.
- `SALES-03` Sales list, newest first: sale number, time, items in short (`2 Flat white, Toasted cheese, Brownie`), method, total. A refunded sale shows `Refunded` in the Danger colour and its total struck through. With all tills shown, the till name follows the sale number when two tills share a number.
- `SALES-04` Detail of the chosen sale: lines, VAT, total, cash received and change, date, time and till. Counter: a side panel. Daylight: the row opens in place. Tape: the paper column as a receipt.
- `SALES-05` `Print receipt again`.
- `SALES-06` `Refund this sale`. Opens `X-02`. For a refunded sale the button is replaced by `Refunded at 11:52 on Till 1.`
- `SALES-07` Summary of the day shown: number of sales, takings, refunds. Daylight: beside the heading. Tape: a row above the list. Counter shows none.
- `SALES-08` Empty state: `No sales on this day.`

### 5.8 Day end (`day-end`)

- `DAY-01` Heading with the date. Figures in `DAY-02`, `DAY-08` and `DAY-09` cover all tills as far as this till has synced; the cash drawer figures in `DAY-03` to `DAY-05` cover this till only, because the drawer belongs to one till.
- `DAY-02` Takings, cash, card and VAT included, with the number of sales and refunds. Counter: four cards. Tape: the paper summary. Daylight: step 2.
- `DAY-03` Expected cash: the float, plus this till's cash sales, minus this till's cash refunds. The float row is a button that opens `X-08`.
- `DAY-04` Counted cash. Counter: a field. Daylight: steppers for `R 200`, `R 100`, `R 50`, `R 20` and `R 10` notes, and a `Coins` row that opens `X-03`. Tape: a keypad.
- `DAY-05` Difference: `Short by` or `Over by` with the amount in the accent colour, or `The drawer balances` in the Good colour.
- `DAY-06` `Close the day`. Asks `X-09`, records the close with the expected and counted cash and the float left in the drawer, prints the summary when a printer is set, and shows `Day closed at 17:42.` in place of the button. Daylight walks three steps: `Count the cash`, `Check the totals` (the `DAY-02`, `DAY-08` and `DAY-09` figures, with `Back`), `Close and print` (the confirmation and the closed state).
- `DAY-07` `Print day summary` (Tape: `Print the summary`). Works before and after closing.
- `DAY-08` Sold today: the six products with the highest takings, each with quantity and amount, then `Everything else` (Tape: `Other`).
- `DAY-09` Sales per till, and `not synced since <when>` for a till that has been away.
- `DAY-10` A day with no sales shows zeros and still closes.

### 5.9 Tills and sync (`tills`)

- `TILL-01` Every till of the shop: name, state (`This till`, `In range`, `Out of range`), last sync, sales today and device name. In range has a solid Good dot; out of range a hollow one.
- `TILL-02` `Sync now`. Runs a sync at once and reports through `SH-03` and the notice bar.
- `TILL-03` Rename this till. Counter: a field and `Save name`. Daylight and Tape: `Rename`, opening `X-10`. The name is at most 30 characters and reaches other tills by sync.
- `TILL-04` Pairing code. It is hidden when the screen opens, behind `Show the code`; shown, it reads in groups of four with `Copy code` (Daylight and Tape: `Copy the code`) and `Hide code`. The pages draw the shown state.
- `TILL-05` Move this till to another shop: a pairing code field and `Join that shop`. Asks `X-11`.
- `TILL-06` The explanation of how tills sync, as drawn.
- `TILL-07` Tape only: `Print this slip`, printing the pairing slip on the receipt printer.
- `TILL-08` A sync problem shows under the list: `Sync problem with <till>: <reason>.`

### 5.10 Settings (`settings`)

- `SET-01` Shop name, printed on receipts. Required, at most 40 characters.
- `SET-02` Currency symbol, at most 4 characters.
- `SET-03` VAT rate in percent, 0 to 100 with up to two decimals. 0 hides every VAT line.
- `SET-04` `Prices already include VAT`. On: totals are the sum of prices and VAT is the part inside them. Off: VAT is added on top and shown as its own line before the total.
- `SET-05` Line at the bottom of receipts, at most 32 characters.
- `SET-06` `Save shop settings`. Shop settings sync to every till.
- `SET-07` Printer address on the network (`192.168.1.50` or `192.168.1.50:9100`). Empty turns printing off.
- `SET-08` Paper width: `58 mm (32 characters)` or `80 mm (48 characters)`.
- `SET-09` `Print a receipt after every sale`.
- `SET-10` `Open the cash drawer on cash sales`.
- `SET-11` `Save printer`. Printer settings stay on this till.
- `SET-12` `Print a test receipt`. Reports `Test receipt sent to <address>.` or the failure.
- `SET-13` Backup passphrase, required for both backup actions.
- `SET-14` `Save a backup`. Opens the save picker and writes one encrypted file.
- `SET-15` `Restore a backup`. Opens the file picker and adds the backup's sales to this till.
- `SET-16` Last backup time, or `No backup yet.`
- `SET-17` About: version, this till's device name, data folder (Counter) and licence.
- `SET-18` `Open the data folder`.
- `SET-19` Look. Not drawn on the pages: a fifth card `Look` after About, spanning both columns, with three choices side by side (`Daylight`, `Counter`, `Tape`), each a button 180 x 112 px showing a miniature of that look's Sell screen above its name. The current look has the Primary action border. Choosing applies at once.
- `SET-20` Tape only: the paper column previews the receipt with the current shop name, currency, VAT and footer as they are typed.

## 6. Look-specific layout notes

- Welcome, Payment and Sale complete have no navigation in any look: they are steps, not destinations.
- Daylight Payment replaces the tab bar with the amount band. Counter Payment keeps a plain top bar. Tape Payment keeps the paper column and drops the bottom bar.
- Daylight Products draws the editor as a dialog; the reference page shows it open over the list.
- Tape uses its paper column on every screen: live receipt (Sell, Payment), printed receipt (Sale complete, Sales), price list (Products), day summary (Day end), pairing slip (Tills), receipt preview (Settings), welcome slip (Welcome).

## 7. States the pages do not draw

Dialogs use the look's card style, centred over a scrim of the Ground colour at 55% (`rgba(16, 22, 53, 0.55)` in Daylight), 480 px wide unless stated, with the title in the heading face at 24 px, body text, and buttons right-aligned with the safe choice first. Escape and the scrim cancel; Enter takes the primary action unless it destroys something.

- `X-01` Clear the sale. `Clear this sale?` with `Keep the sale` and `Clear it` (Danger).
- `X-02` Refund. `Refund sale 0042?` with the total, the method, and `The money goes back the way it was paid. The original sale stays on record.` Buttons `Keep the sale` and `Refund R 155.00` (Danger). Refunding records the refund, returns counted stock, opens the drawer for a cash refund, and prints a refund receipt.
- `X-03` Type an amount. A keypad dialog 360 px wide with the amount, digits, decimal point, delete, `Cancel` and `Use this amount`.
- `X-04` Remove a product. `Remove Flat white from the till?` with `Past sales keep it.` Buttons `Keep it` and `Remove` (Danger).
- `X-05` New category. A name field (required, at most 20 characters, unique ignoring case) with `Cancel` and `Add category`.
- `X-06` Pick a day. A month calendar; days with sales are bold; future days are disabled. Choosing a day closes the dialog.
- `X-07` Backup passphrase (Counter Welcome restore). A passphrase field with `Cancel` and `Restore`.
- `X-08` Float. `Float in the drawer this morning` with an amount field and `Cancel`, `Save float`.
- `X-09` Close the day. `Close Tuesday 17 November?` with the takings, the counted cash and the difference, a field `Float left in the drawer for tomorrow` (default: the counted cash), and `Not yet`, `Close the day`.
- `X-10` Rename this till. A name field with `Cancel` and `Save name`.
- `X-11` Join another shop. `Move this till to another shop?` with `Sales already on this till go with it and will sync to that shop.` Buttons `Stay in this shop` and `Join that shop` (Danger).
- `X-12` A failure that stops the till from opening its data (a missing or damaged key) is a full-screen message with the reason and `Restore from a backup file`, never a silent new empty shop.

Other states:

- Every list has the empty text given in section 5. Every field refusal appears under its field in the Danger colour and in the notice bar, and keeps the typed value.
- Buttons whose action cannot run are disabled, never hidden. A disabled button is the same shape at 45% opacity.
- A printing failure never undoes or blocks a sale, a refund or a day close.

## 8. Data the interface needs

The till records facts as events (see `README.md`). The interface needs these, each with the fields named. Older tills ignore events they do not know.

- `shop.settings`: shop name, currency symbol, VAT rate, prices include VAT, receipt footer. The newest wins.
- `category.defined`: category id, name, position.
- `product.defined`: product id, name, price in cents, category id, key colour, counts stock. The newest for an id wins. This extends the event the till already records.
- `product.removed`: product id.
- `stock.adjusted`: product id, the new count, and the reason (`set` from the editor).
- `sale.completed`: sale id, sale number, till name, lines (product id, name, quantity, unit price in cents), total in cents, VAT in cents, method (`cash` or `card`), cash received in cents, change in cents. This extends the event the till already records.
- `sale.refunded`: refund id, sale id, total in cents, method, till name.
- `day.closed`: date, till, expected cash, counted cash, float left, takings by method.
- `till.named`: device id, name.

Kept on the till only, never synced: the look, the printer settings, the float for today, the last backup time, and the pairing code's shown or hidden state.

- Counted stock for a product is the last `stock.adjusted` count, minus units sold, plus units refunded, since that adjustment.
- Takings, and the cash and card figures, count every completed sale, including one refunded later. Refunds are reported beside them as a count and an amount, and a cash refund leaves the drawer through the expected cash of `DAY-03`. So the fixture's takings of 3 864.00 include sale 0038, and its drawer holds 500.00 + 2 140.00 - 63.00.
- Sale numbers count up per till from 0001 and never restart.
- A day is the till's local calendar day.

## 9. The reference fixture

The pages show one shop on one moment. The same data, built in tests by one fixture, drives every render test:

- Shop `Dune Coffee Co.`, currency `R`, VAT 15% included, footer `Thank you`. This till is `Till 1, counter`; `Till 2, window hatch` is in range and synced at 12:41; `Till 3, market stall` last synced the day before at 17:05. The clock reads Tuesday 17 November 2026, 12:41.
- Categories and products: Coffee (Flat white 35.00, Cappuccino 38.00, Americano 30.00, Cortado 34.00, Chai latte 42.00, Rooibos tea 25.00); Cold drinks (Iced latte 45.00, Still water 18.00, stock 24); Food (Toasted cheese 55.00, stock 14); Bakes (Croissant 32.00, stock 3; Brownie 30.00, stock 9; Muffin 28.00, stock 12).
- The cart and sale 0042: 2 Flat white, 1 Toasted cheese, 1 Brownie; total 155.00, VAT 20.22; cash 200.00, change 45.00.
- The day: 42 sales and 1 refund (sale 0038, 63.00, cash); takings 3 864.00, cash 2 140.00 in 24 sales, card 1 724.00 in 18 sales, VAT 504.00; 27 sales on Till 1 and 15 on Till 2; float 500.00; counted 2 570.00.
- The pages show the day's cash figures for the whole shop in the drawer sums. The fixture puts every cash sale and the refund on Till 1 so the drawer sums on the pages hold under `DAY-01`.

## 10. Out of scope

Not part of this contract, and not drawn: barcode scanning, discounts, split payments, tips, customer accounts, card reader integration, Bluetooth and USB printers, and phone-sized layouts. Each is new work with its own design.
