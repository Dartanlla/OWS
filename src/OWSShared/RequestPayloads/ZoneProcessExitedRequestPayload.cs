namespace OWSShared.RequestPayloads
{
    public class ZoneProcessExitedRequestPayload
    {
        public int ZoneInstanceID { get; set; }
        public int ExitCode { get; set; }
        public string MapName { get; set; }
        public int Port { get; set; }
    }
}
