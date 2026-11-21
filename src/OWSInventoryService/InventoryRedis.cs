using System.Globalization;
using System.Reflection;
using StackExchange.Redis;

namespace OWSInventoryService;

internal sealed class InventoryRedis(IConnectionMultiplexer connection)
{
    private readonly IDatabase cache = connection.GetDatabase();
    private static readonly string ApplyScript = Script("apply");
    private static readonly string ReadScript = Script("read");
    private static readonly string SyncScript = Script("sync");

    private static string Script(string name)
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream($"OWSInventoryService.Redis.{name}.lua")
            ?? throw new InvalidOperationException($"Missing Redis script {name}.");
        using var reader = new StreamReader(resource);
        return reader.ReadToEnd();
    }

    internal static RedisKey Bucket(int bucket, string name) => $"ows:inventory:v1:{{bucket-{bucket:D2}}}:{name}";
    private static RedisKey Key(Owner owner, string name) => Bucket(owner.Bucket, $"{owner.Reference}:{name}");

    public Task<TimeSpan> Ping() => cache.PingAsync();

    public async Task<Submission> Apply(Owner owner, object payload)
    {
        var result = await Evaluate(ApplyScript,
            [Bucket(owner.Bucket, "control"), Bucket(owner.Bucket, "ready"), Bucket(owner.Bucket, "journal"),
                Key(owner, "data"), Key(owner, "index"), Bucket(owner.Bucket, "idle")], InventoryJson.Write(payload));
        var status = (string)result[0]!;
        return status is "pending" or "committed"
            ? new(status, Event: InventoryJson.Read<InventoryEvent>((string)result[1]!), Epoch: Guid.Parse((string)result[2]!))
            : new(status, result.Length == 2 ? (string?)result[1] : null);
    }

    public Task<RedisResult[]> Read(Owner owner, object payload) => Evaluate(ReadScript,
        [Bucket(owner.Bucket, "control"), Bucket(owner.Bucket, "ready"), Key(owner, "data"),
            Key(owner, "index"), Bucket(owner.Bucket, "idle")], InventoryJson.Write(payload));

    public async Task<string?> StateBasis(Owner owner) =>
        (string?)await cache.HashGetAsync(Key(owner, "data"), "state");

    public async Task<long?> Confirmed(int bucket, Guid epoch)
    {
        var fields = await cache.HashGetAsync(Bucket(bucket, "control"), ["epoch", "confirmed"]);
        return fields[0] == epoch.ToString("D") && fields[1].TryParse(out long value) ? value : null;
    }

    public Task<RedisValue> ActiveEpoch(int bucket) => cache.StringGetAsync(Bucket(bucket, "ready"));

    public async Task<bool> HasPendingReceipt(Owner owner, Guid commandId)
    {
        var result = await cache.ScriptEvaluateAsync("""
            local epoch = redis.call('HGET', KEYS[1], 'epoch')
            if not epoch or redis.call('HGET', KEYS[2], 'epoch') ~= epoch then return 0 end
            local event = redis.call('HGET', KEYS[2], ARGV[1])
            if not event then return 0 end
            local confirmed = tonumber(redis.call('HGET', KEYS[1], 'confirmed'))
            return cjson.decode(event).sequence > confirmed and 1 or 0
            """, [Bucket(owner.Bucket, "control"), Key(owner, "data")], [$"receipt:{commandId:D}"]);
        return (int)result == 1;
    }

    public async Task<bool> Hydrate(Owner owner, Guid epoch, string lease, Func<Task<InventorySnapshot?>> load)
    {
        var lockKey = Key(owner, "load");
        if (!await cache.StringSetAsync(lockKey, lease, TimeSpan.FromSeconds(30), When.NotExists))
            return false;

        var temporaryData = Key(owner, $"data:{lease}");
        var temporaryIndex = Key(owner, $"index:{lease}");
        try
        {
            var snapshot = await load();
            if (snapshot is null)
                return true;

            await cache.ScriptEvaluateAsync("""
                redis.call('HSET', KEYS[1], 'epoch', ARGV[1], 'state', ARGV[2], 'sequence', '0', 'containerKeys', ARGV[3])
                redis.call('ZADD', KEYS[2], 0, '!')
                redis.call('PEXPIRE', KEYS[1], 30000)
                redis.call('PEXPIRE', KEYS[2], 30000)
                """, [temporaryData, temporaryIndex],
                [epoch.ToString("D"), InventoryJson.Write(snapshot.State),
                    InventoryJson.Write(snapshot.Containers.Select(box => box.ContainerId))]);

            await cache.HashSetAsync(temporaryData, snapshot.Containers
                .Select(box => new HashEntry($"container:{box.ContainerId}", InventoryJson.Write(box))).ToArray());
            foreach (var chunk in snapshot.Items.Chunk(128))
            {
                var entries = new List<HashEntry>(chunk.Length * 2);
                var scores = new List<SortedSetEntry>(chunk.Length * 2);
                foreach (var item in chunk)
                {
                    var id = item.ItemInstanceId.ToString("D");
                    entries.Add(new($"item:{id}", InventoryJson.Write(item)));
                    scores.Add(new($"p:{item.ContainerId}:{CursorPosition(item)}", 0));
                    if (item.Slotted)
                        entries.Add(new($"slot:{item.ContainerId}:{item.SlotNumber}", id));
                    else if (item.ContainerId != "_loot" && item.Quantity < item.MaxStack)
                        scores.Add(new(FormattableString.Invariant(
                            $"s:{item.ContainerId}:{item.DefinitionId}:{item.Condition?.ToString(CultureInfo.InvariantCulture) ?? "-"}:{item.UsesLeft?.ToString(CultureInfo.InvariantCulture) ?? "-"}:{item.MaxStack - item.Quantity:D7}:{id}"), 0));
                }
                await Task.WhenAll(cache.HashSetAsync(temporaryData, entries.ToArray()),
                    cache.SortedSetAddAsync(temporaryIndex, scores.ToArray()));
            }

            foreach (var chunk in snapshot.Receipts.Chunk(128))
            {
                await Task.WhenAll(cache.HashSetAsync(temporaryData, chunk.Select(receipt =>
                    new HashEntry($"receipt:{receipt.CommandId:D}", InventoryJson.Write(receipt with { Sequence = 0 }))).ToArray()),
                    cache.SortedSetAddAsync(temporaryIndex, chunk.Select(receipt =>
                        new SortedSetEntry(FormattableString.Invariant($"r:{receipt.IssuedAt:D13}:{receipt.CommandId:D}"), 0)).ToArray()));
            }

            await cache.ScriptEvaluateAsync("""
                if redis.call('GET', KEYS[1]) ~= ARGV[1] or redis.call('GET', KEYS[2]) ~= ARGV[2]
                    or redis.call('HGET', KEYS[3], 'epoch') == ARGV[1]
                    or redis.call('EXISTS', KEYS[5], KEYS[6]) ~= 2 then return 0 end
                redis.call('UNLINK', KEYS[3], KEYS[4])
                redis.call('RENAME', KEYS[5], KEYS[3])
                redis.call('RENAME', KEYS[6], KEYS[4])
                redis.call('PERSIST', KEYS[3])
                redis.call('PERSIST', KEYS[4])
                local now = redis.call('TIME')
                redis.call('ZADD', KEYS[7], tonumber(now[1]) * 1000 + math.floor(tonumber(now[2]) / 1000), ARGV[3])
                return 1
                """, [Bucket(owner.Bucket, "ready"), lockKey, Key(owner, "data"), Key(owner, "index"), temporaryData,
                    temporaryIndex, Bucket(owner.Bucket, "idle")], [epoch.ToString("D"), lease, owner.Reference]);
            return true;
        }
        finally
        {
            await cache.ScriptEvaluateAsync("""
                if redis.call('GET', KEYS[1]) == ARGV[1] then redis.call('DEL', KEYS[1]) end
                redis.call('UNLINK', KEYS[2], KEYS[3])
                """, [lockKey, temporaryData, temporaryIndex], [lease]);
        }
    }

    public async Task<StreamEntry[]> Pending(int bucket, long sequence, int count) =>
        await cache.StreamRangeAsync(Bucket(bucket, "journal"), $"{sequence + 1}-0", "+", count);

    public async Task<string> Sync(int bucket, BucketCheckpoint checkpoint, string operation)
    {
        var response = await cache.ScriptEvaluateAsync(SyncScript,
            [Bucket(bucket, "control"), Bucket(bucket, "ready"), Bucket(bucket, "journal")],
            [operation, checkpoint.Epoch.ToString("D"), checkpoint.Generation, checkpoint.Sequence,
                checkpoint.EventId?.ToString("D") ?? ""]);
        return (string)response!;
    }

    public async Task Sweep(int bucket, TimeSpan idle)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(idle).ToUnixTimeMilliseconds();
        var idleKey = Bucket(bucket, "idle");
        var candidates = await cache.SortedSetRangeByScoreAsync(idleKey, stop: cutoff, take: 64);
        await Task.WhenAll(candidates.Select(async entry =>
        {
            var parts = entry.ToString().Split(':');
            var owner = new Owner(Guid.Parse(parts[0]), int.Parse(parts[1], CultureInfo.InvariantCulture));
            await cache.ScriptEvaluateAsync("""
                local score = redis.call('ZSCORE', KEYS[4], ARGV[1])
                if not score or tonumber(score) > tonumber(ARGV[2]) then return 0 end
                local epoch = redis.call('HGET', KEYS[1], 'epoch')
                if not epoch then return 0 end
                local cached = redis.call('HGET', KEYS[2], 'epoch')
                local confirmed = tonumber(redis.call('HGET', KEYS[1], 'confirmed'))
                if cached ~= epoch or (redis.call('HEXISTS', KEYS[2], 'dirty') == 0
                    and tonumber(redis.call('HGET', KEYS[2], 'sequence')) <= confirmed) then
                    redis.call('UNLINK', KEYS[2], KEYS[3])
                    redis.call('ZREM', KEYS[4], ARGV[1])
                end
                """, [Bucket(bucket, "control"), Key(owner, "data"), Key(owner, "index"), idleKey], [owner.Reference, cutoff]);
        }));
    }

    internal static string CursorPosition(InventoryItem item) =>
        FormattableString.Invariant($"{item.SlotNumber ?? 0:D7}:{item.ItemInstanceId:D}");

    private async Task<RedisResult[]> Evaluate(string script, RedisKey[] keys, string payload) =>
        (RedisResult[])(await cache.ScriptEvaluateAsync(script, keys, [payload]))!;
}
