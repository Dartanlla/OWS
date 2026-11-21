using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OWSInventoryService;

public sealed class InventoryOptions
{
    public const int BucketCount = 64;
    public const int HiddenLootCapacity = 10000;
    public int FirstBucket { get; init; }
    public int LastBucket { get; init; } = BucketCount - 1;
    public int FlushMilliseconds { get; init; } = 20;
    public int ConfirmationMilliseconds { get; init; } = 100;
    public int MaxPendingPerBucket { get; init; } = 10000;
    public int IdleSeconds { get; init; } = 300;
    public int RetrySeconds { get; init; } = 600;
    public bool RunWorker { get; init; }
    public InventoryCredential[] Credentials { get; init; } = [];
}

public sealed record InventoryCredential(string Name, Guid CustomerGuid, string Sha256, bool Manage = false);
public sealed record InventoryPrincipal(string Name, Guid CustomerGuid, bool Manage);
public readonly record struct Owner(Guid CustomerGuid, int CharacterId)
{
    public string Reference => FormattableString.Invariant($"{CustomerGuid:D}:{CharacterId}");
    public int Bucket
    {
        get
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(Reference));
            return (int)(BinaryPrimitives.ReadUInt32LittleEndian(digest) % InventoryOptions.BucketCount);
        }
    }
}

public enum InventoryOperation { Unknown, Grant, Remove, Move, Split, Merge, Swap, StageLoot, ClaimLoot, SetAuthority, Close }
public record InventoryCommand(
    Guid CommandId, DateTimeOffset IssuedAt, InventoryOperation Operation,
    Guid? AuthorityGeneration = null, string? ContainerId = null, int? SlotNumber = null,
    Guid? ItemInstanceId = null, long? ExpectedItemRevision = null, Guid? TargetItemInstanceId = null,
    long? ExpectedTargetRevision = null, string? DefinitionId = null, int? Quantity = null,
    int? Condition = null, int? UsesLeft = null, Guid? RewardId = null,
    string? NewAuthority = null, Guid? NewAuthorityGeneration = null);

public sealed record ItemDefinition(string DefinitionId, string Name, int MaxStack, JsonElement? Metadata);
public sealed record ContainerTemplate(string ContainerId, bool Slotted, int? Capacity, int? GridWidth = null);
public sealed record InventoryContainer(string ContainerId, bool Slotted, int? Capacity, int? GridWidth,
    int Occupied, long Revision, bool Hidden);
public sealed record InventoryItem(Guid ItemInstanceId, string ContainerId, bool Slotted, int? SlotNumber,
    string DefinitionId, int Quantity, int MaxStack, long Revision, int? Condition = null,
    int? UsesLeft = null, Guid? RewardId = null);
public sealed record InventoryState(long Revision, string? Authority, Guid? AuthorityGeneration, bool Closed);
public sealed record InventorySnapshot(InventoryState State, InventoryContainer[] Containers,
    InventoryItem[] Items, InventoryEvent[] Receipts);
public sealed record LootReceipt(Guid RewardId, Guid ItemInstanceId, bool Claimed);
public sealed record InventoryEvent(string Owner, Guid CommandId, string Fingerprint, long IssuedAt,
    long PreviousRevision, long Revision, InventoryState State, Guid EventId, int SchemaVersion,
    InventoryItem[]? Upserts = null, Guid[]? Deletes = null, InventoryContainer[]? Containers = null,
    LootReceipt? Loot = null, long? Sequence = null)
{
    public object Result() => new
    {
        commandId = CommandId, revision = Revision,
        items = Upserts ?? [], removedItemIds = Deletes ?? [], containers = Containers ?? [], loot = Loot
    };
}

public sealed record InventoryPage(long Revision, InventoryItem[] Items, string? NextCursor);
public sealed record BucketCheckpoint(Guid Epoch, long Sequence, Guid? EventId, long Generation);
public sealed record Submission(string Status, string? Error = null, InventoryEvent? Event = null, Guid? Epoch = null);

internal static class InventoryJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Options)
        ?? throw new InvalidDataException($"Invalid {typeof(T).Name}.");
}
