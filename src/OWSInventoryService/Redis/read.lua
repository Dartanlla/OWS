local control, ready, data, index, idle = unpack(KEYS)
local p = cjson.decode(ARGV[1])
local epoch = redis.call('GET', ready)
if not epoch or redis.call('HGET', control, 'epoch') ~= epoch
    or redis.call('HGET', data, 'epoch') ~= epoch or redis.call('HEXISTS', data, 'dirty') == 1 then
    return {'cold'}
end
local confirmed = tonumber(redis.call('HGET', control, 'confirmed'))
if tonumber(redis.call('HGET', data, 'sequence')) > confirmed then return {'cold'} end
local state = cjson.decode(redis.call('HGET', data, 'state'))
if not p.manage and (state.authority ~= p.actor or state.authorityGeneration ~= p.generation
    or not p.generation) then return {'forbidden'} end
if state.closed then return {'gone'} end

local clock = redis.call('TIME')
redis.call('ZADD', idle, tonumber(clock[1]) * 1000 + math.floor(tonumber(clock[2]) / 1000), p.owner)
if p.operation == 'containers' then
    local fields = cjson.decode(redis.call('HGET', data, 'containerKeys'))
    local result = {'ok', string.format('%.0f', state.revision)}
    for _, id in ipairs(fields) do
        if id ~= '_loot' then table.insert(result, redis.call('HGET', data, 'container:' .. id)) end
    end
    return result
end

local raw = redis.call('HGET', data, 'container:' .. p.containerId)
if not raw or p.containerId == '_loot' then return {'missing'} end
local box = cjson.decode(raw)
if p.revision and p.revision ~= box.revision then return {'conflict'} end
local prefix = 'p:' .. p.containerId .. ':'
local start = p.after and '(' .. prefix .. p.after or '[' .. prefix
local entries = redis.call('ZRANGEBYLEX', index, start, '[' .. prefix .. '\255', 'LIMIT', 0, p.limit + 1)
local result = {'ok', string.format('%.0f', box.revision), #entries > p.limit and 'more' or 'done'}
for i = 1, math.min(#entries, p.limit) do
    table.insert(result, redis.call('HGET', data, 'item:' .. string.sub(entries[i], -36)))
end
return result
