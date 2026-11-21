-- Bucket keys share a Cluster hash tag. Build the complete after-image before XADD.
local control, ready, stream, data, index, idle = unpack(KEYS)
local p = cjson.decode(ARGV[1])
local c = p.command
local epoch = redis.call('GET', ready)
if not epoch or redis.call('HGET', control, 'epoch') ~= epoch then
    return {'unavailable', 'sync_unavailable'}
end
if redis.call('HGET', data, 'epoch') ~= epoch then return {'cold'} end
local confirmed = tonumber(redis.call('HGET', control, 'confirmed'))
local clock = redis.call('TIME')
local now = tonumber(clock[1]) * 1000 + math.floor(tonumber(clock[2]) / 1000)

local function number(value) return string.format('%.0f', value) end
local dirty = redis.call('HGET', data, 'dirty')
if dirty then
    local marker = cjson.decode(dirty)
    local entry = redis.call('XRANGE', stream, number(marker.sequence) .. '-0', number(marker.sequence) .. '-0')
    if marker.sequence > confirmed and #entry > 0 then
        local event = cjson.decode(entry[1][2][2])
        if event.eventId == marker.eventId then return {'unavailable', 'projection_recovery'} end
    end
    redis.call('DEL', data, index)
    return {'cold'}
end

local state = cjson.decode(redis.call('HGET', data, 'state'))
local admin = c.operation == 'setAuthority' or c.operation == 'close'
if admin and not p.manage then return {'forbidden', 'manage_required'} end
if not admin and (state.authority ~= p.actor or state.authorityGeneration ~= c.authorityGeneration
    or not c.authorityGeneration) then
    return {'forbidden', 'stale_authority'}
end

local receiptKey = 'receipt:' .. c.commandId
local existing = redis.call('HGET', data, receiptKey)
if existing then
    local event = cjson.decode(existing)
    if event.fingerprint ~= p.fingerprint then return {'conflict', 'command_id_reused'} end
    return {event.sequence <= confirmed and 'committed' or 'pending', existing, epoch}
end
if state.closed then return {'gone', 'inventory_closed'} end
if p.issuedAt < now - p.retryMilliseconds then return {'gone', 'command_expired'} end
if p.issuedAt > now + 5000 then return {'invalid', 'command_in_future'} end

local tail = redis.call('XREVRANGE', stream, '+', '-', 'COUNT', 1)
local last = #tail == 0 and 0 or tonumber(string.match(tail[1][1], '^%d+'))
if last < confirmed or last - confirmed >= p.maxPending then
    return {'unavailable', 'sync_backlog'}
end
local sequence = last + 1
local revision = state.revision + 1
if sequence >= 10000000000000 or revision >= 10000000000000 then
    return {'unavailable', 'inventory_sequence_exhausted'}
end

local containers, before, after = {}, {}, {}
local function container(id)
    if not id then return nil end
    if not containers[id] then
        local raw = redis.call('HGET', data, 'container:' .. id)
        if raw then containers[id] = cjson.decode(raw) end
    end
    return containers[id]
end
local function item(id)
    if not id then return nil end
    local raw = redis.call('HGET', data, 'item:' .. id)
    return raw and cjson.decode(raw) or nil
end
local function atSlot(box, slot)
    local id = redis.call('HGET', data, 'slot:' .. box.containerId .. ':' .. number(slot))
    return id and item(id) or nil
end
local function fits(box, slot)
    if not box then return false end
    if box.slotted then return slot ~= nil and slot >= 0 and slot < box.capacity end
    return slot == nil
end
local function copy(original)
    local result = {}
    for k, value in pairs(original) do result[k] = value end
    return result
end
local function placement(original, box, slot)
    local moved = copy(original)
    moved.containerId, moved.slotted, moved.slotNumber = box.containerId, box.slotted, slot
    return moved
end
local function change(old, nextItem)
    local id = old and old.itemInstanceId or nextItem.itemInstanceId
    before[id] = old
    after[id] = nextItem or false
end
local function stackPrefix(value)
    return 's:' .. value.containerId .. ':' .. value.definitionId .. ':'
        .. tostring(value.condition or '-') .. ':' .. tostring(value.usesLeft or '-') .. ':'
end
local function pageEntry(value)
    return 'p:' .. value.containerId .. ':' .. string.format('%07d', value.slotNumber or 0) .. ':' .. value.itemInstanceId
end
local function stackable(value)
    return not value.slotted and value.containerId ~= '_loot' and value.quantity < value.maxStack
end
local function compatible(a, b)
    return a.definitionId == b.definitionId and a.condition == b.condition and a.usesLeft == b.usesLeft
end
local function atRevision(value, expected) return value and value.revision == expected end

local loot = nil
if c.operation == 'setAuthority' then
    if state.authorityGeneration ~= c.authorityGeneration then return {'conflict', 'stale_authority'} end
    state.authority, state.authorityGeneration = c.newAuthority, c.newAuthorityGeneration
elseif c.operation == 'close' then
    if state.authorityGeneration ~= c.authorityGeneration then return {'conflict', 'stale_authority'} end
    state.closed = true
elseif c.operation == 'grant' or c.operation == 'stageLoot' then
    local isLoot = c.operation == 'stageLoot'
    local box = container(isLoot and '_loot' or c.containerId)
    if not fits(box, c.slotNumber) or box.hidden ~= isLoot then return {'invalid', 'invalid_destination'} end
    if not p.maxStack or c.quantity > p.maxStack then return {'invalid', 'invalid_grant_quantity_or_definition'} end
    local granted = {
        itemInstanceId = p.itemId, containerId = box.containerId, slotted = box.slotted, slotNumber = c.slotNumber,
        definitionId = c.definitionId, quantity = c.quantity, maxStack = p.maxStack,
        condition = c.condition, usesLeft = c.usesLeft, rewardId = c.rewardId
    }
    local target
    if isLoot then
        if p.rewardBasis ~= redis.call('HGET', data, 'state') then return {'retry'} end
        if p.rewardExists or redis.call('HEXISTS', data, 'reward:' .. c.rewardId) == 1 then
            return {'conflict', 'reward_already_staged'}
        end
        loot = {rewardId = c.rewardId, itemInstanceId = p.itemId, claimed = false}
    elseif box.slotted then
        target = atSlot(box, c.slotNumber)
        if target and (not compatible(target, granted) or target.quantity + c.quantity > target.maxStack) then
            return {'conflict', 'slot_occupied'}
        end
    elseif c.quantity < p.maxStack then
        local prefix = stackPrefix(granted)
        -- Key includes remaining capacity, so suitable stacks are a bounded range lookup.
        local minimum = prefix .. string.format('%07d', c.quantity) .. ':'
        local candidates = redis.call('ZRANGEBYLEX', index, '[' .. minimum, '[' .. prefix .. '\255', 'LIMIT', 0, 1)
        if #candidates > 0 then target = item(string.sub(candidates[1], -36)) end
    end
    if target then
        local merged = copy(target)
        merged.quantity = target.quantity + c.quantity
        change(target, merged)
    else
        change(nil, granted)
    end
elseif c.operation == 'claimLoot' then
    local source = item(c.itemInstanceId)
    local box = container(c.containerId)
    if not source or source.containerId ~= '_loot' or source.rewardId ~= c.rewardId then
        return {'conflict', 'reward_unavailable'}
    end
    local stagedAt = redis.call('HGET', data, 'reward:' .. c.rewardId)
    if stagedAt and tonumber(stagedAt) > confirmed then return {'conflict', 'reward_not_committed'} end
    if not fits(box, c.slotNumber) or box.hidden then return {'invalid', 'invalid_destination'} end
    if box.slotted and atSlot(box, c.slotNumber) then return {'conflict', 'slot_occupied'} end
    local moved = placement(source, box, c.slotNumber)
    moved.rewardId = nil
    change(source, moved)
    loot = {rewardId = c.rewardId, itemInstanceId = source.itemInstanceId, claimed = true}
else
    local source = item(c.itemInstanceId)
    if not atRevision(source, c.expectedItemRevision) or source.containerId == '_loot' then
        return {'conflict', 'stale_item'}
    end
    if c.operation == 'remove' then
        if c.quantity > source.quantity then return {'conflict', 'insufficient_quantity'} end
        local remaining = copy(source)
        remaining.quantity = source.quantity - c.quantity
        change(source, remaining.quantity > 0 and remaining or nil)
    elseif c.operation == 'move' or c.operation == 'split' then
        local box = container(c.containerId)
        if not fits(box, c.slotNumber) or box.hidden then return {'invalid', 'invalid_destination'} end
        if box.slotted and atSlot(box, c.slotNumber) then return {'conflict', 'slot_occupied'} end
        local moved = placement(source, box, c.slotNumber)
        if c.operation == 'split' then
            if c.quantity >= source.quantity then return {'conflict', 'insufficient_quantity'} end
            local remaining = copy(source)
            remaining.quantity = source.quantity - c.quantity
            moved.itemInstanceId, moved.quantity = p.itemId, c.quantity
            change(source, remaining)
            change(nil, moved)
        else
            change(source, moved)
        end
    else
        local target = item(c.targetItemInstanceId)
        if not atRevision(target, c.expectedTargetRevision) or target.containerId == '_loot'
            or target.itemInstanceId == source.itemInstanceId then
            return {'conflict', 'stale_target'}
        end
        if c.operation == 'merge' then
            if not compatible(source, target) or c.quantity > source.quantity
                or target.quantity + c.quantity > target.maxStack then
                return {'conflict', 'incompatible_or_full_stack'}
            end
            local remaining, merged = copy(source), copy(target)
            remaining.quantity, merged.quantity = source.quantity - c.quantity, target.quantity + c.quantity
            change(source, remaining.quantity > 0 and remaining or nil)
            change(target, merged)
        elseif c.operation == 'swap' then
            if not source.slotted or not target.slotted then return {'invalid', 'swap_requires_slots'} end
            change(source, placement(source, container(target.containerId), target.slotNumber))
            change(target, placement(target, container(source.containerId), source.slotNumber))
        else
            return {'invalid', 'invalid_operation'}
        end
    end
end

local affected = {}
for _, value in pairs(before) do
    local box = container(value.containerId)
    box.occupied = box.occupied - 1
    affected[box.containerId] = box
end
for _, value in pairs(after) do
    if value then
        local box = container(value.containerId)
        box.occupied = box.occupied + 1
        affected[box.containerId] = box
    end
end

local summaries, upserts, deletes = {}, {}, {}
for _, box in pairs(affected) do
    if box.capacity and box.occupied > box.capacity then return {'conflict', 'container_full'} end
    box.revision = revision
    table.insert(summaries, box)
end
for id, value in pairs(after) do
    if value then
        value.revision = revision
        table.insert(upserts, value)
    else
        table.insert(deletes, id)
    end
end
local previousRevision = state.revision
state.revision = revision
local event = cjson.encode({
    owner = p.owner, commandId = c.commandId, fingerprint = p.fingerprint,
    issuedAt = p.issuedAt, previousRevision = previousRevision, revision = revision,
    state = state, eventId = p.eventId, schemaVersion = 1, sequence = sequence,
    upserts = #upserts > 0 and upserts or nil, deletes = #deletes > 0 and deletes or nil,
    containers = #summaries > 0 and summaries or nil, loot = loot
})
redis.call('HSET', data, 'dirty', cjson.encode({sequence = sequence, eventId = p.eventId}))
redis.call('XADD', stream, number(sequence) .. '-0', 'event', event)

local function stackEntry(value)
    return stackPrefix(value) .. string.format('%07d', value.maxStack - value.quantity) .. ':' .. value.itemInstanceId
end
for id, value in pairs(before) do
    redis.call('ZREM', index, pageEntry(value))
    if stackable(value) then redis.call('ZREM', index, stackEntry(value)) end
    if value.slotted then redis.call('HDEL', data, 'slot:' .. value.containerId .. ':' .. number(value.slotNumber)) end
end
for id, value in pairs(after) do
    if value then
        redis.call('HSET', data, 'item:' .. id, cjson.encode(value))
        redis.call('ZADD', index, 0, pageEntry(value))
        if stackable(value) then redis.call('ZADD', index, 0, stackEntry(value)) end
        if value.slotted then
            redis.call('HSET', data, 'slot:' .. value.containerId .. ':' .. number(value.slotNumber), id)
        end
    else
        redis.call('HDEL', data, 'item:' .. id)
    end
end
for id, value in pairs(affected) do
    redis.call('HSET', data, 'container:' .. id, cjson.encode(value))
end
if loot then
    if loot.claimed then
        redis.call('HDEL', data, 'reward:' .. loot.rewardId)
    else
        redis.call('HSET', data, 'reward:' .. loot.rewardId, number(sequence))
    end
end

local expiry = now - p.retryMilliseconds
for _, expired in ipairs(redis.call('ZRANGEBYLEX', index, '[r:', '(r:' .. string.format('%013d', expiry) .. ':', 'LIMIT', 0, 32)) do
    local key = 'receipt:' .. string.sub(expired, -36)
    local prior = redis.call('HGET', data, key)
    if not prior or cjson.decode(prior).sequence <= confirmed then
        redis.call('HDEL', data, key)
        redis.call('ZREM', index, expired)
    end
end
redis.call('ZADD', index, 0, 'r:' .. string.format('%013d', p.issuedAt) .. ':' .. c.commandId)
redis.call('HSET', data, 'state', cjson.encode(state), 'sequence', number(sequence), receiptKey, event)
redis.call('ZADD', idle, now, p.owner)
redis.call('HDEL', data, 'dirty')
return {'pending', event, epoch}
