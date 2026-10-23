using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class UpdateCharacterInventory
    {

        public string ItemIDTag { get; set; }

        public int Quantity { get; set; }

        public int InSlotNumber { get; set; }

        public string CustomData { get; set; }

        // UE sends its FGuid as 32 digits; an empty or all-zero id is stored as NULL.
        public string InstanceId { get; set; }

        public Guid? ParsedInstanceId => Guid.TryParse(InstanceId, out Guid id) && id != Guid.Empty ? id : null;
    }
}
