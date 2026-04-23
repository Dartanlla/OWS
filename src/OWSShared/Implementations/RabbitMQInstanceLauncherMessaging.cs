using Microsoft.Extensions.Options;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.Options;
using RabbitMQ.Client;
using System;

namespace OWSShared.Implementations
{
    public class RabbitMQInstanceLauncherMessaging : IInstanceLauncherMessaging
    {
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
        private IConnection? _connection;
        private IMessagingConnection? _messagingConnection;

        public RabbitMQInstanceLauncherMessaging(IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _rabbitMQOptions = rabbitMQOptions;
        }

        public void PublishSpinUpServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID, string zoneName, int port)
        {
            using var connection = CreateConnectionFactory().CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: "ows.serverspinup", type: "direct", durable: false, autoDelete: false);

            var message = new MQSpinUpServerMessage
            {
                CustomerGUID = customerGUID,
                WorldServerID = worldServerID,
                ZoneInstanceID = zoneInstanceID,
                MapName = zoneName,
                Port = port
            };

            channel.BasicPublish(
                exchange: "ows.serverspinup",
                routingKey: $"ows.serverspinup.{worldServerID}",
                basicProperties: null,
                body: message.Serialize());
        }

        public void PublishShutDownServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID)
        {
            using var connection = CreateConnectionFactory().CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: "ows.servershutdown", type: "direct", durable: false, autoDelete: false);

            var message = new MQShutDownServerMessage
            {
                CustomerGUID = customerGUID,
                ZoneInstanceID = zoneInstanceID
            };

            channel.BasicPublish(
                exchange: "ows.servershutdown",
                routingKey: $"ows.servershutdown.{worldServerID}",
                basicProperties: null,
                body: message.Serialize());
        }

        public void InitializeConnection()
        {
            _connection = CreateConnectionFactory(dispatchConsumersAsync: true).CreateConnection();
            _messagingConnection = new RabbitMQMessagingConnection(_connection);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeSpinUpChannel(int worldServerId)
        {
            var channel = _connection!.CreateModel();
            string queueName = Guid.NewGuid().ToString();

            channel.ExchangeDeclare(exchange: "ows.serverspinup", type: "direct", durable: false, autoDelete: false);
            channel.QueueDeclare(queue: queueName, durable: false, exclusive: true, autoDelete: true, arguments: null);
            channel.QueueBind(queueName, "ows.serverspinup", $"ows.serverspinup.{worldServerId}");
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            return (new RabbitMQMessagingChannel(channel), queueName);
        }

        public (IMessagingChannel Channel, string QueueName) InitializeShutDownChannel(int worldServerId)
        {
            var channel = _connection!.CreateModel();
            string queueName = Guid.NewGuid().ToString();

            channel.ExchangeDeclare(exchange: "ows.servershutdown", type: "direct", durable: false, autoDelete: false);
            channel.QueueDeclare(queue: queueName, durable: false, exclusive: true, autoDelete: true, arguments: null);
            channel.QueueBind(queueName, "ows.servershutdown", $"ows.servershutdown.{worldServerId}");
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            return (new RabbitMQMessagingChannel(channel), queueName);
        }

        public IMessagingConnection GetConnection() => _messagingConnection!;

        private ConnectionFactory CreateConnectionFactory(bool dispatchConsumersAsync = false) => new()
        {
            HostName = _rabbitMQOptions.Value.RabbitMQHostName,
            Port = _rabbitMQOptions.Value.RabbitMQPort,
            UserName = _rabbitMQOptions.Value.RabbitMQUserName,
            Password = _rabbitMQOptions.Value.RabbitMQPassword,
            DispatchConsumersAsync = dispatchConsumersAsync
        };
    }
}
