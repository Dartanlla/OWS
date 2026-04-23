namespace OWSShared.Options
{
    public class SQSOptions
    {
        public const string SectionName = "SQSOptions";
        public string AWSRegion { get; set; } = "us-east-1";
        public string QueueName { get; set; } = "ows-instance-launcher-messaging";
    }
}
