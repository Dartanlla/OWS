using Amazon.SQS;
using OWSShared.Interfaces;
using System;

namespace OWSShared.Implementations
{
    public class SQSMessagingConnection : IMessagingConnection
    {
        private readonly IAmazonSQS _sqsClient;

        public SQSMessagingConnection(IAmazonSQS sqsClient)
        {
            _sqsClient = sqsClient;
        }

        // SQS is a managed service with no persistent connection — this event never fires.
        public event EventHandler? ConnectionShutdown;

        public void Close() => _sqsClient.Dispose();
    }
}
