using System;

namespace OWSShared.Interfaces
{
    public interface IMessagingConnection
    {
        event EventHandler? ConnectionShutdown;
        void Close();
    }
}
