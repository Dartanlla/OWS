using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.RequestPayloads;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests.Users
{
    public class GetServerToConnectToRequest : IRequestHandler<GetServerToConnectToRequest, IActionResult>, IRequest
    {
        //Request Parameters
        public Guid UserSessionGUID { get; set; }
        public string CharacterName { get; set; }
        public string ZoneName { get; set; }
        public int PlayerGroupType { get; set; }

        //Private objects
        private JoinMapByCharName Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private ICharactersRepository charactersRepository;
        private IHttpClientFactory httpClientFactory;

        public void SetData(IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, IHttpClientFactory httpClientFactory)
        {
            this.usersRepository = usersRepository;
            this.charactersRepository = charactersRepository;
            this.httpClientFactory = httpClientFactory;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            Log.Information($"GetServerToConnectTo: Started...");
            Output = new JoinMapByCharName();

            //If ZoneName is empty, look it up from the character.  This is used for the inital login.
            if (String.IsNullOrEmpty(ZoneName) || ZoneName == "GETLASTZONENAME")
            {
                GetCharByCharName character = await charactersRepository.GetCharByCharName(CustomerGUID, CharacterName);

                //If we can't find the character by name, then return BadRequest.
                if (character == null)
                {
                    return new BadRequestResult();
                }

                ZoneName = character.MapName;
                Log.Information($"GetServerToConnectTo: Using last ZoneName for CharacterName: {CharacterName} ZoneName: {ZoneName}");
            }

            //If the ZoneName is empty, return an error
            if (String.IsNullOrEmpty(ZoneName))
            {
                Log.Error($"GetServerToConnectTo: ZoneName is NULL or Empty.  Make sure the character is assigned to a Zone!");
                Output.Success = false;
                Output.ErrorMessage = "GetServerToConnectTo: ZoneName is NULL or Empty.  Make sure the character is assigned to a Zone!";
                return new OkObjectResult(Output);
            }

            JoinMapByCharName joinMapByCharacterName = await charactersRepository.JoinMapByCharName(CustomerGUID, CharacterName, ZoneName, PlayerGroupType);

            if (joinMapByCharacterName == null || joinMapByCharacterName.WorldServerID < 1)
            {
                Log.Error($"GetServerToConnectTo: WorldServerID is less than 1.  Make sure you setup at least one valid World Server and that it is currently running!");
                Output.Success = false;
                Output.ErrorMessage = "GetServerToConnectTo: WorldServerID is less than 1.  Make sure you setup at least one valid World Server and that it is currently running!";
                return new OkObjectResult(Output);
            }

            //There is no zone server running that will accept our connection, so start up a new one and return immediately.
            //The client should cache ServerIP/Port/MapInstanceID and poll GetServerToConnectToStatus until MapInstanceStatus == 2.
            if (joinMapByCharacterName.NeedToStartupMap)
            {
                Log.Information($"GetServerToConnectTo: Starting up server instance MapInstanceID: {joinMapByCharacterName.MapInstanceID}");
                await RequestServerSpinUp(joinMapByCharacterName.WorldServerID, joinMapByCharacterName.MapInstanceID, joinMapByCharacterName.MapNameToStart, joinMapByCharacterName.Port);
            }
            //We found a zone server we can connect to, but it is still spinning up.
            //Return immediately — client should poll GetServerToConnectToStatus until MapInstanceStatus == 2.
            else if (joinMapByCharacterName.MapInstanceID > 0 && joinMapByCharacterName.MapInstanceStatus == 1)
            {
                Log.Information($"GetServerToConnectTo: Found a zone server that is still spinning up (MapInstanceID: {joinMapByCharacterName.MapInstanceID}). Returning immediately for client polling.");
            }
            //We found a zone server we can connect to and it is already ready.
            else if (joinMapByCharacterName.MapInstanceID > 0 && joinMapByCharacterName.MapInstanceStatus == 2)
            {
                Log.Information($"GetServerToConnectTo: Found a ready zone server (MapInstanceID: {joinMapByCharacterName.MapInstanceID}). Connecting character.");
                await charactersRepository.AddCharacterToMapInstanceByCharName(CustomerGUID, CharacterName, joinMapByCharacterName.MapInstanceID);
            }

            Output = joinMapByCharacterName;
            Output.Success = true;
            Output.ErrorMessage = "";
            return new OkObjectResult(Output);
        }

        private async Task<bool> RequestServerSpinUp(int worldServerID, int zoneInstanceID, string zoneName, int port)
        {
            var instanceManagementHttpClient = httpClientFactory.CreateClient("OWSInstanceManagement");

            instanceManagementHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            instanceManagementHttpClient.DefaultRequestHeaders.Add("X-CustomerGUID", CustomerGUID.ToString());

            SpinUpServerInstanceRequestPayload spinUpServerInstanceRequestPayload = new SpinUpServerInstanceRequestPayload {
                WorldServerID = worldServerID,
                ZoneInstanceID = zoneInstanceID,
                ZoneName = zoneName,
                Port = port
            };

            var serverSpinUpPayload = new StringContent(JsonSerializer.Serialize(spinUpServerInstanceRequestPayload), Encoding.UTF8, "application/json");

            Log.Information($"GetServerToConnectTo: Calling the Instance Manager to spin up the server instance on WorldServerId: {worldServerID} ZoneInstanceID: {zoneInstanceID} UE Map Name: {zoneName} Port: {port}");
            var responseMessage = await instanceManagementHttpClient.PostAsync("api/Instance/SpinUpServerInstance", serverSpinUpPayload);

            if (responseMessage.IsSuccessStatusCode)
            {
                Log.Information($"GetServerToConnectTo: Successfully called the Instance Manager to spin up the server: {CustomerGUID} {worldServerID} {zoneInstanceID} {zoneName} {port}");
                return true;
            }
            else
            {
                Log.Error($"GetServerToConnectTo: Failed to call the Instance Manager to spin up the server: {CustomerGUID} {worldServerID} {zoneInstanceID} {zoneName} {port}");
                return false;
            }
        }
    }
}
