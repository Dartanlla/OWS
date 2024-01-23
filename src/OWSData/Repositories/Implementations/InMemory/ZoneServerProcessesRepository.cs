using OWSData.Models;
using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.InMemory
{
    public class ZoneServerProcessesRepository : IZoneServerProcessesRepository
    {
        private List<ZoneServerProcess> zoneServerProcesses;

        public ZoneServerProcessesRepository() => zoneServerProcesses = [];

        public void AddZoneServerProcess(ZoneServerProcess zoneServerProcess) => zoneServerProcesses.Add(zoneServerProcess);

        public List<ZoneServerProcess> GetZoneServerProcesses() => zoneServerProcesses;

        //Returns the processId.  Returns -1 if not found.
        public int FindZoneServerProcessId(int zoneInstanceId) => zoneServerProcesses.Find(item => item.ZoneInstanceId == zoneInstanceId) != null ? zoneServerProcesses.Find(item => item.ZoneInstanceId == zoneInstanceId).ProcessId > 0 ? zoneServerProcesses.Find(item => item.ZoneInstanceId == zoneInstanceId).ProcessId : -1 : -1;
    }
}
