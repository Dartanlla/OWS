using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Options;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class PlayerShopsRepository : IPlayerShopsRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public PlayerShopsRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        // Row shape returned by the conditional stock decrement.
        private sealed class DecrementResult
        {
            public int QuantityRemainingForSale { get; set; }
            public string ItemIDTag { get; set; }
            public string CustomData { get; set; }
            public int UnitPriceGold { get; set; }
            public int TaxRateBps { get; set; }
        }

        private sealed class CharLockRow
        {
            public int CharacterID { get; set; }
            public Guid? UserGUID { get; set; }
            public int Gold { get; set; }
            public long EconomyRevision { get; set; }
        }

        private sealed class ShopLockRow
        {
            public long PlayerShopID { get; set; }
            public int OwnerCharacterID { get; set; }
            public string SaleStatus { get; set; }
            public string SettlementStatus { get; set; }
            public int ProceedsGoldPending { get; set; }
            public int ProceedsGoldClaimed { get; set; }
            public int MerchantXpPending { get; set; }
            public int MerchantXpClaimed { get; set; }
        }

        private static long TaxFor(long total, int taxRateBps)
        {
            if (taxRateBps <= 0) return 0;
            // ceil(total * bps / 10000) in checked int64.
            return checked((total * taxRateBps + 9999) / 10000);
        }

        // Replaces the entire CharInventoryItems set for a character inside the given transaction.
        private static async Task ReplaceInventory(DbConnection conn, DbTransaction tx, Guid customerGUID,
            int characterID, IEnumerable<UpdateCharacterInventory> inventory)
        {
            int? charInventoryID = await conn.QuerySingleOrDefaultAsync<int?>(
                PlayerShopQueries.GetCharInventoryIDByCharacterID,
                new { CustomerGUID = customerGUID, CharacterID = characterID },
                transaction: tx, commandType: CommandType.Text);

            if (charInventoryID == null)
            {
                throw new InvalidOperationException($"No CharInventory row for CharacterID {characterID}.");
            }

            await conn.ExecuteAsync(PlayerShopQueries.DeleteInventoryItemsByInventoryID,
                new { CustomerGUID = customerGUID, CharInventoryID = charInventoryID.Value },
                transaction: tx, commandType: CommandType.Text);

            foreach (UpdateCharacterInventory item in inventory ?? Enumerable.Empty<UpdateCharacterInventory>())
            {
                await conn.ExecuteAsync(PlayerShopQueries.InsertInventoryItem,
                    new
                    {
                        CustomerGUID = customerGUID,
                        CharInventoryID = charInventoryID.Value,
                        item.ItemIDTag,
                        item.Quantity,
                        item.InSlotNumber,
                        item.CustomData
                    },
                    transaction: tx, commandType: CommandType.Text);
            }
        }

        private static async Task RecordOperation(DbConnection conn, DbTransaction tx, Guid customerGUID,
            Guid operationId, string opType, object result)
        {
            await conn.ExecuteAsync(PlayerShopQueries.InsertOperation,
                new
                {
                    OperationId = operationId,
                    CustomerGUID = customerGUID,
                    OpType = opType,
                    ResultJson = JsonSerializer.Serialize(result)
                },
                transaction: tx, commandType: CommandType.Text);
        }

        private async Task<T> ReplayOperation<T>(DbConnection conn, Guid customerGUID, Guid operationId) where T : class
        {
            OperationResultView existing = await conn.QuerySingleOrDefaultAsync<OperationResultView>(
                PlayerShopQueries.GetOperation,
                new { OperationId = operationId, CustomerGUID = customerGUID },
                commandType: CommandType.Text);
            if (existing == null) return null;
            existing.Found = true;
            return string.IsNullOrEmpty(existing.ResultJson) ? null : JsonSerializer.Deserialize<T>(existing.ResultJson);
        }

        // ------------------------------------------------------------------
        // CreateShop
        // ------------------------------------------------------------------
        public async Task<PlayerShopResult> CreateShop(Guid customerGUID, CreateShopInput input)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            PlayerShopResult replay = await ReplayOperation<PlayerShopResult>(conn, customerGUID, input.OperationId);
            if (replay != null) return replay;

            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                CharLockRow character = await conn.QuerySingleOrDefaultAsync<CharLockRow>(
                    PlayerShopQueries.LockCharacterForUpdate,
                    new { CustomerGUID = customerGUID, CharacterID = input.OwnerCharacterID },
                    transaction: tx, commandType: CommandType.Text);

                if (character == null || character.UserGUID == null)
                    { await tx.RollbackAsync(); return FailResult("bad_request"); }
                if (character.EconomyRevision != input.ExpectedRevision)
                    { await tx.RollbackAsync(); return FailResult("stale_revision"); }

                Guid ownerUserGUID = character.UserGUID.Value;

                double secondsSince = await conn.ExecuteScalarAsync<double>(
                    PlayerShopQueries.SecondsSinceLastShopActivity,
                    new { CustomerGUID = customerGUID, OwnerCharacterID = input.OwnerCharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (secondsSince < input.ReopenCooldownSeconds)
                    { await tx.RollbackAsync(); return FailResult("cooldown"); }

                int openCount = await conn.ExecuteScalarAsync<int>(
                    PlayerShopQueries.CountOpenShopsForAccount,
                    new { CustomerGUID = customerGUID, OwnerUserGUID = ownerUserGUID },
                    transaction: tx, commandType: CommandType.Text);
                if (openCount > 0)
                    { await tx.RollbackAsync(); return FailResult("account_cap"); }

                int unclaimed = await conn.ExecuteScalarAsync<int>(
                    PlayerShopQueries.CountUnclaimedShopsForCharacter,
                    new { CustomerGUID = customerGUID, OwnerCharacterID = input.OwnerCharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (unclaimed > 0)
                    { await tx.RollbackAsync(); return FailResult("unclaimed_escrow"); }

                int tooClose = await conn.ExecuteScalarAsync<int>(
                    PlayerShopQueries.CountOpenShopsWithinRadius,
                    new { CustomerGUID = customerGUID, ZoneName = input.ZoneName, input.X, input.Y, Radius = input.MinShopSpacingUnits },
                    transaction: tx, commandType: CommandType.Text);
                if (tooClose > 0)
                    { await tx.RollbackAsync(); return FailResult("too_close"); }

                string ownerCharName = await GetCharNameById(conn, tx, customerGUID, input.OwnerCharacterID);

                long shopId = await conn.ExecuteScalarAsync<long>(
                    PlayerShopQueries.InsertShop,
                    new
                    {
                        CustomerGUID = customerGUID,
                        input.OwnerCharacterID,
                        OwnerUserGUID = ownerUserGUID,
                        // OwnerCharName is a display string, sourced from the DB-derived character.
                        OwnerCharName = ownerCharName,
                        input.ShopName,
                        input.ZoneName,
                        input.X, input.Y, input.Z, input.RX, input.RY, input.RZ,
                        AppearanceJson = input.AppearanceJson ?? ""
                    },
                    transaction: tx, commandType: CommandType.Text);

                foreach (PlayerShopListingInput listing in input.Listings)
                {
                    await conn.ExecuteAsync(PlayerShopQueries.InsertShopItem,
                        new
                        {
                            PlayerShopID = shopId,
                            listing.ListingIndex,
                            listing.ItemIDTag,
                            QuantityListed = listing.Quantity,
                            listing.UnitPriceGold,
                            listing.TaxRateBps,
                            listing.CustomData
                        },
                        transaction: tx, commandType: CommandType.Text);
                }

                // Escrow removal is already reflected in the submitted inventory snapshot, but the
                // fee is charged here rather than trusted from the snapshot: writing the caller's
                // PostEscrowGold verbatim made CreateShop an unconditional gold-set primitive, the
                // one write in the economy where a stale or wrong wallet value won outright.
                // Recomputed from the freshly-locked row, same rule as Purchase/ClaimEscrow/VendorTrade.
                if (input.OpeningFeeGold < 0)
                    { await tx.RollbackAsync(); return FailResult("bad_request"); }
                long newOwnerGold = (long)character.Gold - input.OpeningFeeGold;
                if (newOwnerGold < 0)
                    { await tx.RollbackAsync(); return FailResult("insufficient_funds"); }

                await ReplaceInventory(conn, tx, customerGUID, input.OwnerCharacterID, input.PostEscrowInventory);
                long newRevision = await conn.ExecuteScalarAsync<long>(
                    PlayerShopQueries.SetGoldAndBumpRevision,
                    new { CustomerGUID = customerGUID, CharacterID = input.OwnerCharacterID, Gold = (int)newOwnerGold },
                    transaction: tx, commandType: CommandType.Text);

                PlayerShopResult result = new PlayerShopResult
                {
                    Success = true, ReasonCode = "", PlayerShopID = shopId, NewRevision = newRevision
                };
                await RecordOperation(conn, tx, customerGUID, input.OperationId, "CreateShop", result);
                await tx.CommitAsync();
                return result;
            }
            catch (PostgresException pg) when (pg.SqlState == "23505" && IsOperationIdTaken(pg))
            {
                await SafeRollback(tx);
                return await ReplayOperation<PlayerShopResult>(conn, customerGUID, input.OperationId) ?? FailResult("abandoned");
            }
            catch (PostgresException pg) when (pg.SqlState == "23505")
            {
                // Unique violation on the open-per-account index = raced another open.
                await SafeRollback(tx);
                return new PlayerShopResult { Success = false, ReasonCode = "account_cap" };
            }
            catch
            {
                await SafeRollback(tx);
                throw;
            }
        }

        private static async Task<string> GetCharNameById(DbConnection conn, DbTransaction tx, Guid customerGUID, int characterID)
        {
            return await conn.ExecuteScalarAsync<string>(
                "SELECT CharName FROM Characters WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID",
                new { CustomerGUID = customerGUID, CharacterID = characterID },
                transaction: tx, commandType: CommandType.Text);
        }

        // ------------------------------------------------------------------
        // GetShopsForZone / GetShopSnapshot
        // ------------------------------------------------------------------
        public async Task<IEnumerable<PlayerShopView>> GetShopsForZone(Guid customerGUID, string zoneName)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            var shops = (await conn.QueryAsync<PlayerShopView>(
                PlayerShopQueries.GetOpenShopsForZone,
                new { CustomerGUID = customerGUID, ZoneName = zoneName },
                commandType: CommandType.Text)).ToList();

            foreach (PlayerShopView shop in shops)
            {
                shop.Listings = (await conn.QueryAsync<PlayerShopListingView>(
                    PlayerShopQueries.GetListingsForShop,
                    new { PlayerShopID = shop.PlayerShopID },
                    commandType: CommandType.Text)).ToList();
            }
            return shops;
        }

        public async Task<IEnumerable<PlayerShopView>> GetShopsForOwner(Guid customerGUID, int ownerCharacterID)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            var shops = (await conn.QueryAsync<PlayerShopView>(
                PlayerShopQueries.GetOpenShopsForOwner,
                new { CustomerGUID = customerGUID, OwnerCharacterID = ownerCharacterID },
                commandType: CommandType.Text)).ToList();

            foreach (PlayerShopView shop in shops)
            {
                shop.Listings = (await conn.QueryAsync<PlayerShopListingView>(
                    PlayerShopQueries.GetListingsForShop,
                    new { PlayerShopID = shop.PlayerShopID },
                    commandType: CommandType.Text)).ToList();
            }
            return shops;
        }

        public async Task<PlayerShopView> GetShopSnapshot(Guid customerGUID, long playerShopID)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            PlayerShopView shop = await conn.QuerySingleOrDefaultAsync<PlayerShopView>(
                PlayerShopQueries.GetShopById,
                new { CustomerGUID = customerGUID, PlayerShopID = playerShopID },
                commandType: CommandType.Text);
            if (shop == null) return null;

            shop.Listings = (await conn.QueryAsync<PlayerShopListingView>(
                PlayerShopQueries.GetListingsForShop,
                new { PlayerShopID = playerShopID },
                commandType: CommandType.Text)).ToList();
            return shop;
        }

        // ------------------------------------------------------------------
        // Purchase
        // ------------------------------------------------------------------
        public async Task<PurchaseResult> Purchase(Guid customerGUID, PurchaseInput input)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            // Purchases replay off the purchase row itself.
            var existingPurchase = await conn.QuerySingleOrDefaultAsync(
                PlayerShopQueries.GetPurchaseByOperationId,
                new { OperationId = input.OperationId, CustomerGUID = customerGUID },
                commandType: CommandType.Text);
            if (existingPurchase != null)
            {
                return PurchaseResultFromRow(existingPurchase);
            }

            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                // Lock shop, then buyer, in a consistent order (shop id then character id are stable).
                ShopLockRow shop = await conn.QuerySingleOrDefaultAsync<ShopLockRow>(
                    PlayerShopQueries.LockShopForUpdate,
                    new { CustomerGUID = customerGUID, PlayerShopID = input.PlayerShopID },
                    transaction: tx, commandType: CommandType.Text);
                if (shop == null) { await tx.RollbackAsync(); return PurchaseFail("bad_request"); }
                if (shop.SaleStatus != "open") { await tx.RollbackAsync(); return PurchaseFail("sold_out"); }

                DecrementResult dec = await conn.QuerySingleOrDefaultAsync<DecrementResult>(
                    PlayerShopQueries.TryDecrementStock,
                    new
                    {
                        input.PlayerShopItemID,
                        input.PlayerShopID,
                        input.ExpectedUnitPrice,
                        input.Quantity
                    },
                    transaction: tx, commandType: CommandType.Text);
                if (dec == null) { await tx.RollbackAsync(); return PurchaseFail("stale"); }

                long total = checked((long)dec.UnitPriceGold * input.Quantity);
                long tax = TaxFor(total, dec.TaxRateBps);
                long totalWithTax = checked(total + tax);
                long xp = input.MerchantXpPerGoldBps <= 0 ? 0 : total * input.MerchantXpPerGoldBps / 10000;

                CharLockRow buyer = await conn.QuerySingleOrDefaultAsync<CharLockRow>(
                    PlayerShopQueries.LockCharacterForUpdate,
                    new { CustomerGUID = customerGUID, CharacterID = input.BuyerCharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (buyer == null) { await tx.RollbackAsync(); return PurchaseFail("bad_request"); }
                if (buyer.EconomyRevision != input.BuyerExpectedRevision) { await tx.RollbackAsync(); return PurchaseFail("stale_revision", buyer.EconomyRevision, buyer.Gold); }
                if (buyer.Gold < totalWithTax) { await tx.RollbackAsync(); return PurchaseFail("insufficient_funds", buyer.EconomyRevision, buyer.Gold); }

                long newBuyerRevision = await conn.ExecuteScalarAsync<long>(
                    PlayerShopQueries.SetGoldAndBumpRevision,
                    new { CustomerGUID = customerGUID, CharacterID = input.BuyerCharacterID, Gold = (int)(buyer.Gold - totalWithTax) },
                    transaction: tx, commandType: CommandType.Text);

                await conn.ExecuteAsync(PlayerShopQueries.CreditProceedsAndXp,
                    new { CustomerGUID = customerGUID, PlayerShopID = input.PlayerShopID, Proceeds = (int)total, Xp = (int)xp },
                    transaction: tx, commandType: CommandType.Text);

                await conn.ExecuteAsync(PlayerShopQueries.InsertPurchase,
                    new
                    {
                        OperationId = input.OperationId,
                        CustomerGUID = customerGUID,
                        input.PlayerShopID,
                        input.PlayerShopItemID,
                        input.BuyerCharacterID,
                        input.Quantity,
                        UnitPriceGold = dec.UnitPriceGold,
                        TaxPaid = (int)tax,
                        ProceedsCredited = (int)total,
                        XpCredited = (int)xp,
                        dec.ItemIDTag,
                        dec.CustomData
                    },
                    transaction: tx, commandType: CommandType.Text);

                long shopRemaining = await conn.ExecuteScalarAsync<long>(
                    PlayerShopQueries.CountRemainingStockForShop,
                    new { PlayerShopID = input.PlayerShopID },
                    transaction: tx, commandType: CommandType.Text);
                bool soldOut = shopRemaining <= 0;
                if (soldOut)
                {
                    await conn.ExecuteAsync(PlayerShopQueries.MarkShopSoldOut,
                        new { PlayerShopID = input.PlayerShopID, CustomerGUID = customerGUID },
                        transaction: tx, commandType: CommandType.Text);
                }

                // Only claims the OperationId, so an AbandonOperation tombstone makes this commit fail. The result
                // is read from the purchase row (GetOperationResult).
                await RecordOperation(conn, tx, customerGUID, input.OperationId, "Purchase", new { });
                await tx.CommitAsync();
                return new PurchaseResult
                {
                    Success = true,
                    ItemIDTag = dec.ItemIDTag,
                    CustomData = dec.CustomData,
                    QuantityGranted = input.Quantity,
                    RemainingQuantity = dec.QuantityRemainingForSale,
                    ShopSoldOut = soldOut,
                    NewBuyerRevision = newBuyerRevision,
                    TaxPaid = (int)tax,
                    ProceedsCredited = (int)total,
                    XpCredited = (int)xp
                };
            }
            catch (PostgresException pg) when (pg.SqlState == "23505")
            {
                // The OperationId is taken: a concurrent retry of this purchase committed first (replay it), or the
                // zone abandoned it.
                await SafeRollback(tx);
                var committed = await conn.QuerySingleOrDefaultAsync(
                    PlayerShopQueries.GetPurchaseByOperationId,
                    new { OperationId = input.OperationId, CustomerGUID = customerGUID },
                    commandType: CommandType.Text);
                return committed != null ? PurchaseResultFromRow(committed) : PurchaseFail("abandoned");
            }
            catch
            {
                await SafeRollback(tx);
                throw;
            }
        }

        // buyerRevision/buyerGold let a refused buyer resync from the refusal itself. Without them the
        // only way to learn the committed revision is to write the wallet, which is exactly how a lost
        // Purchase response turns into a refund.
        private static PurchaseResult PurchaseFail(string reason, long buyerRevision = 0, int buyerGold = 0) =>
            new PurchaseResult
            {
                Success = false,
                ReasonCode = reason,
                NewBuyerRevision = buyerRevision,
                BuyerGold = buyerGold
            };

        private static PurchaseResult PurchaseResultFromRow(dynamic row)
        {
            string state = (string)row.state;
            return new PurchaseResult
            {
                Success = state != "restocked",
                ReasonCode = state == "restocked" ? "restocked" : "",
                ItemIDTag = (string)row.itemidtag,
                CustomData = row.customdata as string,
                QuantityGranted = (int)row.quantity,
                RemainingQuantity = 0,
                ShopSoldOut = false,
                TaxPaid = (int)row.taxpaid,
                ProceedsCredited = (int)row.proceedscredited,
                XpCredited = (int)row.xpcredited
            };
        }

        // ------------------------------------------------------------------
        // ConfirmDelivery
        // ------------------------------------------------------------------
        public async Task<SuccessAndErrorMessage> ConfirmDelivery(Guid customerGUID, ConfirmDeliveryInput input)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();
            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                // Lock the purchase row for the whole transition. ResolveUndelivered races this
                // method for the same row, and both used to read it unlocked: each saw 'paid', this
                // one handed over the item, and the refund path gave the gold back on top.
                var purchase = await conn.QuerySingleOrDefaultAsync(
                    PlayerShopQueries.GetPurchaseByOperationIdForUpdate,
                    new { OperationId = input.OperationId, CustomerGUID = customerGUID },
                    transaction: tx, commandType: CommandType.Text);
                if (purchase == null) { await tx.RollbackAsync(); return Err("bad_request"); }
                string state = (string)purchase.state;
                if (state == "delivered") { await tx.CommitAsync(); return Ok(); } // idempotent
                if (state != "paid") { await tx.RollbackAsync(); return Err("already_resolved"); }

                CharLockRow buyer = await conn.QuerySingleOrDefaultAsync<CharLockRow>(
                    PlayerShopQueries.LockCharacterForUpdate,
                    new { CustomerGUID = customerGUID, CharacterID = input.BuyerCharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (buyer == null) { await tx.RollbackAsync(); return Err("bad_request"); }
                if (buyer.EconomyRevision != input.ExpectedRevision) { await tx.RollbackAsync(); return Err("stale_revision"); }

                await ReplaceInventory(conn, tx, customerGUID, input.BuyerCharacterID, input.PostDeliveryInventory);
                await conn.ExecuteAsync(PlayerShopQueries.BumpRevision,
                    new { CustomerGUID = customerGUID, CharacterID = input.BuyerCharacterID },
                    transaction: tx, commandType: CommandType.Text);

                // The state guard on this UPDATE is an assertion, not a formality: if it matches no
                // rows the purchase was resolved by someone else after our read and the item grant
                // above must not stand.
                int delivered = await conn.ExecuteAsync(PlayerShopQueries.MarkPurchaseDelivered,
                    new { OperationId = input.OperationId, CustomerGUID = customerGUID },
                    transaction: tx, commandType: CommandType.Text);
                if (delivered != 1) { await tx.RollbackAsync(); return Err("already_resolved"); }

                await tx.CommitAsync();
                return Ok();
            }
            catch
            {
                await SafeRollback(tx);
                throw;
            }
        }

        // ------------------------------------------------------------------
        // ResolveUndelivered
        // ------------------------------------------------------------------
        public async Task<ResolveUndeliveredResult> ResolveUndelivered(Guid customerGUID, Guid operationId)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();
            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                // Locked read: ConfirmDelivery races this method for the same row. See the comment
                // on GetPurchaseByOperationIdForUpdate.
                var purchase = await conn.QuerySingleOrDefaultAsync(
                    PlayerShopQueries.GetPurchaseByOperationIdForUpdate,
                    new { OperationId = operationId, CustomerGUID = customerGUID },
                    transaction: tx, commandType: CommandType.Text);
                if (purchase == null) { await tx.RollbackAsync(); return new ResolveUndeliveredResult { Success = false, ReasonCode = "bad_request" }; }
                string state = (string)purchase.state;
                if (state == "restocked")
                {
                    // The zone retries only after a lost reply, so it has not mirrored the refund yet: repeat it.
                    CharLockRow refundedBuyer = await conn.QuerySingleOrDefaultAsync<CharLockRow>(
                        PlayerShopQueries.LockCharacterForUpdate,
                        new { CustomerGUID = customerGUID, CharacterID = (int)purchase.buyercharacterid },
                        transaction: tx, commandType: CommandType.Text);
                    await tx.CommitAsync();
                    return new ResolveUndeliveredResult
                    {
                        Success = true,
                        ReasonCode = "restocked",
                        NewBuyerRevision = refundedBuyer?.EconomyRevision ?? 0,
                        GoldRefunded = checked((int)purchase.unitpricegold * (int)purchase.quantity + (int)purchase.taxpaid)
                    };
                }
                if (state != "paid") { await tx.RollbackAsync(); return new ResolveUndeliveredResult { Success = false, ReasonCode = "already_resolved" }; }

                int buyerCharacterID = (int)purchase.buyercharacterid;
                int quantity = (int)purchase.quantity;
                int unitPrice = (int)purchase.unitpricegold;
                int taxPaid = (int)purchase.taxpaid;
                int proceeds = (int)purchase.proceedscredited;
                int xp = (int)purchase.xpcredited;
                long playerShopID = (long)purchase.playershopid;
                long playerShopItemID = (long)purchase.playershopitemid;
                int refund = checked(unitPrice * quantity + taxPaid);

                CharLockRow buyer = await conn.QuerySingleOrDefaultAsync<CharLockRow>(
                    PlayerShopQueries.LockCharacterForUpdate,
                    new { CustomerGUID = customerGUID, CharacterID = buyerCharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (buyer == null) { await tx.RollbackAsync(); return new ResolveUndeliveredResult { Success = false, ReasonCode = "bad_request" }; }

                long newRevision = await conn.ExecuteScalarAsync<long>(
                    PlayerShopQueries.SetGoldAndBumpRevision,
                    new { CustomerGUID = customerGUID, CharacterID = buyerCharacterID, Gold = (int)Math.Min((long)buyer.Gold + refund, int.MaxValue) },
                    transaction: tx, commandType: CommandType.Text);

                await conn.ExecuteAsync(PlayerShopQueries.RestockItem,
                    new { PlayerShopItemID = playerShopItemID, Quantity = quantity },
                    transaction: tx, commandType: CommandType.Text);
                await conn.ExecuteAsync(PlayerShopQueries.DecreditProceedsAndXp,
                    new { CustomerGUID = customerGUID, PlayerShopID = playerShopID, Proceeds = proceeds, Xp = xp },
                    transaction: tx, commandType: CommandType.Text);
                await conn.ExecuteAsync(PlayerShopQueries.RevertSoldOutToOpen,
                    new { CustomerGUID = customerGUID, PlayerShopID = playerShopID },
                    transaction: tx, commandType: CommandType.Text);
                // Same assertion as ConfirmDelivery: 0 rows means the purchase was resolved
                // concurrently and this refund + restock must not stand.
                int restocked = await conn.ExecuteAsync(PlayerShopQueries.MarkPurchaseRestocked,
                    new { OperationId = operationId, CustomerGUID = customerGUID },
                    transaction: tx, commandType: CommandType.Text);
                if (restocked != 1) { await tx.RollbackAsync(); return new ResolveUndeliveredResult { Success = false, ReasonCode = "already_resolved" }; }

                await tx.CommitAsync();
                return new ResolveUndeliveredResult { Success = true, NewBuyerRevision = newRevision, GoldRefunded = refund };
            }
            catch
            {
                await SafeRollback(tx);
                throw;
            }
        }

        public async Task<IEnumerable<PendingDeliveryView>> GetPendingDeliveries(Guid customerGUID, int buyerCharacterID)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();
            return await conn.QueryAsync<PendingDeliveryView>(
                PlayerShopQueries.GetPendingDeliveries,
                new { CustomerGUID = customerGUID, BuyerCharacterID = buyerCharacterID },
                commandType: CommandType.Text);
        }

        // ------------------------------------------------------------------
        // CloseShop
        // ------------------------------------------------------------------
        public async Task<PlayerShopResult> CloseShop(Guid customerGUID, Guid operationId, long playerShopID, int ownerCharacterID)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            PlayerShopResult replay = await ReplayOperation<PlayerShopResult>(conn, customerGUID, operationId);
            if (replay != null) return replay;

            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                long? closed = await conn.ExecuteScalarAsync<long?>(
                    PlayerShopQueries.CloseShopSetStatus,
                    new { PlayerShopID = playerShopID, CustomerGUID = customerGUID, OwnerCharacterID = ownerCharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (closed == null) { await tx.RollbackAsync(); return new PlayerShopResult { Success = false, ReasonCode = "bad_request" }; }

                await conn.ExecuteAsync(PlayerShopQueries.MoveRemainingToPendingReturn,
                    new { PlayerShopID = playerShopID },
                    transaction: tx, commandType: CommandType.Text);

                PlayerShopResult result = new PlayerShopResult { Success = true, PlayerShopID = playerShopID };
                await RecordOperation(conn, tx, customerGUID, operationId, "CloseShop", result);
                await tx.CommitAsync();
                return result;
            }
            catch (PostgresException pg) when (pg.SqlState == "23505" && IsOperationIdTaken(pg))
            {
                await SafeRollback(tx);
                return await ReplayOperation<PlayerShopResult>(conn, customerGUID, operationId) ?? FailResult("abandoned");
            }
            catch
            {
                await SafeRollback(tx);
                throw;
            }
        }

        public async Task<IEnumerable<PendingClaimView>> GetPendingClaims(Guid customerGUID, int ownerCharacterID)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            var shops = (await conn.QueryAsync<PendingClaimView>(
                PlayerShopQueries.GetPendingClaimsForCharacter,
                new { CustomerGUID = customerGUID, OwnerCharacterID = ownerCharacterID },
                commandType: CommandType.Text)).ToList();

            foreach (PendingClaimView shop in shops)
            {
                shop.Items = (await conn.QueryAsync<PendingClaimItemView>(
                    PlayerShopQueries.GetPendingReturnItems,
                    new { PlayerShopID = shop.PlayerShopID },
                    commandType: CommandType.Text)).ToList();
            }
            return shops;
        }

        // ------------------------------------------------------------------
        // ClaimEscrow
        // ------------------------------------------------------------------
        public async Task<ClaimEscrowResult> ClaimEscrow(Guid customerGUID, ClaimEscrowInput input)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            ClaimEscrowResult replay = await ReplayOperation<ClaimEscrowResult>(conn, customerGUID, input.OperationId);
            if (replay != null) return replay;

            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                ShopLockRow shop = await conn.QuerySingleOrDefaultAsync<ShopLockRow>(
                    PlayerShopQueries.LockShopForClaim,
                    new { CustomerGUID = customerGUID, PlayerShopID = input.PlayerShopID },
                    transaction: tx, commandType: CommandType.Text);
                if (shop == null || shop.OwnerCharacterID != input.OwnerCharacterID)
                { await tx.RollbackAsync(); return new ClaimEscrowResult { Success = false, ReasonCode = "bad_request" }; }

                CharLockRow owner = await conn.QuerySingleOrDefaultAsync<CharLockRow>(
                    PlayerShopQueries.LockCharacterForUpdate,
                    new { CustomerGUID = customerGUID, CharacterID = input.OwnerCharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (owner == null) { await tx.RollbackAsync(); return new ClaimEscrowResult { Success = false, ReasonCode = "bad_request" }; }
                if (owner.EconomyRevision != input.ExpectedRevision) { await tx.RollbackAsync(); return new ClaimEscrowResult { Success = false, ReasonCode = "stale_revision" }; }

                // Move claimed item quantities pending->returned. Backend validates each against the row.
                foreach (ClaimItemInput ci in input.ClaimedItems)
                {
                    if (ci.QuantityToReturn <= 0) continue;
                    long? ok = await conn.ExecuteScalarAsync<long?>(
                        PlayerShopQueries.ReturnItemQuantity,
                        new { PlayerShopItemID = ci.PlayerShopItemID, PlayerShopID = input.PlayerShopID, Quantity = ci.QuantityToReturn },
                        transaction: tx, commandType: CommandType.Text);
                    if (ok == null) { await tx.RollbackAsync(); return new ClaimEscrowResult { Success = false, ReasonCode = "bad_item" }; }
                }

                int goldClaimed = 0, xpClaimed = 0;
                if (input.ClaimGold)
                {
                    goldClaimed = shop.ProceedsGoldPending;
                    await conn.ExecuteAsync(PlayerShopQueries.ClaimGold,
                        new { PlayerShopID = input.PlayerShopID, CustomerGUID = customerGUID },
                        transaction: tx, commandType: CommandType.Text);
                }
                if (input.ClaimXp)
                {
                    xpClaimed = shop.MerchantXpPending;
                    await conn.ExecuteAsync(PlayerShopQueries.ClaimXp,
                        new { PlayerShopID = input.PlayerShopID, CustomerGUID = customerGUID },
                        transaction: tx, commandType: CommandType.Text);
                }

                // Owner inventory reflects the granted items (snapshot). Gold is recomputed from the
                // freshly-locked owner row + the escrow proceeds actually claimed here — never the client's
                // PostClaimGold snapshot, which can be stale if an untracked gold sink (e.g. an NPC shop that
                // does not bump EconomyRevision) moved the wallet since the client read it. This matches the
                // authoritative Purchase/ResolveUndelivered gold writes.
                await ReplaceInventory(conn, tx, customerGUID, input.OwnerCharacterID, input.PostClaimInventory);
                int newGold = (int)Math.Min((long)owner.Gold + goldClaimed, int.MaxValue);
                long newRevision = await conn.ExecuteScalarAsync<long>(
                    PlayerShopQueries.SetGoldAndBumpRevision,
                    new { CustomerGUID = customerGUID, CharacterID = input.OwnerCharacterID, Gold = newGold },
                    transaction: tx, commandType: CommandType.Text);

                // Determine settlement status from residue.
                var residue = await conn.QuerySingleAsync(
                    PlayerShopQueries.GetShopSettlementResidue,
                    new { PlayerShopID = input.PlayerShopID, CustomerGUID = customerGUID },
                    transaction: tx, commandType: CommandType.Text);
                long itemsPending = (long)residue.itemspending;
                int proceedsPending = (int)residue.proceedsgoldpending;
                int merchantXpPending = (int)residue.merchantxppending;
                string settlement = (itemsPending == 0 && proceedsPending == 0 && merchantXpPending == 0)
                    ? "claimed" : "partially_claimed";

                await conn.ExecuteAsync(PlayerShopQueries.SetSettlementStatus,
                    new { PlayerShopID = input.PlayerShopID, CustomerGUID = customerGUID, SettlementStatus = settlement },
                    transaction: tx, commandType: CommandType.Text);

                ClaimEscrowResult result = new ClaimEscrowResult
                {
                    Success = true, NewRevision = newRevision, GoldClaimed = goldClaimed, XpClaimed = xpClaimed, SettlementStatus = settlement
                };
                await RecordOperation(conn, tx, customerGUID, input.OperationId, "ClaimEscrow", result);
                await tx.CommitAsync();
                return result;
            }
            catch (PostgresException pg) when (pg.SqlState == "23505" && IsOperationIdTaken(pg))
            {
                await SafeRollback(tx);
                return await ReplayOperation<ClaimEscrowResult>(conn, customerGUID, input.OperationId)
                    ?? new ClaimEscrowResult { Success = false, ReasonCode = "abandoned" };
            }
            catch
            {
                await SafeRollback(tx);
                throw;
            }
        }

        // ------------------------------------------------------------------
        // VendorTrade — NPC vendor sell+buy, committed together or not at all.
        // ------------------------------------------------------------------
        public async Task<VendorTradeResult> VendorTrade(Guid customerGUID, VendorTradeInput input)
        {
            // Validate before the replay probe, not after. OperationId is the idempotency key: a
            // caller that omits it sends Guid.Empty, the first such trade records its result under
            // that key, and every later empty-key trade short-circuits on the replay below and
            // returns the *first* trade's cached result — reporting success with a stale NewGold
            // while moving no money and no items.
            if (input == null || input.OperationId == Guid.Empty)
                return VendorTradeFail("bad_request");
            if (input.SellGoldTotal < 0 || input.BuyGoldTotal < 0)
                return VendorTradeFail("bad_request");

            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            VendorTradeResult replay = await ReplayOperation<VendorTradeResult>(conn, customerGUID, input.OperationId);
            if (replay != null) return replay;

            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                CharLockRow character = await conn.QuerySingleOrDefaultAsync<CharLockRow>(
                    PlayerShopQueries.LockCharacterForUpdate,
                    new { CustomerGUID = customerGUID, CharacterID = input.CharacterID },
                    transaction: tx, commandType: CommandType.Text);
                if (character == null) { await tx.RollbackAsync(); return VendorTradeFail("bad_request"); }
                if (character.EconomyRevision != input.ExpectedRevision)
                    { await tx.RollbackAsync(); return VendorTradeFail("stale"); }

                // Gold is recomputed off the freshly-locked row, never trusted from a client snapshot
                // — same rule as Purchase/ClaimEscrow.
                long newGold = checked((long)character.Gold + input.SellGoldTotal - input.BuyGoldTotal);
                if (newGold < 0) { await tx.RollbackAsync(); return VendorTradeFail("insufficient_funds"); }
                // Characters.Gold is a 32-bit column, so int.MaxValue is the hard cap even when the
                // caller sends none — without this the cast below would wrap a fortune into a debt.
                // The configurable cap only blocks trades that *increase* gold: a character already
                // over the cap (raised then lowered, or credited through a path that does not
                // enforce it) must still be able to spend its way back down, and a flat
                // "newGold > cap" would lock them out of every purchase permanently.
                bool increasesGold = newGold > character.Gold;
                if (newGold > int.MaxValue || (input.GoldCap > 0 && increasesGold && newGold > input.GoldCap))
                    { await tx.RollbackAsync(); return VendorTradeFail("gold_cap"); }

                // The bag the zone server computed with BOTH halves applied. Written in the same
                // transaction as the wallet, so a failure anywhere leaves the player exactly as they
                // were rather than sold-out-and-empty-handed.
                await ReplaceInventory(conn, tx, customerGUID, input.CharacterID, input.PostTradeInventory);
                long newRevision = await conn.ExecuteScalarAsync<long>(
                    PlayerShopQueries.SetGoldAndBumpRevision,
                    new { CustomerGUID = customerGUID, CharacterID = input.CharacterID, Gold = (int)newGold },
                    transaction: tx, commandType: CommandType.Text);

                VendorTradeResult result = new VendorTradeResult
                {
                    Success = true, ReasonCode = "", NewRevision = newRevision, NewGold = (int)newGold
                };
                // Recorded inside the transaction: a retry of the same OperationId replays this exact
                // result instead of moving the money a second time.
                await RecordOperation(conn, tx, customerGUID, input.OperationId, "VendorTrade", result);
                await tx.CommitAsync();
                return result;
            }
            catch (PostgresException pg) when (pg.SqlState == "23505")
            {
                // Two concurrent retries of the same OperationId: the loser replays the winner's row.
                await SafeRollback(tx);
                VendorTradeResult committed = await ReplayOperation<VendorTradeResult>(conn, customerGUID, input.OperationId);
                return committed ?? VendorTradeFail("bad_request");
            }
            catch
            {
                await SafeRollback(tx);
                throw;
            }
        }

        private static VendorTradeResult VendorTradeFail(string reason) =>
            new VendorTradeResult { Success = false, ReasonCode = reason };

        public async Task<OperationResultView> GetOperationResult(Guid customerGUID, Guid operationId)
        {
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();

            // Non-purchase operations and tombstones. A purchase's own row only claims the OperationId.
            OperationResultView op = await conn.QuerySingleOrDefaultAsync<OperationResultView>(
                PlayerShopQueries.GetOperation,
                new { OperationId = operationId, CustomerGUID = customerGUID },
                commandType: CommandType.Text);
            if (op != null && op.OpType != "Purchase") { op.Found = true; return op; }

            // Purchase operations.
            var purchase = await conn.QuerySingleOrDefaultAsync(
                PlayerShopQueries.GetPurchaseByOperationId,
                new { OperationId = operationId, CustomerGUID = customerGUID },
                commandType: CommandType.Text);
            if (purchase != null)
            {
                return new OperationResultView
                {
                    Found = true,
                    OpType = "Purchase",
                    State = (string)purchase.state,
                    ResultJson = JsonSerializer.Serialize(PurchaseResultFromRow(purchase))
                };
            }

            return new OperationResultView { Found = false };
        }

        // The zone's answer to a lost reply: the committed result if the operation committed, otherwise a tombstone
        // that stops it from ever committing. Either way the answer is final.
        public async Task<OperationResultView> AbandonOperation(Guid customerGUID, Guid operationId)
        {
            string abandonedJson = JsonSerializer.Serialize(FailResult("abandoned"));
            using (DbConnection conn = CreateConnection())
            {
                await conn.OpenAsync();
                Guid? tombstone = await conn.ExecuteScalarAsync<Guid?>(
                    PlayerShopQueries.AbandonOperation,
                    new { OperationId = operationId, CustomerGUID = customerGUID, ResultJson = abandonedJson },
                    commandType: CommandType.Text);
                if (tombstone.HasValue)
                {
                    return new OperationResultView { Found = true, OpType = "Abandoned", State = "abandoned", ResultJson = abandonedJson };
                }
            }
            return await GetOperationResult(customerGUID, operationId);
        }

        private static bool IsOperationIdTaken(PostgresException pg) =>
            string.Equals(pg.TableName, "playershopoperations", StringComparison.OrdinalIgnoreCase);

        // ------------------------------------------------------------------
        private static PlayerShopResult FailResult(string reason) => new PlayerShopResult { Success = false, ReasonCode = reason };
        private static SuccessAndErrorMessage Ok() => new SuccessAndErrorMessage { Success = true, ErrorMessage = "" };
        private static SuccessAndErrorMessage Err(string reason) => new SuccessAndErrorMessage { Success = false, ErrorMessage = reason };

        private static async Task SafeRollback(DbTransaction tx)
        {
            try { await tx.RollbackAsync(); } catch { /* connection already gone */ }
        }
    }
}
