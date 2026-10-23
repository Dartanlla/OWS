using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChatServiceApp.Protos;
using Grpc.Core;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Grpc;

namespace OWSChat.Service
{
    public class ChatService : ChatApp.ChatAppBase
    {
        private static readonly TimeSpan MuteRecheckInterval = TimeSpan.FromSeconds(30);

        private sealed class Client
        {
            public string Name;
            public Guid? CustomerGuid;
            public Guid? UserGuid;
            public Sanction Mute;
            public DateTime MuteCheckedAt = DateTime.MinValue;
            public Channel<ServerMessage> Outbox = Channel.CreateBounded<ServerMessage>(new BoundedChannelOptions(128)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }

        private static readonly ConcurrentDictionary<IServerStreamWriter<ServerMessage>, Client> _clients
            = new ConcurrentDictionary<IServerStreamWriter<ServerMessage>, Client>();

        private readonly IUsersRepository _usersRepository;
        private readonly ISanctionsRepository _sanctionsRepository;
        private readonly ChatLogWriter _chatLog;

        public ChatService(IUsersRepository usersRepository, ISanctionsRepository sanctionsRepository, ChatLogWriter chatLog)
        {
            _usersRepository = usersRepository;
            _sanctionsRepository = sanctionsRepository;
            _chatLog = chatLog;
        }

        public override async Task Chat(
            IAsyncStreamReader<ClientMessage> requestStream,
            IServerStreamWriter<ServerMessage> responseStream,
            ServerCallContext context)
        {
            // Bound the number of concurrent chat streams so a flood of connections can't
            // exhaust memory (each stream holds a bounded outbox channel and a writer task).
            using GrpcStreamGuard.ConnectionLease lease = GrpcStreamGuard.TryAcquireConnection();
            if (lease == null)
                throw new RpcException(new Status(StatusCode.ResourceExhausted, "Chat connection limit reached."));

            // With session auth on, the stream speaks as the session's character, not the name the client sends.
            string sessionCharacterName = null;
            GetUserSession session = null;
            Guid customerGuid = Guid.Empty;
            if (GrpcSessionAuth.IsRequired)
            {
                (session, customerGuid) = await GetSession(context);
                sessionCharacterName = session?.SelectedCharacterName?.Trim();
                if (string.IsNullOrEmpty(sessionCharacterName))
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid session for chat."));
            }

            // The first frame identifies the client and is not broadcast as chat.
            if (!await requestStream.MoveNext())
                return;

            var first = requestStream.Current;
            if (string.IsNullOrWhiteSpace(first.PlayerName))
                return;

            var me = new Client
            {
                Name = sessionCharacterName ?? first.PlayerName.Trim(),
                CustomerGuid = session != null ? customerGuid : null,
                UserGuid = session?.UserGuid
            };
            _clients.TryAdd(responseStream, me);

            // Per-connection message rate limit; over-limit messages are dropped, not broadcast.
            GrpcStreamGuard.MessageRateLimiter rateLimiter = GrpcStreamGuard.CreateMessageRateLimiter();

            var writerLoop = Task.Run(async () =>
            {
                try
                {
                    await foreach (var msg in me.Outbox.Reader.ReadAllAsync(context.CancellationToken))
                        await responseStream.WriteAsync(msg);
                }
                catch
                {
                    // Client disconnected or call was cancelled.
                }
            });

            try
            {
                await foreach (var incoming in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    if (!rateLimiter.Allow())
                        continue;

                    await BroadcastFrom(incoming, me);
                }
            }
            finally
            {
                _clients.TryRemove(responseStream, out _);
                me.Outbox.Writer.TryComplete();
                await writerLoop;
            }
        }

        private async Task<(GetUserSession Session, Guid CustomerGuid)> GetSession(ServerCallContext context)
        {
            if (!GrpcSessionAuth.TryReadCredentials(context, out Guid customerGuid, out Guid userSessionGuid))
                return (null, Guid.Empty);

            var session = await _usersRepository.GetUserSession(customerGuid, userSessionGuid);
            if (session == null || !session.UserGuid.HasValue)
                return (null, Guid.Empty);

            return (session, customerGuid);
        }

        /// <summary>The sender's active mute, re-read from AccountSanctions at most every 30 s.</summary>
        private async Task<Sanction> GetActiveMute(Client sender)
        {
            if (!sender.UserGuid.HasValue || !sender.CustomerGuid.HasValue)
                return null;

            if (DateTime.UtcNow - sender.MuteCheckedAt > MuteRecheckInterval)
            {
                try
                {
                    sender.Mute = (await _sanctionsRepository.GetActive(sender.CustomerGuid.Value, sender.UserGuid.Value)).Mute;
                    sender.MuteCheckedAt = DateTime.UtcNow;
                }
                catch (Exception)
                {
                    // Keep the last known state; the next message retries.
                }
            }

            Sanction mute = sender.Mute;
            return mute != null && (!mute.ExpiresAt.HasValue || mute.ExpiresAt.Value > DateTime.UtcNow) ? mute : null;
        }

        private async Task BroadcastFrom(ClientMessage incoming, Client sender)
        {
            if (incoming.Chat == null)
                return;

            var text = incoming.Chat.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            var messageType = incoming.MesssageType;
            var recipientName = incoming.Chat.RecipientName?.Trim();

            if (messageType == ChatType.MessageTypeWhisper && string.IsNullOrWhiteSpace(recipientName))
                return;

            Sanction mute = await GetActiveMute(sender);
            LogChat(sender, messageType, recipientName, text, dropped: mute != null);
            if (mute != null)
            {
                sender.Outbox.Writer.TryWrite(new ServerMessage
                {
                    MessageType = ChatType.MessageTypeGeneral,
                    PlayerName = "[Server]",
                    Chat = new ServerMessageChat { Now = DateTime.UtcNow.ToString("O"), Text = mute.PlayerMessage() }
                });
                return;
            }

            var outgoing = new ServerMessage
            {
                MessageType = messageType,
                PlayerName = sender.Name,
                Chat = new ServerMessageChat
                {
                    Now = DateTime.UtcNow.ToString("O"),
                    Text = text
                }
            };

            foreach (var client in _clients.Values)
            {
                if (messageType == ChatType.MessageTypeWhisper)
                {
                    bool toSender = string.Equals(client.Name, sender.Name, StringComparison.Ordinal);
                    bool toTarget = string.Equals(client.Name, recipientName, StringComparison.Ordinal);

                    if (!toSender && !toTarget)
                        continue;
                }

                client.Outbox.Writer.TryWrite(outgoing);
            }
        }

        private void LogChat(Client sender, ChatType messageType, string recipientName, string text, bool dropped)
        {
            if (!sender.CustomerGuid.HasValue)
                return;

            _chatLog.Enqueue(new ChatLogEntry
            {
                CustomerGUID = sender.CustomerGuid.Value,
                CreateDate = DateTime.UtcNow,
                Channel = messageType switch
                {
                    ChatType.MessageTypeTrade => "trade",
                    ChatType.MessageTypeGuild => "guild",
                    ChatType.MessageTypeParty => "party",
                    ChatType.MessageTypeWhisper => "whisper",
                    _ => "general"
                },
                SenderName = sender.Name,
                SenderUserGUID = sender.UserGuid,
                TargetName = messageType == ChatType.MessageTypeWhisper ? recipientName : null,
                Message = text.Length > 2000 ? text.Substring(0, 2000) : text,
                Dropped = dropped
            });
        }
    }
}
