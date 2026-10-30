using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Implementations.Postgres;
using OWSShared.Options;
using Xunit;

namespace OWSTests.Database
{
    /// <summary>
    /// Passwords are hashed with bcrypt, and a legacy md5-crypt ($1$) hash is upgraded to bcrypt by the first
    /// successful password login. Runs against both the fresh-install schema and the migrated one, so setup.sql and
    /// BcryptPasswordHashes_2026-10-30.sql cannot drift apart.
    /// </summary>
    public class PasswordHashingTests : IClassFixture<PostgresTestDatabases>
    {
        private readonly PostgresTestDatabases _databases;

        public PasswordHashingTests(PostgresTestDatabases databases)
        {
            _databases = databases;
        }

        [PostgresTheory]
        [InlineData(SchemaSource.SetupSql)]
        [InlineData(SchemaSource.Migrated)]
        public async Task RegisterUser_StoresBcryptHash(SchemaSource source)
        {
            UsersRepository users = Repository(source);
            Guid customerGUID = Guid.NewGuid();

            SuccessAndErrorMessage registered = await users.RegisterUser(customerGUID, "new@example.com", "pw", "New", "User");

            Assert.True(registered.Success, registered.ErrorMessage);
            Assert.StartsWith("$2", await PasswordHash(source, customerGUID, "new@example.com"));
            Assert.True((await users.LoginAndCreateSession(customerGUID, "new@example.com", "pw")).Authenticated);
        }

        [PostgresTheory]
        [InlineData(SchemaSource.SetupSql)]
        [InlineData(SchemaSource.Migrated)]
        public async Task Login_UpgradesLegacyMd5Hash(SchemaSource source)
        {
            UsersRepository users = Repository(source);
            Guid customerGUID = Guid.NewGuid();
            await SeedMd5User(source, customerGUID, "legacy@example.com", "oldpw");

            PlayerLoginAndCreateSession login = await users.LoginAndCreateSession(customerGUID, "legacy@example.com", "oldpw");

            Assert.True(login.Authenticated, login.ErrorMessage);
            Assert.StartsWith("$2", await PasswordHash(source, customerGUID, "legacy@example.com"));
            Assert.True((await users.LoginAndCreateSession(customerGUID, "legacy@example.com", "oldpw")).Authenticated);
            Assert.False((await users.LoginAndCreateSession(customerGUID, "legacy@example.com", "wrong")).Authenticated);
        }

        [PostgresTheory]
        [InlineData(SchemaSource.SetupSql)]
        [InlineData(SchemaSource.Migrated)]
        public async Task Login_WrongPasswordLeavesMd5HashUntouched(SchemaSource source)
        {
            UsersRepository users = Repository(source);
            Guid customerGUID = Guid.NewGuid();
            string seeded = await SeedMd5User(source, customerGUID, "legacy@example.com", "oldpw");

            PlayerLoginAndCreateSession login = await users.LoginAndCreateSession(customerGUID, "legacy@example.com", "wrong");

            Assert.False(login.Authenticated);
            Assert.Equal(seeded, await PasswordHash(source, customerGUID, "legacy@example.com"));
        }

        [PostgresTheory]
        [InlineData(SchemaSource.SetupSql)]
        [InlineData(SchemaSource.Migrated)]
        public async Task Login_WithoutPasswordCheckLeavesMd5HashUntouched(SchemaSource source)
        {
            UsersRepository users = Repository(source);
            Guid customerGUID = Guid.NewGuid();
            string seeded = await SeedMd5User(source, customerGUID, "legacy@example.com", "oldpw");

            PlayerLoginAndCreateSession login = await users.LoginAndCreateSession(customerGUID, "legacy@example.com", "wrong", dontCheckPassword: true);

            Assert.True(login.Authenticated);
            Assert.Equal(seeded, await PasswordHash(source, customerGUID, "legacy@example.com"));
        }

        [PostgresTheory]
        [InlineData(SchemaSource.SetupSql)]
        [InlineData(SchemaSource.Migrated)]
        public async Task RegisterUser_RejectsPasswordsOver72Bytes(SchemaSource source)
        {
            UsersRepository users = Repository(source);
            Guid customerGUID = Guid.NewGuid();

            Assert.True((await users.RegisterUser(customerGUID, "ascii72@example.com", new string('a', 72), "A", "B")).Success);
            Assert.False((await users.RegisterUser(customerGUID, "ascii73@example.com", new string('a', 73), "A", "B")).Success);
            // 37 two-byte characters: 37 chars, 74 bytes.
            Assert.False((await users.RegisterUser(customerGUID, "utf8@example.com", new string('é', 37), "A", "B")).Success);
            Assert.Null(await PasswordHash(source, customerGUID, "ascii73@example.com"));

            // AddUser enforces the cap itself for callers that skip the repository.
            await using var connection = new NpgsqlConnection(_databases.ConnectionString(source));
            await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
                "select * from AddUser(@CustomerGUID, 'A', 'B', 'direct@example.com', @Password, 'Player')",
                new { CustomerGUID = customerGUID, Password = new string('a', 73) }));
        }

        [PostgresTheory]
        [InlineData(SchemaSource.SetupSql)]
        [InlineData(SchemaSource.Migrated)]
        public async Task SteamLogin_PlaceholderPasswordIsBcrypt(SchemaSource source)
        {
            UsersRepository users = Repository(source);
            Guid customerGUID = Guid.NewGuid();

            Assert.True((await users.SteamLoginAndCreateSession(customerGUID, "76561198000000001", "Persona")).Authenticated);

            await using var connection = new NpgsqlConnection(_databases.ConnectionString(source));
            string hash = await connection.QuerySingleAsync<string>(
                "SELECT PasswordHash FROM Users WHERE CustomerGUID = @CustomerGUID AND SteamId = '76561198000000001'",
                new { CustomerGUID = customerGUID });
            Assert.StartsWith("$2", hash);
        }

        private UsersRepository Repository(SchemaSource source) =>
            new(Microsoft.Extensions.Options.Options.Create(new StorageOptions { OWSDBBackend = "postgres", OWSDBConnectionString = _databases.ConnectionString(source) }));

        private async Task<string> SeedMd5User(SchemaSource source, Guid customerGUID, string email, string password)
        {
            await using var connection = new NpgsqlConnection(_databases.ConnectionString(source));
            string hash = await connection.QuerySingleAsync<string>(
                @"INSERT INTO Users (UserGUID, CustomerGUID, FirstName, LastName, Email, PasswordHash, CreateDate, LastAccess, Role)
                  VALUES (gen_random_uuid(), @CustomerGUID, 'Legacy', 'User', @Email, crypt(@Password, gen_salt('md5')), NOW(), NOW(), 'Player')
                  RETURNING PasswordHash",
                new { CustomerGUID = customerGUID, Email = email, Password = password });

            Assert.StartsWith("$1$", hash);
            return hash;
        }

        private async Task<string> PasswordHash(SchemaSource source, Guid customerGUID, string email)
        {
            await using var connection = new NpgsqlConnection(_databases.ConnectionString(source));
            return await connection.QuerySingleOrDefaultAsync<string>(
                "SELECT PasswordHash FROM Users WHERE CustomerGUID = @CustomerGUID AND Email = @Email",
                new { CustomerGUID = customerGUID, Email = email });
        }
    }
}
