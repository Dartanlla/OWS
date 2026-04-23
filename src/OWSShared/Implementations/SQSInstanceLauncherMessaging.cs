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

        private readonly IAmazonSQS _sqsClient;
        private readonly IOptions<SQSOptions> _sqsOptions;
        private SQSMessagingConnection? _messagingConnection;

        public SQSInstanceLauncherMessaging(IAmazonSQS sqsClient, IOptions<SQSOptions> sqsOptions)
        {
            _sqsClient = sqsClient;
            _sqsOptions = sqsOptions;
        }

        public void PublishSpinUpServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID, string zoneName, int port)
        {
            try
            {
                string queueUrl = GetQueueUrlAsync(_sqsOptions.Value.QueueName).GetAwaiter().GetResult();

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

                _sqsClient.SendMessageAsync(queueUrl, JsonSerializer.Serialize(wrapper)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("PublishSpinUpServerMessage Failed: " + ex.Message);
            }
        }

        public void PublishShutDownServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID)
        {
            try
            {
                string queueUrl = GetQueueUrlAsync(_sqsOptions.Value.QueueName).GetAwaiter().GetResult();

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

                _sqsClient.SendMessageAsync(queueUrl, JsonSerializer.Serialize(wrapper)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("PublishShutDownServerMessage Failed: " + ex.Message);
            }
        }

        public void InitializeConnection()
        {
            _messagingConnection = new SQSMessagingConnection(_sqsClient);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeSpinUpChannel(int worldServerId)
        {
            string queueUrl = GetQueueUrlAsync(_sqsOptions.Value.QueueName).GetAwaiter().GetResult();
            var channel = new SQSMessagingChannel(_sqsClient, queueUrl,
                msg => msg.MessageType == SpinUpMessageType && msg.WorldServerID == worldServerId);
            return (channel, queueUrl);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeShutDownChannel(int worldServerId)
        {
            string queueUrl = GetQueueUrlAsync(_sqsOptions.Value.QueueName).GetAwaiter().GetResult();
            var channel = new SQSMessagingChannel(_sqsClient, queueUrl,
                msg => msg.MessageType == ShutDownMessageType && msg.WorldServerID == worldServerId);
            return (channel, queueUrl);
        }

        public IMessagingConnection GetConnection() => _messagingConnection!;

        private async Task<string> GetQueueUrlAsync(string queueName)
        {
            var response = await _sqsClient.GetQueueUrlAsync(queueName);
            return response.QueueUrl;
        }
    }
}
