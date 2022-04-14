using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Repositories.Implementations.InMemory
{
    public class OWSInstanceLauncherDataRepository : IOWSInstanceLauncherDataRepository
    {
        private int worldServerID;
        private Guid launcherGUID;

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

        public void SetLauncherGuid(Guid launcherGUID)
        {

            this.launcherGUID = launcherGUID;
     
        }

        public Guid GetLauncherGUID()
        {
            return launcherGUID;
        }
    }
}
