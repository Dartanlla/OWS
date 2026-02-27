using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
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

        private IDbConnection Connection => new NpgsqlConnection(_storageOptions.Value.OWSDBConnectionString);

        public async Task<IEnumerable<GetAllCharacters>> GetAllCharacters(Guid customerGUID, Guid userSessionGUID)
        {
            IEnumerable<GetAllCharacters> outputObject;

            using (var connection = (NpgsqlConnection)Connection)
            {
                try
                {
                    using var conn = Connection;
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);

                    outputObject = await connection.QueryAsync<GetAllCharacters>(GenericQueries.GetAllCharacters,
                        p,
                        commandType: CommandType.Text);
                }
                catch (Exception ex)
                {
                    return [];
                }
            }

            return outputObject;
        }

        public async Task<CreateCharacter> CreateCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className)
        {
            CreateCharacter outputObject = new CreateCharacter();

            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@CharacterName", characterName);
                    p.Add("@ClassName", className);

                    outputObject = await Connection.QuerySingleAsync<CreateCharacter>(GenericQueries.CreateCharacterSQL,
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

        public async Task<SuccessAndErrorMessage> CreateCharacterUsingDefaultCharacterValues(Guid customerGuid, Guid userGuid, string characterName, 
            string defaultSetName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            using (var connection = (NpgsqlConnection)Connection)
            {
                await connection.OpenAsync();

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("CustomerGUID", customerGuid);
                        parameters.Add("UserGUID", userGuid);
                        parameters.Add("CharacterName", characterName);
                        parameters.Add("DefaultSetName", defaultSetName);

                        int outputCharacterId = await connection.QuerySingleOrDefaultAsync<int>(
                            PostgresQueries.AddCharacterUsingDefaultCharacterValues,
                            parameters,
                            commandType: CommandType.Text);

                        parameters.Add("CharacterID", outputCharacterId);
                        await connection.ExecuteAsync(GenericQueries.AddDefaultCustomCharacterData,
                            parameters,
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
                }
            }

            return new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };
        }

        //_PlayerGroupTypeID 0 returns all group types
        public async Task<IEnumerable<GetPlayerGroupsCharacterIsIn>> GetPlayerGroupsCharacterIsIn(Guid customerGUID, Guid userSessionGUID, 
            string characterName, int playerGroupTypeID = 0)
        {
            IEnumerable<GetPlayerGroupsCharacterIsIn> outputObject;

            try
            { 
                using (var connection = (NpgsqlConnection)Connection)
                {
                    using var conn = Connection;
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@CharName", characterName);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@PlayerGroupTypeID", playerGroupTypeID);

                    outputObject = await connection.QueryAsync<GetPlayerGroupsCharacterIsIn>(GenericQueries.GetPlayerGroupsCharacterIsIn,
                        p,
                        commandType: CommandType.Text);
                }

                return outputObject;
            }
            catch (Exception ex)
            {
                return [];
            }
        }

        public async Task<User> GetUser(Guid customerGuid, Guid userGuid)
        {
            User outputObject;

            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    using var conn = Connection;
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserGUID", userGuid);

                    outputObject = await connection.QuerySingleOrDefaultAsync<User>(GenericQueries.GetUser,
                        p,
                        commandType: CommandType.Text);

                    return outputObject;
                }
            }
            catch (Exception ex)
            {
                return new User();
            }
        }

        public async Task<IEnumerable<User>> GetUsers(Guid customerGuid)
        {
            IEnumerable<User> outputObject = null;

            try
            { 
                using (var connection = (NpgsqlConnection)Connection)
                {
                    using var conn = Connection;
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);

                    outputObject = await connection.QueryAsync<User>(GenericQueries.GetUsers, p);
                }

                return outputObject;
            }
            catch (Exception ex)
            {
                return [];
            }
        }

        public async Task<GetUserSession> GetUserSession(Guid customerGuid, Guid userSessionGuid)
        {
            GetUserSession outputObject;

            try 
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserSessionGUID", userSessionGuid);

                    outputObject = await connection.QuerySingleOrDefaultAsync<GetUserSession>(GenericQueries.GetUserSession,
                        p,
                        commandType: CommandType.Text);

                    return outputObject;
                }
            }
            catch (Exception ex)
            {
                return new GetUserSession();
            }
        }

        public async Task<GetUserSession> GetUserSessionORM(Guid customerGuid, Guid userSessionGuid)
        {
            GetUserSession outputObject;

            try
            { 
                using (var connection = (NpgsqlConnection)Connection)
                {
                    outputObject = await connection.QueryFirstOrDefaultAsync<GetUserSession>(PostgresQueries.GetUserSessionSQL, new { @CustomerGUID = customerGuid, @UserSessionGUID = userSessionGuid });
                }

                return outputObject;
            }
            catch (Exception ex)
            {
                return new GetUserSession();
            }
        }

        public async Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGuid, Guid userSessionGuid) //id = UserSessionGUID
        {
            GetUserSessionComposite outputObject = new GetUserSessionComposite();
            UserSessions userSession;
            User user;
            Characters character;

            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    userSession = await connection.QueryFirstOrDefaultAsync<UserSessions>(PostgresQueries.GetUserSessionOnlySQL, new { @CustomerGUID = customerGuid, @UserSessionGUID = userSessionGuid });
                    var userTask = connection.QueryFirstOrDefaultAsync<User>(PostgresQueries.GetUserSQL, new { @CustomerGUID = customerGuid, @UserGUID = userSession.UserGuid });
                    var characterTask = connection.QueryFirstOrDefaultAsync<Characters>(PostgresQueries.GetCharacterByNameSQL, new { @CustomerGUID = customerGuid, @CharacterName = userSession.SelectedCharacterName });

                    Task.WaitAll(userTask, characterTask);

                    user = userTask.Result;
                    character = characterTask.Result;

                    return new GetUserSessionComposite()
                    {
                        userSession = userSession,
                        user = user,
                        character = character
                    };
                }
            }
            catch (Exception ex)
            {
                return new GetUserSessionComposite();
            }
        }

        public async Task<PlayerLoginAndCreateSession> LoginAndCreateSession(Guid customerGuid, string email, string password, bool dontCheckPassword = false)
        {
            PlayerLoginAndCreateSession outputObject;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGuid);
                p.Add("@Email", email);
                p.Add("@Password", password);
                p.Add("@DontCheckPassword", dontCheckPassword);

                outputObject = await connection.QuerySingleOrDefaultAsync<PlayerLoginAndCreateSession>(GenericQueries.PlayerLoginAndCreateSession,
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
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserSessionGUID", userSessionGuid);

                    await connection.ExecuteAsync(GenericQueries.Logout, p, commandType: CommandType.Text);
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
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@SelectedCharacterName", selectedCharacterName);

                    await connection.ExecuteAsync(GenericQueries.UserSessionSetSelectedCharacter,
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

        public async Task<SuccessAndErrorMessage> RegisterUser(Guid customerGUID, string email, string password, string firstName, string lastName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@Email", email);
                    p.Add("@Password", password);
                    p.Add("@FirstName", firstName);
                    p.Add("@LastName", lastName);
                    p.Add("@Role", "Player");

                    await connection.ExecuteAsync(GenericQueries.AddUser,
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

            using (var connection = (NpgsqlConnection)Connection)
            {
                outputObject = await connection.QueryFirstOrDefaultAsync<GetUserSession>(PostgresQueries.GetUserFromEmailSQL, new { @CustomerGUID = customerGUID, @Email = email });
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> RemoveCharacter(Guid customerGUID, Guid userSessionGUID, string characterName)
        {
            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    await connection.OpenAsync();

                    using (IDbTransaction transaction = connection.BeginTransaction())
                    {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@CharacterName", characterName);

                    await connection.ExecuteAsync(GenericQueries.RemoveCharacter,
                        p,
                        transaction,
                        commandType: CommandType.Text);

                        transaction.Commit();
                    }
                }

                return new SuccessAndErrorMessage()
                {
                    Success = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<SuccessAndErrorMessage> UpdateUser(Guid customerGuid, Guid userGuid, string firstName, string lastName, string email)
        {
            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserGUID", userGuid);
                    p.Add("@FirstName", firstName);
                    p.Add("@LastName", lastName);
                    p.Add("@Email", email);

                    await connection.ExecuteAsync(GenericQueries.UpdateUser,
                        p,
                        commandType: CommandType.Text);
                }

                return new SuccessAndErrorMessage()
                {
                    Success = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception ex)
            {
                return new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
