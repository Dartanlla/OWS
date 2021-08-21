using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class CheckMapInstanceStatus
    {
        public int MapInstanceStatus { get; set; }

        public CheckMapInstanceStatus()
        {
            this.MapInstanceStatus = 0;
        }

        public CheckMapInstanceStatus(int MapInstanceStatus)
        {
            this.MapInstanceStatus = MapInstanceStatus;
        }
    }
}
