using Grpc.Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PartyServiceApp.Protos;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Grpc;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using OWSParty.Requests.Party;

namespace OWSParty.Service
{
    public class PartyService : PartyApp.PartyAppBase
    {
        public static ConcurrentDictionary<string, ClientInfo> _partyClients = new ConcurrentDictionary<string, ClientInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly ICharactersRepository _charactersRepository;
        private readonly IUsersRepository _usersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly ILogger<PartyService> _logger;

        public PartyService(ICharactersRepository charactersRepository,
            IUsersRepository usersRepository,
            IHeaderCustomerGUID customerGuid,
            ILogger<PartyService> logger)
        {
            _charactersRepository = charactersRepository;
            _usersRepository = usersRepository;
            _customerGuid = customerGuid;
            _logger = logger;
        }

        public override async Task RegisterParty(PartyRegister request, IServerStreamWriter<PartyToSend> responseStream, ServerCallContext context)
        {
            // Bound concurrent party streams (each holds a long-lived registration).
            using GrpcStreamGuard.ConnectionLease lease = GrpcStreamGuard.TryAcquireConnection();
            if (lease == null)
            {
                throw new RpcException(new Status(StatusCode.ResourceExhausted, "Party connection limit reached."));
            }

            if (!Guid.TryParse(request.CustomerGuid, out Guid parsedCustomerGuid) || parsedCustomerGuid == Guid.Empty)
            {
                return;
            }

            string id = request.UserId;
            string userName = request.UserName?.Trim();

            if (string.IsNullOrWhiteSpace(userName))
            {
                return;
            }

            // Session auth is gated off by default (the current client sends no session token).
            // When enabled, require valid customerguid/usersessionguid metadata, the session must
            // exist, and it must belong to the character being registered — so a client cannot
            // register the stream as someone else.
            if (GrpcSessionAuth.IsRequired && !await IsRegistrationAuthorized(context, parsedCustomerGuid, userName))
            {
                GrpcSessionAuth.LogRejected(context, "party");
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid session for party registration."));
            }

            ClientInfo clientInfo = new ClientInfo
            {
                Id = id,
                UserName = userName,
                ServerStreamWriter = responseStream
            };

            _partyClients.AddOrUpdate(userName, clientInfo, (_, _) => clientInfo);

            try
            {
                _customerGuid.CustomerGUID = parsedCustomerGuid;
                GetInitialPartySettingsRequest getInitalPartySettings = new GetInitialPartySettingsRequest();
                getInitalPartySettings.SetData(_charactersRepository, _customerGuid, userName);

                PartyToSend partyInformation = await getInitalPartySettings.Handle();

                if (partyInformation != null && partyInformation != default && partyInformation.PartyMembers.Count != 0)
                {
                    await responseStream.WriteAsync(partyInformation);
                }

                await Task.Delay(Timeout.Infinite, context.CancellationToken);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (_partyClients.TryGetValue(userName, out ClientInfo registeredClient) && ReferenceEquals(registeredClient, clientInfo))
                {
                    _partyClients.TryRemove(userName, out _);
                }
            }
        }

        private async Task<bool> IsRegistrationAuthorized(ServerCallContext context, Guid requestCustomerGuid, string userName)
        {
            if (!GrpcSessionAuth.TryReadCredentials(context, out Guid metaCustomerGuid, out Guid userSessionGuid))
            {
                return false;
            }

            // The credential's customer must match the one the registration claims.
            if (metaCustomerGuid != requestCustomerGuid)
            {
                return false;
            }

            var session = await _usersRepository.GetUserSession(metaCustomerGuid, userSessionGuid);
            if (session == null || !session.UserGuid.HasValue)
            {
                return false;
            }

            // Bind the stream identity to the session: the registering character must be the
            // session's selected character, so a valid session can't register as someone else.
            return string.Equals(session.SelectedCharacterName?.Trim(), userName, StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<Empty> SendPartyMsg(PartyToSend request, ServerCallContext context)
        {
            if (request == null)
            {
                ThrowInvalidArgument("Party request is required.");
            }

            switch (request.PartyAction)
            {
                case PartyAction.MessageTypeAsk:
                    await ExecutePartyAction(request.PartyAction, () => HandleAsk(request));
                    break;

                case PartyAction.MessageTypeCreate:
                case PartyAction.MessageTypeAdd:
                    await ExecutePartyAction(request.PartyAction, () => HandleCreateOrAdd(request));
                    break;

                case PartyAction.MessageTypeKick:
                case PartyAction.MessageTypeLeave:
                case PartyAction.MessageTypeDismiss:
                    await ExecutePartyAction(request.PartyAction, () => HandleRemoveOrDismiss(request));
                    break;

                case PartyAction.MessageTypeMakeLead:
                    await ExecutePartyAction(request.PartyAction, () => HandleMakeLeader(request));
                    break;

                case PartyAction.MessageTypeUpdateInfo:
                    await ExecutePartyAction(request.PartyAction, () => HandleUpdateInfo(request));
                    break;

                case PartyAction.MessageTypeRaid:
                case PartyAction.MessageTypeLoot:
                    _logger.LogWarning("Party action {PartyAction} is not implemented.", request.PartyAction);
                    ThrowInvalidArgument($"{request.PartyAction} is not implemented.");
                    break;

                default:
                    _logger.LogWarning("Unsupported party action {PartyAction}.", request.PartyAction);
                    ThrowInvalidArgument($"Unsupported party action: {request.PartyAction}.");
                    break;
            }

            return new Empty();
        }

        private async Task HandleAsk(PartyToSend request)
        {
            ValidateAsk(request);

            PartyMemberInfo target = request.PartyMembers[1];
            if (!_partyClients.TryGetValue(target.CharName, out ClientInfo client) || client == null)
            {
                _logger.LogWarning("Party ask for {TargetCharName} dropped: the target has no registered party stream.", target.CharName);
                return;
            }

            await client.ServerStreamWriter.WriteAsync(request);
        }

        private async Task HandleCreateOrAdd(PartyToSend request)
        {
            Guid customerGuid = ValidateCustomerGuid(request);

            if (request.PartyAction == PartyAction.MessageTypeCreate)
            {
                ValidateCreate(request);
            }
            else
            {
                ValidateAdd(request);
            }

            _customerGuid.CustomerGUID = customerGuid;
            CreatePartyOrAddMemberRequest createPartyOrAddMemberRequest = new CreatePartyOrAddMemberRequest();
            createPartyOrAddMemberRequest.SetData(_charactersRepository, _customerGuid, request);

            PartyToSend partyInformation = await createPartyOrAddMemberRequest.Handle();
            await BroadcastToMembers(partyInformation.PartyMembers, partyInformation);
        }

        private async Task HandleRemoveOrDismiss(PartyToSend request)
        {
            Guid customerGuid = ValidateCustomerGuid(request);

            switch (request.PartyAction)
            {
                case PartyAction.MessageTypeKick:
                    ValidateKick(request);
                    break;
                case PartyAction.MessageTypeLeave:
                    ValidateLeave(request);
                    break;
                case PartyAction.MessageTypeDismiss:
                    ValidateDismiss(request);
                    break;
            }

            _customerGuid.CustomerGUID = customerGuid;
            LeavePartyRequest leavePartyRequest = new LeavePartyRequest();
            leavePartyRequest.SetData(_charactersRepository, _customerGuid, request);

            PartyToSend party = await leavePartyRequest.Handle();
            PartyToSend removedParty = new PartyToSend
            {
                PartyAction = request.PartyAction
            };

            IEnumerable<PartyMemberInfo> removedPartyMembers = request.PartyAction switch
            {
                PartyAction.MessageTypeDismiss => party.PartyMembers,
                PartyAction.MessageTypeKick => request.PartyMembers.Where(partyMember => !partyMember.PartyLeader),
                _ => request.PartyMembers
            };

            await BroadcastToMembers(removedPartyMembers, removedParty);

            if (request.PartyAction == PartyAction.MessageTypeDismiss)
            {
                return;
            }

            await BroadcastToMembers(party.PartyMembers, party);
        }

        // Only party_info.party_name is applied. Description, exp and loot distribution reach the same
        // party through their own validated PUT endpoints and stay authoritative there, so a rename
        // cannot clobber a concurrent change to one of them.
        private async Task HandleUpdateInfo(PartyToSend request)
        {
            Guid customerGuid = ValidateCustomerGuid(request);
            ValidateUpdateInfo(request);

            _customerGuid.CustomerGUID = customerGuid;
            PartyMemberInfo leader = GetLeader(request);

            PartyToSend party = await _charactersRepository.UpdatePartyName(
                customerGuid,
                Guid.Parse(request.PartyInfo.PartyGuid),
                leader.CharName,
                leader.CharGuid,
                request.PartyInfo.PartyName);

            await BroadcastToMembers(party.PartyMembers, party);
        }

        private async Task HandleMakeLeader(PartyToSend request)
        {
            Guid customerGuid = ValidateCustomerGuid(request);
            ValidateMakeLeader(request);

            _customerGuid.CustomerGUID = customerGuid;
            ChangePartyLeaderRequest changePartyLeaderRequest = new ChangePartyLeaderRequest();
            changePartyLeaderRequest.SetData(_charactersRepository, _customerGuid, request);

            PartyToSend partyLeaderChanged = await changePartyLeaderRequest.Handle();
            await BroadcastToMembers(partyLeaderChanged.PartyMembers, partyLeaderChanged);
        }

        private async Task ExecutePartyAction(PartyAction partyAction, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (RpcException ex)
            {
                _logger.LogWarning("Party action {PartyAction} rejected: {StatusCode} {Detail}", partyAction, ex.StatusCode, ex.Status.Detail);
                throw;
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                string reason = GetClientSafeValidationMessage(ex);
                _logger.LogWarning(ex, "Party action {PartyAction} rejected: {Reason}", partyAction, reason);
                throw new RpcException(new Status(StatusCode.InvalidArgument, reason));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Party action {PartyAction} failed.", partyAction);
                throw new RpcException(new Status(StatusCode.Internal, "Party action failed."));
            }
        }

        private async Task BroadcastToMembers(IEnumerable<PartyMemberInfo> partyMembers, PartyToSend message)
        {
            await BroadcastPartyUpdate(partyMembers, message, _logger);
        }

        // A member with no registered RegisterParty stream is skipped, and the caller still sees the
        // action succeed — the roster simply never reaches them. Log it: silence here is the reason a
        // working party action looks like nothing happened.
        public static async Task BroadcastPartyUpdate(IEnumerable<PartyMemberInfo> partyMembers, PartyToSend message, ILogger logger = null)
        {
            List<Task> partyTasks = new List<Task>();
            List<string> unreachableMembers = new List<string>();

            foreach (PartyMemberInfo partyMember in partyMembers)
            {
                if (string.IsNullOrWhiteSpace(partyMember.CharName))
                {
                    continue;
                }

                if (!_partyClients.TryGetValue(partyMember.CharName, out ClientInfo client) || client == null)
                {
                    unreachableMembers.Add(partyMember.CharName);
                    continue;
                }

                partyTasks.Add(client.ServerStreamWriter.WriteAsync(message));
            }

            if (unreachableMembers.Count > 0)
            {
                logger?.LogWarning(
                    "Party {PartyAction} update not delivered to {UnreachableCount} of {MemberCount} member(s) with no registered party stream: {Members}.",
                    message.PartyAction,
                    unreachableMembers.Count,
                    unreachableMembers.Count + partyTasks.Count,
                    string.Join(", ", unreachableMembers));
            }

            await Task.WhenAll(partyTasks);
        }

        private static Guid ValidateCustomerGuid(PartyToSend request)
        {
            if (!Guid.TryParse(request.CustomerGuid, out Guid customerGuid) || customerGuid == Guid.Empty)
            {
                ThrowInvalidArgument("A valid customer_guid is required.");
            }

            return customerGuid;
        }

        private static void ValidatePartyGuid(PartyToSend request)
        {
            if (request.PartyInfo == null || !Guid.TryParse(request.PartyInfo.PartyGuid, out Guid partyGuid) || partyGuid == Guid.Empty)
            {
                ThrowInvalidArgument("A valid party_info.party_guid is required.");
            }
        }

        private static void ValidateAsk(PartyToSend request)
        {
            if (request.PartyMembers.Count < 2)
            {
                ThrowInvalidArgument("Ask requires requester and target party members.");
            }

            RequireMemberName(request.PartyMembers[0], "requester");
            RequireMemberName(request.PartyMembers[1], "target");
        }

        private static void ValidateCreate(PartyToSend request)
        {
            ValidateCustomerGuid(request);

            PartyMemberInfo leader = GetLeader(request);
            if (leader == null)
            {
                ThrowInvalidArgument("Create requires a party member with party_leader set to true.");
            }

            RequireMemberName(leader, "leader");
            RequireDistinctMemberNames(request.PartyMembers);
        }

        private static void ValidateAdd(PartyToSend request)
        {
            ValidateCustomerGuid(request);
            ValidatePartyGuid(request);

            PartyMemberInfo leader = GetLeader(request);
            if (leader == null)
            {
                ThrowInvalidArgument("Add requires the acting leader with party_leader set to true.");
            }

            RequireMemberName(leader, "leader");

            List<PartyMemberInfo> targets = GetNonLeaderMembers(request).ToList();
            if (!targets.Any())
            {
                ThrowInvalidArgument("Add requires at least one target member.");
            }

            foreach (PartyMemberInfo target in targets)
            {
                RequireMemberName(target, "target");
                RequireDifferentCharacters(leader, target, "Leader cannot add themself.");
            }

            RequireDistinctMemberNames(request.PartyMembers);
        }

        private static void ValidateKick(PartyToSend request)
        {
            ValidateCustomerGuid(request);
            ValidatePartyGuid(request);

            PartyMemberInfo leader = GetLeader(request);
            if (leader == null)
            {
                ThrowInvalidArgument("Kick requires the acting leader with party_leader set to true.");
            }

            RequireMemberName(leader, "leader");

            List<PartyMemberInfo> targets = GetNonLeaderMembers(request).ToList();
            if (!targets.Any())
            {
                ThrowInvalidArgument("Kick requires at least one target member.");
            }

            foreach (PartyMemberInfo target in targets)
            {
                RequireMemberName(target, "target");
                RequireDifferentCharacters(leader, target, "Leader cannot kick themself.");
            }

            RequireDistinctMemberNames(request.PartyMembers);
        }

        private static void ValidateLeave(PartyToSend request)
        {
            ValidateCustomerGuid(request);
            ValidatePartyGuid(request);

            if (request.PartyMembers.Count != 1)
            {
                ThrowInvalidArgument("Leave requires exactly the leaving member.");
            }

            RequireMemberName(request.PartyMembers[0], "member");
        }

        private static void ValidateDismiss(PartyToSend request)
        {
            ValidateCustomerGuid(request);
            ValidatePartyGuid(request);

            PartyMemberInfo leader = GetLeader(request);
            if (leader == null)
            {
                ThrowInvalidArgument("Dismiss requires the acting leader with party_leader set to true.");
            }

            RequireMemberName(leader, "leader");
        }

        private static void ValidateMakeLeader(PartyToSend request)
        {
            ValidateCustomerGuid(request);
            ValidatePartyGuid(request);

            PartyMemberInfo leader = GetLeader(request);
            if (leader == null)
            {
                ThrowInvalidArgument("Make leader requires the current leader with party_leader set to true.");
            }

            RequireMemberName(leader, "leader");

            List<PartyMemberInfo> targets = GetNonLeaderMembers(request).ToList();
            if (targets.Count != 1)
            {
                ThrowInvalidArgument("Make leader requires exactly one target member.");
            }

            PartyMemberInfo target = targets[0];
            RequireMemberName(target, "target");
            RequireDifferentCharacters(leader, target, "Leader cannot grant leader to themself.");
            RequireDistinctMemberNames(request.PartyMembers);
        }

        private static void ValidateUpdateInfo(PartyToSend request)
        {
            ValidateCustomerGuid(request);
            ValidatePartyGuid(request);

            PartyMemberInfo leader = GetLeader(request);
            if (leader == null)
            {
                ThrowInvalidArgument("Update info requires the acting leader with party_leader set to true.");
            }

            RequireMemberName(leader, "leader");

            if (string.IsNullOrWhiteSpace(request.PartyInfo?.PartyName))
            {
                ThrowInvalidArgument("A party_info.party_name is required.");
            }
        }

        private static PartyMemberInfo GetLeader(PartyToSend request)
        {
            return request.PartyMembers.FirstOrDefault(partyMember => partyMember.PartyLeader);
        }

        private static IEnumerable<PartyMemberInfo> GetNonLeaderMembers(PartyToSend request)
        {
            return request.PartyMembers.Where(partyMember => !partyMember.PartyLeader);
        }

        private static void RequireMemberName(PartyMemberInfo member, string role)
        {
            if (member == null || string.IsNullOrWhiteSpace(member.CharName))
            {
                ThrowInvalidArgument($"A valid {role} char_name is required.");
            }
        }

        private static void RequireDifferentCharacters(PartyMemberInfo actor, PartyMemberInfo target, string message)
        {
            if (string.Equals(actor.CharName, target.CharName, StringComparison.OrdinalIgnoreCase))
            {
                ThrowInvalidArgument(message);
            }
        }

        private static void RequireDistinctMemberNames(IEnumerable<PartyMemberInfo> partyMembers)
        {
            bool hasDuplicate = partyMembers
                .Where(partyMember => !string.IsNullOrWhiteSpace(partyMember.CharName))
                .GroupBy(partyMember => partyMember.CharName, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1);

            if (hasDuplicate)
            {
                ThrowInvalidArgument("Party member char_name values must be unique within the request.");
            }
        }

        private static bool IsValidationException(Exception ex)
        {
            if (ex is ArgumentException || ex is InvalidOperationException)
            {
                return true;
            }

            string exceptionName = ex.GetType().FullName;
            if (!string.Equals(exceptionName, "Npgsql.PostgresException", StringComparison.Ordinal))
            {
                return false;
            }

            string sqlState = ex.GetType().GetProperty("SqlState")?.GetValue(ex)?.ToString();
            return sqlState == "P0001" || sqlState == "23505";
        }

        private static string GetClientSafeValidationMessage(Exception ex)
        {
            string message = ex.Message;
            int detailStart = message.IndexOf("DETAIL:", StringComparison.OrdinalIgnoreCase);
            if (detailStart > 0)
            {
                message = message.Substring(0, detailStart).Trim();
            }

            return string.IsNullOrWhiteSpace(message) ? "Invalid party request." : message;
        }

        private static void ThrowInvalidArgument(string message)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, message));
        }
    }

    public class ClientInfo
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public IServerStreamWriter<PartyToSend> ServerStreamWriter { get; set; }
    }
}
