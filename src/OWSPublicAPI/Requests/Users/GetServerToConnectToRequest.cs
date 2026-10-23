using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OWSData.Models.Composites;
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

        //Where the join ticket starts the character. Only a zone server (service key) sets these, for portal and
        //staff travel; a client's ticket always starts at the character's stored position.
        public JoinTicketStartMode StartMode { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StartZ { get; set; }
        public double StartRX { get; set; }
        public double StartRY { get; set; }
        public double StartRZ { get; set; }

        //Private objects
        private JoinMapByCharName Output;
        private IOptions<PublicAPIOptions> owsGeneralConfig;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private ICharactersRepository charactersRepository;
        private IHttpClientFactory httpClientFactory;
        private IAuditRepository auditRepository;
        private IJoinTicketsRepository joinTicketsRepository;
        private bool callerIsZoneServer;
        private Services.LoginGate loginGate;
        private ILoginQueueRepository loginQueueRepository;

        public void SetData(IOptions<PublicAPIOptions> owsGeneralConfig, IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, IHttpClientFactory httpClientFactory, IAuditRepository auditRepository,
            IJoinTicketsRepository joinTicketsRepository, bool callerIsZoneServer, Services.LoginGate loginGate, ILoginQueueRepository loginQueueRepository)
        {
            this.loginGate = loginGate;
            this.loginQueueRepository = loginQueueRepository;
            this.owsGeneralConfig = owsGeneralConfig;
            this.usersRepository = usersRepository;
            this.charactersRepository = charactersRepository;
            this.httpClientFactory = httpClientFactory;
            this.auditRepository = auditRepository;
            this.joinTicketsRepository = joinTicketsRepository;
            this.callerIsZoneServer = callerIsZoneServer;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            Output = new JoinMapByCharName();

            //Bind the session to the character.  Without this the caller can place any character by
            //name, take over its CharOnMapInstance row, and drive zone spin-ups for characters it
            //does not own.  Ownership is checked against the session's UserGUID rather than
            //SelectedCharacterName: the selection step is a separate call that several paths skip.
            GetUserSession userSession = await usersRepository.GetUserSession(CustomerGUID, UserSessionGUID);

            if (userSession == null || !userSession.UserGuid.HasValue)
            {
                Output.Success = false;
                Output.ErrorMessage = "GetServerToConnectTo: Invalid or expired User Session.";
                return new OkObjectResult(Output);
            }

            //A zone change is never refused for maintenance or the player cap: the zone handles maintenance itself.
            (string refusal, string refusalMessage) = await loginGate.RefusalForUser(CustomerGUID, userSession.UserGuid.Value, userSession.Role, checkMaintenance: !callerIsZoneServer);
            if (refusal != null)
            {
                Output.Success = false;
                Output.ErrorMessage = refusalMessage;
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

            if (!callerIsZoneServer && !OWSData.Models.UserRoles.IsStaff(userSession.Role))
            {
                int cap = await loginGate.PlayerCap();
                int position = cap > 0
                    ? await loginQueueRepository.Check(CustomerGUID, UserSessionGUID, userSession.UserGuid.Value, ownedCharacter.CharName, cap)
                    : 0;
                if (position > 0)
                {
                    Output.Success = false;
                    Output.QueuePosition = position;
                    Output.ErrorMessage = $"The server is full. You are number {position} in line.";
                    return new OkObjectResult(Output);
                }
            }

            //If ZoneName is empty, use the character's last zone.  This is used for the inital login.
            if (String.IsNullOrEmpty(ZoneName) || ZoneName == "GETLASTZONENAME")
            {
                ZoneName = ownedCharacter.MapName;
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

            JoinTicketStart start = callerIsZoneServer
                ? new JoinTicketStart { Mode = StartMode, X = StartX, Y = StartY, Z = StartZ, RX = StartRX, RY = StartRY, RZ = StartRZ }
                : new JoinTicketStart { Mode = JoinTicketStartMode.StoredPosition };

            Output = joinMapByCharacterName;
            Output.JoinTicket = await joinTicketsRepository.IssueTicket(CustomerGUID, UserSessionGUID, userSession.UserGuid.Value,
                ownedCharacter.CharacterId, ownedCharacter.CharName, joinMapByCharacterName.MapInstanceID, start);
            Output.Success = true;
            Output.ErrorMessage = "";
            Log.Information("zone_join {CharacterName} {ZoneName} instance {MapInstanceId} port {Port}",
                CharacterName, ZoneName, joinMapByCharacterName.MapInstanceID, joinMapByCharacterName.Port);
            await auditRepository.Add(CustomerGUID, "publicapi", CharacterName, "zone_join", joinMapByCharacterName.ZoneName ?? ZoneName,
                new { mapInstanceId = joinMapByCharacterName.MapInstanceID, port = joinMapByCharacterName.Port });
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
