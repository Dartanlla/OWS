using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.Options;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Options;
using System;
using System.Text;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class SQSInstanceLauncherMessaging : IInstanceLauncherMessaging
    {
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
            string queueUrl = GetQueueUrlAsync(client, $"{_sqsOptions.Value.SpinUpQueueNamePrefix}-{worldServerID}").GetAwaiter().GetResult();

            var message = new MQSpinUpServerMessage
            {
                CustomerGUID = customerGUID,
                WorldServerID = worldServerID,
                ZoneInstanceID = zoneInstanceID,
                MapName = zoneName,
                Port = port
            };

            client.SendMessageAsync(queueUrl, Encoding.UTF8.GetString(message.Serialize())).GetAwaiter().GetResult();
        }

        public void PublishShutDownServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID)
        {
            using var client = CreateSQSClient();
            string queueUrl = GetQueueUrlAsync(client, $"{_sqsOptions.Value.ShutDownQueueNamePrefix}-{worldServerID}").GetAwaiter().GetResult();

            var message = new MQShutDownServerMessage
            {
                CustomerGUID = customerGUID,
                WorldServerID = worldServerID,
                ZoneInstanceID = zoneInstanceID
            };

            client.SendMessageAsync(queueUrl, Encoding.UTF8.GetString(message.Serialize())).GetAwaiter().GetResult();
        }

        public void InitializeConnection()
        {
            _sqsClient = CreateSQSClient();
            _messagingConnection = new SQSMessagingConnection(_sqsClient);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeSpinUpChannel(int worldServerId)
        {
            string queueUrl = GetQueueUrlAsync(_sqsClient!, $"{_sqsOptions.Value.SpinUpQueueNamePrefix}-{worldServerId}").GetAwaiter().GetResult();
            return (new SQSMessagingChannel(_sqsClient!), queueUrl);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeShutDownChannel(int worldServerId)
        {
            string queueUrl = GetQueueUrlAsync(_sqsClient!, $"{_sqsOptions.Value.ShutDownQueueNamePrefix}-{worldServerId}").GetAwaiter().GetResult();
            return (new SQSMessagingChannel(_sqsClient!), queueUrl);
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
