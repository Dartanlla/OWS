using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.Options
{
    public class ManagementOptions
    {
        public const string SectionName = "OWSManagementOptions";
        public string OWSAPIKey { get; set; }

        /// <summary>The customer the console manages. Empty: the only Customers row.</summary>
        public string CustomerGUID { get; set; }

        /// <summary>Where the hourly pg_dump files are mounted; the System page shows the newest.</summary>
        public string BackupDir { get; set; } = "/backups";

        public string GrafanaUrl { get; set; }
        public string SentryUrl { get; set; }

        /// <summary>A juri change at least this large shows as an Overview signal.</summary>
        public long LargeJuriChange { get; set; } = 100000;
    }
}
