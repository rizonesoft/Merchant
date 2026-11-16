namespace Merchant.Core.Events;

/// <summary>
/// One immutable fact. <see cref="Seq"/> counts up from 1 per originating device, so a store's
/// knowledge is summed up by a version vector: the highest Seq it holds for each device.
/// </summary>
public sealed record EventRecord(
    Guid Id,
    Guid DeviceId,
    long Seq,
    string Type,
    string Payload,
    DateTimeOffset OccurredAt);

public interface IEventStore
{
    /// <summary>Highest Seq held per originating device.</summary>
    Task<Dictionary<Guid, long>> GetVectorAsync(CancellationToken ct = default);

    /// <summary>
    /// Events the holder of <paramref name="knownVector"/> lacks, ordered by device then Seq,
    /// so a truncated batch is always a gap-free prefix.
    /// </summary>
    Task<List<EventRecord>> GetMissingAsync(
        IReadOnlyDictionary<Guid, long> knownVector, int limit, CancellationToken ct = default);

    /// <summary>Stores the events not yet held. Returns how many were new.</summary>
    Task<int> ImportAsync(IReadOnlyList<EventRecord> events, CancellationToken ct = default);
}

public sealed class SyncGapException(string message) : Exception(message);

public static class EventBatch
{
    /// <summary>
    /// Picks the events to insert: already-held ones are dropped, and a gap in a device's
    /// sequence is refused because the version vector would then claim events the store lacks.
    /// <paramref name="vector"/> is advanced as events are accepted.
    /// </summary>
    public static List<EventRecord> Plan(IEnumerable<EventRecord> incoming, IDictionary<Guid, long> vector)
    {
        var accepted = new List<EventRecord>();
        foreach (var e in incoming.OrderBy(e => e.DeviceId).ThenBy(e => e.Seq))
        {
            // Refused before anything is stored: a bad event would otherwise replicate to every
            // till and break each one's view of the shop.
            if (e.Seq < 1 || string.IsNullOrEmpty(e.Type) || !IsJsonObject(e.Payload))
                throw new ArgumentException($"Malformed event {e.Id}.");

            vector.TryGetValue(e.DeviceId, out var held);
            if (e.Seq <= held)
                continue;
            if (e.Seq != held + 1)
                throw new SyncGapException(
                    $"Device {e.DeviceId}: got event {e.Seq} while holding only up to {held}.");

            accepted.Add(e);
            vector[e.DeviceId] = e.Seq;
        }
        return accepted;
    }

    private static bool IsJsonObject(string? payload)
    {
        if (payload is null)
            return false;
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(payload);
            return json.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
