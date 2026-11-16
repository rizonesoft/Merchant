using System.Security.Cryptography;
using System.Text;
using Merchant.Core.Events;

namespace Merchant.Core.Sync;

public sealed record HelloResponse(string Name, string Proof);

public sealed record PullRequest(Dictionary<Guid, long> Vector);

public sealed record PullResponse(List<EventRecord> Events, Dictionary<Guid, long> Vector);

public sealed record PushRequest(List<EventRecord> Events);

public sealed record PushResponse(int Imported);

/// <summary>
/// Devices of one shop share a shop key (the pairing code). A sync host serves HTTPS with a
/// self-signed certificate, and proves it belongs to the shop by signing the caller's nonce
/// together with that certificate's hash. The caller then pins the certificate and presents a
/// token derived from the same key, so neither side ever sends the shop key itself.
/// </summary>
public static class SyncAuth
{
    public const int ShopKeyBytes = 16;
    public const int BatchSize = 500;

    public static byte[] NewShopKey() => RandomNumberGenerator.GetBytes(ShopKeyBytes);

    public static string Proof(byte[] shopKey, byte[] nonce, byte[] certificateHash) =>
        Mac(shopKey, "merchant-hello-v1", nonce, certificateHash);

    public static string Token(byte[] shopKey, byte[] certificateHash) =>
        Mac(shopKey, "merchant-token-v1", certificateHash);

    public static bool Same(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    /// <summary>The shop key as a code people can read out: hex in groups of four.</summary>
    public static string FormatPairingCode(byte[] shopKey) =>
        string.Join('-', Convert.ToHexString(shopKey).Chunk(4).Select(c => new string(c)));

    public static bool TryParsePairingCode(string? code, out byte[] shopKey)
    {
        shopKey = [];
        var hex = new string([.. (code ?? "").Where(char.IsAsciiHexDigit)]);
        if (hex.Length != ShopKeyBytes * 2)
            return false;
        shopKey = Convert.FromHexString(hex);
        return true;
    }

    private static string Mac(byte[] key, string purpose, params byte[][] parts)
    {
        using var mac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
        mac.AppendData(Encoding.ASCII.GetBytes(purpose));
        foreach (var part in parts)
            mac.AppendData(part);
        return Convert.ToHexString(mac.GetHashAndReset());
    }
}
