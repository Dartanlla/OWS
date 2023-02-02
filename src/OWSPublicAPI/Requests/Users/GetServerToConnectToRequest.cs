using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Options;
using OWSShared.RequestPayloads;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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
        private IOptions<PublicAPIOptions> owsGeneralConfig;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private ICharactersRepository charactersRepository;
        private IHttpClientFactory httpClientFactory;

        public void SetData(IOptions<PublicAPIOptions> owsGeneralConfig, IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, IHttpClientFactory httpClientFactory)
        {
            this.owsGeneralConfig = owsGeneralConfig;
            this.usersRepository = usersRepository;
            this.charactersRepository = charactersRepository;
            this.httpClientFactory = httpClientFactory;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
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
            }

            //If the ZoneName is empty, return an error
            if (String.IsNullOrEmpty(ZoneName))
            {
                Output.Success = false;
                Output.ErrorMessage = "GetServerToConnectTo: ZoneName is NULL or Empty.  Make sure the character is assigned to a Zone!";
                return new OkObjectResult(Output);
            }

            JoinMapByCharName joinMapByCharacterName = await charactersRepository.JoinMapByCharName(CustomerGUID, CharacterName, ZoneName, PlayerGroupType);

            bool readyForPlayersToConnect = false;

            if (joinMapByCharacterName == null || joinMapByCharacterName.WorldServerID < 1)
            {
                Output.Success = false;
                Output.ErrorMessage = "GetServerToConnectTo: WorldServerID is less than 1.  Make sure you setup at least one valid World Server and that it is currently running!";
                return new OkObjectResult(Output);
            }

            //There is no zone server running that will accept our connection, so start up a new one
            if (joinMapByCharacterName.NeedToStartupMap)
            {
                bool requestSuccess = await RequestServerSpinUp(joinMapByCharacterName.WorldServerID, joinMapByCharacterName.MapInstanceID, joinMapByCharacterName.MapNameToStart, joinMapByCharacterName.Port);

                //Wait OWSGeneralConfig.SecondsToWaitBeforeFirstPollForSpinUp seconds before the first CheckMapInstanceStatus to give it time to spin up
                System.Threading.Thread.Sleep(owsGeneralConfig.Value.SecondsToWaitBeforeFirstPollForSpinUp);

                readyForPlayersToConnect = await WaitForServerReadyToConnect(CustomerGUID, joinMapByCharacterName.MapInstanceID);
            }
            //We found a zone server we can connect to, but it is still spinning up.  Wait until it is ready to connect to (up to OWSGeneralConfig.SecondsToWaitForServerSpinUp seconds).
            else if (joinMapByCharacterName.MapInstanceID > 0 && joinMapByCharacterName.MapInstanceStatus == 1)
            {
                //CheckMapInstanceStatus every OWSGeneralConfig.SecondsToWaitInBetweenSpinUpPolling seconds for up to OWSGeneralConfig.SecondsToWaitForServerSpinUp seconds
                readyForPlayersToConnect = await WaitForServerReadyToConnect(CustomerGUID, joinMapByCharacterName.MapInstanceID);
            }
            //We found a zone server we can connect to and it is ready to connect
            else if (joinMapByCharacterName.MapInstanceID > 0 && joinMapByCharacterName.MapInstanceStatus == 2)
            {
                //The zone server is ready to connect to
                readyForPlayersToConnect = true;
            }

            //The zone instance is ready, so connect the character to the map instance in our data store
            if (readyForPlayersToConnect)
            {
                await charactersRepository.AddCharacterToMapInstanceByCharName(CustomerGUID, CharacterName, joinMapByCharacterName.MapInstanceID);
            }

            Output = joinMapByCharacterName;
            Output.Success = true;
            Output.ErrorMessage = "";
            return new OkObjectResult(Output);
        }

        private async Task<bool> WaitForServerReadyToConnect(Guid customerGUID, int zoneInstanceID)
        {
            DateTime StartPollingTime = DateTime.Now;

            while (DateTime.Now < StartPollingTime.AddSeconds(owsGeneralConfig.Value.SecondsToWaitForServerSpinUp))
            {
                //Check Map Status
                var resultCheckMapInstanceStatus = await charactersRepository.CheckMapInstanceStatus(CustomerGUID, zoneInstanceID);

                if (resultCheckMapInstanceStatus.Status == 2) //Ready to play
                {
                    return true;
                }

                System.Threading.Thread.Sleep(owsGeneralConfig.Value.SecondsToWaitInBetweenSpinUpPolling);
            }

            //The server did not spin up in time so shut it down


            return false;
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

            var responseMessage = await instanceManagementHttpClient.PostAsync("api/Instance/SpinUpServerInstance", serverSpinUpPayload);

            if (responseMessage.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
