namespace OWSShared.Options
{
    public class ContentCacheOptions
    {
        public const string SectionName = "ContentCacheConfig";
        public string Endpoint { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
    }
}
