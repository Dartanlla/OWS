using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.Models;

namespace OWSData.Repositories.Implementations.MSSQL
{
    public class CharactersRepository : ICharactersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public CharactersRepository(IOptions<StorageOptions> storageOptions)
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

        public async Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName)
        {
            GetCharByCharName outputCharacter;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@EnableAutoLoopBack", dbType: DbType.Boolean, direction: ParameterDirection.Output);

                outputCharacter = await Connection.QuerySingleOrDefaultAsync<GetCharByCharName>("GetCharByCharName",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputCharacter;
        }

        public async Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType)
        {
            JoinMapByCharName outputObject = new JoinMapByCharName();

            string serverIp = "";
            int? worldServerId = 0;
            string worldServerIp = "";
            int worldServerPort = 0;
            int port = 0;
            int mapInstanceID = 0;
            string mapNameToStart = "";
            int? mapInstanceStatus = 0;
            bool needToStartupMap = false;
            bool enableAutoLoopback = false;
            bool noPortForwarding = false;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@ZoneName", zoneName);
                p.Add("@PlayerGroupType", playerGroupType);

                p.Add("@ServerIP", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);
                p.Add("@WorldServerID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@WorldServerIP", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);
                p.Add("@WorldServerPort", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@Port", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@MapInstanceID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@MapNameToStart", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);
                p.Add("@MapInstanceStatus", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@NeedToStartupMap", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@EnableAutoLoopback", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@NoPortForwarding", noPortForwarding, dbType: DbType.Boolean, direction: ParameterDirection.Output);

                await Connection.QuerySingleOrDefaultAsync("JoinMapByCharName",
                    p,
                    commandType: CommandType.StoredProcedure);

                serverIp = p.Get<string>("@ServerIP");
                worldServerId = p.Get<int?>("@WorldServerID");
                worldServerIp = p.Get<string>("@WorldServerIP");
                worldServerPort = p.Get<int>("@WorldServerPort");
                port = p.Get<int>("@Port");
                mapInstanceID = p.Get<int>("@MapInstanceID");
                mapNameToStart = p.Get<string>("@MapNameToStart");
                mapInstanceStatus = p.Get<int?>("@MapInstanceStatus");
                needToStartupMap = p.Get<bool>("@NeedToStartupMap");
                enableAutoLoopback = p.Get<bool>("@EnableAutoLoopback");
                noPortForwarding = p.Get<bool>("@NoPortForwarding");
            }

            outputObject = new JoinMapByCharName() {
                ServerIP = serverIp,
                Port = port,
                WorldServerID = worldServerId ?? -1,
                WorldServerIP = worldServerIp,
                WorldServerPort = worldServerPort,
                MapInstanceID = mapInstanceID,
                MapNameToStart = mapNameToStart,
                MapInstanceStatus = mapInstanceStatus ?? -1,
                NeedToStartupMap = needToStartupMap,
                EnableAutoLoopback = enableAutoLoopback,
                NoPortForwarding = noPortForwarding
            };

            return outputObject;
        }

        public async Task<CheckMapInstanceStatus> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID)
        {
            CheckMapInstanceStatus outputObject = new CheckMapInstanceStatus();

            int mapInstanceStatus = 0;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@MapInstanceID", mapInstanceID);
                p.Add("@MapInstanceStatus", mapInstanceStatus, DbType.Int32, ParameterDirection.Output);

                await Connection.QuerySingleOrDefaultAsync("CheckMapInstanceStatus",
                    p,
                    commandType: CommandType.StoredProcedure);

                mapInstanceStatus = p.Get<int>("@MapInstanceStatus");
            }

            outputObject = new CheckMapInstanceStatus(mapInstanceStatus);
            return outputObject;
        }

        public async Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@MapInstanceID", mapInstanceID);

                await Connection.QuerySingleOrDefaultAsync("AddCharacterToMapInstanceByCharName",
                    p,
                    commandType: CommandType.StoredProcedure);
            }
        }
    }
}
