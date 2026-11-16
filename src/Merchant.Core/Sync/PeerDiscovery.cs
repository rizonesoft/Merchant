using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Makaretu.Dns;

namespace Merchant.Core.Sync;

public sealed record Peer(string Name, IPAddress Address, int Port)
{
    public Uri Uri => new($"https://{Address}:{Port}/");
}

/// <summary>
/// Finds sync hosts on the local network over mDNS (DNS-SD service <c>_merchant-sync._tcp</c>)
/// and announces this device's own host. No internet or router configuration is involved.
/// </summary>
public sealed class PeerDiscovery : IDisposable
{
    public const string ServiceName = "_merchant-sync._tcp";
    private static readonly TimeSpan PeerLifetime = TimeSpan.FromMinutes(2);

    private readonly string _selfName;
    private readonly ServiceDiscovery _discovery = new() { AnswersContainsAdditionalRecords = true };
    private readonly ConcurrentDictionary<string, (Peer Peer, DateTimeOffset Seen)> _peers = new();

    /// <param name="selfName">This device's instance name: a single DNS label, unique in the shop.</param>
    public PeerDiscovery(string selfName)
    {
        _selfName = selfName;
        _discovery.Mdns.AnswerReceived += OnAnswer;
    }

    /// <summary>Hosts heard from recently, excluding this device.</summary>
    public IReadOnlyList<Peer> Peers
    {
        get
        {
            var cutoff = DateTimeOffset.UtcNow - PeerLifetime;
            return [.. _peers.Values.Where(p => p.Seen >= cutoff).Select(p => p.Peer)];
        }
    }

    public void Advertise(int port)
    {
        var profile = new ServiceProfile(_selfName, ServiceName, (ushort)port);
        _discovery.Advertise(profile);
        _discovery.Announce(profile);
    }

    /// <summary>Asks the network who is there. Answers arrive on a background thread.</summary>
    public void Query()
    {
        _discovery.Mdns.SendQuery(ServiceName + ".local", type: DnsType.PTR);
    }

    private void OnAnswer(object? sender, MessageEventArgs e)
    {
        var records = e.Message.Answers.Concat(e.Message.AdditionalRecords).ToList();
        foreach (var srv in records.OfType<SRVRecord>())
        {
            var labels = srv.Name.Labels;
            var isOurs = srv.Name.ToString().EndsWith(
                "." + ServiceName + ".local", StringComparison.OrdinalIgnoreCase);
            if (!isOurs || labels.Count == 0 || labels[0] == _selfName)
                continue;

            // Prefer the address the answer actually came from: it is reachable by definition.
            var address = e.RemoteEndPoint.Address.AddressFamily == AddressFamily.InterNetwork
                ? e.RemoteEndPoint.Address
                : records.OfType<ARecord>().FirstOrDefault(a => a.Name == srv.Target)?.Address;
            if (address is null)
                continue;

            _peers[labels[0]] = (new Peer(labels[0], address, srv.Port), DateTimeOffset.UtcNow);
        }
    }

    public void Dispose()
    {
        _discovery.Mdns.AnswerReceived -= OnAnswer;
        _discovery.Unadvertise();
        _discovery.Dispose();
    }
}
