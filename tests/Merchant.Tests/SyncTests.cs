using System.Net;
using System.Net.Sockets;
using Merchant.Core.Domain;
using Merchant.Core.Printing;
using Merchant.Core.Storage;
using Merchant.Core.Sync;
using Merchant.SyncHost;

namespace Merchant.Tests;

public class SyncTests
{
    private static readonly byte[] ShopKey = SyncAuth.NewShopKey();

    private static Task<SyncServer> Host(Merchant.Core.Events.IEventStore store, byte[] shopKey) =>
        SyncServer.StartAsync("host", store, () => shopKey,
            SyncCertificate.LoadOrCreate(() => null, _ => { }), IPAddress.Loopback, port: 0);

    private static Uri Local(SyncServer server) => new($"https://127.0.0.1:{server.Port}/");

    [Fact]
    public async Task One_session_leaves_both_tills_with_the_same_events()
    {
        using var dir = new TempDir();
        using var a = SqliteEventStore.Open(dir.File("a.db"), "a");
        using var b = SqliteEventStore.Open(dir.File("b.db"), "b");
        // More than one batch each way, so the paging loop is exercised too.
        for (var i = 0; i < SyncAuth.BatchSize + 20; i++)
            a.Append("from.a", "{}");
        for (var i = 0; i < 3; i++)
            b.Append("from.b", "{}");

        await using var server = await Host(b, ShopKey);
        using var client = new SyncClient(Local(server), ShopKey);
        var result = await client.SyncAsync(a);

        Assert.Equal(new SyncResult("host", 3, SyncAuth.BatchSize + 20), result);
        Assert.Equal(a.ReadAll(), b.ReadAll());
        Assert.Equal(SyncAuth.BatchSize + 23, a.ReadAll().Count);

        using var again = new SyncClient(Local(server), ShopKey);
        Assert.Equal(new SyncResult("host", 0, 0), await again.SyncAsync(a));
    }

    [Fact]
    public async Task A_till_from_another_shop_gets_nothing()
    {
        using var dir = new TempDir();
        using var ours = SqliteEventStore.Open(dir.File("ours.db"), "a");
        using var stranger = SqliteEventStore.Open(dir.File("stranger.db"), "b");
        ours.Append("secret.sale", "{}");
        stranger.Append("junk", "{}");

        await using var server = await Host(ours, ShopKey);
        using var client = new SyncClient(Local(server), SyncAuth.NewShopKey());
        await Assert.ThrowsAsync<SyncAuthException>(() => client.SyncAsync(stranger));
        Assert.Single(ours.ReadAll());
        Assert.Single(stranger.ReadAll());

        // Skipping the handshake does not help: the endpoints want the shop's token.
        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });
        var response = await http.PostAsync(new Uri(Local(server), "sync/pull"),
            new StringContent("""{"vector":{}}""", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_receipt_reaches_a_network_printer_as_escpos()
    {
        var sale = new SaleCompleted(Guid.CreateVersion7(),
            [new SaleLine(Guid.CreateVersion7(), "Flat white", 2, 3500)], 7000, Tender.Cash);
        var receipt = Receipt.Build("Merchant", sale, DateTimeOffset.Now);

        using var printer = new TcpListener(IPAddress.Loopback, 0);
        printer.Start();
        var port = ((IPEndPoint)printer.LocalEndpoint).Port;
        var send = NetworkPrinter.SendAsync($"127.0.0.1:{port}", receipt);
        using var connection = await printer.AcceptTcpClientAsync();
        await send;
        using var received = new MemoryStream();
        await connection.GetStream().CopyToAsync(received);

        Assert.Equal(receipt, received.ToArray());
        Assert.Equal([0x1B, 0x40], receipt[..2]);
        var text = System.Text.Encoding.ASCII.GetString(receipt);
        Assert.Contains("2 x Flat white             70.00\n", text);
        Assert.Contains("TOTAL                      70.00\n", text);
        // Cut, then the cash drawer pulse because this was a cash sale.
        Assert.Equal([0x1D, 0x56, 0x42, 0x03, 0x1B, 0x70, 0x00, 0x19, 0xFA], receipt[^9..]);
    }
}
