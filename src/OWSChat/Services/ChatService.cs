using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatServiceApp.Protos;
using OWSShared.Interfaces;

namespace OWSChat.Service
{
    public class ChatService : ChatApp.ChatAppBase
    {
        
        public static List<ClientInfo> _clients = new List<ClientInfo>();

        private readonly IHeaderCustomerGUID _customerGuid;

        public ChatService(IHeaderCustomerGUID customerGuid)
        {
            _customerGuid = customerGuid;
        }

        public override Task RegisterMsg(LoggingMessage request, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
        {
            string id = request.UserId;
            string userName = request.UserName;
            _clients.Add(new ClientInfo
            {
                Id = id,
                UserName = userName,
                serverStreamWriter = responseStream
            });
            Func<bool> isCancelled = () => context.CancellationToken.IsCancellationRequested;
            SpinWait.SpinUntil(isCancelled);
            _clients.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        public override async Task<EmptyMsg> SendMsg(ClientMessage clientMsg, ServerCallContext context)
        {

            ServerMessage serverMessage = new ServerMessage
            {
                MessageType = clientMsg.MesssageType,
                PlayerName = clientMsg.PlayerName,
                Chat = new ServerMessageChat
                {
                    Now = DateTime.UtcNow.ToString(),
                    Text = clientMsg.Chat.Text,
                }
            };
            var tasks = new List<Task>();
            foreach (var client in _clients)
            {
                if (client == null || client == default)
                {
                    continue;
                }
                    switch (clientMsg.MesssageType)
                {
                    case ChatType.MessageTypeWhisper:
                        if (clientMsg.Chat.RecipientName == client.UserName || client.UserName == clientMsg.PlayerName)
                        {
                            tasks.Add(client.serverStreamWriter.WriteAsync(serverMessage));
                        }
                        break;
                    case ChatType.MessageTypeParty:
                        tasks.Add(client.serverStreamWriter.WriteAsync(serverMessage));
                        break;
                    case ChatType.MessageTypeGuild:
                        tasks.Add(client.serverStreamWriter.WriteAsync(serverMessage));
                        break;
                    default:
                        tasks.Add(client.serverStreamWriter.WriteAsync(serverMessage));
                        break;
                }
            }
            await Task.WhenAll(tasks);
            return new EmptyMsg();
        }
    }

    public class ClientInfo
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public IServerStreamWriter<ServerMessage> serverStreamWriter { get; set; }
    }

}