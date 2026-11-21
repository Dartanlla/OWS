using System.Data;
using Dapper;
using Npgsql;

namespace OWSInventoryService;

public sealed class InventoryDatabase(NpgsqlDataSource source)
{
    private const string OwnerFilter = "customer_guid = @CustomerGuid AND character_id = @CharacterId";
    private const string StateRow = "SELECT json_build_object('revision', revision, 'authority', authority, 'authorityGeneration', authority_generation, 'closed', closed)::text FROM ows_inventory.owners WHERE " + OwnerFilter;
    private const string ContainersSql = """
        SELECT json_build_object('containerId', container_id, 'slotted', slotted, 'capacity', capacity,
            'gridWidth', grid_width, 'occupied', occupied, 'revision', revision, 'hidden', hidden)::text
        FROM ows_inventory.containers WHERE
        """ + " " + OwnerFilter;
    private const string ItemJson = """
        json_build_object('itemInstanceId', item_instance_id, 'containerId', container_id,
            'slotted', slotted, 'slotNumber', slot_number, 'definitionId', definition_id, 'quantity', quantity,
            'maxStack', max_stack, 'revision', revision, 'condition', condition, 'usesLeft', uses_left, 'rewardId', reward_id)::text
        """;
    private const string ItemsSql = "SELECT " + ItemJson + " FROM ows_inventory.items WHERE " + OwnerFilter;
    private const string DefinitionSql = """
        SELECT json_build_object('definitionId', definition_id, 'name', name, 'maxStack', max_stack, 'metadata', metadata)::text
        FROM ows_inventory.definitions WHERE customer_guid = @CustomerGuid AND definition_id = @DefinitionId
        """;

    public async Task<bool> Ready(CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(Command(
            "SELECT to_regclass('ows_inventory.buckets') IS NOT NULL", null, cancellationToken));
    }

    public async Task<InventorySnapshot?> Load(Owner owner, long receiptCutoff, bool provision, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        if (provision)
            await EnsureOwner(connection, owner, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var state = await ReadState(connection, transaction, owner, cancellationToken);
        if (state is null)
            return null;

        using var rows = await connection.QueryMultipleAsync(Command($"""
            {ContainersSql} ORDER BY container_id;
            {ItemsSql};
            SELECT payload::text FROM ows_inventory.receipts WHERE {OwnerFilter} AND issued_at >= @receiptCutoff
            """, new { owner.CustomerGuid, owner.CharacterId, receiptCutoff }, cancellationToken, transaction));
        var containers = Parse<InventoryContainer>(await rows.ReadAsync<string>());
        var items = Parse<InventoryItem>(await rows.ReadAsync<string>());
        var receipts = Parse<InventoryEvent>(await rows.ReadAsync<string>());
        await transaction.CommitAsync(cancellationToken);
        return new(state, containers, items, receipts);
    }

    private static async Task EnsureOwner(NpgsqlConnection connection, Owner owner, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(Command("""
            WITH created AS (
                INSERT INTO ows_inventory.owners (customer_guid, character_id)
                SELECT customerguid, characterid FROM public.characters
                WHERE customerguid = @CustomerGuid AND characterid = @CharacterId
                ON CONFLICT DO NOTHING RETURNING *
            )
            INSERT INTO ows_inventory.containers (customer_guid, character_id, container_id, slotted, capacity, grid_width, hidden)
            SELECT created.customer_guid, created.character_id, t.container_id, t.slotted, t.capacity, t.grid_width, false
            FROM created JOIN ows_inventory.templates t USING (customer_guid)
            UNION ALL
            SELECT customer_guid, character_id, '_loot', false, @hiddenCapacity, NULL::int, true FROM created
            """, new { owner.CustomerGuid, owner.CharacterId, hiddenCapacity = InventoryOptions.HiddenLootCapacity }, cancellationToken));
    }

    public async Task<InventoryState?> State(Owner owner, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        return await ReadState(connection, null, owner, cancellationToken);
    }

    private static async Task<InventoryState?> ReadState(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        Owner owner, CancellationToken cancellationToken)
    {
        var value = await connection.QuerySingleOrDefaultAsync<string>(Command(StateRow, owner, cancellationToken, transaction));
        return value is null ? null : InventoryJson.Read<InventoryState>(value);
    }

    public async Task<(InventoryState? State, InventoryContainer[] Containers)> Containers(Owner owner, bool provision, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        if (provision)
            await EnsureOwner(connection, owner, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        using var rows = await connection.QueryMultipleAsync(Command(
            $"{StateRow}; {ContainersSql} AND NOT hidden ORDER BY container_id", owner, cancellationToken, transaction));
        var state = await rows.ReadSingleOrDefaultAsync<string>();
        var containers = Parse<InventoryContainer>(await rows.ReadAsync<string>());
        await transaction.CommitAsync(cancellationToken);
        return (state is null ? null : InventoryJson.Read<InventoryState>(state), containers);
    }

    public async Task<(InventoryState? State, InventoryContainer? Container, InventoryItem[] Items)> Page(
        Owner owner, string containerId, int limit, int afterSlot, Guid? afterItem, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        using var rows = await connection.QueryMultipleAsync(Command($"""
            {StateRow};
            {ContainersSql} AND container_id = @containerId AND NOT hidden;
            {ItemsSql} AND container_id = @containerId AND container_id <> '_loot'
                AND (@afterItem::uuid IS NULL OR (coalesce(slot_number, 0), item_instance_id) > (@afterSlot, @afterItem::uuid))
            ORDER BY coalesce(slot_number, 0), item_instance_id LIMIT @limit
            """, new { owner.CustomerGuid, owner.CharacterId, containerId, limit, afterSlot, afterItem }, cancellationToken, transaction));
        var state = await rows.ReadSingleOrDefaultAsync<string>();
        var container = await rows.ReadSingleOrDefaultAsync<string>();
        var items = Parse<InventoryItem>(await rows.ReadAsync<string>());
        await transaction.CommitAsync(cancellationToken);
        return (state is null ? null : InventoryJson.Read<InventoryState>(state),
            container is null ? null : InventoryJson.Read<InventoryContainer>(container), items);
    }

    public async Task<InventoryEvent?> Receipt(Owner owner, Guid commandId, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        var payload = await connection.QuerySingleOrDefaultAsync<string>(Command($"""
            SELECT payload::text FROM ows_inventory.receipts WHERE {OwnerFilter} AND command_id = @commandId
            """, new { owner.CustomerGuid, owner.CharacterId, commandId }, cancellationToken));
        return payload is null ? null : InventoryJson.Read<InventoryEvent>(payload);
    }

    public async Task<(InventoryState? State, InventoryEvent[] Events)> Changes(Owner owner, long after, int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        using var rows = await connection.QueryMultipleAsync(Command($"""
            {StateRow};
            SELECT payload::text FROM ows_inventory.receipts WHERE {OwnerFilter} AND revision > @after
            ORDER BY revision LIMIT @limit
            """, new { owner.CustomerGuid, owner.CharacterId, after, limit }, cancellationToken, transaction));
        var state = await rows.ReadSingleOrDefaultAsync<string>();
        var events = Parse<InventoryEvent>(await rows.ReadAsync<string>());
        await transaction.CommitAsync(cancellationToken);
        return (state is null ? null : InventoryJson.Read<InventoryState>(state), events);
    }

    public async Task<bool> RewardExists(Owner owner, Guid rewardId, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(Command($"""
            SELECT EXISTS (SELECT FROM ows_inventory.loot WHERE {OwnerFilter} AND reward_id = @rewardId)
            """, new { owner.CustomerGuid, owner.CharacterId, rewardId }, cancellationToken));
    }

    public async Task<InventoryItem[]> StagedLoot(Owner owner, Guid? afterRewardId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        return Parse<InventoryItem>(await connection.QueryAsync<string>(Command($"""
            {ItemsSql} AND container_id = '_loot' AND reward_id IS NOT NULL
                AND (@afterRewardId::uuid IS NULL OR reward_id > @afterRewardId::uuid)
            ORDER BY reward_id LIMIT @limit
            """, new { owner.CustomerGuid, owner.CharacterId, afterRewardId, limit }, cancellationToken)));
    }

    public async Task<LootReceipt?> Loot(Owner owner, Guid rewardId, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        var value = await connection.QuerySingleOrDefaultAsync<string>(Command($"""
            SELECT json_build_object('rewardId', reward_id, 'itemInstanceId', item_instance_id, 'claimed', claimed)::text
            FROM ows_inventory.loot WHERE {OwnerFilter} AND reward_id = @rewardId
            """, new { owner.CustomerGuid, owner.CharacterId, rewardId }, cancellationToken));
        return value is null ? null : InventoryJson.Read<LootReceipt>(value);
    }

    public async Task<ItemDefinition?> Definition(Guid customerGuid, string definitionId, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        var potential = await connection.QuerySingleOrDefaultAsync<string>(Command(DefinitionSql,
            new { CustomerGuid = customerGuid, DefinitionId = definitionId }, cancellationToken));
        return potential is null ? null : InventoryJson.Read<ItemDefinition>(potential);
    }

    public async Task<bool> PutDefinition(Guid customerGuid, ItemDefinition definition, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(Command("""
            INSERT INTO ows_inventory.definitions (customer_guid, definition_id, name, max_stack, metadata)
            VALUES (@customerGuid, @DefinitionId, @Name, @MaxStack, @metadata::jsonb)
            ON CONFLICT (customer_guid, definition_id) DO UPDATE SET name = excluded.name
            WHERE definitions.name = excluded.name AND definitions.max_stack = excluded.max_stack
                AND definitions.metadata IS NOT DISTINCT FROM excluded.metadata
            RETURNING true
            """, new { customerGuid, definition.DefinitionId, definition.Name, definition.MaxStack,
                     metadata = definition.Metadata?.GetRawText() }, cancellationToken));
    }

    public async Task<bool> PutTemplate(Guid customerGuid, ContainerTemplate template, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(Command("SELECT 1 FROM public.customers WHERE customerguid = @customerGuid FOR UPDATE",
            new { customerGuid }, cancellationToken, transaction));
        var saved = await connection.ExecuteScalarAsync<bool>(Command("""
            INSERT INTO ows_inventory.templates (customer_guid, container_id, slotted, capacity, grid_width)
            SELECT @customerGuid, @ContainerId, @Slotted, @Capacity, @GridWidth
            WHERE (SELECT count(*) FROM ows_inventory.templates WHERE customer_guid = @customerGuid) < 32
                OR EXISTS (SELECT FROM ows_inventory.templates WHERE customer_guid = @customerGuid AND container_id = @ContainerId)
            ON CONFLICT (customer_guid, container_id) DO UPDATE SET container_id = excluded.container_id
            WHERE templates.slotted = excluded.slotted AND templates.capacity IS NOT DISTINCT FROM excluded.capacity
                AND templates.grid_width IS NOT DISTINCT FROM excluded.grid_width
            RETURNING true
            """, new { customerGuid, template.ContainerId, template.Slotted, template.Capacity, template.GridWidth },
            cancellationToken, transaction));
        await transaction.CommitAsync(cancellationToken);
        return saved;
    }

    public async Task<BucketCheckpoint> Checkpoint(int bucket, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        using var rows = await connection.QueryMultipleAsync(Command("""
            INSERT INTO ows_inventory.buckets (bucket) VALUES (@bucket) ON CONFLICT DO NOTHING;
            SELECT epoch, sequence, event_id, generation FROM ows_inventory.buckets WHERE bucket = @bucket
            """, new { bucket }, cancellationToken));
        var result = await rows.ReadSingleAsync<(Guid Epoch, long Sequence, Guid? EventId, long Generation)>();
        return new(result.Epoch, result.Sequence, result.EventId, result.Generation);
    }

    public async Task<bool> Rotate(int bucket, BucketCheckpoint checkpoint, Guid nextEpoch, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        var changed = await connection.ExecuteAsync(Command("""
            UPDATE ows_inventory.buckets SET epoch = @nextEpoch, sequence = 0, event_id = NULL, generation = generation + 1
            WHERE bucket = @bucket AND epoch = @Epoch AND sequence = @Sequence
            """, new { bucket, checkpoint.Epoch, checkpoint.Sequence, nextEpoch }, cancellationToken));
        return changed == 1;
    }

    public async Task<bool> Apply(int bucket, BucketCheckpoint checkpoint, string events, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        using var rows = await connection.QueryMultipleAsync(Command("""
            SET LOCAL synchronous_commit = on;
            SELECT ows_inventory.apply_batch(@bucket, @Epoch, @Sequence, @events::jsonb)
            """, new { bucket, checkpoint.Epoch, checkpoint.Sequence, events }, cancellationToken, transaction));
        var applied = await rows.ReadSingleAsync<bool>();
        await transaction.CommitAsync(cancellationToken);
        return applied;
    }

    public async Task PruneReceipts(long cutoff, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(Command("""
            DELETE FROM ows_inventory.receipts
            WHERE ctid IN (SELECT ctid FROM ows_inventory.receipts WHERE issued_at < @cutoff
                ORDER BY issued_at LIMIT 25000 FOR UPDATE SKIP LOCKED)
            """, new { cutoff }, cancellationToken));
    }

    private static CommandDefinition Command(string sql, object? args, CancellationToken cancellationToken, NpgsqlTransaction? transaction = null) =>
        new(sql, args, transaction, cancellationToken: cancellationToken);

    private static T[] Parse<T>(IEnumerable<string> rows) => rows.Select(InventoryJson.Read<T>).ToArray();
}
