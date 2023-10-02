using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSActionHouse.Request.ActionHouse
{
    public class GetActionHousePlayerItemsRequest
    {
        public string CharacterName { get; set; }

        private Guid _customerGUID;
        private IActionHouseRepository _actionHouseRepository;

        private ActionHousePlayerContainer _actionHousePlayerContainer;

        public void SetData(IHeaderCustomerGUID customerGuid, IActionHouseRepository actionHouseRepository)
        {
            _customerGUID = customerGuid.CustomerGUID;
            _actionHouseRepository = actionHouseRepository;
        }

        public async Task<IActionResult> Handle()
        {
            _actionHousePlayerContainer = await _actionHouseRepository.GetActionHousePlayerItems(_customerGUID, CharacterName);

            
            return new OkObjectResult(_actionHousePlayerContainer);
        }
    }
}
