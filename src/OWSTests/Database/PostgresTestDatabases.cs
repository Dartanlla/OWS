using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

namespace OWSTests.Database
{
    /// <summary>The two ways a database gets its schema.</summary>
    public enum SchemaSource
    {
        /// <summary>src/.docker/postgres/setup.sql alone, as a fresh container seeds it.</summary>
        SetupSql,

        /// <summary>setup.sql followed by every script in migration-manifest.txt, as OWSDatabaseMigrations applies them.</summary>
        Migrated
    }

    /// <summary>
    /// Builds a throwaway database per <see cref="SchemaSource"/> on the server named by OWS_TEST_POSTGRES (an admin
    /// connection string, for example "Host=localhost;Username=postgres;Password=postgres"), and drops them afterwards.
    /// Without OWS_TEST_POSTGRES the database tests are skipped, except under CI, where they fail instead.
    /// </summary>
    public sealed class PostgresTestDatabases : IAsyncLifetime
    {
        public const string ConnectionStringVariable = "OWS_TEST_POSTGRES";

        public static string AdminConnectionString => Environment.GetEnvironmentVariable(ConnectionStringVariable);

        internal static string SkipReason =>
            string.IsNullOrWhiteSpace(AdminConnectionString)
            && !string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
                ? $"Set {ConnectionStringVariable} to run the Postgres tests."
                : null;

        private readonly Dictionary<SchemaSource, string> _connectionStrings = new();

        public string ConnectionString(SchemaSource source) => _connectionStrings[source];

        public async Task InitializeAsync()
        {
            // The fixture is built even when every test is skipped.
            if (SkipReason != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(AdminConnectionString))
            {
                throw new InvalidOperationException($"{ConnectionStringVariable} is not set.");
            }

            string repoRoot = FindRepoRoot();
            string postgresDir = Path.Combine(repoRoot, "Databases", "Postgres");

            // setup.sql is a psql script: it creates and \connects to openworldserver. The test database already exists.
            string setupSql = string.Join("\n", File.ReadAllLines(Path.Combine(repoRoot, "src", ".docker", "postgres", "setup.sql"))
                .Where(line => !line.StartsWith("CREATE DATABASE openworldserver", StringComparison.Ordinal)
                    && !line.StartsWith("\\connect", StringComparison.Ordinal)));

            List<string> manifestScripts = File.ReadAllLines(Path.Combine(postgresDir, "migration-manifest.txt"))
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#"))
                .ToList();

            foreach (SchemaSource source in Enum.GetValues<SchemaSource>())
            {
                string connectionString = await CreateDatabase(source);
                _connectionStrings[source] = connectionString;

                await Execute(connectionString, "setup.sql", setupSql);

                if (source == SchemaSource.Migrated)
                {
                    foreach (string script in manifestScripts)
                    {
                        await Execute(connectionString, script,
                            await File.ReadAllTextAsync(Path.Combine(postgresDir, script.Replace('/', Path.DirectorySeparatorChar))));
                    }
                }
            }
        }

        public async Task DisposeAsync()
        {
            if (_connectionStrings.Count == 0)
            {
                return;
            }

            NpgsqlConnection.ClearAllPools();

            await using var connection = new NpgsqlConnection(AdminConnectionString);
            await connection.OpenAsync();

            foreach (string connectionString in _connectionStrings.Values)
            {
                string database = new NpgsqlConnectionStringBuilder(connectionString).Database;
                await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
                await drop.ExecuteNonQueryAsync();
            }
        }

        private static async Task<string> CreateDatabase(SchemaSource source)
        {
            // Database names cannot be bound as parameters; this one is built only from a GUID and the enum name.
            string database = $"ows_test_{source.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

            await using (var connection = new NpgsqlConnection(AdminConnectionString))
            {
                await connection.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
                await create.ExecuteNonQueryAsync();
            }

            return new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = database }.ConnectionString;
        }

        private static async Task Execute(string connectionString, string name, string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 300 };

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException ex)
            {
                throw new InvalidOperationException($"Applying {name} failed: {ex.Message}", ex);
            }
        }

        private static string FindRepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Databases", "Postgres", "migration-manifest.txt")))
                {
                    return dir.FullName;
                }
            }

            throw new DirectoryNotFoundException("Could not find Databases/Postgres/migration-manifest.txt above " + AppContext.BaseDirectory);
        }
    }

    /// <summary>A theory that needs <see cref="PostgresTestDatabases"/>; skipped when no test server is configured.</summary>
    public sealed class PostgresTheoryAttribute : TheoryAttribute
    {
        public PostgresTheoryAttribute()
        {
            Skip = PostgresTestDatabases.SkipReason;
        }
    }
}
