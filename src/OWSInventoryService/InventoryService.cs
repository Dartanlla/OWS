using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace OWSInventoryService;

internal sealed class InventoryService(InventoryDatabase database, InventoryRedis redis, InventoryOptions options)
{
    private readonly MemoryCache definitions = new(new MemoryCacheOptions { SizeLimit = 10000 });
    private readonly SemaphoreSlim coldReads = new(64);
    private readonly SemaphoreSlim receiptReads = new(16);

    public async Task<Submission> Submit(Owner owner, InventoryPrincipal principal, InventoryCommand command, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            InventoryJson.Write(new { principal.Name, command }))));
        var issuedAt = command.IssuedAt.ToUnixTimeMilliseconds();
        var itemId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var provision = principal.Manage && command.Operation is InventoryOperation.SetAuthority or InventoryOperation.Close;
        var definition = command.DefinitionId is { } id && command.Operation is InventoryOperation.Grant or InventoryOperation.StageLoot
            ? await GetDefinition(owner.CustomerGuid, id, cancellationToken) : null;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            var rewardBasis = command.Operation == InventoryOperation.StageLoot ? await redis.StateBasis(owner) : null;
            var rewardExists = rewardBasis is not null &&
                await database.RewardExists(owner, command.RewardId!.Value, cancellationToken);
            var result = await redis.Apply(owner, new
            {
                owner = owner.Reference, actor = principal.Name, manage = principal.Manage, command, fingerprint,
                issuedAt, itemId, eventId, rewardBasis, rewardExists, maxStack = definition?.MaxStack,
                retryMilliseconds = options.RetrySeconds * 1000, maxPending = options.MaxPendingPerBucket
            });
            switch (result.Status)
            {
                case "cold":
                    var epoch = await redis.ActiveEpoch(owner.Bucket);
                    if (!Guid.TryParse(epoch.ToString(), out var active))
                        return new("unavailable", "sync_unavailable");
                    var observed = false;
                    var loaded = await redis.Hydrate(owner, active, Guid.NewGuid().ToString("N"), async () =>
                    {
                        var snapshot = await database.Load(owner, DateTimeOffset.UtcNow.AddSeconds(-options.RetrySeconds - 10)
                            .ToUnixTimeMilliseconds(), provision, cancellationToken);
                        observed = snapshot is not null;
                        return snapshot;
                    });
                    if (loaded && !observed)
                        return new("missing", "character_not_found");
                    if (!loaded)
                        await Task.Delay(20 * (attempt + 1), cancellationToken);
                    continue;
                case "retry":
                    continue;
                case "committed":
                    return await ConfirmReceipt(owner, result, cancellationToken);
                case "pending":
                    InventoryMetrics.Acceptance.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                    var confirmation = await AwaitConfirmation(owner, result, cancellationToken);
                    if (confirmation.Status == "committed")
                        InventoryMetrics.Confirmation.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                    return confirmation;
                default:
                    return result;
            }
        }
        return new("unavailable", "projection_busy");
    }

    private async Task<Submission> AwaitConfirmation(Owner owner, Submission pending, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(options.ConfirmationMilliseconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await redis.Confirmed(owner.Bucket, pending.Epoch!.Value) >= pending.Event!.Sequence)
                return await ConfirmReceipt(owner, pending, cancellationToken);

            await Task.Delay(10, cancellationToken);
        }

        return pending;
    }

    private async Task<Submission> ConfirmReceipt(Owner owner, Submission submission, CancellationToken cancellationToken)
    {
        // A failover can reuse the sequence of a lost Redis event. The watermark only signals
        // that it is time to check the durable receipt for this command.
        if (!await receiptReads.WaitAsync(0, cancellationToken))
            return submission with { Status = "pending" };
        try
        {
            var proposed = submission.Event!;
            var receipt = await database.Receipt(owner, proposed.CommandId, cancellationToken);
            if (receipt is null)
                return submission with { Status = "pending" };
            if (receipt.Fingerprint != proposed.Fingerprint)
                return new("conflict", "command_id_reused");
            return submission with { Status = "committed", Event = receipt };
        }
        finally
        {
            receiptReads.Release();
        }
    }

    public async Task<ItemDefinition?> GetDefinition(Guid tenant, string id, CancellationToken cancellationToken)
    {
        if (definitions.TryGetValue((tenant, id), out ItemDefinition? definition))
            return definition;

        definition = await database.Definition(tenant, id, cancellationToken);
        if (definition is not null)
            definitions.Set((tenant, id), definition, new MemoryCacheEntryOptions()
                .SetSize(1).SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
        return definition;
    }

    public async Task<IResult> Containers(Owner owner, InventoryPrincipal principal, Guid? generation, CancellationToken cancellationToken)
    {
        var cached = await Read(owner, new { owner = owner.Reference, actor = principal.Name, principal.Manage, generation, operation = "containers" });
        if (cached is not null && (string?)cached[0] != "cold")
        {
            var status = (string)cached[0]!;
            if (status != "ok")
                return Error(status, "inventory_unavailable");
            return Results.Ok(new
            {
                revision = long.Parse((string)cached[1]!, CultureInfo.InvariantCulture),
                containers = cached.Skip(2).Select(row => InventoryJson.Read<InventoryContainer>((string)row!)).ToArray()
            });
        }

        if (!await coldReads.WaitAsync(0, cancellationToken))
            return Error("unavailable", "read_capacity");
        try
        {
            var fallback = await database.Containers(owner, principal.Manage, cancellationToken);
            return RejectRead(fallback.State, principal, generation)
                ?? Results.Ok(new { revision = fallback.State!.Revision, containers = fallback.Containers });
        }
        finally
        {
            coldReads.Release();
        }
    }

    public async Task<IResult> Page(Owner owner, InventoryPrincipal principal, Guid? generation, string containerId,
        int limit, string? cursor, CancellationToken cancellationToken)
    {
        var requested = cursor is null ? null : ParseCursor(owner, containerId, cursor);
        if (cursor is not null && requested is null)
            return Error("invalid", "invalid_cursor");

        var cached = await Read(owner, new
        {
            owner = owner.Reference, actor = principal.Name, principal.Manage, generation, operation = "page",
            containerId, limit, revision = requested?.Revision,
            after = requested is null ? null : FormattableString.Invariant($"{requested.Slot:D7}:{requested.Item:D}")
        });
        if (cached is not null && (string?)cached[0] != "cold")
        {
            var status = (string)cached[0]!;
            if (status != "ok")
                return Error(status, status == "conflict" ? "refresh_listing" : "inventory_unavailable");
            var revision = long.Parse((string)cached[1]!, CultureInfo.InvariantCulture);
            var items = cached.Skip(3).Select(row => InventoryJson.Read<InventoryItem>((string)row!)).ToArray();
            return Results.Ok(BuildPage(owner, containerId, items, revision, (string?)cached[2] == "more"));
        }

        if (!await coldReads.WaitAsync(0, cancellationToken))
            return Error("unavailable", "read_capacity");
        try
        {
            var list = await database.Page(owner, containerId, limit + 1, requested?.Slot ?? 0, requested?.Item, cancellationToken);
            if (RejectRead(list.State, principal, generation) is { } rejected)
                return rejected;
            if (list.Container is not { } box)
                return Error("missing", "container_not_found");
            if (requested is not null && requested.Revision != box.Revision)
                return Error("conflict", "refresh_listing");
            return Results.Ok(BuildPage(owner, containerId, list.Items.Take(limit).ToArray(), box.Revision, list.Items.Length > limit));
        }
        finally
        {
            coldReads.Release();
        }
    }

    private async Task<RedisResult[]?> Read(Owner owner, object arguments)
    {
        try
        {
            return await redis.Read(owner, arguments);
        }
        catch (RedisException)
        {
            return null;
        }
    }

    private sealed record Cursor(string Owner, string Container, long Revision, int Slot, Guid Item);

    private static Cursor? ParseCursor(Owner owner, string container, string cursor)
    {
        if (cursor.Length > 512)
            return null;
        try
        {
            var decoded = InventoryJson.Read<Cursor>(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
            return decoded.Owner == owner.Reference && decoded.Container == container && decoded.Revision >= 0
                && decoded.Slot is >= 0 and < 1000000 && decoded.Item != Guid.Empty ? decoded : null;
        }
        catch (Exception error) when (error is FormatException or System.Text.Json.JsonException or InvalidDataException)
        {
            return null;
        }
    }

    private static InventoryPage BuildPage(Owner owner, string container, InventoryItem[] items, long revision, bool more)
    {
        var cursor = more ? Convert.ToBase64String(Encoding.UTF8.GetBytes(InventoryJson.Write(new Cursor(
            owner.Reference, container, revision, items[^1].SlotNumber ?? 0, items[^1].ItemInstanceId)))) : null;
        return new(revision, items, cursor);
    }

    internal static IResult? RejectRead(InventoryState? state, InventoryPrincipal principal, Guid? generation)
    {
        if (state is null)
            return Error("missing", "character_not_found");
        if (!principal.Manage && (generation is null || state.AuthorityGeneration != generation || state.Authority != principal.Name))
            return Error("forbidden", "stale_authority");
        return state.Closed ? Error("gone", "inventory_closed") : null;
    }

    internal static IResult Error(string status, string code) => Results.Json(new { code }, statusCode: status switch
    {
        "invalid" => 400,
        "forbidden" => 403,
        "missing" => 404,
        "conflict" => 409,
        "gone" => 410,
        _ => 503
    });
}
