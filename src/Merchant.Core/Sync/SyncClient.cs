using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Merchant.Core.Events;

namespace Merchant.Core.Sync;

public sealed record SyncResult(string PeerName, int Pulled, int Pushed);

public sealed class SyncAuthException(string message) : Exception(message);

/// <summary>
/// One sync session with a host (another till). The caller pulls what it lacks and
/// pushes what the host lacks, so a single session leaves both sides holding the same events.
/// </summary>
public sealed class SyncClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly byte[] _shopKey;
    private byte[]? _pinnedCertificateHash;

    public SyncClient(Uri host, byte[] shopKey)
    {
        _shopKey = shopKey;
        var handler = new SocketsHttpHandler();
        // The host's certificate is self-signed. The first one seen is pinned for the whole
        // session, and SyncAsync refuses to continue until the host proves it owns that certificate.
        handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, _) =>
        {
            if (certificate is null)
                return false;
            var hash = SHA256.HashData(certificate.GetRawCertData());
            _pinnedCertificateHash ??= hash;
            return CryptographicOperations.FixedTimeEquals(hash, _pinnedCertificateHash);
        };
        _http = new HttpClient(handler) { BaseAddress = host, Timeout = TimeSpan.FromSeconds(15) };
    }

    public async Task<SyncResult> SyncAsync(IEventStore local, CancellationToken ct = default)
    {
        var nonce = RandomNumberGenerator.GetBytes(16);
        var hello = await _http.GetFromJsonAsync<HelloResponse>(
            $"sync/hello?nonce={Convert.ToHexString(nonce)}", ct)
            ?? throw new SyncAuthException("Empty hello from host.");

        var certificateHash = _pinnedCertificateHash
            ?? throw new SyncAuthException("Host did not present a certificate.");
        if (!SyncAuth.Same(hello.Proof, SyncAuth.Proof(_shopKey, nonce, certificateHash)))
            throw new SyncAuthException($"{hello.Name} is not part of this shop.");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", SyncAuth.Token(_shopKey, certificateHash));

        int pulled = 0, pushed = 0;
        while (true)
        {
            var pull = await PostAsync<PullRequest, PullResponse>(
                "sync/pull", new PullRequest(await local.GetVectorAsync(ct)), ct);
            pulled += await local.ImportAsync(pull.Events, ct);

            var outgoing = await local.GetMissingAsync(pull.Vector, SyncAuth.BatchSize, ct);
            if (outgoing.Count > 0)
            {
                var push = await PostAsync<PushRequest, PushResponse>("sync/push", new PushRequest(outgoing), ct);
                pushed += push.Imported;
            }

            if (pull.Events.Count == 0 && outgoing.Count == 0)
                return new SyncResult(hello.Name, pulled, pushed);
        }
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync(path, body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(ct)
            ?? throw new HttpRequestException($"Empty response from {path}.");
    }

    public void Dispose() => _http.Dispose();
}
