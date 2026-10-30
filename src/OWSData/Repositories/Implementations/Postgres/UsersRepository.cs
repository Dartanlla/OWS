using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using OWSData.Models.Tables;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{

    public class UsersRepository : IUsersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public UsersRepository(IOptions<StorageOptions> storageOptions)
        {
            this._storageOptions = storageOptions;
        }

        private readonly AsyncLocal<ScopedDbConnection> _scopedConnection = new AsyncLocal<ScopedDbConnection>();

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        private DbConnection Connection => _scopedConnection.Value ??= new ScopedDbConnection(CreateConnection(), () => _scopedConnection.Value = null);

        public async Task<IEnumerable<GetAllCharacters>> GetAllCharacters(Guid customerGUID, Guid userSessionGUID)
        {
            IEnumerable<GetAllCharacters> outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@UserSessionGUID", userSessionGUID);

                outputObject = await Connection.QueryAsync<GetAllCharacters>(GenericQueries.GetAllCharacters,
                    p,
                    commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<IEnumerable<GetAllSamsaraCharacters>> GetAllSamsaraCharacters(Guid customerGUID, Guid userSessionGUID)
        {
            IEnumerable<GetAllSamsaraCharacters> outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("CustomerGUID", customerGUID);
                p.Add("UserSessionGUID", userSessionGUID);

                outputObject = await Connection.QueryAsync<GetAllSamsaraCharacters>(GenericQueries.GetAllCharacters,
                p,
                commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<CreateCharacter> CreateCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className)
        {
            CreateCharacter outputObject = new CreateCharacter();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@CharacterName", characterName);
                    p.Add("@ClassName", className);

                    outputObject = await Connection.QuerySingleAsync<CreateCharacter>("select * from AddCharacter(@CustomerGUID,@UserSessionGUID,@CharacterName,@ClassName)",
                        p,
                        commandType: CommandType.Text);
                }

                outputObject.Success = String.IsNullOrEmpty(outputObject.ErrorMessage);

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        /// <summary>
        /// Creates a new character using our custom schema.
        /// </summary>
        /// <remarks>
        /// This method duplicates code from <c>CreateCharacter</c> to handle schema changes specific to our project (AddSamsaraCharacter procedure).
        /// I chose to duplicate the code to minimize merge conflicts with ows.
        /// </remarks>
        public async Task<CreateCharacter> CreateSamsaraCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className, string initialPersistentData = null)
        {
            CreateCharacter outputObject = new CreateCharacter();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@CharacterName", characterName);
                    p.Add("@ClassName", className);

                    if (String.IsNullOrWhiteSpace(initialPersistentData))
                    {
                        outputObject = await Connection.QuerySingleAsync<CreateCharacter>("select * from AddSamsaraCharacter(@CustomerGUID,@UserSessionGUID,@CharacterName,@ClassName)",
                            p,
                            commandType: CommandType.Text);
                    }
                    else
                    {
                        p.Add("@InitialPersistentData", initialPersistentData);

                        outputObject = await Connection.QuerySingleAsync<CreateCharacter>("select * from AddSamsaraCharacter(@CustomerGUID,@UserSessionGUID,@CharacterName,@ClassName,@InitialPersistentData)",
                            p,
                            commandType: CommandType.Text);
                    }
                }

                outputObject.Success = String.IsNullOrEmpty(outputObject.ErrorMessage);

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> CreateCharacterUsingDefaultCharacterValues(Guid customerGUID, Guid userGUID, string characterName, string defaultSetName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            using IDbConnection conn = CreateConnection();
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("CustomerGUID", customerGUID);
                parameters.Add("UserGUID", userGUID);
                parameters.Add("CharacterName", characterName);
                parameters.Add("DefaultSetName", defaultSetName);

                int outputCharacterId = await conn.QuerySingleOrDefaultAsync<int>(PostgresQueries.AddCharacterUsingDefaultCharacterValues,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                parameters.Add("CharacterID", outputCharacterId);
                await conn.ExecuteAsync(GenericQueries.AddDefaultCustomCharacterData,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                outputObject = new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return outputObject;
            }

            outputObject = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return outputObject;
        }

        // //_PlayerGroupTypeID 0 returns all group types
        // public async Task<IEnumerable<GetPlayerGroupsCharacterIsIn>> GetPlayerGroupsCharacterIsIn(Guid customerGUID, Guid userSessionGUID, string characterName, int playerGroupTypeID = 0)
        // {
        //     IEnumerable<GetPlayerGroupsCharacterIsIn> OutputObject;

        //     using (Connection)
        //     {
        //         var p = new DynamicParameters();
        //         p.Add("@CustomerGUID", customerGUID);
        //         p.Add("@CharName", characterName);
        //         p.Add("@UserSessionGUID", userSessionGUID);
        //         p.Add("@PlayerGroupTypeID", playerGroupTypeID);

        //         OutputObject = await Connection.QueryAsync<GetPlayerGroupsCharacterIsIn>("select * from GetPlayerGroupsCharacterIsIn(@CustomerGUID,@CharName,@UserSessionGUID,@PlayerGroupTypeID)",
        //             p,
        //             commandType: CommandType.Text);
        //     }

        //     return OutputObject;
        // }

        public async Task<User> GetUser(Guid customerGuid, Guid userGuid)
        {
            User outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGuid);
                p.Add("@UserGUID", userGuid);

                outputObject = await Connection.QuerySingleOrDefaultAsync<User>("select * from GetUser(@CustomerGUID,@UserGUID)",
                    p,
                    commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<IEnumerable<User>> GetUsers(Guid customerGuid)
        {
            IEnumerable<User> outputObject = null;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGuid);

                outputObject = await Connection.QueryAsync<User>(GenericQueries.GetUsers, p);
            }

            return outputObject;
        }

        public async Task<GetUserSession> GetUserSession(Guid customerGUID, Guid userSessionGUID)
        {
            GetUserSession outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@UserSessionGUID", userSessionGUID);

                outputObject = await Connection.QuerySingleOrDefaultAsync<GetUserSession>("select * from GetUserSession(@CustomerGUID,@UserSessionGUID)",
                    p,
                    commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<GetUserSession> GetUserSessionORM(Guid customerGUID, Guid userSessionGUID)
        {
            GetUserSession outputObject;

            using (Connection)
            {
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(PostgresQueries.GetUserSessionSQL, new { @CustomerGUID = customerGUID, @UserSessionGUID = userSessionGUID });
            }

            return outputObject;
        }

        public async Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGUID, Guid userSessionGUID) //id = UserSessionGUID
        {
            GetUserSessionComposite outputObject = new GetUserSessionComposite();
            UserSessions userSession;
            User user;
            Characters character;

            using (IDbConnection userSessionConnection = CreateConnection())
            {
                userSession = await userSessionConnection.QueryFirstOrDefaultAsync<UserSessions>(PostgresQueries.GetUserSessionOnlySQL, new { @CustomerGUID = customerGUID, @UserSessionGUID = userSessionGUID });
            }

            using (IDbConnection userConnection = CreateConnection())
            using (IDbConnection characterConnection = CreateConnection())
            {
                var userTask = userConnection.QueryFirstOrDefaultAsync<User>(PostgresQueries.GetUserSQL, new { @CustomerGUID = customerGUID, @UserGUID = userSession.UserGuid });
                var characterTask = characterConnection.QueryFirstOrDefaultAsync<Characters>(PostgresQueries.GetCharacterByNameSQL, new { @CustomerGUID = customerGUID, @CharacterName = userSession.SelectedCharacterName });

                await Task.WhenAll(userTask, characterTask);

                user = await userTask;
                character = await characterTask;
            }

            outputObject.userSession = userSession;
            outputObject.user = user;
            outputObject.character = character;

            return outputObject;
        }

        public async Task<PlayerLoginAndCreateSession> LoginAndCreateSession(Guid customerGUID, string email, string password, bool dontCheckPassword = false)
        {
            PlayerLoginAndCreateSession outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@Email", email);
                p.Add("@Password", password);
                p.Add("@DontCheckPassword", dontCheckPassword);

                outputObject = await Connection.QuerySingleOrDefaultAsync<PlayerLoginAndCreateSession>($"select * from PlayerLoginAndCreateSession(@CustomerGUID,@Email,@Password,@DontCheckPassword)",
                    p,
                    commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<PlayerLoginAndCreateSession> SteamLoginAndCreateSession(Guid customerGUID, string steamId, string personaName)
        {
            PlayerLoginAndCreateSession outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@SteamId", steamId);
                p.Add("@PersonaName", personaName);

                outputObject = await Connection.QuerySingleOrDefaultAsync<PlayerLoginAndCreateSession>($"select * from SteamLoginAndCreateSession(@CustomerGUID,@SteamId,@PersonaName)",
                    p,
                    commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> Logout(Guid customerGuid, Guid userSessionGuid)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserSessionGUID", userSessionGuid);

                    await Connection.ExecuteAsync(GenericQueries.Logout, p, commandType: CommandType.Text);
                }

                outputObject.Success = true;
                outputObject.ErrorMessage = "";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> UserSessionSetSelectedCharacter(Guid customerGUID, Guid userSessionGUID, string selectedCharacterName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                int updated;
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@SelectedCharacterName", selectedCharacterName);

                    updated = await Connection.ExecuteAsync(PostgresQueries.SetSelectedOwnedCharacterSQL,
                        p,
                        commandType: CommandType.Text);
                }

                outputObject.Success = updated == 1;
                outputObject.ErrorMessage = updated == 1 ? "" : "The character does not belong to this user session.";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        // bcrypt reads only the first 72 bytes of a password. AddUser enforces the same cap; checking here gives a clean error.
        public const int MaxPasswordBytes = 72;

        public async Task<SuccessAndErrorMessage> RegisterUser(Guid customerGUID, string email, string password, string firstName, string lastName, string role = null)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            if (password != null && Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = $"Password must be {MaxPasswordBytes} bytes or fewer.";

                return outputObject;
            }

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@Email", email);
                    p.Add("@Password", password);
                    p.Add("@FirstName", firstName);
                    p.Add("@LastName", lastName);
                    p.Add("@Role", UserRoles.NormalizeOrDefault(role));

                    await Connection.ExecuteAsync("select * from AddUser(@CustomerGUID, @FirstName, @LastName, @Email, @Password, @Role)",
                        p,
                        commandType: CommandType.Text);
                }

                outputObject.Success = true;
                outputObject.ErrorMessage = "";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        public async Task<GetUserSession> GetUserFromEmail(Guid customerGUID, string email)
        {
            GetUserSession outputObject;

            using (Connection)
            {
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(PostgresQueries.GetUserFromEmailSQL, new { @CustomerGUID = customerGUID, @Email = email });
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> RemoveCharacter(Guid customerGUID, Guid userSessionGUID, string characterName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using DbConnection connection = CreateConnection();
                await connection.OpenAsync();
                using IDbTransaction transaction = connection.BeginTransaction();

                async Task DeleteFromOptionalTable(string tableName, string whereClause, object parameters)
                {
                    bool tableExists = await connection.QuerySingleAsync<bool>(
                        "SELECT to_regclass(@TableName) IS NOT NULL",
                        new { TableName = $"public.{tableName.ToLowerInvariant()}" },
                        transaction);

                    if (!tableExists)
                    {
                        return;
                    }

                    await connection.ExecuteAsync($"DELETE FROM {tableName} WHERE {whereClause};", parameters, transaction);
                }

                try
                {
                    int? characterId = await connection.QuerySingleOrDefaultAsync<int?>(
                        @"SELECT C.CharacterID
                        FROM Characters C
                        INNER JOIN UserSessions US ON US.CustomerGUID = C.CustomerGUID AND US.UserGUID = C.UserGUID
                        WHERE C.CustomerGUID = @CustomerGUID
                            AND US.UserSessionGUID = @UserSessionGUID
                            AND C.CharName = @CharacterName",
                        new { CustomerGUID = customerGUID, UserSessionGUID = userSessionGUID, CharacterName = characterName },
                        transaction);

                    if (characterId != null)
                    {
                        var partyMemberships = await connection.QueryAsync(
                            @"SELECT PartyID, PartyLeader
                            FROM PartyMember
                            WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID",
                            new { CustomerGUID = customerGUID, CharacterID = characterId.Value },
                            transaction);

                        foreach (var membership in partyMemberships)
                        {
                            int partyId = membership.partyid;
                            bool wasLeader = membership.partyleader;

                            await connection.ExecuteAsync(
                                @"DELETE FROM PartyMember
                                WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID AND CharacterID = @CharacterID",
                                new { CustomerGUID = customerGUID, PartyID = partyId, CharacterID = characterId.Value },
                                transaction);

                            int remainingCount = await connection.QuerySingleAsync<int>(
                                @"SELECT COUNT(*)
                                FROM PartyMember
                                WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                                new { CustomerGUID = customerGUID, PartyID = partyId },
                                transaction);

                            if (remainingCount == 0)
                            {
                                await connection.ExecuteAsync(
                                    @"DELETE FROM Party
                                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                                    new { CustomerGUID = customerGUID, PartyID = partyId },
                                    transaction);
                            }
                            else if (wasLeader)
                            {
                                int nextLeaderId = await connection.QuerySingleAsync<int>(
                                    @"SELECT CharacterID
                                    FROM PartyMember
                                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID
                                    ORDER BY CharacterID
                                    LIMIT 1",
                                    new { CustomerGUID = customerGUID, PartyID = partyId },
                                    transaction);

                                await connection.ExecuteAsync(
                                    @"UPDATE PartyMember
                                    SET PartyLeader = CharacterID = @NextLeaderID,
                                        PartyRole = CASE WHEN CharacterID = @NextLeaderID THEN 2 ELSE 0 END
                                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                                    new { CustomerGUID = customerGUID, PartyID = partyId, NextLeaderID = nextLeaderId },
                                    transaction);
                            }
                        }

                        await DeleteFromOptionalTable(
                            "CharInventoryCurrency",
                            @"CharInventoryID IN (
                                SELECT CharInventoryID FROM CharInventory
                                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID
                            )",
                            new { CustomerGUID = customerGUID, CharacterID = characterId.Value });

                        await DeleteFromOptionalTable(
                            "CharEquipmentItems",
                            "CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID",
                            new { CustomerGUID = customerGUID, CharacterID = characterId.Value });

                        await DeleteFromOptionalTable(
                            "CharacterPrimaryProfession",
                            "CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID",
                            new { CustomerGUID = customerGUID, CharacterID = characterId.Value });

                        await DeleteFromOptionalTable(
                            "CharacterSecondaryProfession",
                            "CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID",
                            new { CustomerGUID = customerGUID, CharacterID = characterId.Value });

                        await connection.ExecuteAsync(
                            @"DELETE FROM CharAbilityBarAbilities
                            WHERE CustomerGUID = @CustomerGUID
                                AND CharAbilityBarID IN (
                                    SELECT CharAbilityBarID FROM CharAbilityBars
                                    WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID
                                );
                            DELETE FROM CharAbilityBars WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM CharInventoryItems
                            WHERE CustomerGUID = @CustomerGUID
                                AND CharInventoryID IN (
                                    SELECT CharInventoryID FROM CharInventory
                                    WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID
                                );
                            DELETE FROM CharInventory WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM CharQuests WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM CharStats WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM CharOnMapInstance WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM CustomCharacterData WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM CharHasAbilities WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM CharAbilities WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM ChatGroupUsers WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM PlayerGroupCharacters WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;
                            DELETE FROM PlayerGroupMember WHERE CharacterID = @CharacterID;
                            DELETE FROM Characters WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID;",
                            new { CustomerGUID = customerGUID, CharacterID = characterId.Value },
                            transaction);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                outputObject.Success = true;
                outputObject.ErrorMessage = "";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> UpdateUser(Guid customerGuid, Guid userGuid, string firstName, string lastName, string email)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserGUID", userGuid);
                    p.Add("@FirstName", firstName);
                    p.Add("@LastName", lastName);
                    p.Add("@Email", email);

                    await Connection.ExecuteAsync(GenericQueries.UpdateUser,
                        p,
                        commandType: CommandType.Text);
                }

                outputObject.Success = true;
                outputObject.ErrorMessage = "";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        //Region routing. The caller is responsible for validating the region against GameServersConfig;
        //Users.PreferredServerRegion is VARCHAR(32) and the routing queries compare it with exact equality.
        public async Task<SuccessAndErrorMessage> SetPreferredServerRegion(Guid customerGuid, Guid userGuid, string region)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserGUID", userGuid);
                    p.Add("@PreferredServerRegion", region);

                    int rowsAffected = await Connection.ExecuteAsync(GenericQueries.UpdatePreferredServerRegion,
                        p,
                        commandType: CommandType.Text);

                    if (rowsAffected < 1)
                    {
                        outputObject.Success = false;
                        outputObject.ErrorMessage = "User not found.";

                        return outputObject;
                    }
                }

                outputObject.Success = true;
                outputObject.ErrorMessage = "";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> UpdateUserRole(Guid customerGuid, Guid userGuid, string role)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            if (!UserRoles.TryNormalize(role, out string normalizedRole))
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = $"Unknown role: {role}";

                return outputObject;
            }

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserGUID", userGuid);
                    p.Add("@Role", normalizedRole);

                    int rowsAffected = await Connection.ExecuteAsync(GenericQueries.UpdateUserRole,
                        p,
                        commandType: CommandType.Text);

                    if (rowsAffected < 1)
                    {
                        outputObject.Success = false;
                        outputObject.ErrorMessage = "User not found.";

                        return outputObject;
                    }
                }

                outputObject.Success = true;
                outputObject.ErrorMessage = "";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }
        public async Task<IEnumerable<AdminUserSummary>> SearchUsers(Guid customerGuid, string searchText)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGuid);
                // Wildcards travel as data in a bound parameter, never concatenated into SQL.
                p.Add("@SearchPattern", "%" + (searchText ?? string.Empty).Trim().ToLowerInvariant() + "%");

                return await Connection.QueryAsync<AdminUserSummary>(GenericQueries.SearchUsers, p);
            }
        }

        public async Task<SuccessAndErrorMessage> SetNetworkTestFlagForUser(Guid customerGuid, Guid userGuid, bool isInternalNetworkTestUser)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserGUID", userGuid);
                    p.Add("@IsInternalNetworkTestUser", isInternalNetworkTestUser);

                    int rowsAffected = await Connection.ExecuteAsync(GenericQueries.UpdateNetworkTestFlagForUser,
                        p,
                        commandType: CommandType.Text);

                    outputObject.Success = rowsAffected > 0;
                    outputObject.ErrorMessage = rowsAffected > 0 ? "" : "User not found.";

                    return outputObject;
                }
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }

    }
}
