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
    public class ConsoleSettingsRepository : IConsoleSettingsRepository
    {
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(10);
        private static IReadOnlyDictionary<string, string> _cached;
        private static DateTime _cachedAt = DateTime.MinValue;

        private readonly IOptions<StorageOptions> _storageOptions;

        public ConsoleSettingsRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task<IReadOnlyDictionary<string, string>> GetAll()
        {
            IReadOnlyDictionary<string, string> cached = _cached;
            if (cached != null && DateTime.UtcNow - _cachedAt < CacheLifetime)
            {
                return cached;
            }

            IReadOnlyDictionary<string, string> fresh = (await GetAllRows())
                .ToDictionary(row => row.SettingKey, row => row.SettingValue, StringComparer.OrdinalIgnoreCase);
            _cached = fresh;
            _cachedAt = DateTime.UtcNow;
            return fresh;
        }

        public async Task<IEnumerable<ConsoleSetting>> GetAllRows()
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<ConsoleSetting>(
                "SELECT SettingKey, SettingValue, UpdatedAt, UpdatedBy FROM ConsoleSettings ORDER BY SettingKey");
        }

        public async Task Set(string key, string value, string updatedBy)
        {
            await using DbConnection connection = CreateConnection();
            if (string.IsNullOrEmpty(value))
            {
                await connection.ExecuteAsync("DELETE FROM ConsoleSettings WHERE SettingKey = @Key", new { Key = key });
            }
            else
            {
                await connection.ExecuteAsync(@"INSERT INTO ConsoleSettings (SettingKey, SettingValue, UpdatedAt, UpdatedBy)
                    VALUES (@Key, @Value, NOW(), @UpdatedBy)
                    ON CONFLICT (SettingKey) DO UPDATE SET SettingValue = EXCLUDED.SettingValue, UpdatedAt = NOW(), UpdatedBy = EXCLUDED.UpdatedBy",
                    new { Key = key, Value = value, UpdatedBy = updatedBy?.Length > 100 ? updatedBy.Substring(0, 100) : updatedBy });
            }
            _cached = null;
        }
    }
}
