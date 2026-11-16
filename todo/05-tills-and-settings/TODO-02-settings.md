---
schema_version: 1
id: settings
domain: 05-tills-and-settings
status: active
title: "TODO-02 -- Settings"
depends_on: ["looks-and-shell", "receipts-and-printing"]
---

# TODO-02 -- Settings

> **Goal:** The operator sets up the shop, the printer, backups and the look, and each change takes effect where the contract says.

> **Current state:** Measured 2026-11-17. The old Setup tab has a printer address, a backup passphrase with save and restore, and nothing else. `SqliteEventStore.ExportBackup` and `RestoreBackup` exist and are tested.

## Inputs

- `design/SPEC.md` section 5.10 (`SET-01` to `SET-20`).
- `design/interface/<look>/settings.html` and `.png`.

## Outcome

- Changing the VAT rate in a test changes the Sell totals on a second till after sync.
- Choosing Tape changes the visible screen at once and after a restart.

**Adjacency:** list=not-applicable (settings are one form); document=not-applicable (the test receipt is owned by D01 T02 §4); settings=applicable; reporting=not-applicable (no figures of its own: the day's figures belong to Day end); notifications=applicable; permissions=not-applicable (no user accounts); audit=applicable; exchange=applicable @ D05 T02 §3; reverse=applicable

**Adjacency rationale:** This file is the settings surface of the whole product: every tunable named in the contract has its field here and a consumer tested elsewhere. Shop settings are dated facts. Backup and restore are the exchange. Every setting is reversed by setting it back; a restore only adds records.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Shop settings form | -- | [ ] |
| 2 | §2 | Printer form and test receipt | §1 | [ ] |
| 3 | §3 | Backup and restore | §1 | [ ] |
| 4 | §4 | About and the data folder | §1 | [ ] |
| 5 | §5 | Look chooser | §1 | [ ] |
| 6 | §6 | Daylight look: Settings screen | §2, §3, §4, §5 | [ ] |
| 7 | §7 | Counter look: Settings screen | §2, §3, §4, §5 | [ ] |
| 8 | §8 | Tape look: Settings screen | §2, §3, §4, §5, §6, §7 | [ ] |

## 1. Shop settings form

The shop's own settings (`SET-01` to `SET-06`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Add `SettingsViewModel` to `src/Merchant.Till/ViewModels` with the shop form. Done when: saving appends one `shop.settings` fact.

- [ ] Add `src/Merchant.Till/ViewModels/SettingsViewModel.cs` with the five shop fields loaded from `ShopState.Settings`, a refusal per field, and `SaveShopCommand` appending `shop.settings` and reporting `Shop settings saved.`. Done when: a VAT rate of 101 is refused and nothing is appended.
- [ ] Register `SettingsViewModel` as the Settings destination through `ShellViewModel.RegisterDestination` in `src/Merchant.Till/App.axaml.cs`. Done when: navigating to Settings from the shell shows the screen.
- [ ] Add `tests/Merchant.Tests/ShopSettingsFormTests.cs` asserting a second store sees the new VAT rate after sync and that Sell's totals follow `PricesIncludeVat`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ShopSettingsFormTests"` passes.
- [ ] Commit: `settings: shop form`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ShopSettingsFormTests"` passes; saving without validation makes `Bad_vat_rate_is_refused` fail.

## 2. Printer form and test receipt

This till's printer (`SET-07` to `SET-12`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Add the printer form to `SettingsViewModel`. Done when: saved values survive reopening.

- [ ] Add the address, paper width choice and two switches from `PrinterSettings`, `SavePrinterCommand` (validating the address) and `TestPrintCommand` reporting `Test receipt sent to <address>.` or the failure. Done when: a closed port reports the failure sentence.
- [ ] Add `tests/Merchant.Tests/PrinterFormTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~PrinterFormTests"` passes.
- [ ] Commit: `settings: printer form`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~PrinterFormTests"` passes; reporting success on a closed port makes `Dead_printer_is_reported` fail.

## 3. Backup and restore

Backups (`SET-13` to `SET-16`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Add the backup form to `SettingsViewModel`. Done when: a saved backup restores into a fresh store.

- [ ] Add `Passphrase`, `SaveBackupCommand` and `RestoreBackupCommand` through injected file pickers, refusing an empty passphrase with `Type a backup passphrase first.`. Done when: both commands refuse an empty passphrase.
- [ ] Store `last_backup` (time) locally on a successful save and expose `LastBackupText` (`Last backup: yesterday at 17:12.` or `No backup yet.`). Done when: the text changes after a save.
- [ ] Report `Backup saved to <path>.`, `Restored <n> new records from the backup.` or the refusal. Done when: a wrong passphrase reports the SPEC sentence.
- [ ] Add `tests/Merchant.Tests/BackupFormTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~BackupFormTests"` passes.
- [ ] Commit: `settings: backup form`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~BackupFormTests"` passes; allowing an empty passphrase makes `Empty_passphrase_is_refused` fail.

## 4. About and the data folder

What the till is and where its data lives (`SET-17`, `SET-18`).

**Phase:** 5
**Needs:** none
**Surface:** no

**Build order:**

1. Add the about rows to `SettingsViewModel`. Done when: they show real values.

- [ ] Add `VersionText` from the assembly's informational version, `DeviceName`, `DataFolder` and `LicenceText` (`AGPL-3.0, open source`). Done when: the version equals the assembly's.
- [ ] Add `OpenDataFolderCommand` through an injected launcher service (the real one starts the system file manager on the folder). Done when: the test launcher receives the data folder path. Cheaper substitute: starting a process directly from the view model must fail review.
- [ ] Add `tests/Merchant.Tests/AboutTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~AboutTests"` passes.
- [ ] Commit: `settings: about`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~AboutTests"` passes; a hard-coded version makes `Version_is_the_assembly_version` fail.

## 5. Look chooser

Choosing the look (`SET-19`), the one control the pages do not draw.

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** not drawn on a page: built from `design/SPEC.md` `SET-19`
**Job:** Pick the look that suits the light and the operator
**Treatment:** each look's tokens and control styles from D02 T01
**Chrome:** inside the Settings screen

**Build order:**

1. Add the chooser to `SettingsViewModel` and its card to each look. Done when: choosing changes the screen at once.

- [ ] Add `Looks` (three entries with name and `IsCurrent`) and `ChooseLookCommand` calling `LookManager.Set`. Done when: choosing Tape sets the manager to Tape.
- [ ] Add `src/Merchant.Till/Looks/LookChooser.axaml`: a card titled `Look` spanning both columns with three 180 x 112 px buttons, each showing a miniature of that look's Sell screen drawn from its tokens (ground, surface block, accent bar) above its name, the current one with the primary border. Done when: the chooser renders in all three looks with the current look marked. Cheaper substitute: a screenshot bitmap as the miniature must fail review: it is drawn from tokens.
- [ ] Add `tests/Merchant.Tests/Screens/LookChooserTests.cs` rendering the chooser card alone in each look, choosing each entry, and reopening the store to read the choice back. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~LookChooserTests"` passes.
- [ ] Commit: `settings: look chooser`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~LookChooserTests"` passes; not persisting the choice makes `Look_survives_a_restart` fail.

## 6. Daylight look: Settings screen

The Daylight view of this screen, drawn 1:1 from `design/interface/daylight/settings.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/daylight/settings.html` under `design/SPEC.md` section 3
**Job:** Set the shop up once and adjust it rarely
**Treatment:** Daylight tokens and control styles from D02 T01
**Chrome:** the Daylight shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Daylight/SettingsView.axaml` from `design/interface/daylight/settings.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSettingsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Daylight/SettingsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/daylight/settings.html` and every colour from the Daylight tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Daylight/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SET-01` (the shop name field) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-01`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, an empty name is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-02` (the currency symbol field) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-02`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, a new symbol shows in every amount after saving. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-03` (the VAT rate field) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-03`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, 101 is refused and 0 hides the VAT lines. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-04` (the prices include VAT switch) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-04`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, turning it off adds VAT on top in the Sell totals. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-05` (the receipt footer field) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-05`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, the footer prints on the next receipt. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-06` (the save shop settings control) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-06`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, it records one settings event. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-07` (the printer address field) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-07`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, an empty address turns printing off. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-08` (the paper width choice) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-08`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, 80 mm prints 48-character lines. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-09` (the print after every sale switch) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-09`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, off sends nothing to the printer on a sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-10` (the open drawer switch) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-10`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, off leaves the drawer pulse out of a cash receipt. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-11` (the save printer control) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-11`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, the values are still there after reopening the store. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-12` (the test receipt control) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-12`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, it reports sent or the failure. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-13` (the backup passphrase field) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-13`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, both backup actions refuse an empty passphrase. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-14` (the save backup control) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-14`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, it writes a file the restore test can read. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-15` (the restore backup control) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-15`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, it adds the backup's sales and reports how many. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-16` (the last backup time) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-16`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, it reads No backup yet, then the time of the last save. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-17` (the about rows) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-17`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, version, device name and licence show real values. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-18` (the open data folder control) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-18`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, it asks the launcher service to open the data folder. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-19` (the look chooser) in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-19`). Done when: in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, choosing a look changes the visible screen at once and survives a restart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Place the look chooser card after About; the reference page was drawn before `SET-19`, so render the comparison with the chooser collapsed by the test hook `SettingsViewModel.ShowLookChooser = false`, and add a second render with it shown saved as `settings-look.png`. Done when: both renders are written and the first matches the reference. Cheaper substitute: hiding the chooser in the shipped screen must fail review.
- [ ] Add the render test `DaylightSettingsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/DaylightSettingsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/daylight/settings.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `daylight: settings screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DaylightSettingsTests"` passes; moving a control in `src/Merchant.Till/Looks/Daylight/SettingsView.axaml` makes `Matches_the_reference` fail.

## 7. Counter look: Settings screen

The Counter view of this screen, drawn 1:1 from `design/interface/counter/settings.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/counter/settings.html` under `design/SPEC.md` section 3
**Job:** Set the shop up once and adjust it rarely
**Treatment:** Counter tokens and control styles from D02 T01
**Chrome:** the Counter shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Counter/SettingsView.axaml` from `design/interface/counter/settings.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSettingsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Counter/SettingsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/counter/settings.html` and every colour from the Counter tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Counter/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SET-01` (the shop name field) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-01`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, an empty name is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-02` (the currency symbol field) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-02`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, a new symbol shows in every amount after saving. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-03` (the VAT rate field) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-03`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, 101 is refused and 0 hides the VAT lines. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-04` (the prices include VAT switch) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-04`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, turning it off adds VAT on top in the Sell totals. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-05` (the receipt footer field) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-05`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, the footer prints on the next receipt. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-06` (the save shop settings control) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-06`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, it records one settings event. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-07` (the printer address field) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-07`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, an empty address turns printing off. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-08` (the paper width choice) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-08`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, 80 mm prints 48-character lines. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-09` (the print after every sale switch) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-09`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, off sends nothing to the printer on a sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-10` (the open drawer switch) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-10`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, off leaves the drawer pulse out of a cash receipt. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-11` (the save printer control) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-11`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, the values are still there after reopening the store. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-12` (the test receipt control) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-12`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, it reports sent or the failure. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-13` (the backup passphrase field) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-13`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, both backup actions refuse an empty passphrase. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-14` (the save backup control) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-14`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, it writes a file the restore test can read. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-15` (the restore backup control) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-15`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, it adds the backup's sales and reports how many. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-16` (the last backup time) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-16`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, it reads No backup yet, then the time of the last save. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-17` (the about rows) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-17`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, version, device name and licence show real values. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-18` (the open data folder control) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-18`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, it asks the launcher service to open the data folder. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-19` (the look chooser) in `src/Merchant.Till/Looks/Counter/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-19`). Done when: in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, choosing a look changes the visible screen at once and survives a restart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Place the look chooser card after About; the reference page was drawn before `SET-19`, so render the comparison with the chooser collapsed by the test hook `SettingsViewModel.ShowLookChooser = false`, and add a second render with it shown saved as `settings-look.png`. Done when: both renders are written and the first matches the reference. Cheaper substitute: hiding the chooser in the shipped screen must fail review.
- [ ] Add the render test `CounterSettingsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/CounterSettingsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/counter/settings.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `counter: settings screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CounterSettingsTests"` passes; moving a control in `src/Merchant.Till/Looks/Counter/SettingsView.axaml` makes `Matches_the_reference` fail.

## 8. Tape look: Settings screen

The Tape view of this screen, drawn 1:1 from `design/interface/tape/settings.html`. It holds no behaviour of its own: every control binds to the shared view model, so the screen works the same in all three looks.

**Phase:** 5
**Needs:** none
**Surface:** yes
**Fidelity:** 1:1 with `design/interface/tape/settings.html` under `design/SPEC.md` section 3
**Job:** Set the shop up once and adjust it rarely
**Treatment:** Tape tokens and control styles from D02 T01
**Chrome:** the Tape shell (`SH-01` to `SH-06`)

**Build order:**

1. Lay out `src/Merchant.Till/Looks/Tape/SettingsView.axaml` from `design/interface/tape/settings.html`. Done when: it loads with the fixture.
2. Bind each control id listed below and prove it in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`. Done when: every interaction test passes.
3. Compare the render with the reference PNG. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSettingsTests"` passes.

- [ ] Create `src/Merchant.Till/Looks/Tape/SettingsView.axaml` with the page's layout, taking every size, gap, radius and typeface from `design/interface/tape/settings.html` and every colour from the Tape tokens. Done when: the view loads in the headless test with no binding errors logged, and `src/Merchant.Till/Looks/Tape/Views.cs` maps the screen's view model to it. Cheaper substitute: a literal colour or a size that differs from the page must fail review.
- [ ] Build `SET-01` (the shop name field) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-01`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, an empty name is refused. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-02` (the currency symbol field) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-02`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, a new symbol shows in every amount after saving. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-03` (the VAT rate field) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-03`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, 101 is refused and 0 hides the VAT lines. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-04` (the prices include VAT switch) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-04`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, turning it off adds VAT on top in the Sell totals. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-05` (the receipt footer field) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-05`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, the footer prints on the next receipt. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-06` (the save shop settings control) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-06`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, it records one settings event. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-07` (the printer address field) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-07`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, an empty address turns printing off. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-08` (the paper width choice) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-08`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, 80 mm prints 48-character lines. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-09` (the print after every sale switch) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-09`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, off sends nothing to the printer on a sale. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-10` (the open drawer switch) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-10`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, off leaves the drawer pulse out of a cash receipt. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-11` (the save printer control) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-11`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, the values are still there after reopening the store. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-12` (the test receipt control) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-12`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, it reports sent or the failure. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-13` (the backup passphrase field) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-13`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, both backup actions refuse an empty passphrase. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-14` (the save backup control) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-14`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, it writes a file the restore test can read. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-15` (the restore backup control) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-15`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, it adds the backup's sales and reports how many. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-16` (the last backup time) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-16`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, it reads No backup yet, then the time of the last save. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-17` (the about rows) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-17`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, version, device name and licence show real values. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-18` (the open data folder control) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-18`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, it asks the launcher service to open the data folder. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-19` (the look chooser) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-19`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, choosing a look changes the visible screen at once and survives a restart. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Build `SET-20` (the live receipt preview) in `src/Merchant.Till/Looks/Tape/SettingsView.axaml`, bound to the screen's view model (source: `design/SPEC.md` `SET-20`). Done when: in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, typing a shop name changes the paper preview before saving. Cheaper substitute: a control that is drawn but not bound, or bound to look-specific logic, must fail the test.
- [ ] Place the look chooser card after About; the reference page was drawn before `SET-19`, so render the comparison with the chooser collapsed by the test hook `SettingsViewModel.ShowLookChooser = false`, and add a second render with it shown saved as `settings-look.png`. Done when: both renders are written and the first matches the reference. Cheaper substitute: hiding the chooser in the shipped screen must fail review.
- [ ] Add `tests/Merchant.Tests/Screens/SettingsLookSwitchTests.cs`: on the Settings screen choose each look in turn and render Settings after each, now that all three Settings views exist. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SettingsLookSwitchTests"` passes and each render is in the chosen look. Cheaper substitute: switching the view model without the view changing must fail the render assertion.
- [ ] Add the render test `TapeSettingsTests.Matches_the_reference` in `tests/Merchant.Tests/Screens/TapeSettingsTests.cs`, rendering the view with `DuneFixture` at 1280 x 800 through `ScreenRenderer` and comparing it with `design/interface/tape/settings.png` through `ScreenComparer`. Done when: the test passes, and fails when a control is moved 20 px. Cheaper substitute: comparing the render with a copy of itself, or raising the comparer's limits, must fail review.
- [ ] Commit: `tape: settings screen`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TapeSettingsTests"` passes; moving a control in `src/Merchant.Till/Looks/Tape/SettingsView.axaml` makes `Matches_the_reference` fail.

## Verification

- `dotnet test Merchant.slnx` passes.
- The three renders match their references, and `build/screens/<look>/settings-look.png` shows the chooser.
