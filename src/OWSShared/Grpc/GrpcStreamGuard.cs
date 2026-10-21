using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Grpc.Core;

namespace OWSShared.Grpc
{
    /// <summary>
    /// Process-wide guard rails for public gRPC stream services (Chat, Party). Bounds the number
    /// of concurrent connections and provides a per-connection message rate limiter so a single
    /// client cannot exhaust memory/CPU or flood broadcasts. Configured via environment so it
    /// applies uniformly without per-service options plumbing.
    /// </summary>
    public static class GrpcStreamGuard
    {
        private static int _currentConnections;

        private static int MaxConnections => EnvInt("OWS_GRPC_MAX_CONNECTIONS", 5000);
        private static int MessagesPerWindow => EnvInt("OWS_GRPC_MSGS_PER_WINDOW", 20);
        private static int RateWindowSeconds => EnvInt("OWS_GRPC_RATE_WINDOW_SECONDS", 5);

        /// <summary>
        /// Attempts to reserve a connection slot. Returns null when the cap is reached; otherwise
        /// returns a lease that must be disposed when the stream ends to release the slot.
        /// </summary>
        public static ConnectionLease TryAcquireConnection()
        {
            if (Interlocked.Increment(ref _currentConnections) > MaxConnections)
            {
                Interlocked.Decrement(ref _currentConnections);
                return null;
            }

            return new ConnectionLease();
        }

        public static MessageRateLimiter CreateMessageRateLimiter()
            => new MessageRateLimiter(MessagesPerWindow, TimeSpan.FromSeconds(RateWindowSeconds));

        private static int EnvInt(string name, int fallback)
        {
            string raw = Environment.GetEnvironmentVariable(name);
            return int.TryParse(raw, out int value) && value > 0 ? value : fallback;
        }

        public sealed class ConnectionLease : IDisposable
        {
            private int _released;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    Interlocked.Decrement(ref _currentConnections);
                }
            }
        }

        /// <summary>
        /// Sliding-window rate limiter for one connection. Not shared between connections, so a
        /// simple lock is cheap. Allow() returns false when the window is full; callers drop the
        /// offending message rather than tearing down the stream.
        /// </summary>
        public sealed class MessageRateLimiter
        {
            private readonly int _capacity;
            private readonly long _windowTicks;
            private readonly Queue<long> _timestamps = new();

            public MessageRateLimiter(int capacity, TimeSpan window)
            {
                _capacity = capacity;
                _windowTicks = window.Ticks;
            }

            public bool Allow()
            {
                long now = DateTime.UtcNow.Ticks;
                long cutoff = now - _windowTicks;

                lock (_timestamps)
                {
                    while (_timestamps.Count > 0 && _timestamps.Peek() < cutoff)
                    {
                        _timestamps.Dequeue();
                    }

                    if (_timestamps.Count >= _capacity)
                    {
                        return false;
                    }

                    _timestamps.Enqueue(now);
                    return true;
                }
            }
        }
    }

    /// <summary>
    /// Reads the session credentials a client sends as gRPC call metadata. Enforcement is gated by
    /// OWS_GRPC_REQUIRE_SESSION_AUTH (default false). When enabled, callers must supply "customerguid"
    /// and "usersessionguid" metadata, and each service checks that they resolve to a live session
    /// bound to the stream's character; a client that sends no session loses the stream.
    /// </summary>
    public static class GrpcSessionAuth
    {
        public const string CustomerGuidHeader = "customerguid";
        public const string UserSessionGuidHeader = "usersessionguid";

        public static bool IsRequired =>
            string.Equals(Environment.GetEnvironmentVariable("OWS_GRPC_REQUIRE_SESSION_AUTH"), "true", StringComparison.OrdinalIgnoreCase);

        public static bool TryReadCredentials(ServerCallContext context, out Guid customerGuid, out Guid userSessionGuid)
        {
            customerGuid = ReadGuid(context, CustomerGuidHeader);
            userSessionGuid = ReadGuid(context, UserSessionGuidHeader);
            return customerGuid != Guid.Empty && userSessionGuid != Guid.Empty;
        }

        private static Guid ReadGuid(ServerCallContext context, string headerName)
        {
            Metadata.Entry entry = context.RequestHeaders?
                .FirstOrDefault(h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase));

            return entry != null && Guid.TryParse(entry.Value, out Guid value) ? value : Guid.Empty;
        }
    }
}
