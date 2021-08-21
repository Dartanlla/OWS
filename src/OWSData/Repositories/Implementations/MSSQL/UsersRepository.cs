using Dapper;
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

        public async Task<CreateCharacter> CreateCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className)
        {
            CreateCharacter outputObject = new CreateCharacter();

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

                    //await Connection.ExecuteAsync("AddCharacter",
                    //    p,
                    //    commandType: CommandType.StoredProcedure);
                    //errorMessage = p.Get<string>("ErrorMessage");
                }

                if (String.IsNullOrEmpty(outputObject.ErrorMessage))
                {
                    outputObject.Success = true;
                }
                else
                {
                    outputObject.Success = false;
                }

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
            IEnumerable<GetPlayerGroupsCharacterIsIn> outputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", userSessionGUID);
                p.Add("@UserSessionGUID", characterName);
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
        public async Task<GetUserSession> GetUserSession(Guid customerGUID, Guid userSessionGUID)
        {
            GetUserSession outputObject = new GetUserSession();

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
            GetUserSession outputObject = new GetUserSession();

            using (Connection)
            {
                outputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(MSSQLQueries.GetUserSessionSQL, new { CustomerGUID = customerGUID, UserSessionGUID = userSessionGUID });
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

                    await Connection.ExecuteAsync("UserSessionSetSelectedCharacter",
                        p,
                        commandType: CommandType.StoredProcedure);
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
                    p.Add("@UserGUID", dbType: DbType.Guid, direction: ParameterDirection.Output);

                    await Connection.ExecuteAsync("AddUser",
                        p,
                        commandType: CommandType.StoredProcedure);
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
    }
}
