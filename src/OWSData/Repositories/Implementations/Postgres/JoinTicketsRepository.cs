using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class JoinTicketsRepository : IJoinTicketsRepository
    {
        // The zone spin-up wait happens before the ticket is issued, so this covers only the client's map load.
        private const int TicketLifetimeSeconds = 120;

        private const string DeleteExpiredSql = @"DELETE FROM JoinTickets WHERE ExpiresAt < NOW() - INTERVAL '1 day'";

        private const string InsertSql = @"INSERT INTO JoinTickets
                (TicketGUID, CustomerGUID, UserSessionGUID, UserGUID, CharacterID, CharacterName, MapInstanceID,
                 StartMode, X, Y, Z, RX, RY, RZ, ExpiresAt)
                VALUES (@TicketGUID, @CustomerGUID, @UserSessionGUID, @UserGUID, @CharacterID, @CharacterName, @MapInstanceID,
                 @StartMode, @X, @Y, @Z, @RX, @RY, @RZ, NOW() + make_interval(secs => @LifetimeSeconds))";

        // Redeems once: a used, expired or other-zone ticket matches no row. StoredPosition reads the character's
        // position now, not when the ticket was issued.
        private const string RedeemSql = @"WITH T AS (
                    UPDATE JoinTickets SET RedeemedAt = NOW()
                    WHERE TicketGUID = @TicketGUID AND CustomerGUID = @CustomerGUID AND MapInstanceID = @MapInstanceID
                      AND RedeemedAt IS NULL AND ExpiresAt > NOW()
                    RETURNING CharacterID, CharacterName, UserSessionGUID, UserGUID, StartMode, X, Y, Z, RX, RY, RZ)
                SELECT T.CharacterName, T.UserSessionGUID, T.UserGUID, T.StartMode,
                       CASE WHEN T.StartMode = 0 THEN C.X ELSE T.X END AS X,
                       CASE WHEN T.StartMode = 0 THEN C.Y ELSE T.Y END AS Y,
                       CASE WHEN T.StartMode = 0 THEN C.Z ELSE T.Z END AS Z,
                       CASE WHEN T.StartMode = 0 THEN C.RX ELSE T.RX END AS RX,
                       CASE WHEN T.StartMode = 0 THEN C.RY ELSE T.RY END AS RY,
                       CASE WHEN T.StartMode = 0 THEN C.RZ ELSE T.RZ END AS RZ
                FROM T
                JOIN Characters C ON C.CustomerGUID = @CustomerGUID AND C.CharacterID = T.CharacterID";

        private sealed class RedeemedRow
        {
            public string CharacterName { get; set; }
            public Guid UserSessionGUID { get; set; }
            public Guid UserGUID { get; set; }
            public short StartMode { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double RX { get; set; }
            public double RY { get; set; }
            public double RZ { get; set; }
        }

        private readonly IOptions<StorageOptions> _storageOptions;

        public JoinTicketsRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task<Guid> IssueTicket(Guid customerGUID, Guid userSessionGUID, Guid userGUID, int characterID, string characterName,
            int mapInstanceID, JoinTicketStart start)
        {
            Guid ticketGUID = Guid.NewGuid();
            await using DbConnection connection = CreateConnection();
            await connection.ExecuteAsync(DeleteExpiredSql);
            await connection.ExecuteAsync(InsertSql, new
            {
                TicketGUID = ticketGUID,
                CustomerGUID = customerGUID,
                UserSessionGUID = userSessionGUID,
                UserGUID = userGUID,
                CharacterID = characterID,
                CharacterName = characterName,
                MapInstanceID = mapInstanceID,
                StartMode = (short)start.Mode,
                start.X,
                start.Y,
                start.Z,
                start.RX,
                start.RY,
                start.RZ,
                LifetimeSeconds = TicketLifetimeSeconds
            });
            return ticketGUID;
        }

        public async Task<RedeemJoinTicketResult> RedeemTicket(Guid customerGUID, Guid ticketGUID, int mapInstanceID)
        {
            await using DbConnection connection = CreateConnection();
            RedeemedRow row = await connection.QuerySingleOrDefaultAsync<RedeemedRow>(RedeemSql, new
            {
                TicketGUID = ticketGUID,
                CustomerGUID = customerGUID,
                MapInstanceID = mapInstanceID
            });
            if (row == null)
            {
                return new RedeemJoinTicketResult { Success = false, ErrorMessage = "ticket_refused" };
            }
            return new RedeemJoinTicketResult
            {
                Success = true,
                ErrorMessage = "",
                CharacterName = row.CharacterName,
                UserSessionGUID = row.UserSessionGUID,
                UserGUID = row.UserGUID,
                UseMapDefaultStart = row.StartMode == (short)JoinTicketStartMode.MapDefault,
                X = row.X,
                Y = row.Y,
                Z = row.Z,
                RX = row.RX,
                RY = row.RY,
                RZ = row.RZ
            };
        }
    }
}
