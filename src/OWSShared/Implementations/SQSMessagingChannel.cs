using Amazon.SQS;
using Amazon.SQS.Model;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class SQSMessagingChannel : IMessagingChannel
    {
        private readonly IAmazonSQS _sqsClient;
        private CancellationTokenSource? _cts;

        public SQSMessagingChannel(IAmazonSQS sqsClient)
        {
            _sqsClient = sqsClient;
        }

        public void StartConsuming(string queueUrl, Func<byte[], Task> messageHandler)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var receiveRequest = new ReceiveMessageRequest
                        {
                            QueueUrl = queueUrl,
                            MaxNumberOfMessages = 1,
                            WaitTimeSeconds = 20
                        };

                        var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, token);

                        foreach (var message in response.Messages)
                        {
                            await messageHandler(Encoding.UTF8.GetBytes(message.Body));
                            await _sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"SQS polling error on {queueUrl}: {ex.Message}");
                        await Task.Delay(5000, token);
                    }
                }
            }, token);
        }

        public void Close() => _cts?.Cancel();
    }
}
