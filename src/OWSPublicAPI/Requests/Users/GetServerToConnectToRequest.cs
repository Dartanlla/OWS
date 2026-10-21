using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Options;
using OWSShared.RequestPayloads;
using Microsoft.Extensions.Options;
using Serilog;
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

        //Gate for the session-to-character ownership check below.  Default OFF: the UE plugin does
        //not yet send UserSessionGUID on the LaunchZoneInstance / TravelToLastZoneServer paths.
        //Flip to true once every GetServerToConnectTo caller sends the session.
        private static readonly bool RequireSessionCharacterBinding =
            string.Equals(Environment.GetEnvironmentVariable("OWS_REQUIRE_SESSION_CHARACTER_BINDING"), "true", StringComparison.OrdinalIgnoreCase);

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

            //Bind the session to the character.  Without this the caller can place any character by
            //name, take over its CharOnMapInstance row, and drive zone spin-ups for characters it
            //does not own.
            //
            //OPT-IN, and default OFF, because the UE plugin does not send UserSessionGUID on every
            //path that reaches here: LaunchZoneInstance and TravelToLastZoneServer serialize only
            //CharacterName/ZoneName/PlayerGroupType, so the field arrives as Guid.Empty and any
            //unconditional check rejects the whole login flow.  Enabling this before the client
            //sends the session on all four call sites breaks PIE and zone travel.
            //
            //Ownership is checked against the session's UserGUID rather than SelectedCharacterName:
            //the selection step is a separate call that several paths legitimately skip, so keying
            //off it would reject valid logins while adding no real authority.
            if (RequireSessionCharacterBinding)
            {
                GetUserSession userSession = await usersRepository.GetUserSession(CustomerGUID, UserSessionGUID);

                if (userSession == null || !userSession.UserGuid.HasValue)
                {
                    Output.Success = false;
                    Output.ErrorMessage = "GetServerToConnectTo: Invalid or expired User Session.";
                    return new OkObjectResult(Output);
                }

                GetCharByCharName ownedCharacter = await charactersRepository.GetCharByCharName(CustomerGUID, CharacterName);

                if (ownedCharacter == null || !ownedCharacter.UserGuid.HasValue
                    || ownedCharacter.UserGuid.Value != userSession.UserGuid.Value)
                {
                    Output.Success = false;
                    Output.ErrorMessage = "GetServerToConnectTo: CharacterName does not belong to this User Session.";
                    return new OkObjectResult(Output);
                }
            }

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

            if (joinMapByCharacterName == null)
            {
                Output.Success = false;
                Output.ErrorMessage = "GetServerToConnectTo: No response was returned when looking up the destination zone.";
                return new OkObjectResult(Output);
            }

            if (!joinMapByCharacterName.Success && !String.IsNullOrEmpty(joinMapByCharacterName.ErrorMessage))
            {
                return new OkObjectResult(joinMapByCharacterName);
            }

            if (joinMapByCharacterName.WorldServerID < 1)
            {
                Output.Success = false;
                Output.ErrorMessage = "GetServerToConnectTo: WorldServerID is less than 1.  Make sure you setup at least one valid World Server and that it is currently running!";
                return new OkObjectResult(Output);
            }

            //There is no zone server running that will accept our connection, so start up a new one
            if (joinMapByCharacterName.NeedToStartupMap)
            {
                bool requestSuccess;

                try
                {
                    requestSuccess = await RequestServerSpinUp(joinMapByCharacterName.WorldServerID, joinMapByCharacterName.MapInstanceID, joinMapByCharacterName.MapNameToStart, joinMapByCharacterName.Port);
                }
                catch
                {
                    return await ReleaseReservationAndReturnFailure(joinMapByCharacterName, "GetServerToConnectTo: Failed to request zone server spin-up.");
                }

                if (!requestSuccess)
                {
                    return await ReleaseReservationAndReturnFailure(joinMapByCharacterName, "GetServerToConnectTo: Failed to request zone server spin-up.");
                }

                //Wait OWSGeneralConfig.SecondsToWaitBeforeFirstPollForSpinUp seconds before the first CheckMapInstanceStatus to give it time to spin up
                await Task.Delay(TimeSpan.FromSeconds(owsGeneralConfig.Value.SecondsToWaitBeforeFirstPollForSpinUp));

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
            else
            {
                return await ReleaseReservationAndReturnFailure(joinMapByCharacterName, "GetServerToConnectTo: Zone server did not become ready before the timeout.");
            }

            Output = joinMapByCharacterName;
            Output.Success = true;
            Output.ErrorMessage = "";
            Log.Information("zone_join {CharacterName} {ZoneName} instance {MapInstanceId} port {Port}",
                CharacterName, ZoneName, joinMapByCharacterName.MapInstanceID, joinMapByCharacterName.Port);
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

                await Task.Delay(TimeSpan.FromSeconds(owsGeneralConfig.Value.SecondsToWaitInBetweenSpinUpPolling));
            }

            //The server did not spin up in time so shut it down


            return false;
        }

        private async Task<IActionResult> ReleaseReservationAndReturnFailure(JoinMapByCharName joinMapByCharacterName, string errorMessage)
        {
            if (joinMapByCharacterName?.MapInstanceID > 0)
            {
                await charactersRepository.ReleaseCharacterMapReservation(CustomerGUID, CharacterName, joinMapByCharacterName.MapInstanceID);
            }

            Output = joinMapByCharacterName ?? new JoinMapByCharName();
            Output.Success = false;
            Output.ErrorMessage = errorMessage;
            return new OkObjectResult(Output);
        }

        private async Task<bool> RequestServerSpinUp(int worldServerID, int zoneInstanceID, string zoneName, int port)
        {
            var instanceManagementHttpClient = httpClientFactory.CreateClient("OWSInstanceManagement");

            instanceManagementHttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            instanceManagementHttpClient.DefaultRequestHeaders.Add("X-CustomerGUID", CustomerGUID.ToString());

            SpinUpServerInstanceRequestPayload spinUpServerInstanceRequestPayload = new SpinUpServerInstanceRequestPayload
            {
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
