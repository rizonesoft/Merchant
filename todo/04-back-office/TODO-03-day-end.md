---
schema_version: 1
id: day-end
domain: 04-back-office
status: active
title: "TODO-03 -- Day end"
depends_on: ["looks-and-shell", "receipts-and-printing", "payment"]
---

# TODO-03 -- Day end

> **Goal:** At the end of the day the operator sees the takings, counts the drawer, sees the difference, closes the day and prints the summary.

> **Current state:** Measured 2026-11-17. The old window shows today's total and sale count. Nothing counts cash or closes a day.

## Inputs

- `design/SPEC.md` section 5.8 (`DAY-01` to `DAY-10`), `X-03`, `X-08` and `X-09`.
- `design/interface/<look>/day-end.html` and `.png`.

## Outcome

- With the fixture and 2 570.00 counted, each look shows `Short by` R 7.00.
- Closing records one close and the next day's float.

**Adjacency:** list=not-applicable (one day at a time; earlier days are reached on the Sales screen); document=applicable @ D01 T02 §3; settings=applicable; reporting=applicable; notifications=applicable; permissions=not-applicable (no user accounts); audit=applicable; exchange=not-applicable (nothing to import or export here); reverse=not-applicable (a close is a record of a count, corrected by counting again before closing; after closing the next day starts)

**Adjacency rationale:** This file is the report of the sales domain. Its one tunable is the float, set in the float dialog. The close is a dated fact naming its till. Results report through the notice bar.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Day figures | -- | [ ] |
| 2 | §2 | Cash count and difference | §1, D03 T03 §3 | [ ] |
| 3 | §3 | Float dialog | §2 | [ ] |
| 4 | §4 | Closing the day and printing the summary | §2, §3 | [ ] |
| 5 | §5 | Daylight look: Day end screen | §4 | [ ] |
| 6 | §6 | Counter look: Day end screen | §4 | [ ] |
| 7 | §7 | Tape look: Day end screen | §4 | [ ] |

## 1. Day figures

The read-only figures (`DAY-01`, `DAY-02`, `DAY-08`, `DAY-09`, `DAY-10`).

**Phase:** 4
**Needs:** none
**Surface:** no

**Build order:**

1. Add `DayEndViewModel` to `src/Merchant.Till/ViewModels`. Done when: the fixture reproduces the page's figures.

- [ ] Add `src/Merchant.Till/ViewModels/DayEndViewModel.cs` with the heading date, takings, cash, card and VAT texts with their counts, the sold today lines and the per-till lines (`not synced since yesterday` wording for an absent till). Done when: takings read `R 3 864.00` with `42 sales, 1 refund`.
- [ ] Show zeros for a day without sales. Done when: an empty shop reads `R 0.00` and `0 sales`.
- [ ] Register `DayEndViewModel` as the Day end destination through `ShellViewModel.RegisterDestination` in `src/Merchant.Till/App.axaml.cs`. Done when: navigating to Day end from the shell shows the screen.
- [ ] Add `tests/Merchant.Tests/DayEndFiguresTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayEndFiguresTests"` passes.
- [ ] Commit: `day end: figures view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayEndFiguresTests"` passes; using this till's sales only for takings makes `Takings_cover_all_tills` fail.

## 2. Cash count and difference

Three ways to count, one counted amount (`DAY-03`, `DAY-04`, `DAY-05`).

**Phase:** 4
**Needs:** none
**Surface:** no

**Build order:**

1. Add the drawer to `DayEndViewModel`. Done when: short, over and balanced each read correctly.

- [ ] Add the expected lines from `DrawerSummary` and `Counted` driven by a typed amount, by the keypad commands, or by note counts for 200, 100, 50, 20 and 10 with a coins amount. Done when: six, eight, seven, eight and four notes with 20.00 in coins count 2 570.00.
- [ ] Add `DifferenceLabel` and `DifferenceText`: `Short by`, `Over by` or `The drawer balances`, with the accent or the good colour key. Done when: 2 570.00 against 2 577.00 reads `Short by` and `R 7.00`.
- [ ] Open `X-03` for the coins row and use its amount. Done when: typing 20.00 sets the coins amount.
- [ ] Add `tests/Merchant.Tests/DayEndCountTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayEndCountTests"` passes.
- [ ] Commit: `day end: cash count view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayEndCountTests"` passes; swapping the short and over labels makes `Short_drawer_reads_short` fail.

## 3. Float dialog

The float this morning (`DAY-03`, `X-08`).

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** not drawn on a page: built from `design/SPEC.md` `X-08` and section 7
**Job:** Tell the till what cash the day started with
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** the dialog host from D02 T01 §11

**Build order:**

1. Add `FloatDialogViewModel` to `src/Merchant.Till/ViewModels` and its view. Done when: saving changes the expected cash.

- [ ] Add `src/Merchant.Till/ViewModels/FloatDialogViewModel.cs` with an amount field under the money rules, `Cancel` and `Save float`, storing `float_cents`. Done when: saving 600.00 raises expected cash by 100.00.
- [ ] Add `src/Merchant.Till/Looks/FloatDialog.axaml`. Done when: it renders in all three looks.
- [ ] Add `tests/Merchant.Tests/FloatDialogTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~FloatDialogTests"` passes.
- [ ] Commit: `day end: float dialog`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~FloatDialogTests"` passes; not persisting the value makes `Float_survives_reopening` fail.

## 4. Closing the day and printing the summary

The close and its paper (`DAY-06`, `DAY-07`, `X-09`).

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** the dialog is not drawn on a page: built from `design/SPEC.md` `X-09` and section 7
**Job:** Close the books for the day
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** the dialog host from D02 T01 §11

**Build order:**

1. Add closing to `DayEndViewModel`. Done when: one `day.closed` fact is appended and the closed state shows.

- [ ] Add `src/Merchant.Till/ViewModels/CloseDayDialogViewModel.cs` and `src/Merchant.Till/Looks/CloseDayDialog.axaml` with the SPEC wording, the takings, counted cash, difference and the `Float left in the drawer for tomorrow` field defaulting to the counted cash. Done when: the dialog shows the three amounts.
- [ ] Add `CloseCommand`: open `X-09`, on confirm append `day.closed`, store the float left, print the summary when a printer is set, and set `ClosedText` (`Day closed at 17:42.`) in place of the button. Done when: a second execute appends nothing.
- [ ] Add `PrintSummaryCommand` working before and after closing. Done when: the bytes equal `Receipt.BuildDaySummary` for the day.
- [ ] Add the Daylight steps to the view model: `Step` 1 to 3 with `NextCommand` and `BackCommand`. Done when: step 2 shows the figures and step 3 the confirmation.
- [ ] Add `tests/Merchant.Tests/DayCloseFlowTests.cs` including an empty day and a dead printer. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayCloseFlowTests"` passes.
- [ ] Commit: `day end: close and print`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayCloseFlowTests"` passes; allowing a second close makes `Day_closes_once` fail.

## 5. Daylight look: Day end screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/day-end.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/day-end.html` under `design/SPEC.md` section 3
**Job:** Count the drawer and close the day
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** the Daylight shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/DayEndView.axaml` from `design/interface/daylight/day-end.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightDayEndTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/DayEndView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/day-end.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `DAY-01` (the heading with the date) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, it shows the fixture's date. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-02` (the takings figures) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, takings, cash, card, VAT and counts equal the fixture's day. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-03` (the expected cash lines) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, float plus cash sales less cash refunds for this till, and the float row opens `X-08`. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-04` (the counted cash entry) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, entering the count updates the counted total. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-05` (the difference) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, short, over and balanced each show their SPEC wording and colour. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-06` (the close control) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, it opens `X-09`, records the close once and shows the closed state. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-07` (the print summary control) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-07`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, it sends the day summary to the test printer before and after closing. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-08` (the sold today list) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-08`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, six products by takings and one line for the rest, summing to the takings. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-09` (the per-till lines) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-09`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, each till's count shows and an absent till shows since when. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-10` (the empty day) in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-10`). Done when: in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, a day with no sales shows zeros and still closes. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build the three steps of `DAY-06`: the page shows step 1; step 2 lays the `DAY-02`, `DAY-08` and `DAY-09` figures out in the two-card layout of the page, and step 3 shows the closed state. Done when: the test walks all three steps and back.
- [ ] Add the render test `DaylightDayEndTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightDayEndTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/day-end.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: day-end screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightDayEndTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/DayEndView.axaml` makes `Matches_the_reference` fail.

## 6. Counter look: Day end screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/day-end.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/day-end.html` under `design/SPEC.md` section 3
**Job:** Count the drawer and close the day
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** the Counter shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/DayEndView.axaml` from `design/interface/counter/day-end.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterDayEndTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/DayEndView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/day-end.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `DAY-01` (the heading with the date) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-01`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, it shows the fixture's date. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-02` (the takings figures) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-02`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, takings, cash, card, VAT and counts equal the fixture's day. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-03` (the expected cash lines) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-03`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, float plus cash sales less cash refunds for this till, and the float row opens `X-08`. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-04` (the counted cash entry) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-04`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, entering the count updates the counted total. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-05` (the difference) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-05`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, short, over and balanced each show their SPEC wording and colour. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-06` (the close control) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-06`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, it opens `X-09`, records the close once and shows the closed state. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-07` (the print summary control) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-07`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, it sends the day summary to the test printer before and after closing. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-08` (the sold today list) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-08`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, six products by takings and one line for the rest, summing to the takings. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-09` (the per-till lines) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-09`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, each till's count shows and an absent till shows since when. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-10` (the empty day) in `src/Merchant.Till/Looks/Counter/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-10`). Done when: in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, a day with no sales shows zeros and still closes. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `CounterDayEndTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterDayEndTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/day-end.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: day-end screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterDayEndTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/DayEndView.axaml` makes `Matches_the_reference` fail.

## 7. Tape look: Day end screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/day-end.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 4
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/day-end.html` under `design/SPEC.md` section 3
**Job:** Count the drawer and close the day
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** the Tape shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/DayEndView.axaml` from `design/interface/tape/day-end.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeDayEndTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/DayEndView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/day-end.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `DAY-01` (the heading with the date) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-01`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, it shows the fixture's date. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-02` (the takings figures) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-02`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, takings, cash, card, VAT and counts equal the fixture's day. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-03` (the expected cash lines) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-03`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, float plus cash sales less cash refunds for this till, and the float row opens `X-08`. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-04` (the counted cash entry) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-04`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, entering the count updates the counted total. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-05` (the difference) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-05`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, short, over and balanced each show their SPEC wording and colour. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-06` (the close control) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-06`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, it opens `X-09`, records the close once and shows the closed state. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-07` (the print summary control) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-07`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, it sends the day summary to the test printer before and after closing. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-08` (the sold today list) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-08`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, six products by takings and one line for the rest, summing to the takings. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-09` (the per-till lines) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-09`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, each till's count shows and an absent till shows since when. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `DAY-10` (the empty day) in `src/Merchant.Till/Looks/Tape/DayEndView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `DAY-10`). Done when: in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, a day with no sales shows zeros and still closes. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapeDayEndTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeDayEndTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/day-end.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: day-end screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeDayEndTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/DayEndView.axaml` makes `Matches_the_reference` fail.

## Verification

- `dotnet test Merchant.slnx` passes.
- The three renders match their references with 2 570.00 counted.
