using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OWSData.Models.Tickets;
using OWSData.Repositories.Interfaces;
using StackExchange.Redis;

namespace OWSData.Repositories.Implementations.ValKey
{
    public sealed class TicketRepository : ITicketRepository, IDisposable
    {
        private const string KeyPrefix = "ows:connection-ticket:";

        // The read, ServerId check and claim update must happen as one operation.
        // Keep the original TTL when an authenticated UDP retry reclaims the ticket.
        private const string RedeemScript = @"
local payload = redis.call('GET', KEYS[1])
if not payload then return nil end
local record = cjson.decode(payload)
if record.ServerId ~= tonumber(ARGV[1]) then return nil end
if record.RedeemedBy and record.RedeemedBy ~= cjson.null then
    if record.RedeemedBy ~= ARGV[2] then return nil end
else
    record.RedeemedBy = ARGV[2]
    redis.call('SET', KEYS[1], cjson.encode(record), 'KEEPTTL')
end
return cjson.encode(record)";

        private readonly Lazy<ConnectionMultiplexer> _connection;

        public TicketRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "OWSTicketConfig:ConnectionString is required.", nameof(connectionString));
            }

            _connection = new Lazy<ConnectionMultiplexer>(
                () => ConnectionMultiplexer.Connect(connectionString));
        }

        private IDatabase Database => _connection.Value.GetDatabase();

        public async Task<bool> AddTicketAsync(ConnectionTicket ticket, TimeSpan timeToLive)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.Ticket)
                || string.IsNullOrWhiteSpace(ticket.SessionSecretBase64)
                || ticket.CustomerGuid == Guid.Empty || ticket.UserGuid == Guid.Empty
                || ticket.UserSessionGuid == Guid.Empty || ticket.CharacterId <= 0
                || ticket.ServerId <= 0 || ticket.RedeemedBy != null
                || timeToLive <= TimeSpan.Zero)
            {
                throw new ArgumentException("A complete ticket and positive TTL are required.");
            }

            var value = JsonSerializer.Serialize(ticket);
            return await Database.StringSetAsync(
                MakeKey(ticket.Ticket), value, timeToLive, When.NotExists);
        }

        public async Task<ConnectionTicket> RedeemTicketAsync(
            string ticket, int serverId, string claimantId)
        {
            if (string.IsNullOrWhiteSpace(ticket) || serverId <= 0
                || string.IsNullOrWhiteSpace(claimantId))
            {
                return null;
            }

            var result = await Database.ScriptEvaluateAsync(
                RedeemScript,
                new RedisKey[] { MakeKey(ticket) },
                new RedisValue[] { serverId, claimantId });
            if (result.IsNull)
            {
                return null;
            }

            var redeemed = JsonSerializer.Deserialize<ConnectionTicket>((string)result);
            return redeemed?.ExpiresAtUtc > DateTimeOffset.UtcNow ? redeemed : null;
        }

        public async Task<ConnectionTicket> GetTicketAsync(string ticket, int serverId)
        {
            if (string.IsNullOrWhiteSpace(ticket) || serverId <= 0)
            {
                return null;
            }

            var value = await Database.StringGetAsync(MakeKey(ticket));
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            var record = JsonSerializer.Deserialize<ConnectionTicket>(value);
            return record?.ServerId == serverId
                && record.ExpiresAtUtc > DateTimeOffset.UtcNow
                ? record : null;
        }

        private static string MakeKey(string ticket)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ticket));
            return KeyPrefix + Convert.ToHexString(hash);
        }

        public void Dispose()
        {
            if (_connection.IsValueCreated)
            {
                _connection.Value.Dispose();
            }
        }
    }
}
