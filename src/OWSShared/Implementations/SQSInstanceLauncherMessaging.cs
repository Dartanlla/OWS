using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.Options;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Options;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class SQSInstanceLauncherMessaging : IInstanceLauncherMessaging
    {
        private const string SpinUpMessageType = "SpinUp";
        private const string ShutDownMessageType = "ShutDown";

        private readonly IOptions<SQSOptions> _sqsOptions;
        private IAmazonSQS? _sqsClient;
        private SQSMessagingConnection? _messagingConnection;

        public SQSInstanceLauncherMessaging(IOptions<SQSOptions> sqsOptions)
        {
            _sqsOptions = sqsOptions;
        }

        public void PublishSpinUpServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID, string zoneName, int port)
        {
            using var client = CreateSQSClient();
            string queueUrl = GetQueueUrlAsync(client, _sqsOptions.Value.QueueName).GetAwaiter().GetResult();

            var inner = new MQSpinUpServerMessage
            {
                CustomerGUID = customerGUID,
                WorldServerID = worldServerID,
                ZoneInstanceID = zoneInstanceID,
                MapName = zoneName,
                Port = port
            };

            var wrapper = new SQSInstanceLauncherMessage
            {
                MessageType = SpinUpMessageType,
                WorldServerID = worldServerID,
                Payload = Encoding.UTF8.GetString(inner.Serialize())
            };

            client.SendMessageAsync(queueUrl, JsonSerializer.Serialize(wrapper)).GetAwaiter().GetResult();
        }

        public void PublishShutDownServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID)
        {
            using var client = CreateSQSClient();
            string queueUrl = GetQueueUrlAsync(client, _sqsOptions.Value.QueueName).GetAwaiter().GetResult();

            var inner = new MQShutDownServerMessage
            {
                CustomerGUID = customerGUID,
                WorldServerID = worldServerID,
                ZoneInstanceID = zoneInstanceID
            };

            var wrapper = new SQSInstanceLauncherMessage
            {
                MessageType = ShutDownMessageType,
                WorldServerID = worldServerID,
                Payload = Encoding.UTF8.GetString(inner.Serialize())
            };

            client.SendMessageAsync(queueUrl, JsonSerializer.Serialize(wrapper)).GetAwaiter().GetResult();
        }

        public void InitializeConnection()
        {
            _sqsClient = CreateSQSClient();
            _messagingConnection = new SQSMessagingConnection(_sqsClient);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeSpinUpChannel(int worldServerId)
        {
            string queueUrl = GetQueueUrlAsync(_sqsClient!, _sqsOptions.Value.QueueName).GetAwaiter().GetResult();
            var channel = new SQSMessagingChannel(_sqsClient!, queueUrl,
                msg => msg.MessageType == SpinUpMessageType && msg.WorldServerID == worldServerId);
            return (channel, queueUrl);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeShutDownChannel(int worldServerId)
        {
            string queueUrl = GetQueueUrlAsync(_sqsClient!, _sqsOptions.Value.QueueName).GetAwaiter().GetResult();
            var channel = new SQSMessagingChannel(_sqsClient!, queueUrl,
                msg => msg.MessageType == ShutDownMessageType && msg.WorldServerID == worldServerId);
            return (channel, queueUrl);
        }

        public IMessagingConnection GetConnection() => _messagingConnection!;

        private IAmazonSQS CreateSQSClient() =>
            new AmazonSQSClient(RegionEndpoint.GetBySystemName(_sqsOptions.Value.AWSRegion));

        private static async Task<string> GetQueueUrlAsync(IAmazonSQS client, string queueName)
        {
            var response = await client.GetQueueUrlAsync(queueName);
            return response.QueueUrl;
        }
    }
}
