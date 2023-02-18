using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using OWSData.Models.Tables;

namespace OWSData.Repositories.Implementations.MySQL
{
    public class UsersRepository : IUsersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public UsersRepository(IOptions<StorageOptions> storageOptions)
        {
            this._storageOptions = storageOptions;
        }

        private IDbConnection Connection => new MySqlConnection(_storageOptions.Value.OWSDBConnectionString);

        public async Task<IEnumerable<GetAllCharacters>> GetAllCharacters(Guid customerGUID, Guid userSessionGUID)
        {
            IEnumerable<GetAllCharacters> outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@UserSessionGUID", userSessionGUID);

                outputObject = await Connection.QueryAsync<GetAllCharacters>("call GetAllCharacters(@CustomerGUID,@UserSessionGUID)",
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

                    outputObject = await Connection.QuerySingleAsync<CreateCharacter>("call AddCharacter(@CustomerGUID,@UserSessionGUID,@CharacterName,@ClassName)",
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

        //_PlayerGroupTypeID 0 returns all group types
        public async Task<IEnumerable<GetPlayerGroupsCharacterIsIn>> GetPlayerGroupsCharacterIsIn(Guid customerGUID, Guid userSessionGUID, string characterName, int playerGroupTypeID = 0) 
        {
            IEnumerable<GetPlayerGroupsCharacterIsIn> OutputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@UserSessionGUID", userSessionGUID);
                p.Add("@PlayerGroupTypeID", playerGroupTypeID);

                OutputObject = await Connection.QueryAsync<GetPlayerGroupsCharacterIsIn>("call GetPlayerGroupsCharacterIsIn(@CustomerGUID,@CharName,@UserSessionGUID,@PlayerGroupTypeID)",
                    p,
                    commandType: CommandType.Text);
            }

            return OutputObject;
        }

        public async Task<User> GetUser(Guid customerGuid, Guid userGuid)
        {
            User outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGuid);
                p.Add("@UserGUID", userGuid);

                outputObject = await Connection.QuerySingleOrDefaultAsync<User>("call GetUser(@CustomerGUID,@UserGUID)",
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

                outputObject = await Connection.QuerySingleOrDefaultAsync<GetUserSession>("call GetUserSession(@CustomerGUID,@UserSessionGUID)",
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
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(MySQLQueries.GetUserSessionSQL, new { @CustomerGUID = customerGUID, @UserSessionGUID = userSessionGUID });
            }

            return outputObject;
        }

        public async Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGUID, Guid userSessionGUID) //id = UserSessionGUID
        {
            GetUserSessionComposite outputObject = new GetUserSessionComposite();
            UserSessions userSession;
            User user;
            Characters character;

            using (Connection)
            {
                userSession = await Connection.QueryFirstOrDefaultAsync<UserSessions>(MySQLQueries.GetUserSessionOnlySQL, new { @CustomerGUID = customerGUID, @UserSessionGUID = userSessionGUID });
                var userTask = Connection.QueryFirstOrDefaultAsync<User>(MySQLQueries.GetUserSQL, new { @CustomerGUID = customerGUID, @UserGUID = userSession.UserGuid });
                var characterTask = Connection.QueryFirstOrDefaultAsync<Characters>(MySQLQueries.GetCharacterByNameSQL, new { @CustomerGUID = customerGUID, @CharacterName = userSession.SelectedCharacterName });

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

                outputObject = await Connection.QuerySingleOrDefaultAsync<PlayerLoginAndCreateSession>($"call PlayerLoginAndCreateSession(@CustomerGUID,@Email,@Password,@DontCheckPassword)",
                    p,
                    commandType: CommandType.Text);
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> UserSessionSetSelectedCharacter(Guid customerGUID, Guid userSessionGUID, string selectedCharacterName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@SelectedCharacterName", selectedCharacterName);

                    await Connection.ExecuteAsync("call UserSessionSetSelectedCharacter(@CustomerGUID, @UserSessionGUID, @SelectedCharacterName)",
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
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@Email", email);
                    p.Add("@Password", password);
                    p.Add("@FirstName", firstName);
                    p.Add("@LastName", lastName);
                    p.Add("@Role", "Player");

                    await Connection.ExecuteAsync("select AddUser(@CustomerGUID, @FirstName, @LastName, @Email, @Password, @Role)",
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
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(MySQLQueries.GetUserFromEmailSQL, new { @CustomerGUID = customerGUID, @Email = email });
            }

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> RemoveCharacter(Guid customerGUID, Guid userSessionGUID, string characterName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@UserSessionGUID", userSessionGUID);
                    p.Add("@CharacterName", characterName);

                    await Connection.ExecuteAsync("call RemoveCharacter(@CustomerGUID,@UserSessionGUID,@CharacterName)",
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