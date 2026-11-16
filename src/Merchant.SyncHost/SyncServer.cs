using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Merchant.Core.Events;
using Merchant.Core.Sync;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Merchant.SyncHost;

/// <summary>
/// The listening side of sync: three endpoints in front of whichever <see cref="IEventStore"/>
/// the host owns.
/// </summary>
public sealed class SyncServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    public int Port { get; }

    private SyncServer(WebApplication app, int port)
    {
        _app = app;
        Port = port;
    }

    /// <param name="name">Shown to the other side; also this host's mDNS instance name.</param>
    /// <param name="shopKey">Read on every request, so joining another shop takes effect at once.</param>
    /// <param name="bind">Address to listen on, or null for every network interface.</param>
    /// <param name="port">Port to listen on, or 0 to let the system pick one.</param>
    public static async Task<SyncServer> StartAsync(
        string name,
        IEventStore store,
        Func<byte[]> shopKey,
        X509Certificate2 certificate,
        IPAddress? bind,
        int port,
        CancellationToken ct = default)
    {
        var certificateHash = SHA256.HashData(certificate.RawData);

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseKestrelHttpsConfiguration();
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            if (bind is null)
                kestrel.ListenAnyIP(port, listen => listen.UseHttps(certificate));
            else
                kestrel.Listen(bind, port, listen => listen.UseHttps(certificate));
        });

        var app = builder.Build();

        app.MapGet("/sync/hello", (string nonce) =>
        {
            byte[] nonceBytes;
            try
            {
                nonceBytes = Convert.FromHexString(nonce);
            }
            catch (FormatException)
            {
                return Results.BadRequest();
            }
            if (nonceBytes.Length is < 16 or > 64)
                return Results.BadRequest();

            return Results.Ok(new HelloResponse(name, SyncAuth.Proof(shopKey(), nonceBytes, certificateHash)));
        });

        var shop = app.MapGroup("/sync").AddEndpointFilter(async (context, next) =>
        {
            var presented = context.HttpContext.Request.Headers.Authorization.ToString();
            var expected = "Bearer " + SyncAuth.Token(shopKey(), certificateHash);
            return SyncAuth.Same(presented, expected) ? await next(context) : Results.Unauthorized();
        });

        shop.MapPost("/pull", async (PullRequest request, CancellationToken token) =>
            new PullResponse(
                await store.GetMissingAsync(request.Vector, SyncAuth.BatchSize, token),
                await store.GetVectorAsync(token)));

        shop.MapPost("/push", async (PushRequest request, CancellationToken token) =>
        {
            try
            {
                return Results.Ok(new PushResponse(await store.ImportAsync(request.Events, token)));
            }
            catch (Exception e) when (e is SyncGapException or ArgumentException)
            {
                return Results.BadRequest(e.Message);
            }
        });

        await app.StartAsync(ct);

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new SyncServer(app, new Uri(address).Port);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}

public static class SyncCertificate
{
    /// <summary>
    /// Loads this host's self-signed TLS certificate, creating and saving it on first use.
    /// Trust comes from the shop key (see <see cref="SyncAuth"/>), not from a certificate authority.
    /// </summary>
    public static X509Certificate2 LoadOrCreate(Func<byte[]?> load, Action<byte[]> save)
    {
        var pfx = load();
        if (pfx is null)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest("CN=merchant-sync", key, HashAlgorithmName.SHA256);
            var now = DateTimeOffset.UtcNow;
            using var created = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(20));
            pfx = created.Export(X509ContentType.Pfx);
            save(pfx);
        }
        return X509CertificateLoader.LoadPkcs12(pfx, null);
    }
}
