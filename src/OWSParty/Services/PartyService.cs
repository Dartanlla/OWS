using Grpc.Core;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PartyServiceApp.Protos;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Google.Protobuf.WellKnownTypes;
using OWSParty.Requests.Party;
using OWSData.Models.StoredProcs;
using MySqlX.XDevAPI;
using System.Linq;

namespace OWSParty.Service
{
    public class PartyService : PartyApp.PartyAppBase
    {

        public static List<ClientInfo> _partyClients = new List<ClientInfo>();

        private readonly ICharactersRepository _charactersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public PartyService(ICharactersRepository charactersRepository,
            IHeaderCustomerGUID customerGuid)
        {
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
        }
        public override async Task RegisterParty(PartyRegister request, IServerStreamWriter<PartyToSend> responseStream, ServerCallContext context)
        {
            if (Guid.Parse(request.CustomerGuid) == Guid.Empty)
            {
                return;
            }

            string id = request.UserId;
            string userName = request.UserName;

            _partyClients.Add(new ClientInfo
            {
                Id = id,
                UserName = userName,
                serverStreamWriter = responseStream
            });

            _customerGuid.CustomerGUID = Guid.Parse(request.CustomerGuid);
            GetInitialPartySettingsRequest getInitalPartySettings = new GetInitialPartySettingsRequest();
            getInitalPartySettings.SetData(_charactersRepository, _customerGuid, userName);

            PartyToSend partyInformation = await getInitalPartySettings.Handle();

            if(partyInformation != null && partyInformation != default)
            {
                await responseStream.WriteAsync(partyInformation);
            }

            Func<bool> isCancelled = () => context.CancellationToken.IsCancellationRequested;
            SpinWait.SpinUntil(isCancelled);
            _partyClients.RemoveAll(x => x.Id == id);
            return;
        }

        public override async Task<Empty> SendPartyMsg(PartyToSend request, ServerCallContext context)
        {
            switch(request.PartyAction)
            {
                case PartyAction.MessageTypeAsk:
                    break;
                case PartyAction.MessageTypeCreate:
                case PartyAction.MessageTypeAdd:
                    _customerGuid.CustomerGUID = Guid.Parse(request.CustomerGuid);
                    CreatePartyOrAddMemberRequest createPartyOrAddMemberRequest = new CreatePartyOrAddMemberRequest();
                    createPartyOrAddMemberRequest.SetData(_charactersRepository, _customerGuid, request);

                    PartyToSend partyInformation = await createPartyOrAddMemberRequest.Handle();

                    var createPartyTasks = new List<Task>();
                    foreach (var partyMember in partyInformation.PartyMembers)
                    {
                        ClientInfo client = _partyClients.Find(client => client.UserName == partyMember.CharName);
                        if(client == null || client == default)
                        {
                            continue;
                        }
                        createPartyTasks.Add(client.serverStreamWriter.WriteAsync(partyInformation));
                    }
                    await Task.WhenAll(createPartyTasks);
                    break;
                case PartyAction.MessageTypeKick:
                case PartyAction.MessageTypeLeave:
                    _customerGuid.CustomerGUID = Guid.Parse(request.CustomerGuid);
                    LeavePartyRequest leavePartyRequest = new LeavePartyRequest();
                    leavePartyRequest.SetData(_charactersRepository, _customerGuid, request);
                    
                    PartyToSend Party = await leavePartyRequest.Handle();
                    if(Party == request)
                    {
                        break;
                    }

                    var leavePartyTasks = new List<Task>();
                    PartyToSend LeaveParty = new PartyToSend();
                    LeaveParty.PartyAction = request.PartyAction;
                    foreach (var partyMember in request.PartyMembers)
                    {
                        ClientInfo client = _partyClients.Find(client => client.UserName == partyMember.CharName);
                        if (client == null || client == default)
                        {
                            continue;
                        }
                        leavePartyTasks.Add(client.serverStreamWriter.WriteAsync(LeaveParty));
                    }

                    foreach (var partyMember in Party.PartyMembers)
                    {
                        ClientInfo client = _partyClients.Find(client => client.UserName == partyMember.CharName);
                        if (client == null || client == default)
                        {
                            continue;
                        }
                        leavePartyTasks.Add(client.serverStreamWriter.WriteAsync(Party));
                    }
                    await Task.WhenAll(leavePartyTasks);
                    break;

                case PartyAction.MessageTypeMakeLead:
                    _customerGuid.CustomerGUID = Guid.Parse(request.CustomerGuid);
                    ChangePartyLeaderRequest changePartyLeaderRequest = new ChangePartyLeaderRequest();
                    changePartyLeaderRequest.SetData(_charactersRepository, _customerGuid, request);
                    
                    PartyToSend PartyLeaderChanged = await changePartyLeaderRequest.Handle();

                    var makePartyLeaderTasks = new List<Task>();
                    foreach (var partyMember in PartyLeaderChanged.PartyMembers)
                    {
                        ClientInfo client = _partyClients.Find(client => client.UserName == partyMember.CharName);
                        if (client == null || client == default)
                        {
                            continue;
                        }
                        makePartyLeaderTasks.Add(client.serverStreamWriter.WriteAsync(PartyLeaderChanged));
                    }
                    await Task.WhenAll(makePartyLeaderTasks);
                    break;
                case PartyAction.MessageTypeRaid:
                    break;
                case PartyAction.MessageTypeLoot: 
                    break;
            }
            return new Empty();
        }
    }

    public class ClientInfo
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public IServerStreamWriter<PartyToSend> serverStreamWriter { get; set; }
    }

}