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
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using OWSData.Models.Tables;

namespace OWSData.Repositories.Implementations.Postgres
{
    
    public class UsersRepository : IUsersRepository
    {
        private readonly IOptions<StorageOptions> storageOptions;

        public UsersRepository(IOptions<StorageOptions> storageOptions)
        {
            this.storageOptions = storageOptions;
        }

        public IDbConnection Connection
        {
            get
            {
                return new NpgsqlConnection(storageOptions.Value.OWSDBConnectionString);
            }
        }

        public async Task<CreateCharacter> CreateCharacter(Guid _CustomerGUID, Guid _UserSessionGUID, string _CharacterName, string _ClassName)
        {
            CreateCharacter OutputObject = new CreateCharacter();

            try
            {
                string errorMessage = "";

                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("CustomerGUID", _CustomerGUID);
                    p.Add("UserSessionGUID", _UserSessionGUID);
                    p.Add("CharacterName", _CharacterName);
                    p.Add("ClassName", _ClassName);
                    p.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);

                    await Connection.ExecuteAsync("AddCharacter",
                        p,
                        commandType: CommandType.StoredProcedure);

                    errorMessage = p.Get<string>("ErrorMessage");
                }

                if (String.IsNullOrEmpty(errorMessage))
                {
                    OutputObject.Success = true;
                    OutputObject.ErrorMessage = "";
                    OutputObject.CharacterName = _CharacterName;
                }
                else
                {
                    OutputObject.Success = false;
                    OutputObject.ErrorMessage = errorMessage;
                    OutputObject.CharacterName = _CharacterName;
                }

                return OutputObject;
            }
            catch (Exception ex)
            {
                OutputObject.Success = false;
                OutputObject.ErrorMessage = ex.Message;
                OutputObject.CharacterName = "";

                return OutputObject;
            }
        }

        public async Task<IEnumerable<GetAllCharacters>> GetAllCharacters(Guid customerGUID, Guid userSessionGUID)
        {
            IEnumerable<GetAllCharacters> outputObject = new List<GetAllCharacters>();

            //Not Implemented

            return outputObject;
        }

        //_PlayerGroupTypeID 0 returns all group types
        public async Task<IEnumerable<GetPlayerGroupsCharacterIsIn>> GetPlayerGroupsCharacterIsIn(Guid _CustomerGUID, Guid _UserSessionGUID, string _CharacterName, int _PlayerGroupTypeID = 0)
        {
            IEnumerable<GetPlayerGroupsCharacterIsIn> OutputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", _CustomerGUID);
                p.Add("@CharName", _CharacterName);
                p.Add("@UserSessionGUID", _UserSessionGUID);
                p.Add("@PlayerGroupTypeID", _PlayerGroupTypeID);

                OutputObject = await Connection.QueryAsync<GetPlayerGroupsCharacterIsIn>("GetPlayerGroupsCharacterIsIn",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return OutputObject;
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

        public async Task<GetUserSession> GetUserSession(Guid _CustomerGUID, Guid _UserSessionGUID) //id = UserSessionGUID
        {
            GetUserSession OutputObject = new GetUserSession();

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", _CustomerGUID);
                p.Add("@UserSessionGUID", _UserSessionGUID);

                OutputObject = await Connection.QuerySingleOrDefaultAsync<GetUserSession>("GetUserSession",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return OutputObject;
        }

        public async Task<GetUserSession> GetUserSessionORM(Guid _CustomerGUID, Guid _UserSessionGUID) //id = UserSessionGUID
        {
            GetUserSession OutputObject = new GetUserSession();

            using (Connection)
            {
                /*OutputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(@"SELECT US.""CustomerGUID"", US.""UserGUID"", US.""UserSessionGUID"", US.""LoginDate"", 
                US.""SelectedCharacterName"",
                U.""FirstName"", U.""LastName"", U.""CreateDate"", U.""LastAccess"", U.""Role""
                FROM ""public"".""UserSessions"" US
                INNER JOIN ""public"".""Users"" U
                    ON U.""UserGUID""=US.""UserGUID""
                WHERE US.""CustomerGUID""=@CustomerGUID
                AND US.""UserSessionGUID""=@UserSessionGUID", new { CustomerGUID = _CustomerGUID, UserSessionGUID = _UserSessionGUID });*/

                OutputObject = await Connection.QueryFirstOrDefaultAsync<GetUserSession>(PostgreQueries.GetUserSessionSQL, new { CustomerGUID = _CustomerGUID, UserSessionGUID = _UserSessionGUID });
            }

            return OutputObject;
        }

        public async Task<GetUserSessionComposite> GetUserSessionParallel(Guid _CustomerGUID, Guid _UserSessionGUID) //id = UserSessionGUID
        {
            GetUserSessionComposite OutputObject = new GetUserSessionComposite();
        
            return OutputObject;
        }

        public async Task<PlayerLoginAndCreateSession> LoginAndCreateSession(Guid _CustomerGUID, string _Email, string _Password, bool _DontCheckPassword = false)
        {
            PlayerLoginAndCreateSession OutputObject;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", _CustomerGUID);
                p.Add("@Email", _Email);
                p.Add("@Password", _Password);
                p.Add("@DontCheckPassword", _DontCheckPassword);

                OutputObject = await Connection.QuerySingleOrDefaultAsync<PlayerLoginAndCreateSession>("PlayerLoginAndCreateSession",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return OutputObject;
        }

        public async Task<SuccessAndErrorMessage> UserSessionSetSelectedCharacter(Guid _CustomerGUID, Guid _UserSessionGUID, string _SelectedCharacterName)
        {
            SuccessAndErrorMessage OutputObject = new SuccessAndErrorMessage();

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", _CustomerGUID);
                    p.Add("@UserSessionGUID", _UserSessionGUID);
                    p.Add("@SelectedCharacterName", _SelectedCharacterName);

                    await Connection.ExecuteAsync("UserSessionSetSelectedCharacter",
                        p,
                        commandType: CommandType.StoredProcedure);
                }

                OutputObject.Success = true;
                OutputObject.ErrorMessage = "";

                return OutputObject;
            }
            catch (Exception ex)
            {
                OutputObject.Success = false;
                OutputObject.ErrorMessage = ex.Message;

                return OutputObject;
            }
        }

        public async Task<SuccessAndErrorMessage> RegisterUser(Guid _CustomerGUID, string _UserName, string _Password, string _FirstName, string _LastName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            //Add Implementation

            return outputObject;
        }

        public async Task<GetUserSession> GetUserFromEmail(Guid _CustomerGUID, string _Email)
        {
            GetUserSession outputObject = new GetUserSession();

            //Add Implementation

            return outputObject;
        }

        public async Task<SuccessAndErrorMessage> RemoveCharacter(Guid _CustomerGUID, Guid _UserSessionGUID, string _CharacterName)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            //Add Implementation

            return outputObject;
        }
    }
    
}
