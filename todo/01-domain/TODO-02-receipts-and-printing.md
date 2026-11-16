---
schema_version: 1
id: receipts-and-printing
domain: 01-domain
status: active
title: "TODO-02 -- Receipts, slips and the printer"
---

# TODO-02 -- Receipts, slips and the printer

> **Goal:** Every paper the till prints (sale receipt, refund receipt, day summary, pairing slip, test receipt) is built from the shop's settings for either paper width, and printing reports its outcome without ever blocking a sale.

> **Current state:** Measured 2026-11-17. `src/Merchant.Core/Printing/Receipt.cs` builds one 32-column sale receipt with a fixed shop name and no VAT line, and `NetworkPrinter.SendAsync` throws on failure. `TillRuntime.PrinterHost` is the only printer setting.

## Inputs

- `design/SPEC.md` `DONE-06`, `SET-07` to `SET-12`, `DAY-07`, `TILL-07`, `X-02`, and the receipt drawn in `design/interface/counter/sale-complete.html`.
- `src/Merchant.Core/Printing/Receipt.cs` and its test in `tests/Merchant.Tests/SyncTests.cs`.

## Outcome

- A receipt printed for the fixture sale reads line for line like the receipt on the Counter sale complete page, at 32 or 48 columns.
- A printer that is off produces a reported failure and the sale is already on disk.

**Adjacency:** list=not-applicable (papers are produced from records that the Sales screen lists); document=applicable; settings=applicable @ D05 T02 §2; reporting=applicable @ D01 T02 §3; notifications=not-applicable (the outcome is shown to the operator at the till, there is nobody else to notify); permissions=not-applicable (no user accounts); audit=not-applicable (printing changes no record); exchange=not-applicable (paper only); reverse=not-applicable (a printed paper cannot be unprinted; a refund prints its own receipt in §2)

**Adjacency rationale:** This file is the document adjacency of the sales domain. Its settings are edited on the Settings screen, which owns their form.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Sale receipt from shop settings at both paper widths | T01 §3, T01 §7 | [ ] |
| 2 | §2 | Refund receipt | §1, T01 §8 | [ ] |
| 3 | §3 | Day summary print | §1, T01 §11 | [ ] |
| 4 | §4 | Pairing slip and test receipt | §1 | [ ] |
| 5 | §5 | Printer settings and a print service that never throws | §1 | [ ] |

## 1. Sale receipt from shop settings at both paper widths

The receipt must carry the shop's name, the VAT line, the payment lines and the footer, and fit 58 mm (32 characters) or 80 mm (48 characters) paper (`SET-08`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Rework `Receipt.Build` in `src/Merchant.Core/Printing/Receipt.cs` to take settings, width and the full sale. Done when: both widths match the expected text.

- [ ] Change `Receipt.Build` to `Build(ShopSettings settings, SaleCompleted sale, DateTimeOffset when, int columns, bool openDrawer)` and lay out: shop name bold and centred; date, time and `Sale 0042`; a rule; one row per line; a rule; `TOTAL`; `VAT incl. 15%` (or `VAT 15%` when added on top; left out at rate 0); `Cash` and `Change` for cash, `Card` for card; the footer centred. Done when: the fixture sale at 32 columns equals the text of the receipt in `design/interface/counter/sale-complete.html` line for line. Cheaper substitute: a hard-coded shop name must fail the settings test.
- [ ] Wrap a line name that does not fit beside its amount onto a second row, never truncating the amount. Done when: a 40-character product name at 32 columns keeps its full amount right-aligned.
- [ ] Send the drawer pulse only when `openDrawer` is true and the sale is cash. Done when: the last bytes are the cut alone when the setting is off.
- [ ] Add `tests/Merchant.Tests/ReceiptTests.cs` for both widths, VAT included, VAT added, rate 0, card, and the drawer flag, and move the existing receipt assertions out of `SyncTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes.
- [ ] Commit: `printing: sale receipt from shop settings`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes; hard-coding 32 columns makes `Wide_paper_uses_48_columns` fail.

## 2. Refund receipt

A refund prints its own receipt so the customer and the drawer both have paper (`X-02`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `Receipt.BuildRefund`. Done when: it names the refunded sale and the amount returned.

- [ ] Add `Receipt.BuildRefund(ShopSettings settings, SaleCompleted sale, SaleRefunded refund, DateTimeOffset when, int columns, bool openDrawer)` headed `REFUND`, listing the sale's lines, `REFUNDED` with the total, and the method. Done when: the text contains `REFUND`, `Sale 0038` and `REFUNDED` with 63.00.
- [ ] Send the drawer pulse for a cash refund when the setting is on. Done when: a card refund never carries the pulse.
- [ ] Add refund cases to `tests/Merchant.Tests/ReceiptTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes.
- [ ] Commit: `printing: refund receipt`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes; dropping the sale number makes `Refund_names_the_sale` fail.

## 3. Day summary print

The day summary on paper is the Tape look's Day end paper column (`DAY-07`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `Receipt.BuildDaySummary`. Done when: it equals the paper in `design/interface/tape/day-end.html`.

- [ ] Add `Receipt.BuildDaySummary(ShopSettings settings, DateOnly day, DaySummary summary, IReadOnlyList<SoldLine> sold, IReadOnlyList<TillCount> tills, DayClosed? close, int columns)`. Done when: the fixture day at 32 columns equals the Tape day end paper line for line, with the counted cash and difference added when the day is closed.
- [ ] Add day summary cases to `tests/Merchant.Tests/ReceiptTests.cs`, open and closed. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes.
- [ ] Commit: `printing: day summary`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes; leaving out the per-till lines makes `Day_summary_matches_the_paper` fail.

## 4. Pairing slip and test receipt

Two small papers: the pairing slip (`TILL-07`) and the test receipt (`SET-12`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `Receipt.BuildPairingSlip` and `Receipt.BuildTest`. Done when: each matches its page.

- [ ] Add `Receipt.BuildPairingSlip(ShopSettings settings, string pairingCode, int columns)` with the wording of `design/interface/tape/tills.html` and the code in four rows of two groups. Done when: the text holds the code exactly once, in groups of four.
- [ ] Add `Receipt.BuildTest(ShopSettings settings, int columns)`: the fixture sale as a receipt, headed `TEST RECEIPT`, with a ruler line of `columns` digits. Done when: the ruler line is exactly `columns` characters.
- [ ] Add cases to `tests/Merchant.Tests/ReceiptTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes.
- [ ] Commit: `printing: pairing slip and test receipt`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ReceiptTests"` passes; a ruler of the wrong length makes `Test_receipt_ruler_fits_the_paper` fail.

## 5. Printer settings and a print service that never throws

Printer address, paper width, print-after-sale and open-drawer are this till's settings (`SET-07` to `SET-11`). Printing must report its outcome as a value (`DONE-06`), because a failed print never undoes a sale.

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `PrinterSettings` and `PrintService` to `src/Merchant.Core/Printing`. Done when: a dead printer yields a failure value.

- [ ] Add `src/Merchant.Core/Printing/PrinterSettings.cs` (`Address`, `Columns` 32 or 48, `PrintAfterSale`, `OpenDrawer`) with `Load(SqliteEventStore)` and `Save(SqliteEventStore)` on the local settings table, defaulting to no address, 32, true, true, and reading the old `printer_host` value as the address. Done when: saved values are read back from a reopened store, and a store with only `printer_host` loads that address. Cheaper substitute: recording printer settings as events must fail review: they must not sync.
- [ ] Add `PrinterSettings.Validate()`: the address is empty or `host` or `host:port` with a port from 1 to 65535. Done when: `192.168.1.50:99999` is refused.
- [ ] Add `src/Merchant.Core/Printing/PrintService.cs` with `Task<PrintOutcome> PrintAsync(byte[] paper)` returning `Printed(address)`, `Failed(reason)` or `Off`, catching socket and timeout errors. Done when: printing to a closed port returns `Failed` within four seconds and throws nothing. Cheaper substitute: letting the exception escape to the caller must fail the test.
- [ ] Add `IPrintService` so screens and tests use the same seam, with `RecordingPrintService` in the tests capturing the bytes. Done when: a view model test can assert what was sent.
- [ ] Add `tests/Merchant.Tests/PrintServiceTests.cs` using a loopback `TcpListener` for success and a closed port for failure. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~PrintServiceTests"` passes.
- [ ] Commit: `printing: printer settings and print service`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~PrintServiceTests"` passes; removing the catch in `PrintAsync` makes `Dead_printer_is_a_reported_failure` fail.

## Verification

- Every paper prints to a loopback listener in the tests and reads as its design page shows.
- `dotnet test Merchant.slnx` passes.
