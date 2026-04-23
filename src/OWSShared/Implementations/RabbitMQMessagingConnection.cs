using OWSShared.Interfaces;
using RabbitMQ.Client;
using System;

namespace OWSShared.Implementations
{
    public class RabbitMQMessagingConnection : IMessagingConnection
    {
        private readonly IConnection _connection;
        private event EventHandler? _connectionShutdown;

        public RabbitMQMessagingConnection(IConnection connection)
        {
            _connection = connection;
            _connection.ConnectionShutdown += (s, e) => _connectionShutdown?.Invoke(s, EventArgs.Empty);
        }

        public event EventHandler? ConnectionShutdown
        {
            add => _connectionShutdown += value;
            remove => _connectionShutdown -= value;
        }

        public void Close() => _connection?.Close();
    }
}
