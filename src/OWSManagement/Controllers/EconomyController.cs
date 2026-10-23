using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;

namespace OWSManagement.Controllers
{
    /// <summary>Juri in circulation and its flows, the item census, possible duplicates and player shop health.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EconomyController : Controller
    {
        private readonly IConsoleQueriesRepository _queries;
        private readonly IPlayerShopsRepository _shops;
        private readonly ConsoleCustomer _customer;

        public EconomyController(IConsoleQueriesRepository queries, IPlayerShopsRepository shops, ConsoleCustomer customer)
        {
            _queries = queries;
            _shops = shops;
            _customer = customer;
        }

        [HttpGet]
        [Route("")]
        public async Task<ConsoleEconomy> Get()
        {
            return await _queries.GetEconomy(_customer.CustomerGUID);
        }

        /// <summary>Closes a player shop the way its owner would: unsold items and proceeds wait for the owner's next claim.</summary>
        [HttpPost]
        [Route("Shops/{playerShopId:long}/Close")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<PlayerShopResult> CloseShop(long playerShopId)
        {
            PlayerShopView shop = await _shops.GetShopSnapshot(_customer.CustomerGUID, playerShopId);
            if (shop == null)
            {
                return new PlayerShopResult { Success = false, ReasonCode = "unknown_shop" };
            }
            return await _shops.CloseShop(_customer.CustomerGUID, Guid.NewGuid(), playerShopId, shop.OwnerCharacterID);
        }
    }
}
