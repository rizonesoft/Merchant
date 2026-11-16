---
schema_version: 1
id: products
domain: 04-back-office
status: active
title: "TODO-01 -- Products"
depends_on: ["looks-and-shell"]
---

# TODO-01 -- Products

> **Goal:** The operator adds, edits, recolours, restocks and removes products and categories, and every till shows the change after sync.

> **Current state:** Measured 2026-11-17. The old window adds a product from a name and a price. Nothing edits or removes one, and there are no categories or stock.

## Inputs

- `design/SPEC.md` section 5.6 (`PROD-01` to `PROD-09`), `X-04` and `X-05`.
- `design/interface/<look>/products.html` and `.png`.

## Outcome

- Editing Flat white's price in a test changes its button on Sell.
- A removed product leaves Sell and stays on past receipts.

**Adjacency:** list=applicable; document=not-applicable (the Tape price list is a screen, not a print); settings=not-applicable (nothing tunable here); reporting=not-applicable (no figures of its own: the day's figures belong to Day end); notifications=applicable; permissions=not-applicable (no user accounts); audit=applicable; exchange=not-applicable (products travel inside backups, D05 T02 §3); reverse=applicable

**Adjacency rationale:** Search finds a product by name. Every save is a dated fact in the log, which is the audit trail. Removal is reversed by adding the product again; an edit is reversed by another edit; Cancel abandons an unsaved one. Saves and refusals report through the notice bar.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Product list and search | -- | [ ] |
| 2 | §2 | Product editor: fields, validation, save and cancel | §1 | [ ] |
| 3 | §3 | Removing a product | §2 | [ ] |
| 4 | §4 | New category dialog | §2 | [ ] |
| 5 | §5 | Daylight look: Products screen | §3, §4 | [ ] |
| 6 | §6 | Counter look: Products screen | §3, §4 | [ ] |
| 7 | §7 | Tape look: Products screen | §3, §4 | [ ] |

## 1. Product list and search

The list side of Products (`PROD-01`, `PROD-03`, `PROD-09`).

**Phase:** 4
**Needs:** none
**Surface:** no

**Build order:**

1. Add `ProductsViewModel` to `src/Merchant.Till/ViewModels`. Done when: the fixture lists twelve products in name order.

- [ ] Add `src/Merchant.Till/ViewModels/ProductsViewModel.cs` with `Search`, `Rows` (name, category name, price text, stock text: `Not tracked`, the count, or `3 low`) and `ByCategory` for the Daylight cards and the paper list. Done when: Croissant's stock text is `3 low`.
- [ ] Add `SelectCommand(row)` opening that product in the editor and `IsEmpty` with the SPEC sentence. Done when: selecting Flat white loads it.
- [ ] Register `ProductsViewModel` as the Products destination through `ShellViewModel.RegisterDestination` in `src/Merchant.Till/App.axaml.cs`. Done when: navigating to Products from the shell shows the screen.
- [ ] Add `tests/Merchant.Tests/ProductsListTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductsListTests"` passes.
- [ ] Commit: `products: list view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductsListTests"` passes; sorting by price makes `Rows_are_in_name_order` fail.

## 2. Product editor: fields, validation, save and cancel

The editor (`PROD-02`, `PROD-04`, `PROD-05`, `PROD-08`).

**Phase:** 4
**Needs:** none
**Surface:** no

**Build order:**

1. Add the editor to `ProductsViewModel`. Done when: a save appends the right facts.

- [ ] Add `Editor` with `Title` (`New product` or `Edit <name>`), `Name`, `Category`, `Price`, `CountsStock`, `InStock`, `KeyColour`, and a refusal per field with the SPEC wording. Done when: a price of `1.234` shows `Enter a price such as 45 or 45.50.` and keeps the text.
- [ ] Add `SaveCommand`: validate, append `product.defined`, and `stock.adjusted` when the count was changed while stock is counted. Done when: saving a price change appends one `product.defined` and no stock fact. Cheaper substitute: writing to `ShopState` directly must fail the reopened-store assertion.
- [ ] Add `CancelCommand` discarding the edit and `AddCommand` opening an empty editor whose colour follows the chosen category. Done when: cancelling after typing leaves the log unchanged.
- [ ] Enable `InStock` only while `CountsStock` is on, and refuse a negative or non-whole count. Done when: `-1` and `2.5` are refused.
- [ ] Add `tests/Merchant.Tests/ProductEditorTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductEditorTests"` passes.
- [ ] Commit: `products: editor view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductEditorTests"` passes; skipping validation makes `Bad_price_is_refused` fail.

## 3. Removing a product

Removal asks first and keeps history (`PROD-06`, `X-04`).

**Phase:** 4
**Needs:** none
**Surface:** no

**Build order:**

1. Add `RemoveCommand` to the editor. Done when: confirm appends `product.removed`.

- [ ] Add `RemoveCommand` opening `X-04` with the product's name and appending `product.removed` only on `Remove`. Done when: `Keep it` appends nothing.
- [ ] After removal close the editor and show `Removed <name>.` in the notice bar. Done when: the list no longer has the row.
- [ ] Add `tests/Merchant.Tests/ProductRemoveTests.cs` asserting a past sale still shows the product's name. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductRemoveTests"` passes.
- [ ] Commit: `products: remove with confirmation`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductRemoveTests"` passes; appending on `Keep it` makes `Keeping_changes_nothing` fail.

## 4. New category dialog

Categories are added from the editor's list or, in Daylight, from `Add category` (`PROD-07`, `X-05`).

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** not drawn on a page: built from `design/SPEC.md` `X-05` and section 7
**Job:** Add a category without leaving the product
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** the dialog host from D02 T01 §11

**Build order:**

1. Add `CategoryDialogViewModel` to `src/Merchant.Till/ViewModels` and its view. Done when: a valid name appends `category.defined`.

- [ ] Add `src/Merchant.Till/ViewModels/CategoryDialogViewModel.cs` with `Name`, the three refusals (required, over 20 characters, duplicate ignoring case), `Cancel` and `Add category`. Done when: `coffee` is refused when Coffee exists.
- [ ] Append `category.defined` with the next position and select the new category in the editor that opened the dialog. Done when: the editor's category is the new one.
- [ ] Add `src/Merchant.Till/Looks/CategoryDialog.axaml` in the look's card style. Done when: it renders in all three looks.
- [ ] Add `tests/Merchant.Tests/CategoryDialogTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CategoryDialogTests"` passes.
- [ ] Commit: `products: new category dialog`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CategoryDialogTests"` passes; a case-sensitive duplicate check makes `Duplicate_is_refused` fail.

## 5. Daylight look: Products screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/products.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/products.html` under `design/SPEC.md` section 3
**Job:** Keep the price list right
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** the Daylight shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/ProductsView.axaml` from `design/interface/daylight/products.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightProductsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/ProductsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/products.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `PROD-01` (the product search field) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, typing narrows the list. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-02` (the add product control) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, it opens the editor empty with the New product title. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-03` (the product list) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, choosing a product opens it in the editor and low stock is marked. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-04` (the editor fields) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, each field shows the product's value and each refusal shows its SPEC text. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-05` (the save and cancel controls) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, Save records the product and Cancel leaves the log unchanged. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-06` (the remove control) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, it opens `X-04` and its confirm removes the product from Sell. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-09` (the empty state) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-09`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, a shop with no products shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-07` (the add category control) in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-07`). Done when: in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, it opens `X-05`. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `DaylightProductsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightProductsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/products.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: products screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightProductsTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/ProductsView.axaml` makes `Matches_the_reference` fail.

## 6. Counter look: Products screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/products.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/products.html` under `design/SPEC.md` section 3
**Job:** Keep the price list right
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** the Counter shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/ProductsView.axaml` from `design/interface/counter/products.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterProductsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/ProductsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/products.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `PROD-01` (the product search field) in `src/Merchant.Till/Looks/Counter/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-01`). Done when: in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, typing narrows the list. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-02` (the add product control) in `src/Merchant.Till/Looks/Counter/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-02`). Done when: in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, it opens the editor empty with the New product title. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-03` (the product list) in `src/Merchant.Till/Looks/Counter/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-03`). Done when: in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, choosing a product opens it in the editor and low stock is marked. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-04` (the editor fields) in `src/Merchant.Till/Looks/Counter/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-04`). Done when: in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, each field shows the product's value and each refusal shows its SPEC text. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-05` (the save and cancel controls) in `src/Merchant.Till/Looks/Counter/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-05`). Done when: in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, Save records the product and Cancel leaves the log unchanged. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-06` (the remove control) in `src/Merchant.Till/Looks/Counter/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-06`). Done when: in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, it opens `X-04` and its confirm removes the product from Sell. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-09` (the empty state) in `src/Merchant.Till/Looks/Counter/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-09`). Done when: in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, a shop with no products shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `CounterProductsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterProductsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/products.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: products screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterProductsTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/ProductsView.axaml` makes `Matches_the_reference` fail.

## 7. Tape look: Products screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/products.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/products.html` under `design/SPEC.md` section 3
**Job:** Keep the price list right
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** the Tape shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/ProductsView.axaml` from `design/interface/tape/products.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeProductsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/ProductsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/products.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `PROD-01` (the product search field) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-01`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, typing narrows the list. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-02` (the add product control) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-02`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, it opens the editor empty with the New product title. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-03` (the product list) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-03`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, choosing a product opens it in the editor and low stock is marked. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-04` (the editor fields) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-04`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, each field shows the product's value and each refusal shows its SPEC text. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-05` (the save and cancel controls) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-05`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, Save records the product and Cancel leaves the log unchanged. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-06` (the remove control) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-06`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, it opens `X-04` and its confirm removes the product from Sell. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-09` (the empty state) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-09`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, a shop with no products shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PROD-08` (the key colour swatches and key preview) in `src/Merchant.Till/Looks/Tape/ProductsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PROD-08`). Done when: in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, choosing a swatch recolours the preview and is saved with the product. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapeProductsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeProductsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/products.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: products screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeProductsTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/ProductsView.axaml` makes `Matches_the_reference` fail.

## Verification

- `dotnet test Merchant.slnx` passes.
- The three renders match their references with Flat white open in the editor.
