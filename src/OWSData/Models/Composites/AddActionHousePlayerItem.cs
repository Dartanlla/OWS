using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Models.Composites
{
    public class AddActionHousePlayerItem
    {
        public AddActionHousePlayerItem() { }

        public string CharacterName { get; set; }

        public int SlotIndex { get; set; }

        public string ItemId { get; set; }

        public int InProgressQuantity { get; set; }

        public int TotalQuantity { get; set; }

        public int SetPrice { get; set; }

        public int TotalItemQuantityInStorage { get; set; }

        public int TotalCurrencyInStorage { get; set; }

        public int ActionHouseActionID { get; set; }
    }
}
