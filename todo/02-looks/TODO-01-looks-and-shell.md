---
schema_version: 1
id: looks-and-shell
domain: 02-looks
status: active
title: "TODO-01 -- Three looks, the shell and the fixture"
depends_on: ["build-and-checks", "shop-facts"]
---

# TODO-01 -- Three looks, the shell and the fixture

> **Goal:** The till can show any screen in any of its three looks, switch look at once, move between screens, show notices and dialogs, and render the reference shop for comparison.

> **Current state:** Measured 2026-11-17. `src/Merchant.Till/Views/MainWindow.axaml` is one window with a Till tab and a Setup tab in the Fluent theme, bound to `MainViewModel`. There are no looks, no shell, no dialogs and no fixture. `TillRuntime.SyncStatusChanged` reports a formatted string.

## Inputs

- `design/SPEC.md` sections 2 (looks), 4 (tokens), 5.1 (shell), 6 (layout notes), 7 (dialog rules) and 9 (fixture).
- `design/interface/<look>/sell.html` and `settings.html` for the frame and the control styles.
- `assets/brand/merchant-icon.svg` for the mark.

## Outcome

- With the fixture loaded, the shell renders in each look and the six destinations switch screens.
- Changing the look in a test changes the next render with no restart.

**Adjacency:** list=not-applicable (the shell holds no records); document=not-applicable (nothing is printed here); settings=applicable @ D05 T02 §5; reporting=not-applicable (no data of its own); notifications=applicable @ D02 T01 §10; permissions=not-applicable (no user accounts); audit=not-applicable (navigation changes no record); exchange=not-applicable (nothing to import or export); reverse=not-applicable (switching look or screen is its own undo)

**Adjacency rationale:** The one tunable is the look, chosen on the Settings screen. The notice bar is how every other file tells the operator what happened.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Look setting and switching | -- | [ ] |
| 2 | §2 | Daylight tokens and control styles | §1 | [ ] |
| 3 | §3 | Counter tokens and control styles | §1 | [ ] |
| 4 | §4 | Tape tokens and control styles | §1 | [ ] |
| 5 | §5 | Navigation and screen host | §1 | [ ] |
| 6 | §6 | Daylight shell | §2, §5, §9, §10, §13 | [ ] |
| 7 | §7 | Counter shell | §3, §5, §9, §10, §13 | [ ] |
| 8 | §8 | Tape shell | §4, §5, §9, §10, §13 | [ ] |
| 9 | §9 | Sync status indicator | §5 | [ ] |
| 10 | §10 | Notice bar | §2, §3, §4 | [ ] |
| 11 | §11 | Dialog host | §5 | [ ] |
| 12 | §12 | Reference fixture and fixed clock | D01 T01 §11 | [ ] |
| 13 | §13 | Mark and navigation icons as vectors | -- | [ ] |

## 1. Look setting and switching

The till has three looks and one behaviour (`design/SPEC.md` section 2). This section adds the setting, its default, and the switch that swaps resources and views at once.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Add `Look` and `LookManager` to `src/Merchant.Till/Looks`. Done when: changing the look swaps the resource dictionary and the view lookup with no restart.

- [ ] Add `src/Merchant.Till/Looks/Look.cs` (`Daylight`, `Counter`, `Tape`) and `src/Merchant.Till/Looks/LookManager.cs` with `Current`, `Changed` and `Set(Look)`, stored as the local setting `look` and defaulting to `Daylight` when unset or unreadable. Done when: a new store reports Daylight, and a stored `tape` reports Tape after reopening. Cheaper substitute: recording the look as a synced event must fail review: it is per till.
- [ ] Add `LookManager.Register(look, tokens, controls)` and, in `Set`, replace the application's merged look dictionaries with the pair registered for the look; §2 to §4 each register their own. Done when: with two stand-in dictionaries registered in the test, a resource read after `Set` comes from the new look's dictionary. Cheaper substitute: loading dictionaries by path inside `Set` must fail review: a look that is not registered must throw naming it.
- [ ] Rewrite `src/Merchant.Till/ViewLocator.cs` to resolve `<Screen>ViewModel` to `Merchant.Till.Looks.<Look>.<Screen>View` for the current look, through one hand-written map per look (`Looks/<Look>/Views.cs`) that each screen section adds its entry to, with no reflection over type names. Done when: a mapped pair resolves to its view, and an unmapped pair throws an exception naming the look and the view model. Cheaper substitute: one view with look switches inside it must fail review.
- [ ] Add `tests/Merchant.Tests/Screens/LookManagerTests.cs` covering the default, persistence, the dictionary swap and the lookup of a mapped and an unmapped pair. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~LookManagerTests"` passes.
- [ ] Commit: `looks: setting, switching and view lookup`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~LookManagerTests"` passes; skipping the dictionary swap in `Set` makes `Switch_changes_resources` fail.

## 2. Daylight tokens and control styles

The Daylight look's colours, radii and control styles from `design/SPEC.md` section 4 and the `daylight` pages, as named resources so no view holds a literal colour.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Write `src/Merchant.Till/Looks/Daylight/Tokens.axaml` and `src/Merchant.Till/Looks/Daylight/Controls.axaml`. Done when: the style sheet test renders every control variant.

- [ ] Add `src/Merchant.Till/Looks/Daylight/Tokens.axaml` with one brush per token row of `design/SPEC.md` section 4 for Daylight, named `Ground`, `Surface`, `Raised`, `Line`, `ControlBorder`, `Text`, `DimText`, `PrimaryFill`, `PrimaryText`, `Accent`, `SelectedRow`, `Good`, `Danger`, `Paper`, `PaperInk`, plus the radii as `CornerRadius` resources. Done when: a test reads every key from the dictionary and each colour equals the SPEC table. Cheaper substitute: a missing key falling back to another look's value must fail the test.
- [ ] Add `src/Merchant.Till/Looks/Daylight/Controls.axaml` styling `Button` (plain, `primary`, `danger`, disabled at 45% opacity), `TextBox`, `ComboBox`, `CheckBox`, the card `Border`, headings in Sora and body text in Inter, with the sizes, borders and weights in `design/interface/daylight/settings.html`. Done when: each variant in the style sheet view has the page's height, radius and border width. Cheaper substitute: styling controls inside individual views must fail review.
- [ ] Add `tests/Merchant.Tests/Screens/DaylightStyleTests.cs` rendering one view that holds every variant and asserting the measured height, corner radius and brush of each against the page's CSS values. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightStyleTests"` passes.
- [ ] Commit: `daylight: tokens and control styles`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightStyleTests"` passes; changing the `PrimaryFill` brush makes `Primary_button_uses_the_token` fail.

## 3. Counter tokens and control styles

The Counter look's colours, radii and control styles from `design/SPEC.md` section 4 and the `counter` pages, as named resources so no view holds a literal colour.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Write `src/Merchant.Till/Looks/Counter/Tokens.axaml` and `src/Merchant.Till/Looks/Counter/Controls.axaml`. Done when: the style sheet test renders every control variant.

- [ ] Add `src/Merchant.Till/Looks/Counter/Tokens.axaml` with one brush per token row of `design/SPEC.md` section 4 for Counter, named `Ground`, `Surface`, `Raised`, `Line`, `ControlBorder`, `Text`, `DimText`, `PrimaryFill`, `PrimaryText`, `Accent`, `SelectedRow`, `Good`, `Danger`, `Paper`, `PaperInk`, plus the radii as `CornerRadius` resources. Done when: a test reads every key from the dictionary and each colour equals the SPEC table. Cheaper substitute: a missing key falling back to another look's value must fail the test.
- [ ] Add `src/Merchant.Till/Looks/Counter/Controls.axaml` styling `Button` (plain, `primary`, `danger`, disabled at 45% opacity), `TextBox`, `ComboBox`, `CheckBox`, the card `Border`, headings in Sora and body text in Inter, with the sizes, borders and weights in `design/interface/counter/settings.html`. Done when: each variant in the style sheet view has the page's height, radius and border width. Cheaper substitute: styling controls inside individual views must fail review.
- [ ] Add `tests/Merchant.Tests/Screens/CounterStyleTests.cs` rendering one view that holds every variant and asserting the measured height, corner radius and brush of each against the page's CSS values. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterStyleTests"` passes.
- [ ] Commit: `counter: tokens and control styles`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterStyleTests"` passes; changing the `PrimaryFill` brush makes `Primary_button_uses_the_token` fail.

## 4. Tape tokens and control styles

The Tape look's colours, radii and control styles from `design/SPEC.md` section 4 and the `tape` pages, as named resources so no view holds a literal colour.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Write `src/Merchant.Till/Looks/Tape/Tokens.axaml` and `src/Merchant.Till/Looks/Tape/Controls.axaml`. Done when: the style sheet test renders every control variant.

- [ ] Add `src/Merchant.Till/Looks/Tape/Tokens.axaml` with one brush per token row of `design/SPEC.md` section 4 for Tape, named `Ground`, `Surface`, `Raised`, `Line`, `ControlBorder`, `Text`, `DimText`, `PrimaryFill`, `PrimaryText`, `Accent`, `SelectedRow`, `Good`, `Danger`, `Paper`, `PaperInk`, plus the radii as `CornerRadius` resources. Done when: a test reads every key from the dictionary and each colour equals the SPEC table. Cheaper substitute: a missing key falling back to another look's value must fail the test.
- [ ] Add `src/Merchant.Till/Looks/Tape/Controls.axaml` styling `Button` (plain, `primary`, `danger`, disabled at 45% opacity), `TextBox`, `ComboBox`, `CheckBox`, the card `Border`, headings in Sora and body text in Inter, with the sizes, borders and weights in `design/interface/tape/settings.html`. Done when: each variant in the style sheet view has the page's height, radius and border width. Cheaper substitute: styling controls inside individual views must fail review.
- [ ] Add `src/Merchant.Till/Looks/Tape/PaperColumn.axaml`: the 380 px paper column with its dashed rule, row and torn-edge parts as in `design/interface/tape/sale-complete.html`, in JetBrains Mono on the `Paper` brush. Done when: the style sheet view shows a paper column whose torn edge has 19 teeth across 380 px. Cheaper substitute: a bitmap of the torn edge must fail review: it is a path.
- [ ] Add `tests/Merchant.Tests/Screens/TapeStyleTests.cs` rendering one view that holds every variant and asserting the measured height, corner radius and brush of each against the page's CSS values. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeStyleTests"` passes.
- [ ] Commit: `tape: tokens and control styles`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeStyleTests"` passes; changing the `PrimaryFill` brush makes `Primary_button_uses_the_token` fail.

## 5. Navigation and screen host

One view model owns which screen is showing, in every look (`SH-01`). Welcome, Payment and Sale complete are steps without navigation.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Add `ShellViewModel` to `src/Merchant.Till/ViewModels`. Done when: navigating changes `Current` and step screens report no navigation.

- [ ] Add `src/Merchant.Till/ViewModels/ShellViewModel.cs` with the six destinations in SPEC order, `RegisterDestination(key, factory)`, `Current`, `Navigate(destination)`, `ShowStep(viewModel)` and `ShowsNavigation`; each screen's file registers its own destination (D03 to D05). Done when: with test doubles registered, `Navigate(Sales)` sets `Current` to the sales double and marks it current, and navigating to an unregistered destination throws naming it.
- [ ] Add `src/Merchant.Till/Looks/ShellHost.axaml`: a `ContentControl` that shows the current look's shell view for a `ShellViewModel`, for tests and for the window that D03 T02 §6 wires up. Done when: a test hosts a shell with two test doubles and switches between them.
- [ ] Add `tests/Merchant.Tests/ShellViewModelTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ShellViewModelTests"` passes.
- [ ] Commit: `shell: navigation and screen host`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ShellViewModelTests"` passes; removing the unregistered-destination check makes `Unregistered_destination_throws` fail.

## 6. Daylight shell

The frame around every destination screen in the Daylight look: a 68 px top bar with the mark, six tabs with an amber underline on the current one, then the shop and till name and the sync status on the right.

**Phase:** 2
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with the frame of `design/interface/daylight/sell.html` under `design/SPEC.md` section 3
**Job:** Move between the six parts of the till and see sync at a glance
**Treatment:** Daylight tokens and control styles (this file)
**Chrome:** this section is the chrome

**Build order:**

1. Write `src/Merchant.Till/Looks/Daylight/ShellView.axaml` bound to `ShellViewModel`. Done when: the six destinations switch screens.

- [ ] Add `src/Merchant.Till/Looks/Daylight/ShellView.axaml` with the navigation laid out as in `design/interface/daylight/sell.html`: a 68 px top bar with the mark, six tabs with an amber underline on the current one, then the shop and till name and the sync status on the right. Done when: it loads with the fixture and hosts the current screen.
- [ ] Build `SH-01` (the six-destination navigation) in `src/Merchant.Till/Looks/Daylight/ShellView.axaml` (source: `design/SPEC.md` `SH-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightShellTests.cs`, activating each destination in the headless test shows its screen and marks it current. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-02` (the shop name and till name) in `src/Merchant.Till/Looks/Daylight/ShellView.axaml` (source: `design/SPEC.md` `SH-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightShellTests.cs`, renaming the shop in the fixture changes the text. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-03` (the sync status with its dot) in `src/Merchant.Till/Looks/Daylight/ShellView.axaml` (source: `design/SPEC.md` `SH-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightShellTests.cs`, each of the four states shows its own text and dot, and activating it asks the shell for Tills. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-05` (the Merchant mark) in `src/Merchant.Till/Looks/Daylight/ShellView.axaml` (source: `design/SPEC.md` `SH-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightShellTests.cs`, it renders from the vector resource at the page's size. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-06` (the notice bar) in `src/Merchant.Till/Looks/Daylight/ShellView.axaml` (source: `design/SPEC.md` `SH-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightShellTests.cs`, a result shows for five seconds and a refusal stays until corrected. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Hide the navigation on Welcome, Payment and Sale complete (`design/SPEC.md` section 6). Done when: those three screens render without it.
- [ ] Commit: `daylight: shell`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightShellTests"` passes; unbinding the navigation makes `Destinations_switch_screens` fail.

## 7. Counter shell

The frame around every destination screen in the Counter look: a 92 px side rail with the mark, five destinations with icon above label, and Settings at the bottom; the top bar belongs to each screen.

**Phase:** 2
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with the frame of `design/interface/counter/sell.html` under `design/SPEC.md` section 3
**Job:** Move between the six parts of the till and see sync at a glance
**Treatment:** Counter tokens and control styles (this file)
**Chrome:** this section is the chrome

**Build order:**

1. Write `src/Merchant.Till/Looks/Counter/ShellView.axaml` bound to `ShellViewModel`. Done when: the six destinations switch screens.

- [ ] Add `src/Merchant.Till/Looks/Counter/ShellView.axaml` with the navigation laid out as in `design/interface/counter/sell.html`: a 92 px side rail with the mark, five destinations with icon above label, and Settings at the bottom; the top bar belongs to each screen. Done when: it loads with the fixture and hosts the current screen.
- [ ] Build `SH-01` (the six-destination navigation) in `src/Merchant.Till/Looks/Counter/ShellView.axaml` (source: `design/SPEC.md` `SH-01`). Done when: in `tests/Merchant.Tests/Screens/CounterShellTests.cs`, activating each destination in the headless test shows its screen and marks it current. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-02` (the shop name and till name) in `src/Merchant.Till/Looks/Counter/ShellView.axaml` (source: `design/SPEC.md` `SH-02`). Done when: in `tests/Merchant.Tests/Screens/CounterShellTests.cs`, renaming the shop in the fixture changes the text. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-03` (the sync status with its dot) in `src/Merchant.Till/Looks/Counter/ShellView.axaml` (source: `design/SPEC.md` `SH-03`). Done when: in `tests/Merchant.Tests/Screens/CounterShellTests.cs`, each of the four states shows its own text and dot, and activating it asks the shell for Tills. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-05` (the Merchant mark) in `src/Merchant.Till/Looks/Counter/ShellView.axaml` (source: `design/SPEC.md` `SH-05`). Done when: in `tests/Merchant.Tests/Screens/CounterShellTests.cs`, it renders from the vector resource at the page's size. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-06` (the notice bar) in `src/Merchant.Till/Looks/Counter/ShellView.axaml` (source: `design/SPEC.md` `SH-06`). Done when: in `tests/Merchant.Tests/Screens/CounterShellTests.cs`, a result shows for five seconds and a refusal stays until corrected. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-04` (the today's takings pill) in `src/Merchant.Till/Looks/Counter/ShellView.axaml` (source: `design/SPEC.md` `SH-04`). Done when: in `tests/Merchant.Tests/Screens/CounterShellTests.cs`, completing a sale in the test raises the amount and the count. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Hide the navigation on Welcome, Payment and Sale complete (`design/SPEC.md` section 6). Done when: those three screens render without it.
- [ ] Commit: `counter: shell`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterShellTests"` passes; unbinding the navigation makes `Destinations_switch_screens` fail.

## 8. Tape shell

The frame around every destination screen in the Tape look: a 72 px bottom bar with six destinations, icon beside label, the current one filled amber, and the sync status on the right.

**Phase:** 2
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with the frame of `design/interface/tape/sell.html` under `design/SPEC.md` section 3
**Job:** Move between the six parts of the till and see sync at a glance
**Treatment:** Tape tokens and control styles (this file)
**Chrome:** this section is the chrome

**Build order:**

1. Write `src/Merchant.Till/Looks/Tape/ShellView.axaml` bound to `ShellViewModel`. Done when: the six destinations switch screens.

- [ ] Add `src/Merchant.Till/Looks/Tape/ShellView.axaml` with the navigation laid out as in `design/interface/tape/sell.html`: a 72 px bottom bar with six destinations, icon beside label, the current one filled amber, and the sync status on the right. Done when: it loads with the fixture and hosts the current screen.
- [ ] Build `SH-01` (the six-destination navigation) in `src/Merchant.Till/Looks/Tape/ShellView.axaml` (source: `design/SPEC.md` `SH-01`). Done when: in `tests/Merchant.Tests/Screens/TapeShellTests.cs`, activating each destination in the headless test shows its screen and marks it current. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-02` (the shop name and till name) in `src/Merchant.Till/Looks/Tape/ShellView.axaml` (source: `design/SPEC.md` `SH-02`). Done when: in `tests/Merchant.Tests/Screens/TapeShellTests.cs`, renaming the shop in the fixture changes the text. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-03` (the sync status with its dot) in `src/Merchant.Till/Looks/Tape/ShellView.axaml` (source: `design/SPEC.md` `SH-03`). Done when: in `tests/Merchant.Tests/Screens/TapeShellTests.cs`, each of the four states shows its own text and dot, and activating it asks the shell for Tills. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-05` (the Merchant mark) in `src/Merchant.Till/Looks/Tape/ShellView.axaml` (source: `design/SPEC.md` `SH-05`). Done when: in `tests/Merchant.Tests/Screens/TapeShellTests.cs`, it renders from the vector resource at the page's size. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Build `SH-06` (the notice bar) in `src/Merchant.Till/Looks/Tape/ShellView.axaml` (source: `design/SPEC.md` `SH-06`). Done when: in `tests/Merchant.Tests/Screens/TapeShellTests.cs`, a result shows for five seconds and a refusal stays until corrected. Cheaper substitute: a control drawn but not bound must fail the test.
- [ ] Hide the navigation on Welcome, Payment and Sale complete (`design/SPEC.md` section 6). Done when: those three screens render without it.
- [ ] Commit: `tape: shell`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeShellTests"` passes; unbinding the navigation makes `Destinations_switch_screens` fail.

## 9. Sync status indicator

One small view model turns the till's sync activity into the four states of `SH-03`.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Add `SyncStatusViewModel` to `src/Merchant.Till/ViewModels` over `TillRuntime`. Done when: each state has its text and dot.

- [ ] Extend `src/Merchant.Till/TillRuntime.cs` to raise a typed `SyncReport` (peers reached, events in and out, time, problem text) instead of a formatted string. Done when: the existing two-till test reads the counts from the report. Cheaper substitute: parsing the old status string must fail review.
- [ ] Add `src/Merchant.Till/ViewModels/SyncStatusViewModel.cs` with `State` (`Synced`, `NobodyInRange`, `Syncing`, `Problem`), `Text` in both the long form and Daylight's short form, and `Open` that navigates to Tills. Done when: each state produces the SPEC wording, with the time in 24-hour form.
- [ ] Add `tests/Merchant.Tests/SyncStatusViewModelTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SyncStatusViewModelTests"` passes.
- [ ] Commit: `shell: sync status`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SyncStatusViewModelTests"` passes; mapping a problem to Synced makes `Problem_shows_its_own_text` fail.

## 10. Notice bar

Results and refusals appear in one place (`SH-06`).

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Add `NoticeViewModel` to `src/Merchant.Till/ViewModels`. Done when: results expire and refusals stay.

- [ ] Add `src/Merchant.Till/ViewModels/NoticeViewModel.cs` with `Show(text)`, `Refuse(text)` and `Clear()`; a result clears after five seconds on an injected clock or at the next `Show`, and a refusal stays until cleared. Done when: advancing the test clock five seconds clears a result and leaves a refusal. Cheaper substitute: a real `Task.Delay` in the view model must fail review: tests would wait.
- [ ] Add the notice strip to each look's `Controls.axaml` as a named template: 48 px high, `Surface` fill, `Line` border, `Danger` text for a refusal. Done when: the three style sheet views each show a result and a refusal.
- [ ] Add `tests/Merchant.Tests/NoticeViewModelTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~NoticeViewModelTests"` passes.
- [ ] Commit: `shell: notice bar`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~NoticeViewModelTests"` passes; expiring refusals makes `Refusal_stays` fail.

## 11. Dialog host

Every dialog in `design/SPEC.md` section 7 uses one host: a scrim, a centred card, Escape to cancel and Enter for the primary action unless it destroys something.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Add `DialogHost` to `src/Merchant.Till/Looks` and `DialogViewModel` to `src/Merchant.Till/ViewModels`. Done when: a test dialog opens, cancels and confirms by keyboard.

- [ ] Add `src/Merchant.Till/ViewModels/DialogViewModel.cs` (title, body, safe action, primary action, `PrimaryIsDestructive`) and `ShellViewModel.ShowDialog`/`CloseDialog` returning the choice as a task. Done when: a view model test awaits a confirm and a cancel.
- [ ] Add `src/Merchant.Till/Looks/DialogHost.axaml` drawn over the shell: the scrim at 55% of the look's ground, the card 480 px wide in the look's card style, title in Sora 24 px, buttons right-aligned with the safe choice first. Done when: the dialog renders centred over a screen in all three looks.
- [ ] Handle Escape and a scrim press as cancel, and Enter as the primary action only when it is not destructive. Done when: Enter does nothing on a destructive dialog and confirms a non-destructive one. Cheaper substitute: Enter confirming a refund must fail the test.
- [ ] Add `tests/Merchant.Tests/Screens/DialogHostTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DialogHostTests"` passes.
- [ ] Commit: `shell: dialog host`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DialogHostTests"` passes; letting Enter confirm a destructive dialog makes `Enter_never_destroys` fail.

## 12. Reference fixture and fixed clock

Every render test shows the shop of `design/SPEC.md` section 9 at 12:41 on 17 November 2026. One fixture builds it as real events in a real store.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Add `IClock` to the till and `DuneFixture` to `tests/Merchant.Tests/Fixtures`. Done when: the fixture's day reproduces every section 9 figure.

- [ ] Add `src/Merchant.Core/IClock.cs` with a system implementation, and take it wherever the till reads the time (`SqliteEventStore.Append`, view models). Done when: a test clock makes `Append` stamp the test's time. Cheaper substitute: `DateTime.Now` left in a view model must fail the fixture's render tests at another hour.
- [ ] Add `tests/Merchant.Tests/Fixtures/DuneFixture.cs` building a temporary store with the shop settings, four categories, twelve products with their stock, three named tills, 42 sales and one refund matching every figure in `design/SPEC.md` section 9, and the open cart of sale 0042. Done when: `DaySummary`, `DrawerSummary`, `SoldOn` and `SalesPerTill` return the section 9 figures. Cheaper substitute: figures typed into view models instead of events must fail this test.
- [ ] Give the fixture the sync state of the pages: Till 2 in range and synced at 12:41, Till 3 last synced the day before at 17:05. Done when: the sync status reads `2 tills in range, synced 12:41`.
- [ ] Add `tests/Merchant.Tests/Fixtures/DuneFixtureTests.cs` asserting each figure once. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DuneFixtureTests"` passes.
- [ ] Commit: `tests: reference fixture and fixed clock`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DuneFixtureTests"` passes; removing one sale from the fixture makes `Day_matches_section_9` fail.

## 13. Mark and navigation icons as vectors

The mark and the six navigation icons are paths, so they stay sharp at any size and need no image files.

**Phase:** 2
**Needs:** none
**Surface:** no

**Build order:**

1. Add `src/Merchant.Till/Looks/Icons.axaml`. Done when: each icon renders from its geometry.

- [ ] Add `src/Merchant.Till/Looks/Icons.axaml` with `StreamGeometry` resources for the six icons, taken from the `<path>` and `<circle>` data in `design/interface/counter/sell.html`, and a `MerchantMark` control drawing the tile, the two peaks (stroke 58 on a 512 grid, round joins) and the coin from `assets/brand/merchant-icon.svg`. Done when: each geometry parses and the mark at 48 px matches the page's mark. Cheaper substitute: PNG icons must fail review.
- [ ] Add `tests/Merchant.Tests/Screens/IconTests.cs` rendering the seven at 48 px and asserting each has ink inside its box and none outside. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~IconTests"` passes.
- [ ] Commit: `looks: mark and navigation icons`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~IconTests"` passes; an empty geometry makes `Every_icon_has_ink` fail.

## Verification

- `dotnet test Merchant.slnx` passes.
- The three shell tests render `build/screens/<look>/shell.png` and a reviewer can see three distinct frames around the same Sell placeholder.
