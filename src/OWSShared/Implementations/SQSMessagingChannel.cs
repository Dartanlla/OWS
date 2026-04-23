using Amazon.SQS;
using Amazon.SQS.Model;
using OWSShared.Interfaces;
using OWSShared.Messages;
using Serilog;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OWSShared.Implementations
{
    public class SQSMessagingChannel : IMessagingChannel
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;
        private readonly Func<SQSInstanceLauncherMessage, bool> _filter;
        private CancellationTokenSource? _cts;

        public SQSMessagingChannel(IAmazonSQS sqsClient, string queueUrl, Func<SQSInstanceLauncherMessage, bool> filter)
        {
            _sqsClient = sqsClient;
            _queueUrl = queueUrl;
            _filter = filter;
        }

        // queueName parameter is unused — queue URL is fixed at construction time.
        public void StartConsuming(string queueName, Func<byte[], Task> messageHandler)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            MaxNumberOfMessages = 1,
                            WaitTimeSeconds = 20
                        }, token);

                        foreach (var sqsMessage in response.Messages)
                        {
                            var wrapper = JsonSerializer.Deserialize<SQSInstanceLauncherMessage>(sqsMessage.Body);

                            if (wrapper != null && _filter(wrapper))
                            {
                                await messageHandler(Encoding.UTF8.GetBytes(wrapper.Payload));
                                await _sqsClient.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle, token);
                            }
                            else
                            {
                                // Not for this launcher — make it immediately visible to other consumers.
                                await _sqsClient.ChangeMessageVisibilityAsync(_queueUrl, sqsMessage.ReceiptHandle, 0, token);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"SQS polling error on {_queueUrl}: {ex.Message}");
                        await Task.Delay(5000, token);
                    }
                }
            }, token);
        }

        public void Close() => _cts?.Cancel();
    }
}
