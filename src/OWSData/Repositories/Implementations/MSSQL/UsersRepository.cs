﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Npgsql;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.MSSQL
{
    public class UsersRepository : IUsersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public UsersRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(_storageOptions.Value.OWSDBConnectionString);
            }
        }

        public async Task<IEnumerable<GetAllCharacters>> GetAllCharacters(Guid customerGUID, Guid userSessionGUID)
        {
            IEnumerable<GetAllCharacters> outputObject = new List<GetAllCharacters>();

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("CustomerGUID", customerGUID);
                p.Add("UserSessionGUID", userSessionGUID);

                outputObject = await Connection.QueryAsync<GetAllCharacters>(GenericQueries.GetAllCharacters,
                p,
                commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<CreateCharacter> CreateCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className)
        {
            CreateCharacter outputObject = default;

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("CustomerGUID", customerGUID);
                    p.Add("UserSessionGUID", userSessionGUID);
                    p.Add("CharacterName", characterName);
                    p.Add("ClassName", className);
                    p.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);

                    outputObject = await Connection.QuerySingleAsync<CreateCharacter>("AddCharacter",
                    p,
                    commandType: CommandType.StoredProcedure);
                }

                outputObject = outputObject with { Success = string.IsNullOrEmpty(outputObject.ErrorMessage) };

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject = outputObject with
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return outputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> CreateCharacterUsingDefaultCharacterValues(Guid customerGUID, Guid userGUID, string characterName, string defaultSetName)
        {
            SuccessAndErrorMessage outputObject = default;

            IDbConnection conn = Connection;
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("CustomerGUID", customerGUID);
                parameters.Add("UserGUID", userGUID);
                parameters.Add("CharacterName", characterName);
                parameters.Add("DefaultSetName", defaultSetName);

                int outputCharacterId = await Connection.QuerySingleOrDefaultAsync<int>(MSSQLQueries.AddCharacterUsingDefaultCharacterValues,
                    parameters,
                commandType: CommandType.Text);

                parameters.Add("CharacterID", outputCharacterId);
                await Connection.ExecuteAsync(GenericQueries.AddDefaultCustomCharacterData,
                    parameters,
                    commandType: CommandType.Text);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                outputObject = default(SuccessAndErrorMessage) with
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return outputObject;
            }

            outputObject = default(SuccessAndErrorMessage) with
            {
                Success = true,
                ErrorMessage = ""
            };

            return outputObject;
        }

        //_PlayerGroupTypeID 0 returns all group types
        public async Task<IEnumerable<GetPlayerGroupsCharacterIsIn>> GetPlayerGroupsCharacterIsIn(Guid customerGUID, Guid userSessionGUID, string characterName, int playerGroupTypeID = 0)
        {
            IEnumerable<GetPlayerGroupsCharacterIsIn> outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@UserSessionGUID", userSessionGUID);
                p.Add("@PlayerGroupTypeID", playerGroupTypeID);

                outputObject = await Connection.QueryAsync<GetPlayerGroupsCharacterIsIn>("GetPlayerGroupsCharacterIsIn",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputObject;
        }

        public async Task<User> GetUser(Guid customerGuid, Guid userGuid)
        {
            User outputObject = default(User);

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGuid);
                p.Add("@UserGUID", userGuid);

                outputObject = await Connection.QuerySingleOrDefaultAsync<User>("GetUser",
                    p,
                    commandType: CommandType.StoredProcedure);
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

                try
                {
                    outputObject = await Connection.QueryAsync<User>(GenericQueries.GetUsers, p);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return outputObject;
        }

        public async Task<GetUserSession> GetUserSession(Guid customerGUID, Guid userSessionGUID)
        {
            GetUserSession outputObject = default;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@UserSessionGUID", userSessionGUID);

                outputObject = await Connection.QuerySingleOrDefaultAsync<GetUserSession>("GetUserSession",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputObject;
        }

        public async Task<GetUserSession> GetUserSessionORM(Guid customerGUID, Guid userSessionGUID)
        {
            GetUserSession outputObject = default;

            using (Connection)
            {
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(MSSQLQueries.GetUserSessionSQL, new { CustomerGUID = customerGUID, UserSessionGUID = userSessionGUID });
            }

            return outputObject;
        }

        public async Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGUID, Guid userSessionGUID) //id = UserSessionGUID
        {
            GetUserSessionComposite outputObject = default;
            UserSessions userSession = default;
            User user = default;
            Characters character = default;

            using (Connection)
            {
                userSession = await Connection.QueryFirstOrDefaultAsync<UserSessions>(MSSQLQueries.GetUserSessionOnlySQL, new { CustomerGUID = customerGUID, UserSessionGUID = userSessionGUID });
                var userTask = Connection.QueryFirstOrDefaultAsync<User>(MSSQLQueries.GetUserSQL, new { CustomerGUID = customerGUID, UserGUID = userSession.UserGuid });
                var characterTask = Connection.QueryFirstOrDefaultAsync<Characters>(MSSQLQueries.GetCharacterByNameSQL, new { CustomerGUID = customerGUID, CharacterName = userSession.SelectedCharacterName });

                user = await userTask;
                character = await characterTask;
            }

            outputObject = outputObject with
            {
                UserSession = userSession,
                User = user,
                Character = character
            };

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

                outputObject = await Connection.QuerySingleOrDefaultAsync<PlayerLoginAndCreateSession>("PlayerLoginAndCreateSession",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> Logout(Guid customerGuid, Guid userSessionGuid)
        {
            SuccessAndErrorMessage outputObject = default;

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGuid);
                    p.Add("@UserSessionGUID", userSessionGuid);

                    await Connection.ExecuteAsync(GenericQueries.Logout, p, commandType: CommandType.Text);
                }

                outputObject = default(SuccessAndErrorMessage) with
                {
                    Success = true,
                    ErrorMessage = ""
                };

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject = outputObject with
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return outputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> UserSessionSetSelectedCharacter(Guid customerGUID, Guid userSessionGUID, string selectedCharacterName)
        {
            SuccessAndErrorMessage outputObject = default;

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@SelectedCharacterName", selectedCharacterName);

                    await Connection.ExecuteAsync("UserSessionSetSelectedCharacter",
                        p,
                        commandType: CommandType.StoredProcedure);
                }

                outputObject = outputObject with
                {
                    Success = true,
                    ErrorMessage = ""
                }

                ;

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject = outputObject with
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return outputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> RegisterUser(Guid customerGUID, string email, string password, string firstName, string lastName)
        {
            SuccessAndErrorMessage outputObject = default;

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
                    p.Add("@Role", "Player");
                    p.Add("@UserGUID", dbType: DbType.Guid, direction: ParameterDirection.Output);

                    await Connection.ExecuteAsync("AddUser",
                        p,
                        commandType: CommandType.StoredProcedure);
                }

                outputObject = outputObject with { Success = true,
                ErrorMessage = "" };

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject = outputObject with
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return outputObject;
            }
        }

        public async Task<GetUserSession> GetUserFromEmail(Guid customerGUID, string email)
        {
            GetUserSession outputObject = default;

            using (Connection)
            {
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(MSSQLQueries.GetUserFromEmailSQL, new { CustomerGUID = customerGUID, Email = email });
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> RemoveCharacter(Guid customerGUID, Guid userSessionGUID, string characterName)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@CharacterName", characterName);

                    await Connection.ExecuteAsync("RemoveCharacter",
                        p,
                        commandType: CommandType.StoredProcedure);
                }

                return default;
            }
            catch (Exception ex)
            {
                return default(SuccessAndErrorMessage) with { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<SuccessAndErrorMessage> UpdateUser(Guid customerGuid, Guid userGuid, string firstName, string lastName, string email)
        {
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

                return default;
            }
            catch (Exception ex)
            {
                return default(SuccessAndErrorMessage) with { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
