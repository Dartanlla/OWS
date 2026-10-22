using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSCharacterPersistence.Requests.PlayerShops;

namespace OWSCharacterPersistence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerShopsController : Controller
    {
        private readonly IPlayerShopsRepository _repo;
        private readonly IHeaderCustomerGUID _customerGuid;

        public PlayerShopsController(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            _repo = repo;
            _customerGuid = customerGuid;
        }

        private bool ServiceKeyOk() => SamsaraServiceKey.IsValid(Request);

        [HttpPost]
        [Route("CreateShop")]
        [Produces(typeof(PlayerShopResult))]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("GetShopsForZone")]
        [Produces(typeof(IEnumerable<PlayerShopView>))]
        public async Task<IActionResult> GetShopsForZone([FromBody] GetShopsForZoneRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("GetShopSnapshot")]
        [Produces(typeof(PlayerShopView))]
        public async Task<IActionResult> GetShopSnapshot([FromBody] GetShopSnapshotRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("GetShopsForOwner")]
        [Produces(typeof(IEnumerable<PlayerShopView>))]
        public async Task<IActionResult> GetShopsForOwner([FromBody] GetShopsForOwnerRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("Purchase")]
        [Produces(typeof(PurchaseResult))]
        public async Task<IActionResult> Purchase([FromBody] PurchaseRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("ConfirmDelivery")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<IActionResult> ConfirmDelivery([FromBody] ConfirmDeliveryRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("ResolveUndelivered")]
        [Produces(typeof(ResolveUndeliveredResult))]
        public async Task<IActionResult> ResolveUndelivered([FromBody] ResolveUndeliveredRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("GetPendingDeliveries")]
        [Produces(typeof(IEnumerable<PendingDeliveryView>))]
        public async Task<IActionResult> GetPendingDeliveries([FromBody] GetPendingDeliveriesRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("CloseShop")]
        [Produces(typeof(PlayerShopResult))]
        public async Task<IActionResult> CloseShop([FromBody] CloseShopRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("GetPendingClaims")]
        [Produces(typeof(IEnumerable<PendingClaimView>))]
        public async Task<IActionResult> GetPendingClaims([FromBody] GetPendingClaimsRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("ClaimEscrow")]
        [Produces(typeof(ClaimEscrowResult))]
        public async Task<IActionResult> ClaimEscrow([FromBody] ClaimEscrowRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("VendorTrade")]
        [Produces(typeof(VendorTradeResult))]
        public async Task<IActionResult> VendorTrade([FromBody] VendorTradeRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("GetOperationResult")]
        [Produces(typeof(OperationResultView))]
        public async Task<IActionResult> GetOperationResult([FromBody] GetOperationResultRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }

        [HttpPost]
        [Route("AbandonOperation")]
        [Produces(typeof(OperationResultView))]
        public async Task<IActionResult> AbandonOperation([FromBody] AbandonOperationRequest request)
        {
            if (!ServiceKeyOk()) return Unauthorized();
            request.SetData(_repo, _customerGuid);
            return Ok(await request.Handle());
        }
    }
}
