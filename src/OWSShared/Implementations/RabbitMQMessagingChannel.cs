using OWSShared.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class RabbitMQMessagingChannel : IMessagingChannel
    {
        private readonly IModel _channel;

        public RabbitMQMessagingChannel(IModel channel)
        {
            _channel = channel;
        }

        public void StartConsuming(string queueName, Func<byte[], Task> messageHandler)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) => await messageHandler(ea.Body.ToArray());
            _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }

        public void Close() => _channel?.Close();
    }
}
