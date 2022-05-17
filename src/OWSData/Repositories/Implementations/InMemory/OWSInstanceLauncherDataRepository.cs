using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Repositories.Implementations.InMemory
{
    public class OWSInstanceLauncherDataRepository : IOWSInstanceLauncherDataRepository
    {
        private int worldServerID;
        private Guid launcherGuid;

        public void SetWorldServerID(int worldServerID)
        {
            if (worldServerID > 0)
            {
                this.worldServerID = worldServerID;
            }
        }

        public int GetWorldServerID()
        {
            return worldServerID;
        }

        public void SetLauncherGuid(Guid launcherGuid)
        {
            this.launcherGuid = launcherGuid;
        }

        public Guid GetLauncherGuid()
        {
            return launcherGuid;
        }
    }
}
