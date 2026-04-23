namespace OWSShared.Messages
{
    public class SQSInstanceLauncherMessage
    {
        public string MessageType { get; set; } = string.Empty;
        public int WorldServerID { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}
