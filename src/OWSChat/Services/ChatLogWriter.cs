using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using Serilog;

namespace OWSChat.Service
{
    /// <summary>
    /// Writes ChatLog off the send path: messages queue in memory and go to the database in batches every 2 s. A full
    /// queue drops the oldest lines rather than slowing chat down.
    /// </summary>
    public sealed class ChatLogWriter
    {
        private const int BatchSize = 500;

        private readonly Channel<ChatLogEntry> _queue = Channel.CreateBounded<ChatLogEntry>(new BoundedChannelOptions(10000)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        private readonly IChatLogRepository _repository;

        public ChatLogWriter(IChatLogRepository repository)
        {
            _repository = repository;
            _ = Task.Run(RunAsync);
        }

        public void Enqueue(ChatLogEntry entry)
        {
            _queue.Writer.TryWrite(entry);
        }

        private async Task RunAsync()
        {
            var batch = new List<ChatLogEntry>(BatchSize);
            while (await _queue.Reader.WaitToReadAsync())
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                while (batch.Count < BatchSize && _queue.Reader.TryRead(out ChatLogEntry entry))
                {
                    batch.Add(entry);
                }

                try
                {
                    await _repository.AddBatch(batch);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "chat_log_write_failed {Count}", batch.Count);
                }
                batch.Clear();
            }
        }
    }
}
