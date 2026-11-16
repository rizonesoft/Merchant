---
schema_version: 1
id: sales-and-refunds
domain: 04-back-office
status: active
title: "TODO-02 -- Sales and refunds"
depends_on: ["looks-and-shell", "receipts-and-printing", "sale-complete"]
---

# TODO-02 -- Sales and refunds

> **Goal:** The operator finds any sale by day and till, reads it, prints it again and refunds it once.

> **Current state:** Measured 2026-11-17. There is no sales list and no refund in the till. The log holds every sale.

## Inputs

- `design/SPEC.md` section 5.7 (`SALES-01` to `SALES-08`), `X-02` and `X-06`.
- `design/interface/<look>/sales.html` and `.png`.

## Outcome

- With the fixture, today lists 42 sales with 0038 marked refunded.
- Refunding 0042 in a test marks it, returns stock and prints a refund receipt.

**Adjacency:** list=applicable; document=applicable @ D01 T02 §2; settings=not-applicable (nothing tunable here); reporting=applicable; notifications=applicable; permissions=not-applicable (no user accounts); audit=applicable; exchange=not-applicable (sales travel inside backups, D05 T02 §3); reverse=applicable @ D03 T04 §1

**Adjacency rationale:** This file is the list of the sales domain; its reverse is the refund flow of D03 T04 §1, which records a dated fact naming its till. The day summary line is the small report on this screen; the full one is Day end. The result of a refund and a failed print report through the notice bar.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Sales list, filters and detail | D03 T04 §1 | [ ] |
| 2 | §2 | Pick a day dialog | §1 | [ ] |
| 3 | §3 | Printing a past receipt again | §1 | [ ] |
| 4 | §4 | Daylight look: Sales screen | §2, §3 | [ ] |
| 5 | §5 | Counter look: Sales screen | §2, §3 | [ ] |
| 6 | §6 | Tape look: Sales screen | §2, §3 | [ ] |

## 1. Sales list, filters and detail

The list and the chosen sale (`SALES-01` to `SALES-04`, `SALES-07`, `SALES-08`).

**Phase:** 4
**Needs:** none
**Surface:** no

**Build order:**

1. Add `SalesViewModel` to `src/Merchant.Till/ViewModels`. Done when: the fixture day lists 42 rows newest first.

- [ ] Add `src/Merchant.Till/ViewModels/SalesViewModel.cs` with `Day`, `TodayCommand`, `YesterdayCommand`, `AllTills` (default on), `Rows` (number, time, short items text, method or `Refunded`, total, struck-through flag) and `Summary`. Done when: the first row is 0042 and the summary reads 42 sales, R 3 864.00 and 1 refund.
- [ ] Build the short items text as quantity then name, the quantity left out at one, joined by commas. Done when: sale 0042 reads `2 Flat white, Toasted cheese, Brownie`.
- [ ] Append the till name to the number when two tills in the list share it. Done when: two tills' sale 0005 read `0005 Till 1, counter` and `0005 Till 2, window hatch`.
- [ ] Add `Selected` with the detail: lines, VAT, total, cash received and change, date, time and till, selecting the newest sale when the list changes. Done when: 0042 is selected on opening.
- [ ] Add `RefundCommand` opening the refund dialog of D03 T04 §1 for the selected sale, and `RefundedText` in its place for a refunded sale. Done when: sale 0038 shows `Refunded at 11:52 on Till 1.` and no refund command.
- [ ] Register `SalesViewModel` as the Sales destination through `ShellViewModel.RegisterDestination` in `src/Merchant.Till/App.axaml.cs`. Done when: navigating to Sales from the shell shows the screen.
- [ ] Add `tests/Merchant.Tests/SalesViewModelTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SalesViewModelTests"` passes.
- [ ] Commit: `sales: list and detail view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SalesViewModelTests"` passes; listing oldest first makes `Newest_first` fail.

## 2. Pick a day dialog

A calendar for older days (`SALES-01`, `X-06`).

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** not drawn on a page: built from `design/SPEC.md` `X-06` and section 7
**Job:** Reach any earlier day's sales
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** the dialog host from D02 T01 §11

**Build order:**

1. Add `DayPickerViewModel` to `src/Merchant.Till/ViewModels` and its view. Done when: choosing a day lists that day's sales.

- [ ] Add `src/Merchant.Till/ViewModels/DayPickerViewModel.cs` with the month shown, previous and next month, days with sales flagged and future days disabled. Done when: tomorrow cannot be chosen.
- [ ] Add `src/Merchant.Till/Looks/DayPickerDialog.axaml`: a seven-column month grid with 44 px day buttons, bold for days with sales. Done when: it renders in all three looks.
- [ ] Close on a choice and set `SalesViewModel.Day`. Done when: choosing the 16th lists the fixture's previous day.
- [ ] Add `tests/Merchant.Tests/DayPickerTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayPickerTests"` passes.
- [ ] Commit: `sales: pick a day dialog`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayPickerTests"` passes; enabling future days makes `Future_days_are_disabled` fail.

## 3. Printing a past receipt again

Any sale can be printed again (`SALES-05`).

**Phase:** 4
**Needs:** none
**Surface:** no

**Build order:**

1. Add `PrintAgainCommand` to `SalesViewModel`. Done when: the bytes equal the original receipt.

- [ ] Add `PrintAgainCommand` building the receipt for the selected sale with the current shop settings and paper width, never opening the drawer, and reporting the outcome in the notice bar. Done when: the recorded bytes hold no drawer pulse. Cheaper substitute: opening the drawer on a reprint must fail the test.
- [ ] Add the case to `tests/Merchant.Tests/SalesViewModelTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SalesViewModelTests"` passes.
- [ ] Commit: `sales: print again`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SalesViewModelTests"` passes; sending the drawer pulse makes `Reprint_never_opens_the_drawer` fail.

## 4. Daylight look: Sales screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/sales.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/sales.html` under `design/SPEC.md` section 3
**Job:** Find a sale and put it right
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** the Daylight shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/SalesView.axaml` from `design/interface/daylight/sales.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSalesTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/SalesView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/sales.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SALES-01` (the day filter) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, Today, Yesterday and a picked day each list that day's sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-02` (the till filter) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, turning it lists only this till's sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-03` (the sales list) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, rows are newest first and a refunded sale is marked and struck through. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-04` (the sale detail) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, choosing a row shows its lines, VAT, total, cash and till. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-05` (the print again control) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, it sends the chosen sale's receipt to the test printer. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-06` (the refund control) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, it opens `X-02`, and a refunded sale shows when and where instead. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-08` (the empty state) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-08`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, a day without sales shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-07` (the day summary) in `src/Merchant.Till/Looks/Daylight/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-07`). Done when: in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, count, takings and refunds match the listed day. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `DaylightSalesTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightSalesTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/sales.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: sales screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSalesTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/SalesView.axaml` makes `Matches_the_reference` fail.

## 5. Counter look: Sales screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/sales.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/sales.html` under `design/SPEC.md` section 3
**Job:** Find a sale and put it right
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** the Counter shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/SalesView.axaml` from `design/interface/counter/sales.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSalesTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/SalesView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/sales.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SALES-01` (the day filter) in `src/Merchant.Till/Looks/Counter/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-01`). Done when: in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, Today, Yesterday and a picked day each list that day's sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-02` (the till filter) in `src/Merchant.Till/Looks/Counter/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-02`). Done when: in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, turning it lists only this till's sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-03` (the sales list) in `src/Merchant.Till/Looks/Counter/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-03`). Done when: in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, rows are newest first and a refunded sale is marked and struck through. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-04` (the sale detail) in `src/Merchant.Till/Looks/Counter/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-04`). Done when: in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, choosing a row shows its lines, VAT, total, cash and till. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-05` (the print again control) in `src/Merchant.Till/Looks/Counter/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-05`). Done when: in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, it sends the chosen sale's receipt to the test printer. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-06` (the refund control) in `src/Merchant.Till/Looks/Counter/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-06`). Done when: in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, it opens `X-02`, and a refunded sale shows when and where instead. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-08` (the empty state) in `src/Merchant.Till/Looks/Counter/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-08`). Done when: in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, a day without sales shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `CounterSalesTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterSalesTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/sales.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: sales screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSalesTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/SalesView.axaml` makes `Matches_the_reference` fail.

## 6. Tape look: Sales screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/sales.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/sales.html` under `design/SPEC.md` section 3
**Job:** Find a sale and put it right
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** the Tape shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/SalesView.axaml` from `design/interface/tape/sales.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSalesTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/SalesView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/sales.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SALES-01` (the day filter) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-01`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, Today, Yesterday and a picked day each list that day's sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-02` (the till filter) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-02`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, turning it lists only this till's sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-03` (the sales list) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-03`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, rows are newest first and a refunded sale is marked and struck through. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-04` (the sale detail) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-04`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, choosing a row shows its lines, VAT, total, cash and till. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-05` (the print again control) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-05`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, it sends the chosen sale's receipt to the test printer. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-06` (the refund control) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-06`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, it opens `X-02`, and a refunded sale shows when and where instead. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-08` (the empty state) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-08`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, a day without sales shows the SPEC sentence. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SALES-07` (the day summary) in `src/Merchant.Till/Looks/Tape/SalesView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SALES-07`). Done when: in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, count, takings and refunds match the listed day. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapeSalesTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeSalesTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/sales.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: sales screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSalesTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/SalesView.axaml` makes `Matches_the_reference` fail.

## Verification

- `dotnet test Merchant.slnx` passes.
- The three renders match their references with sale 0042 chosen.
