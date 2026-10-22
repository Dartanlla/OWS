using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Requests.PlayerShops
{
    // One file for the player-shop endpoint handlers. Each mirrors the Characters request
    // pattern: SetData(repo, customerGuid) then Handle(). Backend is authoritative; requests
    // carry stable CharacterIDs and OperationIds (see the transaction-contract doc).

    public class CreateShopRequest
    {
        public CreateShopInput Input { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<PlayerShopResult> Handle() => await repo.CreateShop(customerGUID, Input);
    }

    public class GetShopsForZoneRequest
    {
        public string ZoneName { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<IEnumerable<PlayerShopView>> Handle() => await repo.GetShopsForZone(customerGUID, ZoneName);
    }

    public class GetShopSnapshotRequest
    {
        public long PlayerShopID { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<PlayerShopView> Handle() => await repo.GetShopSnapshot(customerGUID, PlayerShopID);
    }

    public class GetShopsForOwnerRequest
    {
        public int OwnerCharacterID { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<IEnumerable<PlayerShopView>> Handle() => await repo.GetShopsForOwner(customerGUID, OwnerCharacterID);
    }

    public class PurchaseRequest
    {
        public PurchaseInput Input { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<PurchaseResult> Handle() => await repo.Purchase(customerGUID, Input);
    }

    public class ConfirmDeliveryRequest
    {
        public ConfirmDeliveryInput Input { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<SuccessAndErrorMessage> Handle() => await repo.ConfirmDelivery(customerGUID, Input);
    }

    public class ResolveUndeliveredRequest
    {
        public Guid OperationId { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<ResolveUndeliveredResult> Handle() => await repo.ResolveUndelivered(customerGUID, OperationId);
    }

    public class GetPendingDeliveriesRequest
    {
        public int BuyerCharacterID { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<IEnumerable<PendingDeliveryView>> Handle() => await repo.GetPendingDeliveries(customerGUID, BuyerCharacterID);
    }

    public class CloseShopRequest
    {
        public Guid OperationId { get; set; }
        public long PlayerShopID { get; set; }
        public int OwnerCharacterID { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<PlayerShopResult> Handle() => await repo.CloseShop(customerGUID, OperationId, PlayerShopID, OwnerCharacterID);
    }

    public class GetPendingClaimsRequest
    {
        public int OwnerCharacterID { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<IEnumerable<PendingClaimView>> Handle() => await repo.GetPendingClaims(customerGUID, OwnerCharacterID);
    }

    public class ClaimEscrowRequest
    {
        public ClaimEscrowInput Input { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<ClaimEscrowResult> Handle() => await repo.ClaimEscrow(customerGUID, Input);
    }

    public class VendorTradeRequest
    {
        public VendorTradeInput Input { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<VendorTradeResult> Handle() => await repo.VendorTrade(customerGUID, Input);
    }

    public class GetOperationResultRequest
    {
        public Guid OperationId { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<OperationResultView> Handle() => await repo.GetOperationResult(customerGUID, OperationId);
    }

    public class AbandonOperationRequest
    {
        public Guid OperationId { get; set; }

        private Guid customerGUID;
        private IPlayerShopsRepository repo;
        public void SetData(IPlayerShopsRepository repo, IHeaderCustomerGUID customerGuid)
        {
            this.repo = repo;
            customerGUID = customerGuid.CustomerGUID;
        }
        public async Task<OperationResultView> Handle() => await repo.AbandonOperation(customerGUID, OperationId);
    }
}
