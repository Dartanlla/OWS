namespace OWSShared.Options
{
    public class StorageOptions
    {
        public const string SectionName = "OWSStorageConfig";
        public string OWSDBBackend { get; set; }
        public string OWSDBConnectionString { get; set; }
    }
}
