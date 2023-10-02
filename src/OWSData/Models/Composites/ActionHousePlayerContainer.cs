using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Models.Composites
{
    public class ActionHousePlayerContainer
    {
        public ActionHousePlayerContainer() { }

        public string SuccessMessage { get; set; }

        public string ErrorMessage { get; set; }


        public IEnumerable<ActionHousePlayerItem> Items { get; set; }

    }

    public class ActionHousePlayerItem
    {
        ActionHousePlayerItem()
        {

        }
        public int SlotIndex { get; set; }

        public string ItemIDTag { get; set; }

        public int InProgressQuantity { get; set; }

        public int TotalQuantity { get; set; }

        public int SetPrice { get; set; }

        public int TotalItemQuantityInStorage { get; set; }

        public int TotalCurrencyInStorage { get; set; }

        public int ActionHouseActionID { get; set; }
    }
}
