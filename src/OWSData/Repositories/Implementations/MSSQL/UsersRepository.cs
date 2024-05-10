using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using CryptSharp.Core;
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

        static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
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
            CreateCharacter outputObject = new CreateCharacter();

            IDbConnection conn = Connection;
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("CustomerGUID", customerGUID);
                parameters.Add("UserSessionGUID", userSessionGUID);
                parameters.Add("CharName", characterName);
                parameters.Add("ClassName", className);

                UserSessions outputUserSession = await Connection.QueryFirstOrDefaultAsync<UserSessions>(GenericQueries.GetUserBySession, parameters);

                parameters.Add("@UserGUID", outputUserSession.UserGuid);

                // Ensure a valid User Session
                if (outputUserSession.UserGuid == Guid.Empty)
                {
                    outputObject.ErrorMessage = "Invalid Session.";
                    outputObject.Success = false;
                    return outputObject;
                }

                Characters outputCharacter = await Connection.QueryFirstOrDefaultAsync<Characters>(GenericQueries.GetCharacterByName, parameters);

                // Prevent Duplicate Character Names
                if (outputCharacter != null)
                {
                    outputObject.ErrorMessage = "Character Name already in use.";
                    outputObject.Success = false;
                    return outputObject;
                }

                int outputClassId = await Connection.QuerySingleOrDefaultAsync<int>(GenericQueries.GetClassIdByName,
                    parameters,
                    commandType: CommandType.Text);

                parameters.Add("ClassID", outputClassId);

                int outputCharacterId = await Connection.QuerySingleOrDefaultAsync<int>(MSSQLQueries.AddCharacterUsingClassID,
                    parameters,
                    commandType: CommandType.Text);

                parameters.Add("CharacterID", outputCharacterId);

                int outputClassInventorySize = await Connection.QuerySingleOrDefaultAsync<int>(GenericQueries.GetClassInventoryCount,
                    parameters,
                    commandType: CommandType.Text);

                if (outputClassInventorySize > 0)
                {
                    await Connection.ExecuteAsync(GenericQueries.AddCharacterInventoryFromClass,
                        parameters,
                        commandType: CommandType.Text);
                }
                else
                {
                    await Connection.ExecuteAsync(GenericQueries.AddDefaultCharacterInventory,
                        parameters,
                        commandType: CommandType.Text);
                }

                outputObject = await Connection.QuerySingleOrDefaultAsync<CreateCharacter>(GenericQueries.GetCreateCharacterByID,
                                parameters,
                                commandType: CommandType.Text);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                outputObject = new CreateCharacter()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return outputObject;
            }

            outputObject.Success = true;
            outputObject.ErrorMessage = "";

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> CreateCharacterUsingDefaultCharacterValues(Guid customerGUID, Guid userGUID, string characterName, string defaultSetName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

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
            User outputObject = new User();

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
            GetUserSession outputObject = new GetUserSession();

            using (Connection)
            {
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(GenericQueries.GetUserSession, new { @CustomerGUID = customerGUID, @UserSessionGUID = userSessionGUID });
            }

            return outputObject;
        }

        public async Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGUID, Guid userSessionGUID) //id = UserSessionGUID
        {
            GetUserSessionComposite outputObject = new GetUserSessionComposite();
            UserSessions userSession = new UserSessions();
            User user = new User();
            Characters character = new Characters();

            using (Connection)
            {
                userSession = await Connection.QueryFirstOrDefaultAsync<UserSessions>(MSSQLQueries.GetUserSessionOnlySQL, new { CustomerGUID = customerGUID, UserSessionGUID = userSessionGUID });
                var userTask = Connection.QueryFirstOrDefaultAsync<User>(MSSQLQueries.GetUserSQL, new { CustomerGUID = customerGUID, UserGUID = userSession.UserGuid });
                var characterTask = Connection.QueryFirstOrDefaultAsync<Characters>(MSSQLQueries.GetCharacterByNameSQL, new { CustomerGUID = customerGUID, CharacterName = userSession.SelectedCharacterName });

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
            PlayerLoginAndCreateSession outputObject = new PlayerLoginAndCreateSession();
            User outputUser;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@Email", email);
                parameters.Add("@Role", "Player");

                outputUser = await Connection.QueryFirstOrDefaultAsync<User>(GenericQueries.GetUserByEmail, parameters);

                parameters.Add("@UserGUID", outputUser.UserGuid);

                outputObject.Authenticated = Crypter.CheckPassword(password,
                    Base64Decode(outputUser.PasswordHash));

                if (outputObject.Authenticated || dontCheckPassword)
                {
                    await Connection.ExecuteAsync(GenericQueries.DeleteUserSessionsForUser, parameters, commandType: CommandType.Text);
                    outputObject.UserSessionGuid = Guid.NewGuid();
                    parameters.Add("@UserSessionGUID", outputObject.UserSessionGuid);
                    await Connection.QuerySingleOrDefaultAsync<Guid>(MSSQLQueries.AddUserSession, parameters, commandType: CommandType.Text);
                }
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
                using (Connection)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CustomerGUID", customerGUID);
                    parameters.Add("@UserSessionGUID", userSessionGUID);
                    parameters.Add("@SelectedCharacterName", selectedCharacterName);

                    await Connection.ExecuteAsync(GenericQueries.UpdateUserSessionSetSelectedCharacter,
                        parameters,
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
            string salt = Crypter.Blowfish.GenerateSalt();
            string cryptedPassword = Crypter.Blowfish.Crypt(password, salt);

            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CustomerGUID", customerGUID);
                    parameters.Add("@Email", email);
                    parameters.Add("@Salt", Base64Encode(salt));
                    parameters.Add("@PasswordHash", Base64Encode(cryptedPassword));
                    parameters.Add("@FirstName", firstName);
                    parameters.Add("@LastName", lastName);
                    parameters.Add("@Role", "Player");
                    parameters.Add("@UserGUID", Guid.NewGuid());

                    await Connection.ExecuteAsync(MSSQLQueries.AddUser,
                        parameters,
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
            GetUserSession outputObject = new GetUserSession();

            using (Connection)
            {
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(MSSQLQueries.GetUserFromEmailSQL, new { CustomerGUID = customerGUID, Email = email });
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> RemoveCharacter(Guid customerGUID, Guid userSessionGUID, string characterName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            IDbConnection conn = Connection;
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@UserSessionGUID", userSessionGUID);
                parameters.Add("@CharName", characterName);

                UserSessions outputUserSession = await Connection.QueryFirstOrDefaultAsync<UserSessions>(GenericQueries.GetUserBySession, parameters);

                parameters.Add("@UserGUID", outputUserSession.UserGuid);

                Characters outputCharacter = await Connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByNameAndUser,
                    parameters,
                    commandType: CommandType.Text);

                parameters.Add("@CharacterID", outputCharacter.CharacterId);

                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterAbilities, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterAbilityBars, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterHasAbilities, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterHasItems, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterInventoryItems, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterInventory, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterGroupUsers, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterCharacterData, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacterFromPlayerGroupCharacters, parameters, commandType: CommandType.Text);
                await Connection.ExecuteAsync(GenericQueries.RemoveCharacter, parameters, commandType: CommandType.Text);
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
    }
}
