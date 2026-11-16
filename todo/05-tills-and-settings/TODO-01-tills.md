---
schema_version: 1
id: tills
domain: 05-tills-and-settings
status: active
title: "TODO-01 -- Tills and sync"
depends_on: ["looks-and-shell", "receipts-and-printing"]
---

# TODO-01 -- Tills and sync

> **Goal:** The operator sees every till of the shop and when it last synced, adds a till with the pairing code, renames this one, moves it to another shop, and syncs on demand.

> **Current state:** Measured 2026-11-17. Tills find each other and sync every 15 seconds (`TillRuntime`). The old Setup tab shows the pairing code permanently and has a join field and `Sync now`. Nothing lists tills or records when each last synced.

## Inputs

- `design/SPEC.md` section 5.9 (`TILL-01` to `TILL-08`), `X-10` and `X-11`.
- `design/interface/<look>/tills.html` and `.png`.
- `src/Merchant.Core/Sync/PeerDiscovery.cs` and `src/Merchant.Till/TillRuntime.cs`.

## Outcome

- With the fixture each look lists three tills in their three states.
- The pairing code is hidden until asked for.

**Adjacency:** list=applicable; document=applicable @ D01 T02 §4; settings=applicable; reporting=not-applicable (no figures of its own: the day's figures belong to Day end); notifications=applicable; permissions=applicable; audit=applicable; exchange=not-applicable (nothing to import or export here); reverse=applicable

**Adjacency rationale:** The tills list is the list. The pairing slip is the document. The till's name is the setting. Sync results and problems are the notices. The pairing code is the permission, hidden by default and tested both ways in D03 T01. Renames are dated facts. Joining another shop is reversed by joining back with the first shop's code, which the confirmation says.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Tills roster with sync state | -- | [ ] |
| 2 | §2 | Renaming this till | §1 | [ ] |
| 3 | §3 | Pairing code: show, hide, copy and print | §1 | [ ] |
| 4 | §4 | Moving this till to another shop | §1 | [ ] |
| 5 | §5 | Sync now | §1 | [ ] |
| 6 | §6 | Daylight look: Tills screen | §2, §3, §4, §5 | [ ] |
| 7 | §7 | Counter look: Tills screen | §2, §3, §4, §5 | [ ] |
| 8 | §8 | Tape look: Tills screen | §2, §3, §4, §5 | [ ] |

## 1. Tills roster with sync state

Who is in the shop and when each was last heard from (`TILL-01`, `TILL-06`, `TILL-08`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Record each peer's last sync in the till and add `TillsViewModel` to `src/Merchant.Till/ViewModels`. Done when: the fixture shows three states.

- [ ] Store `last_sync:<device id>` (time) in the local settings whenever `TillRuntime` completes a session with a peer, and expose `PeersInRange`. Done when: after a sync with one peer its time is stored.
- [ ] Add `src/Merchant.Till/ViewModels/TillsViewModel.cs` with `Rows`: name, state (`This till`, `In range`, `Out of range`), detail sentence as on the page, and device name. Done when: Till 3 reads `Out of range. Last synced yesterday at 17:05.`.
- [ ] Add `ProblemText` (`Sync problem with <till>: <reason>.`) from the last `SyncReport`. Done when: a failing peer shows its name and reason.
- [ ] Register `TillsViewModel` as the Tills destination through `ShellViewModel.RegisterDestination` in `src/Merchant.Till/App.axaml.cs`. Done when: navigating to Tills from the shell shows the screen.
- [ ] Add `tests/Merchant.Tests/TillsViewModelTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TillsViewModelTests"` passes.
- [ ] Commit: `tills: roster view model`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TillsViewModelTests"` passes; treating an unseen peer as in range makes `Absent_till_is_out_of_range` fail.

## 2. Renaming this till

A name people recognise (`TILL-03`, `X-10`).

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** the dialog is not drawn on a page: built from `design/SPEC.md` `X-10` and section 7
**Job:** Give this till a name people recognise
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** the dialog host from D02 T01 §11

**Build order:**

1. Add renaming to `TillsViewModel`. Done when: a new name is recorded and shown.

- [ ] Add `Name` and `SaveNameCommand` (Counter's inline form) and `RenameCommand` opening `X-10` (Daylight and Tape), both appending `till.named` after validation. Done when: an empty name is refused with a field refusal.
- [ ] Add `src/Merchant.Till/ViewModels/RenameDialogViewModel.cs` and `src/Merchant.Till/Looks/RenameDialog.axaml`. Done when: it renders in all three looks.
- [ ] Add `tests/Merchant.Tests/TillRenameTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TillRenameTests"` passes.
- [ ] Commit: `tills: rename`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TillRenameTests"` passes; skipping validation makes `Empty_name_is_refused` fail.

## 3. Pairing code: show, hide, copy and print

The code is a secret that opens the shop (`TILL-04`, `TILL-07`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Add the code panel to `TillsViewModel`. Done when: it is hidden on opening.

- [ ] Add `IsCodeShown` (false whenever the screen opens), `ShowCodeCommand`, `HideCodeCommand` and `CodeText` in groups of four. Done when: navigating away and back hides the code again. Cheaper substitute: remembering the shown state must fail the test.
- [ ] Add `CopyCodeCommand` through an injected clipboard service and report `Pairing code copied.`. Done when: the test clipboard holds the code.
- [ ] Add `PrintSlipCommand` printing `Receipt.BuildPairingSlip`. Done when: the recorded bytes hold the code once.
- [ ] Add `tests/Merchant.Tests/PairingCodeTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~PairingCodeTests"` passes.
- [ ] Commit: `tills: pairing code panel`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~PairingCodeTests"` passes; showing the code on opening makes `Code_is_hidden_on_opening` fail.

## 4. Moving this till to another shop

Joining another shop is rare and has consequences, so it asks (`TILL-05`, `X-11`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Add the move to `TillsViewModel`. Done when: confirm changes the shop key.

- [ ] Add `OtherShopCode` and `JoinOtherShopCommand`: refuse an incomplete code with the SPEC sentence, else open `X-11` and call `TillRuntime.JoinShop` only on `Join that shop`. Done when: `Stay in this shop` leaves the key unchanged.
- [ ] Add `tests/Merchant.Tests/JoinOtherShopTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~JoinOtherShopTests"` passes.
- [ ] Commit: `tills: move to another shop`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~JoinOtherShopTests"` passes; joining without the dialog makes `Staying_changes_nothing` fail.

## 5. Sync now

Sync on demand (`TILL-02`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Add `SyncNowCommand` to `TillsViewModel`. Done when: it runs one sync and reports.

- [ ] Add `SyncNowCommand` calling `TillRuntime.SyncNowAsync`, disabled while a sync runs, reporting `Synced with 2 tills: 3 in, 1 out.` or `No other tills in range.`. Done when: two runtimes in a test exchange an event through the command.
- [ ] Add `tests/Merchant.Tests/SyncNowTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SyncNowTests"` passes.
- [ ] Commit: `tills: sync now`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SyncNowTests"` passes; reporting success with no peers makes `No_peers_is_reported` fail.

## 6. Daylight look: Tills screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/tills.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/tills.html` under `design/SPEC.md` section 3
**Job:** See the shop's tills and add another
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** the Daylight shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/TillsView.axaml` from `design/interface/daylight/tills.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightTillsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/TillsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/tills.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `TILL-01` (the tills list) in `src/Merchant.Till/Looks/Daylight/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, this till, a till in range and a till out of range each show their state, sync time and sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-02` (the sync now control) in `src/Merchant.Till/Looks/Daylight/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, it runs one sync and reports the result. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-03` (the rename control) in `src/Merchant.Till/Looks/Daylight/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, a new name is recorded and shown, and an empty name is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-04` (the pairing code panel) in `src/Merchant.Till/Looks/Daylight/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, the code is hidden on opening, Show reveals it in groups of four and Copy puts it on the clipboard. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-05` (the move to another shop controls) in `src/Merchant.Till/Looks/Daylight/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, a full code opens `X-11` and a short one is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-06` (the sync explanation) in `src/Merchant.Till/Looks/Daylight/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, the paragraph matches the page. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-08` (the sync problem line) in `src/Merchant.Till/Looks/Daylight/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-08`). Done when: in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, a failing peer shows its name and reason under the list. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `DaylightTillsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightTillsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/tills.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: tills screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightTillsTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/TillsView.axaml` makes `Matches_the_reference` fail.

## 7. Counter look: Tills screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/tills.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/tills.html` under `design/SPEC.md` section 3
**Job:** See the shop's tills and add another
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** the Counter shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/TillsView.axaml` from `design/interface/counter/tills.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterTillsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/TillsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/tills.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `TILL-01` (the tills list) in `src/Merchant.Till/Looks/Counter/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-01`). Done when: in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, this till, a till in range and a till out of range each show their state, sync time and sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-02` (the sync now control) in `src/Merchant.Till/Looks/Counter/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-02`). Done when: in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, it runs one sync and reports the result. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-03` (the rename control) in `src/Merchant.Till/Looks/Counter/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-03`). Done when: in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, a new name is recorded and shown, and an empty name is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-04` (the pairing code panel) in `src/Merchant.Till/Looks/Counter/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-04`). Done when: in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, the code is hidden on opening, Show reveals it in groups of four and Copy puts it on the clipboard. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-05` (the move to another shop controls) in `src/Merchant.Till/Looks/Counter/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-05`). Done when: in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, a full code opens `X-11` and a short one is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-06` (the sync explanation) in `src/Merchant.Till/Looks/Counter/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-06`). Done when: in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, the paragraph matches the page. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-08` (the sync problem line) in `src/Merchant.Till/Looks/Counter/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-08`). Done when: in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, a failing peer shows its name and reason under the list. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `CounterTillsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterTillsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/tills.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: tills screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterTillsTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/TillsView.axaml` makes `Matches_the_reference` fail.

## 8. Tape look: Tills screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/tills.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/tills.html` under `design/SPEC.md` section 3
**Job:** See the shop's tills and add another
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** the Tape shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/TillsView.axaml` from `design/interface/tape/tills.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeTillsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/TillsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/tills.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `TILL-01` (the tills list) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-01`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, this till, a till in range and a till out of range each show their state, sync time and sales. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-02` (the sync now control) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-02`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, it runs one sync and reports the result. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-03` (the rename control) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-03`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, a new name is recorded and shown, and an empty name is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-04` (the pairing code panel) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-04`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, the code is hidden on opening, Show reveals it in groups of four and Copy puts it on the clipboard. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-05` (the move to another shop controls) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-05`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, a full code opens `X-11` and a short one is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-06` (the sync explanation) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-06`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, the paragraph matches the page. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-08` (the sync problem line) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-08`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, a failing peer shows its name and reason under the list. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `TILL-07` (the print slip control) in `src/Merchant.Till/Looks/Tape/TillsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `TILL-07`). Done when: in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, it sends the pairing slip to the test printer. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Add the render test `TapeTillsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeTillsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/tills.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: tills screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeTillsTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/TillsView.axaml` makes `Matches_the_reference` fail.

## Verification

- `dotnet test Merchant.slnx` passes.
- The three renders match their references with the pairing code shown, as the pages draw it.
