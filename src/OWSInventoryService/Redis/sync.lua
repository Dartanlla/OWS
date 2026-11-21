local control, ready, stream = unpack(KEYS)
local operation, epoch, generation, sequence, eventId = unpack(ARGV)
local current = redis.call('HGET', control, 'epoch')
local currentGeneration = tonumber(redis.call('HGET', control, 'generation')) or 0
local confirmed = tonumber(redis.call('HGET', control, 'confirmed')) or 0

if currentGeneration > tonumber(generation) or (current == epoch and confirmed > tonumber(sequence)) then
    return 'stale'
end
if operation == 'pause' or operation == 'quarantine' then
    if current == epoch then
        redis.call('DEL', ready)
        if operation == 'quarantine' then redis.call('HSET', control, 'quarantined', '1') end
    end
    return 'paused'
end
if operation == 'install' and currentGeneration < tonumber(generation) then
    redis.call('DEL', ready)
    redis.call('UNLINK', stream)
    redis.call('HSET', control, 'epoch', epoch, 'generation', generation, 'confirmed', '0')
    redis.call('HDEL', control, 'quarantined')
    current = epoch
end
if current ~= epoch then
    redis.call('DEL', ready)
    return 'diverged'
end
if redis.call('HEXISTS', control, 'quarantined') == 1 then return 'quarantined' end
if sequence ~= '0' then
    -- Keep the committed anchor in the stream even after trimming.
    local pos = sequence .. '-0'
    local anchor = redis.call('XRANGE', stream, pos, pos, 'COUNT', 1)
    if #anchor == 0 or cjson.decode(anchor[1][2][2]).eventId ~= eventId then
        redis.call('DEL', ready)
        return 'diverged'
    end
end

if operation == 'verify' then return 'ready' end
redis.call('HSET', control, 'confirmed', sequence)
redis.call('SET', ready, epoch, 'PX', 5000)
if sequence ~= '0' then redis.call('XTRIM', stream, 'MINID', sequence .. '-0') end
return 'ready'
