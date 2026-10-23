using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class ChatLogRepository : IChatLogRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public ChatLogRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task AddBatch(IReadOnlyCollection<ChatLogEntry> entries)
        {
            if (entries == null || entries.Count == 0) return;

            await using DbConnection connection = CreateConnection();
            await connection.ExecuteAsync(@"INSERT INTO ChatLog (CustomerGUID, CreateDate, Channel, SenderName, SenderUserGUID, TargetName, Message, Dropped)
                SELECT * FROM UNNEST(@CustomerGUIDs, @CreateDates, @Channels, @SenderNames, @SenderUserGUIDs, @TargetNames, @Messages, @Dropped)",
                new
                {
                    CustomerGUIDs = entries.Select(e => e.CustomerGUID).ToArray(),
                    CreateDates = entries.Select(e => DateTime.SpecifyKind(e.CreateDate, DateTimeKind.Utc)).ToArray(),
                    Channels = entries.Select(e => e.Channel).ToArray(),
                    SenderNames = entries.Select(e => Truncate(e.SenderName, 50)).ToArray(),
                    SenderUserGUIDs = entries.Select(e => e.SenderUserGUID).ToArray(),
                    TargetNames = entries.Select(e => Truncate(e.TargetName, 50)).ToArray(),
                    Messages = entries.Select(e => e.Message).ToArray(),
                    Dropped = entries.Select(e => e.Dropped).ToArray()
                });
        }

        public async Task<IEnumerable<ChatLogEntry>> Search(Guid customerGUID, string sender, string text, long beforeID, int limit)
        {
            text = (text ?? "").Trim();
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<ChatLogEntry>(@"SELECT ChatLogID, CreateDate, Channel, SenderName, SenderUserGUID, TargetName, Message, Dropped
                FROM ChatLog
                WHERE CustomerGUID = @CustomerGUID
                  AND (@Sender = '' OR LOWER(SenderName) = LOWER(@Sender) OR LOWER(TargetName) = LOWER(@Sender))
                  AND (@Text = '' OR Message ILIKE @Pattern)
                  AND (@BeforeID <= 0 OR ChatLogID < @BeforeID)
                ORDER BY ChatLogID DESC LIMIT @Limit",
                new
                {
                    CustomerGUID = customerGUID,
                    Sender = sender?.Trim() ?? "",
                    Text = text,
                    Pattern = "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%",
                    BeforeID = beforeID,
                    Limit = Math.Clamp(limit, 1, 500)
                });
        }

        private static string Truncate(string value, int maxLength)
        {
            return value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
