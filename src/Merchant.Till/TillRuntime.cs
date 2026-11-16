using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Merchant.Core.Storage;
using Merchant.Core.Sync;
using Merchant.SyncHost;

namespace Merchant.Till;

/// <summary>
/// Everything the till runs besides its screen: the local store, the sync host other tills can
/// reach, discovery of those tills, and the loop that keeps exchanging events with them.
/// </summary>
public sealed class TillRuntime : IAsyncDisposable
{
    private const string PrinterSetting = "printer_host";
    private const string CertificateSetting = "sync_cert";
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(15);

    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _syncing = new(1, 1);
    private SyncServer? _server;
    private PeerDiscovery? _discovery;
    private Task? _loop;

    public SqliteEventStore Store { get; }
    public string Name { get; }

    /// <summary>Raised from a background thread whenever the sync status line changes.</summary>
    public event Action<string>? SyncStatusChanged;

    private TillRuntime(SqliteEventStore store)
    {
        Store = store;
        Name = "till-" + store.DeviceId.ToString("N")[^8..];
        if (store.GetSetting(SqliteEventStore.ShopKeySetting) is null)
            store.SetSetting(SqliteEventStore.ShopKeySetting, SyncAuth.NewShopKey());
    }

    public static string DefaultDataDir =>
        Environment.GetEnvironmentVariable("MERCHANT_DATA_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Merchant");

    public static TillRuntime Open(string dataDir)
    {
        var key = DeviceKeyStore.GetOrCreate(dataDir);
        return new TillRuntime(SqliteEventStore.Open(Path.Combine(dataDir, "till.db"), key));
    }

    public byte[] ShopKey => Store.GetSetting(SqliteEventStore.ShopKeySetting)!;

    public string PairingCode => SyncAuth.FormatPairingCode(ShopKey);

    public bool JoinShop(string? pairingCode)
    {
        if (!SyncAuth.TryParsePairingCode(pairingCode, out var key))
            return false;
        Store.SetSetting(SqliteEventStore.ShopKeySetting, key);
        return true;
    }

    public string PrinterHost
    {
        get => Store.GetText(PrinterSetting) ?? "";
        set => Store.SetText(PrinterSetting, value.Trim());
    }

    /// <summary>Starts listening for other tills, announces this one, and begins the sync loop.</summary>
    public async Task StartNetworkAsync()
    {
        var certificate = SyncCertificate.LoadOrCreate(
            () => Store.GetSetting(CertificateSetting),
            pfx => Store.SetSetting(CertificateSetting, pfx));

        _server = await SyncServer.StartAsync(Name, Store, () => ShopKey, certificate, bind: null, port: 0, _stop.Token);
        _discovery = new PeerDiscovery(Name);
        _discovery.Advertise(_server.Port);
        _discovery.Query();
        _loop = Task.Run(LoopAsync);
    }

    private async Task LoopAsync()
    {
        using var timer = new PeriodicTimer(SyncInterval);
        try
        {
            // A first pass soon after start, once discovery has had a moment to hear answers.
            await Task.Delay(TimeSpan.FromSeconds(2), _stop.Token);
            do
            {
                await SyncNowAsync();
            }
            while (await timer.WaitForNextTickAsync(_stop.Token));
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
            // The till is closing.
        }
    }

    /// <summary>Exchanges events with every sync host currently in range.</summary>
    public async Task SyncNowAsync()
    {
        if (_discovery is null)
            return;

        await _syncing.WaitAsync(_stop.Token);
        try
        {
            _discovery.Query();
            int reached = 0, pulled = 0, pushed = 0;
            string? problem = null;
            foreach (var peer in _discovery.Peers)
            {
                try
                {
                    using var client = new SyncClient(peer.Uri, ShopKey);
                    var result = await client.SyncAsync(Store, _stop.Token);
                    reached++;
                    pulled += result.Pulled;
                    pushed += result.Pushed;
                }
                catch (SyncAuthException)
                {
                    // A till from another shop on the same network: not ours to sync with.
                }
                catch (Exception e) when (!_stop.IsCancellationRequested)
                {
                    // Includes a peer that stopped answering: its timeout must not end the loop.
                    problem = $"{peer.Name}: {e.Message}";
                }
            }

            var time = DateTime.Now.ToString("HH:mm:ss");
            SyncStatusChanged?.Invoke(problem is not null
                ? $"{time}  Sync problem with {problem}"
                : reached == 0
                    ? $"{time}  No other tills in range"
                    : $"{time}  Synced with {reached} device(s): {pulled} in, {pushed} out");
        }
        finally
        {
            _syncing.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        if (_loop is not null)
            await _loop;
        _discovery?.Dispose();
        if (_server is not null)
            await _server.DisposeAsync();
        Store.Dispose();
    }
}
