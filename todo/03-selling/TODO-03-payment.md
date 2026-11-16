---
schema_version: 1
id: payment
domain: 03-selling
status: active
title: "TODO-03 -- Payment"
depends_on: ["sell", "receipts-and-printing"]
---

# TODO-03 -- Payment

> **Goal:** The operator takes cash with the right change, or records a card payment, and the sale is on disk before anything prints.

> **Current state:** Measured 2026-11-17. `MainViewModel.CompleteSale` records a sale with no amount received and no change, and prints a fixed receipt.

## Inputs

- `design/SPEC.md` section 5.4 (`PAY-01` to `PAY-08`) and `X-03`.
- `design/interface/<look>/payment.html` and `.png`.

## Outcome

- A cash sale of 155.00 with 200.00 received records change of 45.00 and opens Sale complete.
- A card sale records no cash fields.
- With the printer off the sale still completes.

**Adjacency:** list=not-applicable (the sale appears on the Sales screen, D04 T02 §1); document=applicable @ D01 T02 §1; settings=not-applicable (printer and VAT settings are owned by D05 T02); reporting=not-applicable (no figures of its own: the day's figures belong to Day end); notifications=not-applicable (results and refusals show in place); permissions=not-applicable (no user accounts); audit=applicable; exchange=not-applicable (nothing to import or export here); reverse=applicable @ D03 T04 §1

**Adjacency rationale:** Payment creates the sale record, with its till, number and time. Its document is the receipt and its undo is the refund.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Cash entry: keypad, quick amounts and notes | -- | [ ] |
| 2 | §2 | Completing a sale by cash or card | §1 | [ ] |
| 3 | §3 | Type an amount dialog | §1 | [ ] |
| 4 | §4 | Daylight look: Payment screen | §2, §3 | [ ] |
| 5 | §5 | Counter look: Payment screen | §2, §3 | [ ] |
| 6 | §6 | Tape look: Payment screen | §2, §3 | [ ] |

## 1. Cash entry: keypad, quick amounts and notes

Three looks, two ways of entering cash, one amount received (`PAY-04`, `PAY-05`).

**Phase:** 3
**Needs:** none
**Surface:** no

**Build order:**

1. Add `PaymentViewModel` to `src/Merchant.Till/ViewModels` with cash entry. Done when: keypad, quick amounts and notes all drive `Received`.

- [ ] Add `src/Merchant.Till/ViewModels/PaymentViewModel.cs`, shown by implementing `ISaleFlow.ShowPayment` in `ShellViewModel`, holding the cart's built sale, `Method`, `Received`, `ChangeText`, `ShortfallText` and `CanComplete`. Done when: 200.00 received on 155.00 shows `R 45.00` change and can complete; 100.00 shows `Still to pay` with `R 55.00` and cannot.
- [ ] Add keypad commands (digits, decimal point, delete) editing the received text with at most two decimals. Done when: `2`, `0`, `0` reads 200.00 and a third decimal digit is ignored.
- [ ] Add `QuickAmounts`: exact, next multiple of 10, next multiple of 100 and the one after, without duplicates. Done when: 155.00 gives 155, 160, 200, 300 and 200.00 gives 200, 300, 400.
- [ ] Add note commands `R 200` to `R 1` that add to the amount, `ExactCommand`, `StartAgainCommand` and `TappedSummary` (`1 x R 200`). Done when: R 100 twice reads 200.00 and `2 x R 100`.
- [ ] Add `tests/Merchant.Tests/PaymentCashTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~PaymentCashTests"` passes.
- [ ] Commit: `payment: cash entry view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~PaymentCashTests"` passes; allowing completion on a shortfall makes `Short_cash_cannot_complete` fail.

## 2. Completing a sale by cash or card

Completing records the sale, then prints (`PAY-06`, `PAY-07`).

**Phase:** 3
**Needs:** none
**Surface:** no

**Build order:**

1. Add completion to `PaymentViewModel`. Done when: one event is appended and Sale complete opens.

- [ ] Add `CompleteCommand`: take `NextSaleNumber`, build the sale through `SaleBuilder`, append `sale.completed`, then print through `IPrintService` when `PrintAfterSale` is on, and call `ISaleFlow.ShowSaleComplete(sale, outcome)`, which D03 T04 §2 implements. Done when: the event is in the store before `RecordingPrintService` is called. Cheaper substitute: printing first, or awaiting the printer before appending, must fail the ordering test.
- [ ] Add the card step: `ChooseCardCommand`, `CardApprovedCommand` (records tender card with no cash fields) and `BackToCashCommand`. Done when: a card sale's event has no received or change values.
- [ ] Guard against a double tap: `CompleteCommand` and `CardApprovedCommand` run once per payment. Done when: executing twice appends one event.
- [ ] Add `BackCommand` returning to Sell with the cart unchanged. Done when: the cart lines are the same objects after Back.
- [ ] Add `tests/Merchant.Tests/PaymentCompleteTests.cs` including the printer-off and printer-failed cases. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~PaymentCompleteTests"` passes.
- [ ] Commit: `payment: complete by cash or card`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~PaymentCompleteTests"` passes; removing the once-only guard makes `Double_tap_records_one_sale` fail.

## 3. Type an amount dialog

The keypad dialog used by Daylight payment and by the coins row at day end (`X-03`).

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** not drawn on a page: built from `design/SPEC.md` `X-03` and section 7
**Job:** Enter an exact amount on a touch screen
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** the dialog host from D02 T01 §11

**Build order:**

1. Add `AmountDialogViewModel` to `src/Merchant.Till/ViewModels` and its view. Done when: it returns the typed amount or nothing.

- [ ] Add `src/Merchant.Till/ViewModels/AmountDialogViewModel.cs` reusing the keypad commands, with `Cancel` and `Use this amount`. Done when: typing 12.50 and confirming returns 1250; cancelling returns nothing.
- [ ] Add `src/Merchant.Till/Looks/AmountDialog.axaml`, 360 px wide, in the look's card and key styles. Done when: it renders in all three looks over the dialog host.
- [ ] Add `tests/Merchant.Tests/AmountDialogTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~AmountDialogTests"` passes.
- [ ] Commit: `payment: type an amount dialog`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~AmountDialogTests"` passes; returning zero on cancel makes `Cancel_returns_nothing` fail.

## 4. Daylight look: Payment screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/payment.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/payment.html` under `design/SPEC.md` section 3
**Job:** Take the money and give the right change
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/PaymentView.axaml` from `design/interface/daylight/payment.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightPaymentTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/PaymentView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/payment.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `PAY-01` (the back control) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, it returns to Sell with the cart unchanged. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-02` (the order summary) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, lines, VAT and amount due match the cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-03` (the cash or card choice) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, choosing card swaps the cash entry for the card step and back again. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-04` (the cash entry controls) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, the amount received follows every key, quick amount or note. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-05` (the received and change display) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, a shortfall shows Still to pay and disables completing. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-06` (the complete control) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, it records one sale event and opens Sale complete. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-07` (the card step) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-07`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, Card payment approved records a card sale with no cash fields. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-08` (the sale number and till name) in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-08`). Done when: in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, they show the next number for this till. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Open `X-03` from `Type an amount` (source: `design/SPEC.md` `PAY-04`). Done when: the amount typed in the dialog becomes the amount received.
- [ ] Add the render test `DaylightPaymentTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightPaymentTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/payment.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: payment screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightPaymentTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/PaymentView.axaml` makes `Matches_the_reference` fail.

## 5. Counter look: Payment screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/payment.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/payment.html` under `design/SPEC.md` section 3
**Job:** Take the money and give the right change
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/PaymentView.axaml` from `design/interface/counter/payment.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterPaymentTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/PaymentView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/payment.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `PAY-01` (the back control) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-01`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, it returns to Sell with the cart unchanged. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-02` (the order summary) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-02`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, lines, VAT and amount due match the cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-03` (the cash or card choice) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-03`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, choosing card swaps the cash entry for the card step and back again. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-04` (the cash entry controls) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-04`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, the amount received follows every key, quick amount or note. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-05` (the received and change display) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-05`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, a shortfall shows Still to pay and disables completing. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-06` (the complete control) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-06`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, it records one sale event and opens Sale complete. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-07` (the card step) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-07`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, Card payment approved records a card sale with no cash fields. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-08` (the sale number and till name) in `src/Merchant.Till/Looks/Counter/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-08`). Done when: in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, they show the next number for this till. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `CounterPaymentTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterPaymentTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/payment.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: payment screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterPaymentTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/PaymentView.axaml` makes `Matches_the_reference` fail.

## 6. Tape look: Payment screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/payment.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/payment.html` under `design/SPEC.md` section 3
**Job:** Take the money and give the right change
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/PaymentView.axaml` from `design/interface/tape/payment.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapePaymentTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/PaymentView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/payment.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `PAY-01` (the back control) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-01`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, it returns to Sell with the cart unchanged. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-02` (the order summary) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-02`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, lines, VAT and amount due match the cart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-03` (the cash or card choice) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-03`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, choosing card swaps the cash entry for the card step and back again. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-04` (the cash entry controls) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-04`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, the amount received follows every key, quick amount or note. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-05` (the received and change display) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-05`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, a shortfall shows Still to pay and disables completing. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-06` (the complete control) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-06`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, it records one sale event and opens Sale complete. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-07` (the card step) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-07`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, Card payment approved records a card sale with no cash fields. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `PAY-08` (the sale number and till name) in `src/Merchant.Till/Looks/Tape/PaymentView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `PAY-08`). Done when: in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, they show the next number for this till. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapePaymentTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapePaymentTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/payment.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: payment screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapePaymentTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/PaymentView.axaml` makes `Matches_the_reference` fail.

## Verification

- In each look the test pays the fixture cart with 200.00 cash and reads R 45.00 change on the next screen.
- `dotnet test Merchant.slnx` passes.
