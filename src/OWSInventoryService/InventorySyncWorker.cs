using System.Diagnostics;
using System.Text.Json;
using Npgsql;
using StackExchange.Redis;

namespace OWSInventoryService;

internal sealed class InventorySyncWorker(
    InventoryDatabase database, InventoryRedis redis, InventoryOptions options, ILogger<InventorySyncWorker> logger)
    : BackgroundService
{
    private const int BatchSize = 128;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buckets = Enumerable.Range(options.FirstBucket, options.LastBucket - options.FirstBucket + 1);
        return Task.WhenAll(buckets.Select(bucket => RunBucket(bucket, stoppingToken)));
    }

    private async Task RunBucket(int bucket, CancellationToken cancellationToken)
    {
        BucketCheckpoint? checkpoint = null;
        var heartbeat = DateTimeOffset.MinValue;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var refreshLease = checkpoint is null || now >= heartbeat;
                if (refreshLease)
                {
                    checkpoint = await database.Checkpoint(bucket, cancellationToken);
                    var status = await redis.Sync(bucket, checkpoint, "verify");
                    if (status == "diverged")
                    {
                        var nextEpoch = Guid.NewGuid();
                        if (await database.Rotate(bucket, checkpoint, nextEpoch, cancellationToken))
                        {
                            if (checkpoint.Sequence > 0)
                                logger.LogWarning("Redis journal diverged from PostgreSQL in bucket {Bucket}. Rehydrating committed state.", bucket);
                            checkpoint = new(nextEpoch, 0, null, checkpoint.Generation + 1);
                            await redis.Sync(bucket, checkpoint, "install");
                        }
                        else
                        {
                            checkpoint = null;
                            continue;
                        }
                    }
                    if (status == "stale")
                    {
                        checkpoint = null;
                        continue;
                    }
                    if (status == "quarantined")
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                        continue;
                    }
                    heartbeat = now.AddSeconds(1);
                    await redis.Sweep(bucket, TimeSpan.FromSeconds(options.IdleSeconds));
                    if (bucket == 0)
                        await database.PruneReceipts(now.AddSeconds(-options.RetrySeconds - 30).ToUnixTimeMilliseconds(), cancellationToken);
                }

                var entries = await redis.Pending(bucket, checkpoint!.Sequence, BatchSize);
                if (entries.Length > 0)
                {
                    // A second worker or a failover can advance or replace the Redis stream while we read it.
                    // Verify the durable anchor immediately before applying, under the same Redis epoch.
                    if (await redis.Sync(bucket, checkpoint, "verify") != "ready")
                    {
                        checkpoint = null;
                        continue;
                    }
                    var start = Stopwatch.GetTimestamp();
                    var events = new JsonElement[entries.Length];
                    long expected = checkpoint.Sequence;
                    for (var i = 0; i < entries.Length; i++)
                    {
                        expected++;
                        if (entries[i].Id != $"{expected}-0" || entries[i].Values.Length != 1)
                            throw new InvalidDataException($"Inventory journal lost sequence {expected}.");
                        using var payload = JsonDocument.Parse(entries[i].Values[0].Value.ToString());
                        events[i] = payload.RootElement.Clone();
                    }

                    if (await database.Apply(bucket, checkpoint, InventoryJson.Write(events), cancellationToken))
                    {
                        var eventId = events[^1].GetProperty("eventId").GetGuid();
                        checkpoint = checkpoint with { Sequence = expected, EventId = eventId };
                        InventoryMetrics.FlushDuration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                        InventoryMetrics.Persisted.Add(events.Length);
                        InventoryMetrics.PersistenceLag.Record((DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(
                            events[0].GetProperty("issuedAt").GetInt64())).TotalMilliseconds);
                        await redis.Sync(bucket, checkpoint, "refresh");
                    }
                    else
                    {
                        checkpoint = null;
                        continue;
                    }
                    if (entries.Length == BatchSize)
                        continue;
                }
                else if (refreshLease)
                {
                    await redis.Sync(bucket, checkpoint, "refresh");
                }

                await Task.Delay(options.FlushMilliseconds, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                var invariant = error is InvalidDataException or JsonException
                    || error is PostgresException postgres && (postgres.SqlState == "P0001"
                        || postgres.SqlState.StartsWith("22", StringComparison.Ordinal)
                        || postgres.SqlState.StartsWith("23", StringComparison.Ordinal));
                if (invariant)
                {
                    InventoryMetrics.Quarantined.Add(1);
                    logger.LogError(error, "Inventory invariant failed. Quarantining bucket {Bucket}.", bucket);
                }
                else
                {
                    logger.LogWarning(error, "Inventory sync paused for bucket {Bucket}.", bucket);
                }
                if (checkpoint is not null)
                {
                    try
                    {
                        await redis.Sync(bucket, checkpoint, invariant ? "quarantine" : "pause");
                    }
                    catch (RedisException)
                    {
                        // The readiness marker also has a short expiry.
                    }
                }

                checkpoint = null;
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
