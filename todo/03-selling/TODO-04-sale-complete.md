---
schema_version: 1
id: sale-complete
domain: 03-selling
status: active
title: "TODO-04 -- Sale complete"
depends_on: ["payment", "receipts-and-printing"]
---

# TODO-04 -- Sale complete

> **Goal:** After a sale the operator sees the change to give, knows whether the receipt printed, and starts the next sale with one key.

> **Current state:** Measured 2026-11-17. There is no screen after a sale: the old window clears the cart and shows a status line.

## Inputs

- `design/SPEC.md` section 5.5 (`DONE-01` to `DONE-07`).
- `design/interface/<look>/sale-complete.html` and `.png`.

## Outcome

- After the fixture sale each look shows R 45.00 change and the printer line.
- Enter starts a new sale.

**Adjacency:** list=not-applicable (one sale, shown once); document=applicable @ D01 T02 §1; settings=not-applicable (nothing tunable here); reporting=not-applicable (no figures of its own: the day's figures belong to Day end); notifications=applicable; permissions=not-applicable (no user accounts); audit=not-applicable (the record was made at payment); exchange=not-applicable (nothing to import or export here); reverse=applicable

**Adjacency rationale:** The printer line is the notice that matters here: the operator must know a receipt did not print. The refund flow lives in this file because this is the first screen that offers it; the Sales screen reuses it.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Refunding a sale | -- | [ ] |
| 2 | §2 | Sale complete view model | §1 | [ ] |
| 3 | §3 | Daylight look: Sale complete screen | §2 | [ ] |
| 4 | §4 | Counter look: Sale complete screen | §2 | [ ] |
| 5 | §5 | Tape look: Sale complete screen | §2 | [ ] |

## 1. Refunding a sale

The refund dialog and what a refund does (`DONE-05`, `SALES-06`, `X-02`). Sale complete is the first screen to offer it and the Sales screen reuses it.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** not drawn on a page: built from `design/SPEC.md` `X-02` and section 7
**Job:** Give the money back and keep the books straight
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** the dialog host from D02 T01 §11

**Build order:**

1. Add `RefundDialogViewModel` to `src/Merchant.Till/ViewModels`. Done when: a confirmed refund appends one fact and prints.

- [ ] Add `src/Merchant.Till/ViewModels/RefundDialogViewModel.cs` with the SPEC wording, the total and method, `Keep the sale` and `Refund R <total>` marked destructive. Done when: Enter does not confirm it.
- [ ] On confirm append `sale.refunded`, then print `Receipt.BuildRefund` through `IPrintService` with the drawer pulse for cash, and report `Refunded sale 0042.`. Done when: one refund fact is in the store before the print call. Cheaper substitute: editing or deleting the sale event must fail review.
- [ ] Add `RefundDialogViewModel.TryOpen(saleId)` returning the `RefundedText` (`Refunded at 11:52 on Till 1.`) instead of a dialog for a sale already refunded. Done when: the fixture's sale 0038 opens no dialog.
- [ ] Add `src/Merchant.Till/Looks/RefundDialog.axaml`. Done when: it renders in all three looks.
- [ ] Add `tests/Merchant.Tests/RefundFlowTests.cs` covering confirm, keep, the double refund, stock returning and a dead printer. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~RefundFlowTests"` passes.
- [ ] Commit: `refunds: dialog and flow`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~RefundFlowTests"` passes; allowing a second refund makes `Refunded_sale_offers_no_refund` fail.

## 2. Sale complete view model

One view model for the confirmation, the change, printing again and the way on (`DONE-01` to `DONE-06`).

**Phase:** 3
**Needs:** none
**Surface:** no

**Build order:**

1. Add `SaleCompleteViewModel` to `src/Merchant.Till/ViewModels`. Done when: it reflects the recorded sale and the print outcome.

- [ ] Add `src/Merchant.Till/ViewModels/SaleCompleteViewModel.cs`, shown by implementing `ISaleFlow.ShowSaleComplete` in `ShellViewModel`, taking the recorded sale and the `PrintOutcome`: `Heading`, `Detail`, `ChangeText` or `PaidByCardText`, `PrinterLine` with the three SPEC sentences and the drawer sentence on a cash sale, and `ReceiptLines` for the preview. Done when: a failed print reads `The receipt did not print: <reason>.`.
- [ ] Add `NewSaleCommand` (empty cart, Sell), `PrintAgainCommand` (the sale's receipt through `IPrintService` with no drawer pulse, updating the printer line) and `RefundCommand` (opens the refund dialog of §1 for this sale). Done when: print again sends the receipt's lines and no drawer pulse.
- [ ] Mark `NewSaleCommand` as the default action so Enter runs it. Done when: the default command is New sale.
- [ ] Add `tests/Merchant.Tests/SaleCompleteViewModelTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SaleCompleteViewModelTests"` passes.
- [ ] Commit: `sale complete: view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SaleCompleteViewModelTests"` passes; reusing the cash wording for a card sale makes `Card_sale_shows_paid_by_card` fail.

## 3. Daylight look: Sale complete screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/sale-complete.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/sale-complete.html` under `design/SPEC.md` section 3
**Job:** Give the change and move to the next customer
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml` from `design/interface/daylight/sale-complete.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSaleCompleteTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/sale-complete.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `DONE-01` (the saved confirmation) in `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`, it shows the recorded sale's number, method and time. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-02` (the change to give) in `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`, a cash sale shows the change and a card sale shows Paid by card. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-03` (the new sale control) in `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`, it has focus and opens Sell with an empty cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-04` (the print again control) in `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`, it sends the sale's receipt to the test printer again, without the drawer pulse. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-05` (the refund control) in `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`, it opens `X-02` for this sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-06` (the printer line) in `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`, printed, failed and printing-off each show their own SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `DaylightSaleCompleteTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightSaleCompleteTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/sale-complete.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: sale-complete screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSaleCompleteTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/SaleCompleteView.axaml` makes `Matches_the_reference` fail.

## 4. Counter look: Sale complete screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/sale-complete.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/sale-complete.html` under `design/SPEC.md` section 3
**Job:** Give the change and move to the next customer
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml` from `design/interface/counter/sale-complete.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSaleCompleteTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/sale-complete.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `DONE-01` (the saved confirmation) in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-01`). Done when: in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, it shows the recorded sale's number, method and time. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-02` (the change to give) in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-02`). Done when: in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, a cash sale shows the change and a card sale shows Paid by card. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-03` (the new sale control) in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-03`). Done when: in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, it has focus and opens Sell with an empty cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-04` (the print again control) in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-04`). Done when: in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, it sends the sale's receipt to the test printer again, without the drawer pulse. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-05` (the refund control) in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-05`). Done when: in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, it opens `X-02` for this sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-06` (the printer line) in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-06`). Done when: in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, printed, failed and printing-off each show their own SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-07` (the receipt preview) in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-07`). Done when: in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, its lines and totals equal the recorded sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `CounterSaleCompleteTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterSaleCompleteTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/sale-complete.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: sale-complete screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSaleCompleteTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/SaleCompleteView.axaml` makes `Matches_the_reference` fail.

## 5. Tape look: Sale complete screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/sale-complete.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/sale-complete.html` under `design/SPEC.md` section 3
**Job:** Give the change and move to the next customer
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml` from `design/interface/tape/sale-complete.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSaleCompleteTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/sale-complete.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `DONE-01` (the saved confirmation) in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-01`). Done when: in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, it shows the recorded sale's number, method and time. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-02` (the change to give) in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-02`). Done when: in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, a cash sale shows the change and a card sale shows Paid by card. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-03` (the new sale control) in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-03`). Done when: in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, it has focus and opens Sell with an empty cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-04` (the print again control) in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-04`). Done when: in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, it sends the sale's receipt to the test printer again, without the drawer pulse. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-05` (the refund control) in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-05`). Done when: in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, it opens `X-02` for this sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-06` (the printer line) in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-06`). Done when: in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, printed, failed and printing-off each show their own SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DONE-07` (the receipt preview) in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DONE-07`). Done when: in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, its lines and totals equal the recorded sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapeSaleCompleteTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeSaleCompleteTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/sale-complete.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: sale-complete screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSaleCompleteTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/SaleCompleteView.axaml` makes `Matches_the_reference` fail.

## Verification

- `dotnet test Merchant.slnx` passes.
- The three renders match their references with the fixture sale.
