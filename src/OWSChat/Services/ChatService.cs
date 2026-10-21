using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChatServiceApp.Protos;
using Grpc.Core;
using OWSData.Repositories.Interfaces;
using OWSShared.Grpc;

namespace OWSChat.Service
{
    public class ChatService : ChatApp.ChatAppBase
    {
        private sealed class Client
        {
            public string Name;
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

        public ChatService(IUsersRepository usersRepository)
        {
            _usersRepository = usersRepository;
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
            if (GrpcSessionAuth.IsRequired)
            {
                sessionCharacterName = await GetSessionCharacterName(context);
                if (sessionCharacterName == null)
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid session for chat."));
            }

            // The first frame identifies the client and is not broadcast as chat.
            if (!await requestStream.MoveNext())
                return;

            var first = requestStream.Current;
            if (string.IsNullOrWhiteSpace(first.PlayerName))
                return;

            var me = new Client { Name = sessionCharacterName ?? first.PlayerName.Trim() };
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

        private async Task<string> GetSessionCharacterName(ServerCallContext context)
        {
            if (!GrpcSessionAuth.TryReadCredentials(context, out Guid customerGuid, out Guid userSessionGuid))
                return null;

            var session = await _usersRepository.GetUserSession(customerGuid, userSessionGuid);
            if (session == null || !session.UserGuid.HasValue)
                return null;

            string name = session.SelectedCharacterName?.Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }

        private Task BroadcastFrom(ClientMessage incoming, Client sender)
        {
            if (incoming.Chat == null)
                return Task.CompletedTask;

            var text = incoming.Chat.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return Task.CompletedTask;

            var messageType = incoming.MesssageType;
            var recipientName = incoming.Chat.RecipientName?.Trim();

            if (messageType == ChatType.MessageTypeWhisper && string.IsNullOrWhiteSpace(recipientName))
                return Task.CompletedTask;

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

            return Task.CompletedTask;
        }
    }
}
