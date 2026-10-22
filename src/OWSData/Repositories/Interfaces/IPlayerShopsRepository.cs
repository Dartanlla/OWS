using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    public interface IPlayerShopsRepository
    {
        Task<PlayerShopResult> CreateShop(Guid customerGUID, CreateShopInput input);
        Task<IEnumerable<PlayerShopView>> GetShopsForZone(Guid customerGUID, string zoneName);
        Task<PlayerShopView> GetShopSnapshot(Guid customerGUID, long playerShopID);
        Task<IEnumerable<PlayerShopView>> GetShopsForOwner(Guid customerGUID, int ownerCharacterID);
        Task<PurchaseResult> Purchase(Guid customerGUID, PurchaseInput input);
        Task<SuccessAndErrorMessage> ConfirmDelivery(Guid customerGUID, ConfirmDeliveryInput input);
        Task<ResolveUndeliveredResult> ResolveUndelivered(Guid customerGUID, Guid operationId);
        Task<IEnumerable<PendingDeliveryView>> GetPendingDeliveries(Guid customerGUID, int buyerCharacterID);
        Task<PlayerShopResult> CloseShop(Guid customerGUID, Guid operationId, long playerShopID, int ownerCharacterID);
        Task<IEnumerable<PendingClaimView>> GetPendingClaims(Guid customerGUID, int ownerCharacterID);
        Task<ClaimEscrowResult> ClaimEscrow(Guid customerGUID, ClaimEscrowInput input);
        Task<VendorTradeResult> VendorTrade(Guid customerGUID, VendorTradeInput input);
        Task<OperationResultView> GetOperationResult(Guid customerGUID, Guid operationId);
        Task<OperationResultView> AbandonOperation(Guid customerGUID, Guid operationId);
    }
}
