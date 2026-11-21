\connect openworldserver
\set ON_ERROR_STOP on

BEGIN;
CREATE SCHEMA IF NOT EXISTS ows_inventory;

CREATE TABLE IF NOT EXISTS ows_inventory.owners (
    customer_guid uuid NOT NULL,
    character_id integer NOT NULL,
    revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
    authority text,
    authority_generation uuid,
    closed boolean NOT NULL DEFAULT false,
    PRIMARY KEY (customer_guid, character_id),
    FOREIGN KEY (customer_guid, character_id) REFERENCES public.characters (customerguid, characterid) ON DELETE CASCADE,
    CHECK ((authority IS NULL) = (authority_generation IS NULL))
);

CREATE TABLE IF NOT EXISTS ows_inventory.definitions (
    customer_guid uuid REFERENCES public.customers (customerguid) ON DELETE CASCADE,
    definition_id text NOT NULL CHECK (definition_id ~ '^[A-Za-z0-9_.-]{1,80}$'),
    name text NOT NULL CHECK (char_length(name) BETWEEN 1 AND 160),
    max_stack integer NOT NULL CHECK (max_stack BETWEEN 1 AND 1000000),
    metadata jsonb CHECK (metadata IS NULL OR jsonb_typeof(metadata) = 'object'),
    PRIMARY KEY (customer_guid, definition_id),
    UNIQUE (customer_guid, definition_id, max_stack)
);

CREATE TABLE IF NOT EXISTS ows_inventory.templates (
    customer_guid uuid REFERENCES public.customers (customerguid) ON DELETE CASCADE,
    container_id text NOT NULL CHECK (container_id ~ '^[A-Za-z0-9_.-]{1,80}$' AND container_id <> '_loot'),
    slotted boolean NOT NULL,
    capacity integer CHECK (capacity BETWEEN 1 AND 1000000),
    grid_width integer CHECK (grid_width BETWEEN 1 AND capacity),
    PRIMARY KEY (customer_guid, container_id),
    CHECK (NOT slotted OR capacity IS NOT NULL),
    CHECK (grid_width IS NULL OR slotted)
);

CREATE TABLE IF NOT EXISTS ows_inventory.containers (
    customer_guid uuid NOT NULL,
    character_id integer NOT NULL,
    container_id text NOT NULL,
    slotted boolean NOT NULL,
    capacity integer CHECK (capacity BETWEEN 1 AND 1000000),
    grid_width integer CHECK (grid_width BETWEEN 1 AND capacity),
    hidden boolean NOT NULL DEFAULT false,
    occupied integer NOT NULL DEFAULT 0 CHECK (occupied >= 0 AND (capacity IS NULL OR occupied <= capacity)),
    revision bigint NOT NULL DEFAULT 0,
    PRIMARY KEY (customer_guid, character_id, container_id),
    FOREIGN KEY (customer_guid, character_id) REFERENCES ows_inventory.owners ON DELETE CASCADE,
    UNIQUE (customer_guid, character_id, container_id, slotted),
    CHECK (NOT slotted OR capacity IS NOT NULL),
    CHECK (NOT hidden OR (container_id = '_loot' AND NOT slotted))
);

CREATE TABLE IF NOT EXISTS ows_inventory.items (
    customer_guid uuid NOT NULL,
    character_id integer NOT NULL,
    item_instance_id uuid NOT NULL,
    container_id text NOT NULL,
    slotted boolean NOT NULL,
    slot_number integer CHECK (slot_number >= 0),
    definition_id text NOT NULL,
    max_stack integer NOT NULL,
    quantity integer NOT NULL CHECK (quantity BETWEEN 1 AND max_stack),
    revision bigint NOT NULL CHECK (revision > 0),
    condition integer CHECK (condition BETWEEN 0 AND 100),
    uses_left integer CHECK (uses_left >= 0),
    reward_id uuid,
    PRIMARY KEY (customer_guid, character_id, item_instance_id),
    FOREIGN KEY (customer_guid, character_id, container_id, slotted)
        REFERENCES ows_inventory.containers (customer_guid, character_id, container_id, slotted) ON DELETE CASCADE,
    FOREIGN KEY (customer_guid, definition_id, max_stack)
        REFERENCES ows_inventory.definitions (customer_guid, definition_id, max_stack),
    CONSTRAINT inventory_unique_slot
        UNIQUE (customer_guid, character_id, container_id, slot_number) DEFERRABLE INITIALLY IMMEDIATE,
    CHECK (slotted = (slot_number IS NOT NULL)),
    CHECK (reward_id IS NULL OR container_id = '_loot')
);

CREATE INDEX IF NOT EXISTS inventory_page ON ows_inventory.items
    (customer_guid, character_id, container_id, (coalesce(slot_number, 0)), item_instance_id);

CREATE TABLE IF NOT EXISTS ows_inventory.receipts (
    customer_guid uuid NOT NULL,
    character_id integer NOT NULL,
    command_id uuid NOT NULL,
    issued_at bigint NOT NULL,
    revision bigint NOT NULL,
    payload jsonb NOT NULL,
    PRIMARY KEY (customer_guid, character_id, command_id),
    FOREIGN KEY (customer_guid, character_id) REFERENCES ows_inventory.owners ON DELETE CASCADE,
    UNIQUE (customer_guid, character_id, revision)
);
CREATE INDEX IF NOT EXISTS inventory_receipt_expiry ON ows_inventory.receipts (issued_at);

CREATE TABLE IF NOT EXISTS ows_inventory.loot (
    customer_guid uuid NOT NULL,
    character_id integer NOT NULL,
    reward_id uuid NOT NULL,
    item_instance_id uuid NOT NULL,
    claimed boolean NOT NULL DEFAULT false,
    PRIMARY KEY (customer_guid, character_id, reward_id),
    FOREIGN KEY (customer_guid, character_id) REFERENCES ows_inventory.owners ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ows_inventory.buckets (
    bucket integer PRIMARY KEY,
    epoch uuid NOT NULL DEFAULT gen_random_uuid(),
    sequence bigint NOT NULL DEFAULT 0,
    event_id uuid,
    generation bigint NOT NULL DEFAULT 1,
    CHECK (sequence >= 0 AND (sequence = 0) = (event_id IS NULL))
);

CREATE INDEX IF NOT EXISTS inventory_pending_loot ON ows_inventory.items (customer_guid, character_id, reward_id)
    WHERE container_id = '_loot' AND reward_id IS NOT NULL;

CREATE OR REPLACE FUNCTION ows_inventory.require_closed_owner() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM ows_inventory.owners
               WHERE customer_guid = OLD.customerguid AND character_id = OLD.characterid AND NOT closed) THEN
        RAISE EXCEPTION 'Close the character inventory before deleting the character'
            USING ERRCODE = '23503', CONSTRAINT = 'ows_inventory_character_open';
    END IF;
    RETURN OLD;
END
$$;

DROP TRIGGER IF EXISTS ows_inventory_character_delete ON public.characters;
CREATE TRIGGER ows_inventory_character_delete BEFORE DELETE ON public.characters
FOR EACH ROW EXECUTE FUNCTION ows_inventory.require_closed_owner();

CREATE OR REPLACE FUNCTION ows_inventory.apply_batch(
    p_bucket integer, p_epoch uuid, p_sequence bigint, p_events jsonb
) RETURNS boolean LANGUAGE plpgsql AS $$
DECLARE
    checkpoint ows_inventory.buckets%ROWTYPE;
    entry jsonb;
    changed jsonb;
    entry_state jsonb;
    tenant uuid;
    character integer;
    next_revision bigint;
    updated_rows integer;
BEGIN
    SELECT * INTO STRICT checkpoint FROM ows_inventory.buckets WHERE bucket = p_bucket FOR UPDATE;
    IF checkpoint.epoch <> p_epoch OR checkpoint.sequence <> p_sequence THEN
        RETURN false;
    END IF;

    SET CONSTRAINTS ows_inventory.inventory_unique_slot DEFERRED;

    FOR entry IN SELECT value FROM jsonb_array_elements(p_events) LOOP
        IF (entry->>'sequence')::bigint <> checkpoint.sequence + 1 OR (entry->>'schemaVersion')::int <> 1 THEN
            RAISE EXCEPTION 'Invalid inventory journal sequence or schema';
        END IF;

        tenant := split_part(entry->>'owner', ':', 1)::uuid;
        character := split_part(entry->>'owner', ':', 2)::integer;
        next_revision := (entry->>'revision')::bigint;
        entry_state := entry->'state';
        IF next_revision <> (entry->>'previousRevision')::bigint + 1 THEN
            RAISE EXCEPTION 'Invalid inventory journal revision';
        END IF;

        UPDATE ows_inventory.owners
        SET revision = next_revision, authority = entry_state->>'authority',
            authority_generation = (entry_state->>'authorityGeneration')::uuid, closed = (entry_state->>'closed')::boolean
        WHERE customer_guid = tenant AND character_id = character
          AND revision = (entry->>'previousRevision')::bigint AND NOT closed;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'Inventory owner revision mismatch in bucket %', p_bucket;
        END IF;

        FOR changed IN SELECT value FROM jsonb_array_elements(coalesce(entry->'containers', '[]')) LOOP
            UPDATE ows_inventory.containers
            SET occupied = (changed->>'occupied')::integer, revision = next_revision
            WHERE customer_guid = tenant AND character_id = character AND container_id = changed->>'containerId';
            IF NOT FOUND THEN
                RAISE EXCEPTION 'Inventory container missing';
            END IF;
        END LOOP;

        FOR changed IN SELECT value FROM jsonb_array_elements(coalesce(entry->'deletes', '[]')) LOOP
            DELETE FROM ows_inventory.items
            WHERE customer_guid = tenant AND character_id = character AND item_instance_id = (changed#>>'{}')::uuid;
            IF NOT FOUND THEN
                RAISE EXCEPTION 'Inventory item to remove missing';
            END IF;
        END LOOP;

        FOR changed IN SELECT value FROM jsonb_array_elements(coalesce(entry->'upserts', '[]')) LOOP
            IF NOT EXISTS (SELECT 1 FROM ows_inventory.containers
                WHERE customer_guid = tenant AND character_id = character AND container_id = changed->>'containerId'
                  AND (NOT slotted OR (changed->>'slotNumber')::int < capacity)) THEN
                RAISE EXCEPTION 'Invalid inventory placement';
            END IF;

            INSERT INTO ows_inventory.items (customer_guid, character_id, item_instance_id,
                container_id, slotted, slot_number, definition_id, max_stack, quantity, revision, condition, uses_left, reward_id)
            VALUES (tenant, character, (changed->>'itemInstanceId')::uuid,
                changed->>'containerId', (changed->>'slotted')::boolean, (changed->>'slotNumber')::int,
                changed->>'definitionId', (changed->>'maxStack')::int, (changed->>'quantity')::int,
                next_revision, (changed->>'condition')::int, (changed->>'usesLeft')::int, (changed->>'rewardId')::uuid)
            ON CONFLICT (customer_guid, character_id, item_instance_id) DO UPDATE SET
                container_id = excluded.container_id, slotted = excluded.slotted, slot_number = excluded.slot_number,
                quantity = excluded.quantity, revision = excluded.revision, reward_id = excluded.reward_id
            WHERE items.definition_id = excluded.definition_id AND items.max_stack = excluded.max_stack
              AND items.condition IS NOT DISTINCT FROM excluded.condition
              AND items.uses_left IS NOT DISTINCT FROM excluded.uses_left;
            GET DIAGNOSTICS updated_rows = ROW_COUNT;
            IF updated_rows <> 1 THEN
                RAISE EXCEPTION 'Invalid inventory item mutation';
            END IF;
        END LOOP;

        changed := entry->'loot';
        IF changed IS NOT NULL THEN
            IF (changed->>'claimed')::boolean THEN
                UPDATE ows_inventory.loot SET claimed = true
                WHERE customer_guid = tenant AND character_id = character AND reward_id = (changed->>'rewardId')::uuid
                  AND item_instance_id = (changed->>'itemInstanceId')::uuid AND NOT claimed;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Invalid inventory loot claim';
                END IF;
            ELSE
                INSERT INTO ows_inventory.loot (customer_guid, character_id, reward_id, item_instance_id)
                VALUES (tenant, character, (changed->>'rewardId')::uuid, (changed->>'itemInstanceId')::uuid);
            END IF;
        END IF;

        INSERT INTO ows_inventory.receipts (customer_guid, character_id, command_id, issued_at, revision, payload)
        VALUES (tenant, character, (entry->>'commandId')::uuid, (entry->>'issuedAt')::bigint, next_revision, entry);
        checkpoint.sequence := checkpoint.sequence + 1;
        checkpoint.event_id := (entry->>'eventId')::uuid;
    END LOOP;

    UPDATE ows_inventory.buckets SET sequence = checkpoint.sequence, event_id = checkpoint.event_id
    WHERE bucket = p_bucket;
    RETURN true;
END
$$;
COMMIT;
