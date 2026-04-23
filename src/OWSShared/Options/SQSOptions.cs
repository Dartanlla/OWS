namespace OWSShared.Options
{
    public class SQSOptions
    {
        public const string SectionName = "SQSOptions";
        public string AWSRegion { get; set; } = "us-east-1";
        public string SpinUpQueueNamePrefix { get; set; } = "ows-serverspinup";
        public string ShutDownQueueNamePrefix { get; set; } = "ows-servershutdown";
    }
}
