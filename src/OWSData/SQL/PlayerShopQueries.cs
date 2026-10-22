namespace OWSData.SQL
{
    // Postgres-only. Player-shop (offline vending) + economy-revision queries.
    public static class PlayerShopQueries
    {
        // ---- Economy revision ----

        // Locks the character row and returns identity + current revision.
        public static readonly string LockCharacterForUpdate = @"SELECT CharacterID, UserGUID, Gold, EconomyRevision
                FROM Characters
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID
                FOR UPDATE";

        public static readonly string SetGoldAndBumpRevision = @"UPDATE Characters
                SET Gold = @Gold, EconomyRevision = EconomyRevision + 1
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID
                RETURNING EconomyRevision";

        public static readonly string BumpRevision = @"UPDATE Characters
                SET EconomyRevision = EconomyRevision + 1
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID
                RETURNING EconomyRevision";

        // Persistence-API save paths keyed by CharName (that API has the name, not the ID).
        // Locks the row so a currency or inventory write serializes with shop economy
        // transactions, and returns the current revision so a caller on the optimistic protocol can
        // check its snapshot against it before writing.
        public static readonly string LockCharacterByNameForUpdate = @"SELECT CharacterID, EconomyRevision, Gold
                FROM Characters
                WHERE CustomerGUID = @CustomerGUID AND CharName = @CharName
                FOR UPDATE";

        public static readonly string SetGoldAndBumpRevisionByName = @"UPDATE Characters
                SET Gold = @Gold, EconomyRevision = EconomyRevision + 1
                WHERE CustomerGUID = @CustomerGUID AND CharName = @CharName
                RETURNING EconomyRevision";

        // Revision bump with no wallet change, for inventory writes.
        // Only used on the opt-in path: a caller that sent an ExpectedRevision has proven it reads
        // the new value back, so bumping cannot strand it. The default autosave path deliberately
        // does NOT bump — see UpdateCharacterInventory for why, and note that until the zone server
        // opts in, a stale bag snapshot can still overwrite a committed shop transaction.
        public static readonly string BumpRevisionByName = @"UPDATE Characters
                SET EconomyRevision = EconomyRevision + 1
                WHERE CustomerGUID = @CustomerGUID AND CharName = @CharName
                RETURNING EconomyRevision";

        public static readonly string GetCharInventoryIDByCharacterID = @"SELECT CI.CharInventoryID
                FROM CharInventory CI
                WHERE CI.CharacterID = @CharacterID
                  AND CI.CustomerGUID = @CustomerGUID
                LIMIT 1";

        public static readonly string DeleteInventoryItemsByInventoryID = @"DELETE FROM CharInventoryItems
                WHERE CharInventoryID = @CharInventoryID AND CustomerGUID = @CustomerGUID";

        public static readonly string InsertInventoryItem = @"INSERT INTO CharInventoryItems
                (CustomerGUID, CharInventoryID, ItemIDTag, Quantity, InSlotNumber, CustomData)
                VALUES (@CustomerGUID, @CharInventoryID, @ItemIDTag, @Quantity, @InSlotNumber, @CustomData)";

        // ---- Operations idempotency ----

        public static readonly string GetOperation = @"SELECT OperationId, OpType, State, ResultJson
                FROM PlayerShopOperations
                WHERE OperationId = @OperationId AND CustomerGUID = @CustomerGUID";

        public static readonly string InsertOperation = @"INSERT INTO PlayerShopOperations
                (OperationId, CustomerGUID, OpType, State, ResultJson)
                VALUES (@OperationId, @CustomerGUID, @OpType, 'committed', @ResultJson)";

        // Tombstone for an operation whose reply the zone lost: every operation records its OperationId as the
        // last statement of its transaction, so once this row exists a late commit fails on the primary key.
        // No row returned = the OperationId is already taken (the operation committed).
        public static readonly string AbandonOperation = @"INSERT INTO PlayerShopOperations
                (OperationId, CustomerGUID, OpType, State, ResultJson)
                VALUES (@OperationId, @CustomerGUID, 'Abandoned', 'abandoned', @ResultJson)
                ON CONFLICT (OperationId) DO NOTHING
                RETURNING OperationId";

        // ---- Shop lifecycle ----

        // Reopen guards: 0 rows returned = clear to open.
        public static readonly string CountOpenShopsForAccount = @"SELECT COUNT(*)
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND OwnerUserGUID = @OwnerUserGUID AND SaleStatus = 'open'";

        public static readonly string CountUnclaimedShopsForCharacter = @"SELECT COUNT(*)
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND OwnerCharacterID = @OwnerCharacterID
                  AND SettlementStatus <> 'claimed'";

        // Most-recent activity timestamp for cooldown (seconds since).
        public static readonly string SecondsSinceLastShopActivity = @"SELECT COALESCE(EXTRACT(EPOCH FROM (now() - MAX(GREATEST(CreatedAt, COALESCE(ClosedAt, CreatedAt))))), 999999)
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND OwnerCharacterID = @OwnerCharacterID";

        // Spacing: count OPEN shops within radius in the same zone.
        public static readonly string CountOpenShopsWithinRadius = @"SELECT COUNT(*)
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND ZoneName = @ZoneName AND SaleStatus = 'open'
                  AND ((X - @X) * (X - @X) + (Y - @Y) * (Y - @Y)) < (@Radius * @Radius)";

        public static readonly string InsertShop = @"INSERT INTO PlayerShops
                (CustomerGUID, OwnerCharacterID, OwnerUserGUID, OwnerCharName, ShopName, ZoneName,
                 X, Y, Z, RX, RY, RZ, SaleStatus, SettlementStatus, AppearanceJson)
                VALUES
                (@CustomerGUID, @OwnerCharacterID, @OwnerUserGUID, @OwnerCharName, @ShopName, @ZoneName,
                 @X, @Y, @Z, @RX, @RY, @RZ, 'open', 'pending', @AppearanceJson)
                RETURNING PlayerShopID";

        public static readonly string InsertShopItem = @"INSERT INTO PlayerShopItems
                (PlayerShopID, ListingIndex, ItemIDTag, QuantityListed, QuantityRemainingForSale, UnitPriceGold, TaxRateBps, CustomData)
                VALUES
                (@PlayerShopID, @ListingIndex, @ItemIDTag, @QuantityListed, @QuantityListed, @UnitPriceGold, @TaxRateBps, @CustomData)";

        // ---- Zone / snapshot reads ----

        public static readonly string GetOpenShopsForZone = @"SELECT PlayerShopID, ShopName, OwnerCharName, OwnerCharacterID, ZoneName,
                    X, Y, Z, RX, RY, RZ, SaleStatus, AppearanceJson
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND ZoneName = @ZoneName AND SaleStatus = 'open'";

        public static readonly string GetShopById = @"SELECT PlayerShopID, ShopName, OwnerCharName, OwnerCharacterID, ZoneName,
                    X, Y, Z, RX, RY, RZ, SaleStatus, AppearanceJson
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND PlayerShopID = @PlayerShopID";

        // The owner's currently-open shop(s) — drives remote manage/close from anywhere.
        public static readonly string GetOpenShopsForOwner = @"SELECT PlayerShopID, ShopName, OwnerCharName, OwnerCharacterID, ZoneName,
                    X, Y, Z, RX, RY, RZ, SaleStatus, AppearanceJson
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND OwnerCharacterID = @OwnerCharacterID AND SaleStatus = 'open'";

        public static readonly string GetListingsForShop = @"SELECT PlayerShopItemID, ListingIndex, ItemIDTag, QuantityRemainingForSale, UnitPriceGold, TaxRateBps, CustomData
                FROM PlayerShopItems
                WHERE PlayerShopID = @PlayerShopID
                ORDER BY ListingIndex";

        // ---- Purchase ----

        public static readonly string LockShopForUpdate = @"SELECT PlayerShopID, OwnerCharacterID, SaleStatus, SettlementStatus
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND PlayerShopID = @PlayerShopID
                FOR UPDATE";

        // Conditional decrement: 0 rows affected = stale price or insufficient stock.
        public static readonly string TryDecrementStock = @"UPDATE PlayerShopItems
                SET QuantityRemainingForSale = QuantityRemainingForSale - @Quantity
                WHERE PlayerShopItemID = @PlayerShopItemID
                  AND PlayerShopID = @PlayerShopID
                  AND UnitPriceGold = @ExpectedUnitPrice
                  AND QuantityRemainingForSale >= @Quantity
                RETURNING QuantityRemainingForSale, ItemIDTag, CustomData, UnitPriceGold, TaxRateBps";

        public static readonly string CountRemainingStockForShop = @"SELECT COALESCE(SUM(QuantityRemainingForSale), 0)
                FROM PlayerShopItems WHERE PlayerShopID = @PlayerShopID";

        public static readonly string CreditProceedsAndXp = @"UPDATE PlayerShops
                SET ProceedsGoldPending = ProceedsGoldPending + @Proceeds,
                    MerchantXpPending = MerchantXpPending + @Xp
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID";

        public static readonly string MarkShopSoldOut = @"UPDATE PlayerShops
                SET SaleStatus = 'sold_out', ClosedAt = now()
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID AND SaleStatus = 'open'";

        public static readonly string InsertPurchase = @"INSERT INTO PlayerShopPurchases
                (OperationId, CustomerGUID, PlayerShopID, PlayerShopItemID, BuyerCharacterID, Quantity,
                 UnitPriceGold, TaxPaid, ProceedsCredited, XpCredited, ItemIDTag, CustomData, State)
                VALUES
                (@OperationId, @CustomerGUID, @PlayerShopID, @PlayerShopItemID, @BuyerCharacterID, @Quantity,
                 @UnitPriceGold, @TaxPaid, @ProceedsCredited, @XpCredited, @ItemIDTag, @CustomData, 'paid')";

        public static readonly string GetPurchaseByOperationId = @"SELECT *
                FROM PlayerShopPurchases
                WHERE OperationId = @OperationId AND CustomerGUID = @CustomerGUID";

        // Locking variant for the delivery/refund state machine. ConfirmDelivery and
        // ResolveUndelivered both transition the same 'paid' row, and with an unlocked read both
        // observe it as unresolved: one grants the item while the other refunds the gold and
        // restocks the listing, and the loser's guarded UPDATE quietly matches 0 rows instead of
        // stopping it. The plain read above stays for the replay probe in Purchase and for
        // GetOperationResult, which are read-only and run outside a transaction.
        public static readonly string GetPurchaseByOperationIdForUpdate = @"SELECT *
                FROM PlayerShopPurchases
                WHERE OperationId = @OperationId AND CustomerGUID = @CustomerGUID
                FOR UPDATE";

        public static readonly string GetPendingDeliveries = @"SELECT OperationId, PlayerShopID, PlayerShopItemID, ItemIDTag, CustomData, Quantity, UnitPriceGold, TaxPaid
                FROM PlayerShopPurchases
                WHERE CustomerGUID = @CustomerGUID AND BuyerCharacterID = @BuyerCharacterID AND State = 'paid'";

        public static readonly string MarkPurchaseDelivered = @"UPDATE PlayerShopPurchases
                SET State = 'delivered', ResolvedAt = now()
                WHERE OperationId = @OperationId AND CustomerGUID = @CustomerGUID AND State = 'paid'";

        public static readonly string MarkPurchaseRestocked = @"UPDATE PlayerShopPurchases
                SET State = 'restocked', ResolvedAt = now()
                WHERE OperationId = @OperationId AND CustomerGUID = @CustomerGUID AND State = 'paid'";

        public static readonly string RestockItem = @"UPDATE PlayerShopItems
                SET QuantityRemainingForSale = QuantityRemainingForSale + @Quantity
                WHERE PlayerShopItemID = @PlayerShopItemID";

        public static readonly string DecreditProceedsAndXp = @"UPDATE PlayerShops
                SET ProceedsGoldPending = GREATEST(ProceedsGoldPending - @Proceeds, 0),
                    MerchantXpPending = GREATEST(MerchantXpPending - @Xp, 0)
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID";

        public static readonly string RevertSoldOutToOpen = @"UPDATE PlayerShops
                SET SaleStatus = 'open', ClosedAt = NULL
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID
                  AND SaleStatus = 'sold_out' AND SettlementStatus = 'pending'";

        // ---- Close ----

        // Closing: sellable stock becomes pending-return; sale status -> closed.
        public static readonly string CloseShopSetStatus = @"UPDATE PlayerShops
                SET SaleStatus = 'closed', ClosedAt = COALESCE(ClosedAt, now())
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID
                  AND OwnerCharacterID = @OwnerCharacterID AND SaleStatus IN ('open','sold_out')
                RETURNING PlayerShopID";

        public static readonly string MoveRemainingToPendingReturn = @"UPDATE PlayerShopItems
                SET QuantityPendingReturn = QuantityPendingReturn + QuantityRemainingForSale,
                    QuantityRemainingForSale = 0
                WHERE PlayerShopID = @PlayerShopID";

        // ---- Claim ----

        public static readonly string GetPendingClaimsForCharacter = @"SELECT PlayerShopID, ShopName, SaleStatus, SettlementStatus, ProceedsGoldPending, MerchantXpPending
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND OwnerCharacterID = @OwnerCharacterID
                  AND SaleStatus IN ('closed','sold_out')
                  AND SettlementStatus <> 'claimed'";

        public static readonly string GetPendingReturnItems = @"SELECT PlayerShopItemID, ListingIndex, ItemIDTag, QuantityPendingReturn, CustomData
                FROM PlayerShopItems
                WHERE PlayerShopID = @PlayerShopID AND QuantityPendingReturn > 0";

        public static readonly string LockShopForClaim = @"SELECT PlayerShopID, OwnerCharacterID, SaleStatus, SettlementStatus,
                    ProceedsGoldPending, ProceedsGoldClaimed, MerchantXpPending, MerchantXpClaimed
                FROM PlayerShops
                WHERE CustomerGUID = @CustomerGUID AND PlayerShopID = @PlayerShopID
                FOR UPDATE";

        public static readonly string ReturnItemQuantity = @"UPDATE PlayerShopItems
                SET QuantityPendingReturn = QuantityPendingReturn - @Quantity,
                    QuantityReturned = QuantityReturned + @Quantity
                WHERE PlayerShopItemID = @PlayerShopItemID AND PlayerShopID = @PlayerShopID
                  AND QuantityPendingReturn >= @Quantity
                RETURNING PlayerShopItemID";

        public static readonly string ClaimGold = @"UPDATE PlayerShops
                SET ProceedsGoldClaimed = ProceedsGoldClaimed + ProceedsGoldPending,
                    ProceedsGoldPending = 0
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID";

        public static readonly string ClaimXp = @"UPDATE PlayerShops
                SET MerchantXpClaimed = MerchantXpClaimed + MerchantXpPending,
                    MerchantXpPending = 0
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID";

        // After moves: fully settled if nothing pending remains.
        public static readonly string GetShopSettlementResidue = @"SELECT
                    (SELECT COALESCE(SUM(QuantityPendingReturn),0) FROM PlayerShopItems WHERE PlayerShopID = @PlayerShopID) AS ItemsPending,
                    ProceedsGoldPending, MerchantXpPending
                FROM PlayerShops WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID";

        public static readonly string SetSettlementStatus = @"UPDATE PlayerShops
                SET SettlementStatus = @SettlementStatus,
                    SettledAt = CASE WHEN @SettlementStatus = 'claimed' THEN now() ELSE SettledAt END
                WHERE PlayerShopID = @PlayerShopID AND CustomerGUID = @CustomerGUID";
    }
}
