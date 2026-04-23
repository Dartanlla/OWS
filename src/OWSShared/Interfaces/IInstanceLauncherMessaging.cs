using RabbitMQ.Client;
using System;

namespace OWSShared.Interfaces
{
    public interface IInstanceLauncherMessaging
    {
        void PublishSpinUpServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID, string zoneName, int port);
        void PublishShutDownServerMessage(Guid customerGUID, int worldServerID, int zoneInstanceID);

        void InitializeConnection();
        (IModel Channel, string QueueName) InitializeSpinUpChannel(int worldServerId);
        (IModel Channel, string QueueName) InitializeShutDownChannel(int worldServerId);
        IConnection GetConnection();
    }
}
