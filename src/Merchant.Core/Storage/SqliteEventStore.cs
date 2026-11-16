using System.Text;
using Merchant.Core.Events;
using Microsoft.Data.Sqlite;

namespace Merchant.Core.Storage;

/// <summary>
/// The till's local store: one encrypted SQLite file holding the event log and the device's settings.
/// </summary>
public sealed class SqliteEventStore : IEventStore, IDisposable
{
    public const string ShopKeySetting = "shop_key";

    private readonly SqliteConnection _db;
    private readonly Lock _gate = new();
    private readonly string _key;

    public Guid DeviceId { get; }

    /// <summary>Raised (on the calling thread) after events were appended or imported.</summary>
    public event Action? Changed;

    private SqliteEventStore(SqliteConnection db, string key)
    {
        _db = db;
        _key = key;

        // WAL with synchronous=FULL: a sale that was acknowledged survives a power cut.
        Execute("PRAGMA journal_mode=WAL");
        Execute("PRAGMA synchronous=FULL");
        Execute("""
            CREATE TABLE IF NOT EXISTS events(
                device_id   TEXT    NOT NULL,
                seq         INTEGER NOT NULL,
                id          TEXT    NOT NULL UNIQUE,
                type        TEXT    NOT NULL,
                payload     TEXT    NOT NULL,
                occurred_ms INTEGER NOT NULL,
                PRIMARY KEY(device_id, seq)
            ) WITHOUT ROWID
            """);
        Execute("CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY, value BLOB NOT NULL) WITHOUT ROWID");

        var id = GetSetting("device_id");
        if (id is null)
        {
            id = Guid.CreateVersion7().ToByteArray();
            SetSetting("device_id", id);
        }
        DeviceId = new Guid(id);
    }

    public static SqliteEventStore Open(string path, string key)
    {
        var db = Connect(path, key, SqliteOpenMode.ReadWriteCreate);
        try
        {
            return new SqliteEventStore(db, key);
        }
        catch
        {
            db.Dispose();
            throw;
        }
    }

    private static SqliteConnection Connect(string path, string key, SqliteOpenMode mode)
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Password = key,
            Mode = mode,
            Pooling = false,
        }.ToString());
        db.Open();
        return db;
    }

    /// <summary>Records a new fact from this device.</summary>
    public EventRecord Append(string type, string payload)
    {
        EventRecord record;
        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            long seq;
            using (var max = _db.CreateCommand())
            {
                max.Transaction = tx;
                max.CommandText = "SELECT COALESCE(MAX(seq), 0) FROM events WHERE device_id = $d";
                max.Parameters.AddWithValue("$d", Text(DeviceId));
                seq = (long)max.ExecuteScalar()! + 1;
            }

            var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            record = new EventRecord(Guid.CreateVersion7(now), DeviceId, seq, type, payload, now);
            Insert(tx, record);
            tx.Commit();
        }
        Changed?.Invoke();
        return record;
    }

    /// <summary>Every event in time order (UUIDv7 ids sort by creation time).</summary>
    public List<EventRecord> ReadAll()
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT id, device_id, seq, type, payload, occurred_ms FROM events ORDER BY id";
            return ReadEvents(cmd);
        }
    }

    public Task<Dictionary<Guid, long>> GetVectorAsync(CancellationToken ct = default)
    {
        lock (_gate)
            return Task.FromResult(Vector(null));
    }

    public Task<List<EventRecord>> GetMissingAsync(
        IReadOnlyDictionary<Guid, long> knownVector, int limit, CancellationToken ct = default)
    {
        var result = new List<EventRecord>();
        lock (_gate)
        {
            foreach (var (device, held) in Vector(null).OrderBy(p => p.Key))
            {
                var known = knownVector.GetValueOrDefault(device);
                if (held <= known || result.Count >= limit)
                    continue;

                using var cmd = _db.CreateCommand();
                cmd.CommandText = """
                    SELECT id, device_id, seq, type, payload, occurred_ms FROM events
                    WHERE device_id = $d AND seq > $known ORDER BY seq LIMIT $limit
                    """;
                cmd.Parameters.AddWithValue("$d", Text(device));
                cmd.Parameters.AddWithValue("$known", known);
                cmd.Parameters.AddWithValue("$limit", limit - result.Count);
                result.AddRange(ReadEvents(cmd));
            }
        }
        return Task.FromResult(result);
    }

    public Task<int> ImportAsync(IReadOnlyList<EventRecord> events, CancellationToken ct = default)
    {
        int count;
        lock (_gate)
        {
            using var tx = _db.BeginTransaction();
            var accepted = EventBatch.Plan(events, Vector(tx));
            foreach (var e in accepted)
                Insert(tx, e);
            tx.Commit();
            count = accepted.Count;
        }
        if (count > 0)
            Changed?.Invoke();
        return Task.FromResult(count);
    }

    public byte[]? GetSetting(string key)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = "SELECT value FROM settings WHERE key = $k";
            cmd.Parameters.AddWithValue("$k", key);
            return cmd.ExecuteScalar() as byte[];
        }
    }

    public void SetSetting(string key, byte[] value)
    {
        lock (_gate)
        {
            using var cmd = _db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO settings(key, value) VALUES($k, $v)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value
                """;
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$v", value);
            cmd.ExecuteNonQuery();
        }
    }

    public string? GetText(string key) => GetSetting(key) is { } bytes ? Encoding.UTF8.GetString(bytes) : null;

    public void SetText(string key, string value) => SetSetting(key, Encoding.UTF8.GetBytes(value));

    /// <summary>
    /// Writes a consistent copy of the store to <paramref name="destination"/>, encrypted with
    /// <paramref name="passphrase"/> instead of this device's key so it can be restored anywhere.
    /// </summary>
    public void ExportBackup(string destination, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase))
            throw new ArgumentException("A backup passphrase is required.");

        // Built beside the destination and moved into place only when complete, so a failed
        // export never costs the merchant the backup they already had.
        var working = destination + ".partial";
        File.Delete(working);
        try
        {
            lock (_gate)
            {
                using var cmd = _db.CreateCommand();
                cmd.CommandText = "VACUUM INTO $dest";
                cmd.Parameters.AddWithValue("$dest", working);
                cmd.ExecuteNonQuery();
            }

            // The copy carries the device key; swap it for the passphrase.
            using (var copy = Connect(working, _key, SqliteOpenMode.ReadWrite))
            {
                using var quote = copy.CreateCommand();
                quote.CommandText = "SELECT quote($p)";
                quote.Parameters.AddWithValue("$p", passphrase);
                var quoted = (string)quote.ExecuteScalar()!;
                using var rekey = copy.CreateCommand();
                rekey.CommandText = "PRAGMA rekey = " + quoted;
                rekey.ExecuteNonQuery();
            }
            File.Move(working, destination, overwrite: true);
        }
        finally
        {
            File.Delete(working);
        }
    }

    /// <summary>
    /// Merges a backup into this store: its events are imported and its shop key adopted.
    /// The device keeps its own identity, so a restored till never reuses another till's sequence.
    /// </summary>
    public int RestoreBackup(string source, string passphrase)
    {
        List<EventRecord> events;
        byte[]? shopKey;
        try
        {
            using var backup = Connect(source, passphrase, SqliteOpenMode.ReadOnly);
            using var cmd = backup.CreateCommand();
            cmd.CommandText = "SELECT id, device_id, seq, type, payload, occurred_ms FROM events ORDER BY device_id, seq";
            events = ReadEvents(cmd);
            using var key = backup.CreateCommand();
            key.CommandText = "SELECT value FROM settings WHERE key = $k";
            key.Parameters.AddWithValue("$k", ShopKeySetting);
            shopKey = key.ExecuteScalar() as byte[];
        }
        catch (SqliteException e)
        {
            throw new InvalidOperationException("Wrong passphrase, or the file is not a Merchant backup.", e);
        }

        var imported = ImportAsync(events).GetAwaiter().GetResult();
        if (shopKey is not null)
            SetSetting(ShopKeySetting, shopKey);
        return imported;
    }

    public void Dispose() => _db.Dispose();

    private Dictionary<Guid, long> Vector(SqliteTransaction? tx)
    {
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT device_id, MAX(seq) FROM events GROUP BY device_id";
        using var reader = cmd.ExecuteReader();
        var vector = new Dictionary<Guid, long>();
        while (reader.Read())
            vector[Guid.Parse(reader.GetString(0))] = reader.GetInt64(1);
        return vector;
    }

    private void Insert(SqliteTransaction tx, EventRecord e)
    {
        using var cmd = _db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO events(device_id, seq, id, type, payload, occurred_ms)
            VALUES($d, $seq, $id, $type, $payload, $ms)
            """;
        cmd.Parameters.AddWithValue("$d", Text(e.DeviceId));
        cmd.Parameters.AddWithValue("$seq", e.Seq);
        cmd.Parameters.AddWithValue("$id", Text(e.Id));
        cmd.Parameters.AddWithValue("$type", e.Type);
        cmd.Parameters.AddWithValue("$payload", e.Payload);
        cmd.Parameters.AddWithValue("$ms", e.OccurredAt.ToUnixTimeMilliseconds());
        cmd.ExecuteNonQuery();
    }

    private static List<EventRecord> ReadEvents(SqliteCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        var events = new List<EventRecord>();
        while (reader.Read())
        {
            events.Add(new EventRecord(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetInt64(2),
                reader.GetString(3),
                reader.GetString(4),
                DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(5))));
        }
        return events;
    }

    private void Execute(string sql)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    // Lower-case "D" text keeps UUIDv7 ids sortable by creation time.
    private static string Text(Guid id) => id.ToString("D");
}
