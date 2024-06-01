using OWSData.Models;
using System;
using System.Collections.Generic;
using System.Text;
using OWSShared.Options;

namespace OWSData.Repositories.Interfaces
{
    public interface IZoneServerProcessesRepository
    {
        void AddZoneServerProcess(ZoneServerProcess zoneServerProcess);
        List<ZoneServerProcess> GetZoneServerProcesses();
        int FindZoneServerProcessId(int zoneInstanceId);
    }
}
