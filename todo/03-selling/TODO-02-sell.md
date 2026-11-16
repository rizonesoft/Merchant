---
schema_version: 1
id: sell
domain: 03-selling
status: active
title: "TODO-02 -- Sell screen"
depends_on: ["looks-and-shell"]
---

# TODO-02 -- Sell screen

> **Goal:** The operator finds products, builds a sale and moves to payment, in all three looks.

> **Current state:** Measured 2026-11-17. `MainViewModel` holds a flat product list, a cart with add only, and cash and card buttons that record a sale at once. There is no search, category filter, quantity change, clear confirmation or stock hint.

## Inputs

- `design/SPEC.md` section 5.3 (`SELL-01` to `SELL-13`) and `X-01`.
- `design/interface/<look>/sell.html` and `.png`.

## Outcome

- With the fixture, each look's Sell screen matches its reference and builds sale 0042's cart by taps.
- An empty shop shows the empty catalogue state with its button.

**Adjacency:** list=applicable; document=not-applicable (the receipt is printed at payment, D03 T03 §2); settings=not-applicable (VAT and currency come from shop settings, D05 T02 §1); reporting=not-applicable (no figures of its own: the day's figures belong to Day end); notifications=not-applicable (results and refusals show in place); permissions=not-applicable (no user accounts); audit=not-applicable (a cart is not a record until it is paid); exchange=not-applicable (nothing to import or export here); reverse=applicable

**Adjacency rationale:** Search and the category filter are how a product is found without scrolling. Every cart action has its undo before payment: minus, remove line and clear with confirmation.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Catalogue: search, categories and stock hints | -- | [ ] |
| 2 | §2 | Cart: lines, quantities, totals and clearing | §1 | [ ] |
| 3 | §3 | Daylight look: Sell screen | §2 | [ ] |
| 4 | §4 | Counter look: Sell screen | §2 | [ ] |
| 5 | §5 | Tape look: Sell screen | §2 | [ ] |
| 6 | §6 | Application startup on the shell | §3, T01 §2 | [ ] |

## 1. Catalogue: search, categories and stock hints

The product side of Sell (`SELL-01`, `SELL-02`, `SELL-12`, `SELL-13`).

**Phase:** 3
**Needs:** none
**Surface:** no

**Build order:**

1. Add `SellViewModel` to `src/Merchant.Till/ViewModels` with the catalogue. Done when: search and category narrow the product list.

- [ ] Add `src/Merchant.Till/ViewModels/SellViewModel.cs` with `Search`, `Categories` (with `All` first and a count each), `SelectedCategory` and `Products` (name, price text, key colour, stock hint), rebuilt from `ShopState` when the store changes. Done when: typing `fl` leaves Flat white, and choosing Bakes leaves three products.
- [ ] Set the stock hint to `3 left` at three or less and `None left` at zero or below, and nothing otherwise or when stock is not counted. Done when: Croissant shows `3 left` in the fixture.
- [ ] Expose `NoMatchText` (`No products match "<text>".`) and `IsCatalogueEmpty` with `AddFirstProductCommand` asking the shell for Products with an empty editor. Done when: an empty shop sets the flag and the command makes that one request.
- [ ] Register `SellViewModel` as the Sell destination through `ShellViewModel.RegisterDestination` in `src/Merchant.Till/App.axaml.cs`. Done when: navigating to Sell from the shell shows the screen.
- [ ] Add `tests/Merchant.Tests/SellCatalogueTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SellCatalogueTests"` passes.
- [ ] Commit: `sell: catalogue view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SellCatalogueTests"` passes; a case-sensitive search makes `Search_ignores_case` fail.

## 2. Cart: lines, quantities, totals and clearing

The sale side of Sell (`SELL-03` to `SELL-11`, `X-01`).

**Phase:** 3
**Needs:** none
**Surface:** no

**Build order:**

1. Add the cart to `SellViewModel`. Done when: the fixture cart totals 155.00 with 20.22 VAT.

- [ ] Add `Cart` lines with `AddCommand(product)`, per-line `PlusCommand` and `MinusCommand` (removing at one), and `Totals` (item count, VAT text, total) computed through `SaleBuilder.Totals` from the shop settings. Done when: two Flat white, one Toasted cheese and one Brownie give `4 items, VAT included`, `R 20.22` and `R 155.00`. Cheaper substitute: summing prices in the view model without `SaleBuilder.Totals` must fail the VAT-added case.
- [ ] Add `NextKeyQuantity` (1 to 5) applied by the next `AddCommand` and then reset to 1. Done when: x3 then Americano adds three, and the next key adds one.
- [ ] Add `SelectedLine`, `SelectLineCommand` and `RemoveLineCommand`. Done when: selecting then removing leaves the other lines.
- [ ] Add `ClearCommand` that opens `X-01` through the dialog host when the cart has lines and empties it only on `Clear it`. Done when: choosing `Keep the sale` leaves the cart.
- [ ] Add `PayCashCommand`, `PayCardCommand` and `TakePaymentCommand`, disabled on an empty cart, each calling `ISaleFlow.ShowPayment(cart, method)` (an interface in `src/Merchant.Till/ViewModels/ISaleFlow.cs` that D03 T03 §1 implements). Done when: an empty cart cannot execute any of the three, and a filled one calls the flow once with the chosen method.
- [ ] Add `tests/Merchant.Tests/SellCartTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SellCartTests"` passes.
- [ ] Commit: `sell: cart view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SellCartTests"` passes; removing the confirmation makes `Clear_asks_first` fail.

## 3. Daylight look: Sell screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/sell.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/sell.html` under `design/SPEC.md` section 3
**Job:** Ring up what the customer asked for, fast
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** the Daylight shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/SellView.axaml` from `design/interface/daylight/sell.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSellTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/SellView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/sell.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SELL-02` (the category filter) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, choosing a category shows only its products and All shows every product. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-01` (the product search field) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, typing narrows the products and no match shows the SPEC text. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-03` (the product buttons) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, activating one adds a cart line and a second activation raises its quantity. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-04` (the cart lines) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, each line shows quantity, name and line total from the view model. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-05` (the line minus and plus) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, plus raises the quantity and minus at one removes the line. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-06` (the clear control) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, it opens `X-01` and only its confirm empties the cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-07` (the totals) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-07`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, item count, VAT and total follow the cart and the shop's VAT settings. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-08` (the pay buttons) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-08`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, they are disabled on an empty cart and call the sale flow once with the right method. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-11` (the empty cart text) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-11`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, an empty cart shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-12` (the empty catalogue state) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-12`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, a shop with no products shows the text and its button asks the shell for Products with an empty editor. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-13` (the low stock hint) in `src/Merchant.Till/Looks/Daylight/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-13`). Done when: in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, a product at three or less shows its count in the accent colour and still sells at zero. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `DaylightSellTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightSellTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/sell.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: sell screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSellTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/SellView.axaml` makes `Matches_the_reference` fail.

## 4. Counter look: Sell screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/sell.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/sell.html` under `design/SPEC.md` section 3
**Job:** Ring up what the customer asked for, fast
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** the Counter shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/SellView.axaml` from `design/interface/counter/sell.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterSellTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSellTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/SellView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/sell.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SELL-02` (the category filter) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-02`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, choosing a category shows only its products and All shows every product. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SH-04` (the today's takings pill) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SH-04`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, completing a sale in the test raises the amount and the count. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-01` (the product search field) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-01`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, typing narrows the products and no match shows the SPEC text. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-03` (the product buttons) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-03`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, activating one adds a cart line and a second activation raises its quantity. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-04` (the cart lines) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-04`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, each line shows quantity, name and line total from the view model. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-05` (the line minus and plus) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-05`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, plus raises the quantity and minus at one removes the line. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-06` (the clear control) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-06`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, it opens `X-01` and only its confirm empties the cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-07` (the totals) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-07`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, item count, VAT and total follow the cart and the shop's VAT settings. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-08` (the pay buttons) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-08`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, they are disabled on an empty cart and call the sale flow once with the right method. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-11` (the empty cart text) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-11`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, an empty cart shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-12` (the empty catalogue state) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-12`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, a shop with no products shows the text and its button asks the shell for Products with an empty editor. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-13` (the low stock hint) in `src/Merchant.Till/Looks/Counter/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-13`). Done when: in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, a product at three or less shows its count in the accent colour and still sells at zero. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `CounterSellTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterSellTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/sell.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: sell screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSellTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/SellView.axaml` makes `Matches_the_reference` fail.

## 5. Tape look: Sell screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/sell.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/sell.html` under `design/SPEC.md` section 3
**Job:** Ring up what the customer asked for, fast
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** the Tape shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/SellView.axaml` from `design/interface/tape/sell.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeSellTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSellTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/SellView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/sell.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SELL-01` (the product search field) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-01`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, typing narrows the products and no match shows the SPEC text. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-03` (the product buttons) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-03`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, activating one adds a cart line and a second activation raises its quantity. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-04` (the cart lines) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-04`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, each line shows quantity, name and line total from the view model. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-05` (the line minus and plus) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-05`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, plus raises the quantity and minus at one removes the line. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-06` (the clear control) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-06`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, it opens `X-01` and only its confirm empties the cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-07` (the totals) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-07`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, item count, VAT and total follow the cart and the shop's VAT settings. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-08` (the pay buttons) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-08`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, they are disabled on an empty cart and call the sale flow once with the right method. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-11` (the empty cart text) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-11`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, an empty cart shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-12` (the empty catalogue state) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-12`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, a shop with no products shows the text and its button asks the shell for Products with an empty editor. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-13` (the low stock hint) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-13`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, a product at three or less shows its count in the accent colour and still sells at zero. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-09` (the selected-line strip) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-09`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, activating a paper line selects it and Remove line deletes it. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SELL-10` (the next-key quantity keys) in `src/Merchant.Till/Looks/Tape/SellView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SELL-10`). Done when: in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, x3 then a product key adds three and the choice returns to x1. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapeSellTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeSellTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/sell.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: sell screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSellTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/SellView.axaml` makes `Matches_the_reference` fail.

## 6. Application startup on the shell

With Welcome and Sell built, the window can show the shell instead of the first plain window.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** the window frame only: 1280 x 800 by default under `design/SPEC.md` section 3
**Job:** Open the till on the right first screen
**Treatment:** the current look's shell from D02 T01
**Chrome:** the look's shell

**Build order:**

1. Wire `App.axaml.cs` and `MainWindow.axaml` to the shell. Done when: the app starts on Welcome or Sell.

- [ ] Replace the content of `src/Merchant.Till/Views/MainWindow.axaml` with `ShellHost`, 1280 x 800 by default with a 1024 x 700 minimum, and build `ShellViewModel` in `src/Merchant.Till/App.axaml.cs`. Done when: the old tab control is gone from the window. Cheaper substitute: keeping the old tabs beside the shell must fail review.
- [ ] Start on Welcome when `TillRuntime.HasShop` is false and on the Sell destination otherwise. Done when: a fresh data folder starts on Welcome and one with a shop starts on Sell.
- [ ] Register a destination that later files have not built yet as `ComingSoonViewModel` naming the TODO file that owns it, shown with the look's card style, so the running app never throws between phases; each later file replaces its own registration. Done when: navigating to Tills in the running app shows the owner's name until D05 T01 registers the real screen. Cheaper substitute: leaving any `ComingSoonViewModel` registration after its owner ships must fail D06 T01 §6.
- [ ] Add `tests/Merchant.Tests/Screens/StartupTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~StartupTests"` passes.
- [ ] Commit: `sell: start the app on the shell`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~StartupTests"` passes; starting on Sell without a shop makes `Fresh_till_starts_on_welcome` fail.

## Verification

- In each look, the test taps the fixture cart together and the totals read R 155.00.
- `dotnet test Merchant.slnx` passes.
