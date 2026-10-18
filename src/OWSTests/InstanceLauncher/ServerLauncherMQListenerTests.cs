using OWSInstanceLauncher.Services;
using Xunit;

namespace OWSTests.InstanceLauncher
{
    public class ServerLauncherMQListenerTests
    {
        [Fact]
        public void ExpandCustomFlags_ReplacesZoneInstanceAndPortTokens()
        {
            string result = ServerLauncherMQListener.ExpandCustomFlags(
                " -AbsLog=\"C:/OWS/Logs/UEServer-{zoneinstanceid}-{port}.log\" -Tag={zoneinstanceid} ", 42, 7779);

            Assert.Equal("-AbsLog=\"C:/OWS/Logs/UEServer-42-7779.log\" -Tag=42", result);
        }

        [Fact]
        public void ExpandCustomFlags_LeavesOtherBracesUnchanged()
        {
            string result = ServerLauncherMQListener.ExpandCustomFlags("-Path=C:/{0}/{ZoneInstanceId}", 42, 7779);

            Assert.Equal("-Path=C:/{0}/{ZoneInstanceId}", result);
        }
    }
}
