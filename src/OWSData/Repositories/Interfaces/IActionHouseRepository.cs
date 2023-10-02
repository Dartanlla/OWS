using OWSData.Models.Composites;
using OWSData.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Repositories.Interfaces
{
    public interface IActionHouseRepository
    {
        Task<ActionHousePlayerContainer> GetActionHousePlayerItems(Guid customerGUID, string characterName);

        Task GetActionHouseItemSearch(Guid customerGUID, string ItemSearch);



    }
}
