---
layout: default
title: OWSInventoryService
---

# OWSInventoryService

OWSInventoryService is an opt-in private API for OWS characters on PostgreSQL. Redis validates and queues each item transaction. The sync worker commits the item changes and their receipt to PostgreSQL before the API reports success. The service uses the OWS customer and character IDs. A character can have both numbered-slot and slotless containers.

## Start the service

For an existing PostgreSQL installation, apply [z-inventory.sql](../../src/.docker/postgres/z-inventory.sql) with psql before starting the API or worker. The script connects to the OWS database named openworldserver. The OWS PostgreSQL Docker image applies it during a fresh initialization. Use PostgreSQL and Redis 7 or a compatible Redis deployment that supports the same Stream and Lua commands.

For local Compose, select PostgreSQL and set INVENTORY_REDIS_PASSWORD to a high-entropy value without commas. Set the service credential environment variables in src/.env.inventory. From src, run:

    docker compose -f docker-compose.yml -f docker-compose.inventory.yml --profile inventory up -d --build owsinventoryservice owsinventoryworker

The API listens on the loopback interface on port 44440 by default. For a shared deployment, use a private HTTPS entry point and keep PostgreSQL and Redis private. Use a dedicated Redis store with AOF, a no-eviction policy, and capacity for the active projections, replay queue, and persistence rewrite. Configure PostgreSQL standby durability to match the recovery guarantee you want after a PostgreSQL failover.

The API and worker use ConnectionStrings__InventoryPostgres and ConnectionStrings__InventoryRedis. Use Inventory__RunWorker=true in worker processes. Worker partitions range from 0 to 63. FirstBucket and LastBucket select an inclusive range for a worker deployment. Cover every bucket. The default flush interval is 20 ms, and the API waits up to 100 ms for confirmation. Inventory__RetrySeconds defaults to 600 and must match across the API and workers.

Configure each trusted caller with a unique bearer token of at least 32 characters. Give the API the token's SHA-256 hex digest through Inventory__Credentials__0__Sha256 and configure Inventory__Credentials__0__Name and Inventory__Credentials__0__CustomerGuid. Set Inventory__Credentials__0__Manage=true only for the account that provisions definitions, templates, and character authority. Use a separate credential per Region authority. The tenant comes from the verified credential. If supplied, X-CustomerGUID must match it.

## API

Character routes use /v1/characters/{characterId}/inventory. The CharacterId is the positive integer from OWS. Responses have camel-case JSON fields.

| Method and path | Purpose |
| --- | --- |
| PUT /v1/definitions/{definitionId} | Create a tenant item definition with definitionId, name, maxStack and optional game metadata. Management only. |
| GET /v1/definitions/{definitionId} | Read a definition for the caller's tenant. |
| PUT /v1/container-templates/{containerId} | Add a template with containerId, slotted, optional capacity and gridWidth. Slotted containers require capacity. Management only. |
| GET /authority | Read the committed Region authority and generation. Management only. |
| GET /containers | List visible containers and the current inventory revision. |
| GET /containers/{containerId}/items?limit=50&cursor=… | List a bounded page. Use nextCursor for the next request. A 409 refresh_listing means the container changed and paging must restart. |
| POST /commands | Submit an item operation or an authority change. |
| GET /commands/{commandId} | Resolve a pending or uncertain submission. |
| GET /changes?afterRevision=0&limit=50 | Fetch committed changes. Resume at the last returned change revision. A 410 refresh_inventory means the consumer must reload. |
| GET /loot?limit=50&afterRewardId=… | Read committed and unclaimed personal loot for this character. |
| GET /loot/{rewardId} | Check a personal reward's durable item identity and claimed status. |

Definitions and templates are immutable in this version. Register templates before assigning authority to a character for the first time. Each inventory copies the configured templates once and receives an internal loot container. Container and definition IDs are case-sensitive. A slotless page has no gameplay sort order and uses a cursor for stable traversal. Filters and display sorting belong in the game. Apply filters to all pages for the selected container when the UI must show matches across the entire container.

The trusted admission service sends setAuthority using its management credential after authenticating a character. Choose a fresh newAuthorityGeneration UUID and supply the newAuthority credential name. Include the current authorityGeneration for handoff and omit it for first admission. Subsequent gameplay commands send that generation in the JSON body. Gameplay reads send it in X-Inventory-Authority. Use a new generation on handoff, including reconnects that establish a new authority. Wait for the handoff command to commit before routing inventory actions through the new Region. On logout or authority revocation, send setAuthority with the current generation and omit both new authority fields.

Every command requires a commandId UUID, issuedAt in UTC, and operation. Retain the same payload, timestamp, and command ID for retries. A successful response returns the committed revision, affected items and containers, and any removed item IDs. A 202 includes a status URL and confirms that the command is pending. A timeout, 503, or missing receipt can have an uncertain outcome. Poll the status URL or resubmit the original command within the retry window. A confirmed receipt remains queryable for the configured window. Reusing a retained command ID with a changed payload returns 409.

| Operation | Additional fields |
| --- | --- |
| grant | containerId, definitionId, quantity, slotNumber for slotted containers, and optional condition and usesLeft. |
| remove | itemInstanceId, expectedItemRevision, quantity. |
| move | itemInstanceId, expectedItemRevision, containerId, slotNumber for slotted containers. |
| split | Same as move, with a quantity smaller than the source stack. |
| merge | itemInstanceId, expectedItemRevision, targetItemInstanceId, expectedTargetRevision, quantity. |
| swap | Both item IDs and expected revisions. Both items occupy numbered slots. |
| stageLoot | rewardId, definitionId, quantity, and optional condition and usesLeft. Uses the hidden container. |
| claimLoot | rewardId, itemInstanceId, destination containerId and slotNumber for slotted containers. |
| setAuthority | newAuthority, newAuthorityGeneration, and the previous authorityGeneration for a handoff. Management only. |
| close | Current authorityGeneration if assigned. Management only. |

A grant fits in one stack. Slotless grants consolidate into a compatible stack with room or create a new stack. Numbered slots start at zero and hold one stack. Move targets an empty slot or any slotless container with room. Use merge or swap when moving into an occupied slot. A full destination leaves the source unchanged.

## Nilux personal loot flow

1. Bind the inventory authority to the Region using the verified OWS ticket identity. Provision slotless bag and storage templates for the Nilux customer.
2. The authoritative game server creates a stable personal reward ID and sends stageLoot. Make that reward visible only after the service confirms the write to PostgreSQL. If the server restarts, GET /loot recovers the staged rewards for the new authority.
3. When the player takes a reward, send claimLoot with its committed item instance ID. The same transaction moves that item into the destination and marks the reward claimed. If capacity is exhausted, the committed reward stays in the hidden container. Keep the reservation associated with the original command until an uncertain submission is resolved.
4. Display visible item pages and apply change deltas keyed by itemInstanceId. On reconnect, resume from a retained change revision or reload the pages. Claiming a reward after reconnect can use the same reward record through the new authority.

The game assigns a personal reward to one character before staging it. If the game offers the same world drop to several characters, the world-drop authority must select and durably reserve the winner first. Inventory tracks the permanent reward ID for the selected character so it cannot be restaged after a claim, even after the command receipt expires.

## Recovery and deletion

The worker reads each Redis Stream in order from its durable PostgreSQL checkpoint. The checkpoint and item transaction commit together. Redis retains the confirmed anchor for reconciliation. If Redis loses the durable anchor, the worker fences and restarts the affected bucket and reloads committed projections from PostgreSQL. Unconfirmed requests can require resubmission with the original ID. Confirmed personal loot remains discoverable in PostgreSQL during Redis recovery. A pending command or stale cache does not appear in a normal inventory listing as committed.

The command API verifies the exact PostgreSQL receipt before returning success. If Redis loses a queued command and another command reuses its sequence, the original request stays pending until its own receipt exists.

The OWS.Inventory .NET meter publishes acceptance, confirmation, batch duration, persistence lag, persisted commands, and quarantined buckets. A journal sequence or SQL invariant failure closes admission for the affected bucket and logs the failure. Inspect and repair the reported invariant before clearing that bucket's quarantined field from its Redis control hash.

For a character with provisioned inventory, first submit close through the management API and wait for confirmation before calling OWS RemoveCharacter. Close follows pending commands in order and prevents further mutations. OWS then deletes the inventory through the character foreign key. Characters without provisioned inventory continue through the existing removal path.

The [integration test](../../src/OWSInventoryService/tests/Inventory.Integration.ps1) uses disposable PostgreSQL and Redis containers. Run it with PowerShell 7.4 or later, Docker, and the .NET 10 SDK. The [implementation plan](../ows-inventory-service-plan.md) describes the production concurrency and failover tests.

