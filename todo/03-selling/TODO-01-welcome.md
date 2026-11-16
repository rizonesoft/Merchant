---
schema_version: 1
id: welcome
domain: 03-selling
status: active
title: "TODO-01 -- Welcome and first run"
depends_on: ["looks-and-shell"]
---

# TODO-01 -- Welcome and first run

> **Goal:** A till with no shop opens on Welcome and can create a shop, join one or restore a backup; a till whose data cannot be opened says so instead of starting empty.

> **Current state:** Measured 2026-11-17. `TillRuntime` creates a shop key silently on first open (`src/Merchant.Till/TillRuntime.cs`, constructor), so there is no first run. Joining and restoring exist as `TillRuntime.JoinShop` and `SqliteEventStore.RestoreBackup`, reached from the old Setup tab.

## Inputs

- `design/SPEC.md` section 5.2 (`WEL-01` to `WEL-04`), `X-07` and `X-12`.
- `design/interface/<look>/welcome.html` and `.png`.

## Outcome

- A fresh data folder shows Welcome; each of the three paths ends on Sell with the right shop.
- A data folder whose key file is missing shows the failure screen and creates nothing.

**Adjacency:** list=not-applicable (first run holds no records); document=not-applicable (nothing printed); settings=not-applicable (the shop name set here is edited later on Settings, D05 T02 §1); reporting=not-applicable (no figures of its own: the day's figures belong to Day end); notifications=not-applicable (refusals show in place); permissions=applicable; audit=applicable; exchange=applicable; reverse=applicable @ D05 T01 §4

**Adjacency rationale:** Joining is the one permission in the product: a till without the pairing code gets nothing, which §1 tests both ways. Creating a shop is recorded as a settings fact with its time. Restore is the import side of backup. Joining the wrong shop is reversed by moving the till to another shop on the Tills screen.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | First-run state and Welcome view model | -- | [ ] |
| 2 | §2 | Daylight look: Welcome screen | §1 | [ ] |
| 3 | §3 | Counter look: Welcome screen | §1 | [ ] |
| 4 | §4 | Tape look: Welcome screen | §1 | [ ] |
| 5 | §5 | Startup failure screen | §1 | [ ] |

## 1. First-run state and Welcome view model

The till must know whether it belongs to a shop, and offer the three ways in (`WEL-01` to `WEL-03`).

**Phase:** 3
**Needs:** none
**Surface:** no

**Build order:**

1. Stop `TillRuntime` creating a key by itself and add `WelcomeViewModel` to `src/Merchant.Till/ViewModels`. Done when: each path produces a shop and opens Sell.

- [ ] Remove the automatic shop key from the `TillRuntime` constructor and add `HasShop`, `CreateShop(name)` (new key, `shop.settings` event with the name, `till.named` default) and keep `JoinShop(code)`. Done when: a fresh runtime reports no shop and starts no sync host until it has one. Cheaper substitute: a hidden default shop must fail `Fresh_runtime_has_no_shop`.
- [ ] Add `src/Merchant.Till/ViewModels/WelcomeViewModel.cs` with `ShopName`, `CreateShopCommand`, `PairingCode`, `JoinShopCommand`, `RestorePassphrase`, `RestoreCommand` (file path from an injected picker) and one refusal text per path with the SPEC wording. Done when: an empty name, a short code and a wrong passphrase each set their own refusal and change nothing.
- [ ] On success start the network, then raise `Completed`, which the shell answers by navigating to Sell (with a test double for Sell until D03 T02 §1 registers it). Done when: the sync host is listening and the shell was asked for Sell once.
- [ ] Add `tests/Merchant.Tests/WelcomeViewModelTests.cs` covering all three successes and all three refusals, and that a till without the code cannot sync with the new shop. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~WelcomeViewModelTests"` passes.
- [ ] Commit: `welcome: first run and view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~WelcomeViewModelTests"` passes; restoring the automatic key makes `Fresh_runtime_has_no_shop` fail.

## 2. Daylight look: Welcome screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/welcome.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/welcome.html` under `design/SPEC.md` section 3
**Job:** Set up a new till in under a minute, without an account
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/WelcomeView.axaml` from `design/interface/daylight/welcome.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightWelcomeTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightWelcomeTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/WelcomeView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/welcome.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `WEL-01` (the shop name field and Create shop) in `src/Merchant.Till/Looks/Daylight/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightWelcomeTests.cs`, an empty name is refused and a valid one opens Sell with the settings recorded. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-02` (the pairing code field and Join shop) in `src/Merchant.Till/Looks/Daylight/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightWelcomeTests.cs`, a short code shows the SPEC refusal and a full code stores the key and opens Sell. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-03` (the restore controls) in `src/Merchant.Till/Looks/Daylight/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightWelcomeTests.cs`, a wrong passphrase shows the SPEC refusal and a good backup opens Sell with its sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-04` (the brand panel) in `src/Merchant.Till/Looks/Daylight/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightWelcomeTests.cs`, the mark, name, strapline and version render as on the page. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `DaylightWelcomeTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightWelcomeTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/welcome.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: welcome screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightWelcomeTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/WelcomeView.axaml` makes `Matches_the_reference` fail.

## 3. Counter look: Welcome screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/welcome.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/welcome.html` under `design/SPEC.md` section 3
**Job:** Set up a new till in under a minute, without an account
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/WelcomeView.axaml` from `design/interface/counter/welcome.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterWelcomeTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterWelcomeTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/WelcomeView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/welcome.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `WEL-01` (the shop name field and Create shop) in `src/Merchant.Till/Looks/Counter/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-01`). Done when: in `tests/Merchant.Tests/Screens/CounterWelcomeTests.cs`, an empty name is refused and a valid one opens Sell with the settings recorded. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-02` (the pairing code field and Join shop) in `src/Merchant.Till/Looks/Counter/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-02`). Done when: in `tests/Merchant.Tests/Screens/CounterWelcomeTests.cs`, a short code shows the SPEC refusal and a full code stores the key and opens Sell. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-03` (the restore controls) in `src/Merchant.Till/Looks/Counter/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-03`). Done when: in `tests/Merchant.Tests/Screens/CounterWelcomeTests.cs`, a wrong passphrase shows the SPEC refusal and a good backup opens Sell with its sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-04` (the brand panel) in `src/Merchant.Till/Looks/Counter/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-04`). Done when: in `tests/Merchant.Tests/Screens/CounterWelcomeTests.cs`, the mark, name, strapline and version render as on the page. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Open `X-07` (the passphrase dialog) from `Restore from a backup file` after the file is picked (source: `design/SPEC.md` `X-07`). Done when: the test picks a file, types the passphrase in the dialog and lands on Sell.
- [ ] Add the render test `CounterWelcomeTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterWelcomeTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/welcome.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: welcome screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterWelcomeTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/WelcomeView.axaml` makes `Matches_the_reference` fail.

## 4. Tape look: Welcome screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/welcome.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/welcome.html` under `design/SPEC.md` section 3
**Job:** Set up a new till in under a minute, without an account
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** none: a step screen without navigation

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/WelcomeView.axaml` from `design/interface/tape/welcome.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeWelcomeTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeWelcomeTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/WelcomeView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/welcome.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `WEL-01` (the shop name field and Create shop) in `src/Merchant.Till/Looks/Tape/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-01`). Done when: in `tests/Merchant.Tests/Screens/TapeWelcomeTests.cs`, an empty name is refused and a valid one opens Sell with the settings recorded. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-02` (the pairing code field and Join shop) in `src/Merchant.Till/Looks/Tape/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-02`). Done when: in `tests/Merchant.Tests/Screens/TapeWelcomeTests.cs`, a short code shows the SPEC refusal and a full code stores the key and opens Sell. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-03` (the restore controls) in `src/Merchant.Till/Looks/Tape/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-03`). Done when: in `tests/Merchant.Tests/Screens/TapeWelcomeTests.cs`, a wrong passphrase shows the SPEC refusal and a good backup opens Sell with its sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `WEL-04` (the brand panel) in `src/Merchant.Till/Looks/Tape/WelcomeView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `WEL-04`). Done when: in `tests/Merchant.Tests/Screens/TapeWelcomeTests.cs`, the mark, name, strapline and version render as on the page. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapeWelcomeTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeWelcomeTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/welcome.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: welcome screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeWelcomeTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/WelcomeView.axaml` makes `Matches_the_reference` fail.

## 5. Startup failure screen

If the data folder holds a database but its key cannot be read, the till must say so (`X-12`). Starting empty would look like every sale had vanished.

**Phase:** 3
**Needs:** none
**Surface:** yes
**Fidelity:** not drawn on a page: built from `design/SPEC.md` `X-12` with Daylight tokens
**Job:** Tell the operator the data could not be opened and offer the way back
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** none: shown instead of the shell

**Build order:**

1. Detect the failure in `App.axaml.cs` and show `StartupFailureView`. Done when: no new store is created over the old one.

- [ ] In `TillRuntime.Open`, throw `TillDataException` with a plain reason when `till.db` exists and `device.key` is missing or does not open it. Done when: the old database file is byte for byte unchanged after the attempt. Cheaper substitute: creating a fresh key beside an existing database must fail the test.
- [ ] Add `src/Merchant.Till/ViewModels/StartupFailureViewModel.cs` and `src/Merchant.Till/Looks/StartupFailureView.axaml` (one view for all looks, Daylight tokens): the reason, the data folder path and `Restore from a backup file`. Done when: the screen renders with the reason and the button.
- [ ] Add `tests/Merchant.Tests/StartupFailureTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~StartupFailureTests"` passes.
- [ ] Commit: `welcome: startup failure screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~StartupFailureTests"` passes; catching the exception and opening a new store makes `Existing_data_is_never_replaced` fail.

## Verification

- Starting the built till with an empty `MERCHANT_DATA_DIR` shows Welcome in Daylight; creating a shop opens Sell.
- `dotnet test Merchant.slnx` passes.
