---
schema_version: 1
id: shop-facts
domain: 01-domain
status: active
title: "TODO-01 -- The facts the interface needs"
---

# TODO-01 -- The facts the interface needs

> **Goal:** The event log records everything the interface shows (`design/SPEC.md` section 8): shop settings, categories, richer products, stock, numbered sales with VAT, refunds, day closes and till names, in an order that a wrong clock cannot scramble.

> **Current state:** Measured 2026-11-17. `src/Merchant.Core/Domain/ShopEvents.cs` defines `product.defined` (id, name, price) and `sale.completed` (id, lines, total, tender). `src/Merchant.Core/Domain/ShopState.cs` derives the product list and per-day totals. `SqliteEventStore.ReadAll` orders by wall-clock time, then device, then sequence. There are no settings, categories, stock, refunds, day closes or till names. `MainViewModel.TryParsePrice` and `MainViewModel.Money` hold the money rules.

## Inputs

- `design/SPEC.md` section 8 (the facts and their fields) and section 9 (the figures the queries must reproduce).
- `src/Merchant.Core/Events/EventRecord.cs`, `src/Merchant.Core/Storage/SqliteEventStore.cs`, `src/Merchant.Core/Sync/SyncProtocol.cs`: where the logical clock is added.
- `README.md` (how events and sync work).

## Outcome

- Two tills that have synced derive the same shop, whatever order events arrived in and whatever their clocks said.
- Logs written by the build before this file still open and show their products and sales.
- Every figure on the Sales and Day end screens comes from a `ShopState` query with a test.

**Adjacency:** list=applicable @ D04 T02 §1; document=applicable @ D01 T02 §1; settings=applicable @ D05 T02 §1; reporting=applicable @ D04 T03 §1; notifications=not-applicable (a till has one operator in front of it and sends no notices); permissions=not-applicable (a paired till is fully trusted and there are no user accounts); audit=applicable; exchange=applicable @ D05 T02 §3; reverse=applicable @ D03 T04 §1

**Adjacency rationale:** The log itself is the audit trail: every fact names its till and time and nothing is edited or deleted, which §8 and §9 keep true for refunds and closes. Sales are found without their number through the Sales screen, carried as receipts, summarised at day end, exported and imported as backups, and reversed by refund; each of those is owned by the section named on the line.

## Implementation Order

| Order | Section | Deliverable | Depends On | Status |
| ----- | ------- | ----------- | ---------- | ------ |
| 1 | §1 | Event order that survives a clock change | -- | [ ] |
| 2 | §2 | Money and VAT arithmetic | -- | [ ] |
| 3 | §3 | Shop settings as a synced fact | §1 | [ ] |
| 4 | §4 | Categories | §1 | [ ] |
| 5 | §5 | Products with category, key colour and removal | §4 | [ ] |
| 6 | §6 | Counted stock | §5, §8 | [ ] |
| 7 | §7 | Sales with number, VAT and payment detail | §2, §3 | [ ] |
| 8 | §8 | Refunds | §7 | [ ] |
| 9 | §9 | Day close and float | §7 | [ ] |
| 10 | §10 | Till names | §1 | [ ] |
| 11 | §11 | Sales and day queries | §7, §8, §9, §10 | [ ] |

## 1. Event order that survives a clock change

Events replay in wall-clock order today (`SqliteEventStore.ReadAll` orders by `occurred_ms`, then device, then sequence), so a product edit made after the till's clock is set back loses to the older edit. Each event gains a logical clock that only moves forward, and replay follows it.

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add a `Clock` value to `EventRecord` and the `events` table. Done when: appended and imported events carry it.
2. Replay in clock order. Done when: the backwards-clock test passes.

- [ ] Add `long Clock` to `EventRecord` in `src/Merchant.Core/Events/EventRecord.cs` and a `clock INTEGER NOT NULL` column to `events` in `src/Merchant.Core/Storage/SqliteEventStore.cs`. Done when: a new store creates the column and `ReadAll` returns the value stored. Cheaper substitute: deriving the clock from the wall time must fail the test below.
- [ ] Migrate an existing `events` table in `SqliteEventStore` on open: add the column when it is missing and number existing rows 1..n in their current replay order (`occurred_ms`, `device_id`, `seq`), in one transaction. Done when: a store file written by the previous build opens and replays its events in the same order as before. Cheaper substitute: dropping and recreating the table must fail review: it would lose sales.
- [ ] In `Append`, set the clock to one more than the highest clock in the store; in `ImportAsync`, keep each event's own clock. Done when: after importing an event with clock 900, the next local event has clock 901.
- [ ] Change `ReadAll` to `ORDER BY clock, device_id, seq`. Done when: two stores holding the same events return them in the same order.
- [ ] Carry `Clock` through `SyncProtocol` records so it syncs unchanged. Done when: the sync test's two stores are equal including clocks.
- [ ] Add `tests/Merchant.Tests/ClockOrderTests.cs`: define a product, import a second definition stamped earlier than the first but with a higher clock (as a clock set back would produce), and assert the second wins in `ShopState`; assert a pre-migration store file still loads. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ClockOrderTests"` passes and fails when `ReadAll` orders by `occurred_ms`.
- [ ] Commit: `core: order events by a logical clock`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ClockOrderTests"` passes; restoring `ORDER BY occurred_ms` in `ReadAll` makes `Later_edit_wins_after_clock_set_back` fail.

## 2. Money and VAT arithmetic

Every amount on every screen and receipt comes from one place. Amounts are whole cents; display follows `design/SPEC.md` section 4 (`R 3 864.00`); VAT is either inside the price or added on top (`SET-03`, `SET-04`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `Money` and `Vat` to `src/Merchant.Core/Domain`. Done when: the cases below pass.

- [ ] Add `src/Merchant.Core/Domain/Money.cs` with `Format(long cents, string symbol)` giving the symbol, a space, thousands separated by a space and two decimals, and `FormatBare(long cents)` without the symbol. Done when: 386400 with `R` gives `R 3 864.00` and -6300 gives `- R 63.00`. Cheaper substitute: culture-dependent formatting must fail on a machine set to another region.
- [ ] Move `TryParsePrice` from `src/Merchant.Till/ViewModels/MainViewModel.cs` to `src/Merchant.Core/Domain/Money.cs` as `Money.TryParse`, keeping its rules. Done when: the existing price theory tests pass against the new home.
- [ ] Add `src/Merchant.Core/Domain/Vat.cs` with `Included(long total, decimal ratePercent)` (the VAT inside a total, rounded half away from zero to the cent) and `Added(long net, decimal ratePercent)`. Done when: `Included(15500, 15)` is 2022 and `Added(10000, 15)` is 1500; a rate of 0 gives 0.
- [ ] Add `tests/Merchant.Tests/MoneyTests.cs` covering formatting, parsing and both VAT modes, including 386400 at 15% included giving 50400. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~MoneyTests"` passes and fails if rounding is changed to truncation.
- [ ] Commit: `core: money formatting and VAT arithmetic`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~MoneyTests"` passes; truncating instead of rounding in `Vat.Included` makes the 155.00 case fail.

## 3. Shop settings as a synced fact

Shop name, currency symbol, VAT rate, whether prices include VAT, and the receipt footer belong to the shop and must reach every till (`SET-01` to `SET-06`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add the `shop.settings` event and `ShopSettings` to `src/Merchant.Core/Domain`. Done when: the newest event wins in `ShopState`.

- [ ] Add `ShopSettingsChanged(string Name, string CurrencySymbol, decimal VatRatePercent, bool PricesIncludeVat, string ReceiptFooter)` and `EventTypes.ShopSettings = "shop.settings"` to `src/Merchant.Core/Domain/ShopEvents.cs`. Done when: the payload round-trips through `ShopJson`.
- [ ] Add `ShopState.Settings` in `src/Merchant.Core/Domain/ShopState.cs`, defaulting to an empty name, `R`, 15, true and `Thank you`, replaced by each `shop.settings` event in order. Done when: two events in a log leave the second one's values. Cheaper substitute: keeping settings in the till's local settings table must fail the two-store sync assertion.
- [ ] Add validation `ShopSettingsChanged.Validate()` returning the first refusal: name required and at most 40, symbol 1 to 4, rate 0 to 100 with at most two decimals, footer at most 32. Done when: each bad value returns its own message and a good value returns none.
- [ ] Add `tests/Merchant.Tests/ShopSettingsTests.cs` for defaults, newest-wins, validation and sync between two stores. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ShopSettingsTests"` passes.
- [ ] Commit: `core: shop settings event`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ShopSettingsTests"` passes; ignoring the event in `ShopState.From` makes `Newest_settings_win` fail.

## 4. Categories

Products are grouped into categories (`SELL-02`, `PROD-04`, `X-05`), which also give Tape keys their default colour.

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `category.defined` to `src/Merchant.Core/Domain`. Done when: `ShopState.Categories` lists them in position order.

- [ ] Add `CategoryDefined(Guid CategoryId, string Name, int Position)` and its event type to `src/Merchant.Core/Domain/ShopEvents.cs`. Done when: the payload round-trips.
- [ ] Add `ShopState.Categories` (id, name, position, default key colour from the four colours in `design/SPEC.md` section 4 by position modulo 4), newest definition per id winning. Done when: four categories get amber, blue, coral and lilac in order, and a fifth gets amber.
- [ ] Add `CategoryDefined.Validate(existingNames)`: required, at most 20 characters, unique ignoring case. Done when: a duplicate differing only in case is refused.
- [ ] Add `tests/Merchant.Tests/CategoryTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~CategoryTests"` passes.
- [ ] Commit: `core: categories`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~CategoryTests"` passes; removing the uniqueness check makes `Duplicate_name_is_refused` fail.

## 5. Products with category, key colour and removal

The existing `product.defined` event carries only name and price. The interface needs category, key colour and whether stock is counted, and a way to take a product off the till without touching past sales (`PROD-04`, `PROD-06`, `PROD-08`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Extend `ProductDefined` and add `product.removed` in `src/Merchant.Core/Domain`. Done when: old events still load and removed products leave `ShopState.Products`.

- [ ] Add optional `Guid? CategoryId`, `string? KeyColour` and `bool CountsStock` to `ProductDefined` in `src/Merchant.Core/Domain/ShopEvents.cs`. Done when: an event written by the previous build (name and price only) still deserialises, with no category, no colour and stock not counted. Cheaper substitute: a new event type replacing `product.defined` must fail the old-log test.
- [ ] Add `ProductRemoved(Guid ProductId)` and its event type; `ShopState.Products` leaves out a product whose newest fact is a removal, and a later definition brings it back. Done when: define, remove, define again leaves the product listed once.
- [ ] Extend `Product` in `src/Merchant.Core/Domain/ShopState.cs` with category id, effective key colour (its own, else its category's default, else amber) and `CountsStock`. Done when: a product without a colour in the Coffee category reports amber.
- [ ] Add `ProductDefined.Validate()`: name required and at most 30 characters, price 0 or more. Done when: an empty name and a negative price are each refused.
- [ ] Add `tests/Merchant.Tests/ProductFactTests.cs` covering the old payload, removal, re-adding and colour fallback. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductFactTests"` passes.
- [ ] Commit: `core: product category, key colour and removal`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~ProductFactTests"` passes; treating `product.removed` as unknown makes `Removed_product_leaves_the_list` fail.

## 6. Counted stock

A product may count stock. The count is the last adjustment, less units sold, plus units refunded since (`design/SPEC.md` section 8). Selling never fails for lack of stock (`SELL-13`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `stock.adjusted` and the derived count to `src/Merchant.Core/Domain`. Done when: the count follows sales and refunds.

- [ ] Add `StockAdjusted(Guid ProductId, int Count, string Reason)` and its event type to `src/Merchant.Core/Domain/ShopEvents.cs`. Done when: the payload round-trips.
- [ ] Add `ShopState.StockOf(Guid productId)` returning null for a product that does not count stock, else the derived count, which may be negative. Done when: set 5, sell 2, refund 1 gives 4; sell 6 more gives -2. Cheaper substitute: clamping at zero must fail the negative case.
- [ ] Add `tests/Merchant.Tests/StockTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~StockTests"` passes.
- [ ] Commit: `core: counted stock`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~StockTests"` passes; ignoring refunds in `StockOf` makes `Refund_returns_stock` fail.

## 7. Sales with number, VAT and payment detail

The existing `sale.completed` carries lines, total and tender. Receipts, the sales list and day end need the sale number, the till's name, the VAT amount, and for cash the amount received and the change (`PAY-06`, `SALES-03`, `SALES-04`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Extend `SaleCompleted` in `src/Merchant.Core/Domain`. Done when: old sales still load and new ones carry every field.
2. Give each till a sale number that never repeats. Done when: numbers count up across restarts.

- [ ] Add optional `int? Number`, `string? TillName`, `long? VatCents`, `long? CashReceivedCents` and `long? ChangeCents` to `SaleCompleted` in `src/Merchant.Core/Domain/ShopEvents.cs`. Done when: a sale written by the previous build still deserialises with those fields empty.
- [ ] Add `SqliteEventStore.NextSaleNumber()` in `src/Merchant.Core/Storage/SqliteEventStore.cs`: read, increment and store the `sale_number` setting in one transaction. Done when: three calls return 1, 2, 3 and a reopened store returns 4. Cheaper substitute: counting existing sale events must fail review: a restored till would reuse numbers.
- [ ] Add `src/Merchant.Core/Domain/SaleBuilder.cs` with `Build(lines, settings, tender, cashReceived, number, tillName)` computing total and VAT through `Vat` for both `PricesIncludeVat` modes, and change through subtraction. Done when: the fixture cart gives total 15500, VAT 2022 and change 4500 for 20000 received; with VAT added on top the total is 17825. Cheaper substitute: computing VAT in a view model must fail review.
- [ ] Refuse in `SaleBuilder`: an empty cart, a cash amount below the total, and a card sale carrying cash fields. Done when: each returns a refusal and builds nothing.
- [ ] Add `tests/Merchant.Tests/SaleBuilderTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~SaleBuilderTests"` passes.
- [ ] Commit: `core: sale number, VAT and payment detail`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~SaleBuilderTests"` passes; removing the cash-below-total refusal makes `Short_cash_is_refused` fail.

## 8. Refunds

A refund is its own fact that points at the sale; the sale is never changed or deleted (`X-02`, `SALES-06`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `sale.refunded` to `src/Merchant.Core/Domain`. Done when: a refunded sale is marked in `ShopState` and cannot be refunded twice.

- [ ] Add `SaleRefunded(Guid RefundId, Guid SaleId, long TotalCents, string Tender, string TillName)` and its event type to `src/Merchant.Core/Domain/ShopEvents.cs`. Done when: the payload round-trips.
- [ ] Record refunds in `ShopState`: each sale exposes `RefundedAt` and `RefundedOnTill`, and the first refund for a sale is the one that counts. Done when: two tills refunding the same sale offline leave one refund counted after sync. Cheaper substitute: summing both refunds must fail the two-till test.
- [ ] Add `ShopState.CanRefund(Guid saleId)` false for an unknown or already refunded sale. Done when: the second refund attempt is refused before an event is appended.
- [ ] Add `tests/Merchant.Tests/RefundTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~RefundTests"` passes.
- [ ] Commit: `core: refunds`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~RefundTests"` passes; counting every refund event makes `Double_refund_counts_once` fail.

## 9. Day close and float

Closing a day records what the drawer should have held and what was counted, and the float left for the next day (`DAY-06`, `X-08`, `X-09`). The float for today is this till's own value.

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `day.closed` to `src/Merchant.Core/Domain` and the float setting to the store. Done when: a closed day is visible in `ShopState` and the float carries over.

- [ ] Add `DayClosed(DateOnly Day, Guid DeviceId, string TillName, long ExpectedCashCents, long CountedCashCents, long FloatLeftCents, long CashTakingsCents, long CardTakingsCents)` and its event type to `src/Merchant.Core/Domain/ShopEvents.cs`. Done when: the payload round-trips.
- [ ] Add `ShopState.ClosedDay(DateOnly day, Guid deviceId)` returning this till's close for that day, or null. Done when: closing Tuesday on Till 1 leaves Till 2's Tuesday open.
- [ ] Add `float_cents` to the till's local settings through `SqliteEventStore.GetText`/`SetText`, set to `FloatLeftCents` when a day closes. Done when: after a close with 500.00 left, the next day's float reads 50000. Cheaper substitute: syncing the float must fail review: each drawer has its own.
- [ ] Add `tests/Merchant.Tests/DayCloseTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayCloseTests"` passes.
- [ ] Commit: `core: day close and float`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayCloseTests"` passes; reading the float from `ShopState` makes `Float_is_per_till` fail.

## 10. Till names

Tills get names people recognise (`TILL-03`). The name reaches other tills by sync.

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add `till.named` to `src/Merchant.Core/Domain`. Done when: `ShopState.TillName(deviceId)` returns the newest name.

- [ ] Add `TillNamed(Guid DeviceId, string Name)` and its event type to `src/Merchant.Core/Domain/ShopEvents.cs`; validate required and at most 30 characters. Done when: an empty name is refused.
- [ ] Add `ShopState.TillName(Guid deviceId)` falling back to `till-` and the last eight hex digits of the id. Done when: an unnamed till reports its device name.
- [ ] Add `ShopState.Tills`: every device id that has recorded an event or been named. Done when: a till that only renamed itself is listed.
- [ ] Add `tests/Merchant.Tests/TillNameTests.cs`. Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~TillNameTests"` passes.
- [ ] Commit: `core: till names`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~TillNameTests"` passes; removing the fallback makes `Unnamed_till_has_device_name` fail.

## 11. Sales and day queries

The Sales and Day end screens read the log through queries, so no screen folds events itself (`SALES-03`, `SALES-07`, `DAY-02`, `DAY-03`, `DAY-08`, `DAY-09`).

**Phase:** 1
**Needs:** none
**Surface:** no

**Build order:**

1. Add the queries to `src/Merchant.Core/Domain/ShopState.cs`. Done when: the fixture's day reproduces the figures in `design/SPEC.md` section 9.

- [ ] Add `ShopState.SalesOn(DateOnly day, Guid? onlyDevice)` returning sales newest first with number, time, till, lines, total, tender, cash fields and refund state. Done when: the fixture returns 42 sales for the day and 27 for Till 1.
- [ ] Add `ShopState.DaySummary(DateOnly day)`: sales count, refund count, takings, cash takings and count, card takings and count, VAT, where a refunded sale is left out of takings. Done when: the fixture gives 42, 1, 386400, 214000 in 24, 172400 in 18 and 50400. Cheaper substitute: counting refunded sales in takings must fail the fixture figures.
- [ ] Add `ShopState.DrawerSummary(DateOnly day, Guid deviceId, long floatCents)`: float, this till's cash sales, this till's cash refunds, expected cash. Done when: the fixture gives 50000, 214000, 6300 and 257700 for Till 1.
- [ ] Add `ShopState.SoldOn(DateOnly day)`: products by takings, the first six by amount and one `rest` entry with the summed quantity and amount. Done when: the fixture's seven lines sum to 386400 and start with Flat white 31 for 108500.
- [ ] Add `ShopState.SalesPerTill(DateOnly day)` with each till's count. Done when: the fixture gives 27 and 15.
- [ ] Add `tests/Merchant.Tests/DayQueryTests.cs` over a small hand-built log (the full fixture arrives in D02 T01 §12). Done when: `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayQueryTests"` passes.
- [ ] Commit: `core: sales and day queries`

**Test checkpoint:** `dotnet test Merchant.slnx --filter "FullyQualifiedName~DayQueryTests"` passes; including refunded sales in takings makes `Refunded_sale_leaves_takings` fail.

## Verification

- `dotnet test Merchant.slnx` passes, including the two-store sync test with every new event type.
- A store file made by the build before this file opens and shows its products and sales.
