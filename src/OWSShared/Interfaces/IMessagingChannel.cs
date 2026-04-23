using System;
using System.Threading.Tasks;

namespace OWSShared.Interfaces
{
    public interface IMessagingChannel
    {
        void StartConsuming(string queueName, Func<byte[], Task> messageHandler);
        void Close();
    }
}
