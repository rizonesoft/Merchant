using System.Text;
using Merchant.Core.Domain;
using Merchant.Core.Events;
using Merchant.Core.Storage;
using Microsoft.Data.Sqlite;

namespace Merchant.Tests;

public class StoreTests
{
    private const string Key = "test-device-key";

    [Fact]
    public void Appended_events_survive_a_reopen_in_sequence()
    {
        using var dir = new TempDir();
        Guid device;
        using (var store = SqliteEventStore.Open(dir.File("till.db"), Key))
        {
            device = store.DeviceId;
            store.Append("a", "{}");
            store.Append("b", "{}");
        }

        using var reopened = SqliteEventStore.Open(dir.File("till.db"), Key);
        var events = reopened.ReadAll();
        Assert.Equal(device, reopened.DeviceId);
        Assert.Equal(["a", "b"], events.Select(e => e.Type));
        Assert.Equal([1L, 2L], events.Select(e => e.Seq));
        Assert.Equal(3, reopened.Append("c", "{}").Seq);
    }

    [Fact]
    public async Task Events_from_one_millisecond_replay_in_their_sequence()
    {
        using var dir = new TempDir();
        using var store = SqliteEventStore.Open(dir.File("till.db"), Key);
        var till = Guid.CreateVersion7();
        var when = DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000);
        // Two ids with the same timestamp, the later event holding the smaller id.
        var larger = new Guid("01a31c5b-8000-7fff-bfff-ffffffffffff");
        var smaller = new Guid("01a31c5b-8000-7000-8000-000000000000");

        await store.ImportAsync(
        [
            new EventRecord(larger, till, 1, "first", "{}", when),
            new EventRecord(smaller, till, 2, "second", "{}", when),
        ]);

        Assert.Equal(["first", "second"], store.ReadAll().Select(e => e.Type));
    }

    [Fact]
    public void The_database_file_is_encrypted()
    {
        using var dir = new TempDir();
        using (var store = SqliteEventStore.Open(dir.File("till.db"), Key))
            store.Append("sale.completed", """{"marker":"PLAINTEXT-MARKER"}""");

        var raw = Encoding.Latin1.GetString(File.ReadAllBytes(dir.File("till.db")));
        Assert.DoesNotContain("PLAINTEXT-MARKER", raw);
        Assert.DoesNotContain("SQLite format 3", raw);
        Assert.Throws<SqliteException>(() => SqliteEventStore.Open(dir.File("till.db"), "wrong-key"));
    }

    [Fact]
    public async Task Import_is_idempotent_and_refuses_gaps()
    {
        using var dir = new TempDir();
        using var store = SqliteEventStore.Open(dir.File("till.db"), Key);
        var other = Guid.CreateVersion7();
        EventRecord Event(long seq) => new(Guid.CreateVersion7(), other, seq, "x", "{}", DateTimeOffset.UtcNow);
        var first = Event(1);
        var second = Event(2);

        Assert.Equal(2, await store.ImportAsync([second, first]));
        Assert.Equal(0, await store.ImportAsync([first, second]));
        await Assert.ThrowsAsync<SyncGapException>(() => store.ImportAsync([Event(4)]));
        await Assert.ThrowsAsync<ArgumentException>(() => store.ImportAsync([Event(3) with { Payload = "null" }]));
        await Assert.ThrowsAsync<ArgumentException>(() => store.ImportAsync([Event(3) with { Payload = "not json" }]));
        Assert.Equal(2, (await store.GetVectorAsync())[other]);
    }

    [Fact]
    public void A_backup_restores_onto_a_new_till_only_with_its_passphrase()
    {
        using var dir = new TempDir();
        using var original = SqliteEventStore.Open(dir.File("old.db"), Key);
        original.SetSetting(SqliteEventStore.ShopKeySetting, [1, 2, 3]);
        original.Append("sale.completed", """{"marker":"PLAINTEXT-MARKER"}""");
        original.Append("sale.completed", "{}");
        // Exporting over an earlier backup replaces it only once the new one is complete.
        File.WriteAllText(dir.File("shop.merchantbackup"), "earlier backup");
        Assert.Throws<ArgumentException>(() => original.ExportBackup(dir.File("shop.merchantbackup"), " "));
        Assert.Equal("earlier backup", File.ReadAllText(dir.File("shop.merchantbackup")));
        original.ExportBackup(dir.File("shop.merchantbackup"), "correct horse");
        Assert.False(File.Exists(dir.File("shop.merchantbackup.partial")));

        var raw = Encoding.Latin1.GetString(File.ReadAllBytes(dir.File("shop.merchantbackup")));
        Assert.DoesNotContain("PLAINTEXT-MARKER", raw);

        using var replacement = SqliteEventStore.Open(dir.File("new.db"), "another-device-key");
        Assert.Throws<InvalidOperationException>(() => replacement.RestoreBackup(dir.File("shop.merchantbackup"), "wrong"));
        // The device key that made the backup must not open it either.
        Assert.Throws<InvalidOperationException>(() => replacement.RestoreBackup(dir.File("shop.merchantbackup"), Key));

        Assert.Equal(2, replacement.RestoreBackup(dir.File("shop.merchantbackup"), "correct horse"));
        Assert.Equal(original.ReadAll(), replacement.ReadAll());
        Assert.Equal([1, 2, 3], replacement.GetSetting(SqliteEventStore.ShopKeySetting));
        Assert.NotEqual(original.DeviceId, replacement.DeviceId);
    }

    [Fact]
    public void Shop_state_is_derived_from_the_log()
    {
        var device = Guid.CreateVersion7();
        var coffee = Guid.CreateVersion7();
        var now = DateTimeOffset.Now;
        var seq = 0;
        EventRecord Event<T>(string type, T payload, DateTimeOffset at) =>
            new(Guid.CreateVersion7(), device, ++seq, type, ShopJson.Write(payload), at);

        var state = ShopState.From(
        [
            Event(EventTypes.ProductDefined, new ProductDefined(coffee, "Coffee", 3000), now),
            Event(EventTypes.ProductDefined, new ProductDefined(coffee, "Flat white", 3500), now),
            Event(EventTypes.SaleCompleted, new SaleCompleted(Guid.CreateVersion7(), [], 7000, Tender.Cash), now),
            Event(EventTypes.SaleCompleted, new SaleCompleted(Guid.CreateVersion7(), [], 500, Tender.Card), now.AddDays(-1)),
            Event("from.a.newer.till", new { anything = 1 }, now),
        ]);

        Assert.Equal([new Product(coffee, "Flat white", 3500)], state.Products);
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        Assert.Equal(7000, state.TotalCentsOn(today));
        Assert.Equal(1, state.SaleCountOn(today));
    }
}
