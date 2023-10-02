using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Tables;
using OWSData.Models.Composites;
using OWSActionHouse.Request.ActionHouse;

namespace OWSCharacterPersistence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActionHouseController : Controller
    {
        private readonly IActionHouseRepository _actionHouseRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public ActionHouseController(IActionHouseRepository actionHouseRepository,
            IHeaderCustomerGUID customerGuid)
        {
            _actionHouseRepository = actionHouseRepository;
            _customerGuid = customerGuid;
        }

        [HttpPost]
        [Route("GetActionHousePlayerItems")]
        [Produces(typeof(ActionHousePlayerContainer))]
        public async Task<IActionResult> GetActionHousePlayerItems([FromBody] GetActionHousePlayerItemsRequest request)
        {
            request.SetData(_customerGuid, _actionHouseRepository);
            return await request.Handle();
        }

        
    }
}
