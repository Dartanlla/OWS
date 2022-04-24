using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models
{
    public class StorageOptions
    {
        public const string SectionName = "OWSStorageConfig";
        public string OWSDBBackend { get; set; }
        public string OWSDBConnectionString { get; set; }
    }
}
