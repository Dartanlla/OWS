using System;

namespace OWSData.Models.Tables
{
    public sealed class EdgeServers
    {
        private DateTime updatedAtUtc;

        public Guid CustomerGuid { get; set; }
        public int EdgeServerId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAtUtc
        {
            get => updatedAtUtc;
            set => updatedAtUtc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
