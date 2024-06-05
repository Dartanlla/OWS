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

        public ZoneServerProcessesRepository()
        {
            zoneServerProcesses = new List<ZoneServerProcess>();
        }

        public void AddZoneServerProcess(ZoneServerProcess zoneServerProcess)
        {
            zoneServerProcesses.Add(zoneServerProcess);
        }

        public List<ZoneServerProcess> GetZoneServerProcesses()
        {
            return zoneServerProcesses;
        }

        //Returns the processId.  Returns -1 if not found.
        public int FindZoneServerProcessId(int zoneInstanceId)
        {
            var foundZoneServerProcess = zoneServerProcesses.Find(item => item.ZoneInstanceId == zoneInstanceId);

            if (foundZoneServerProcess == null)
            {
                return -1;
            }

            return (foundZoneServerProcess.ProcessId > 0 ? foundZoneServerProcess.ProcessId : -1);
        }
    }
}
