using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class GenericQueries
    {

        #region System Queries

        public static readonly string GetAbilities = @"SELECT AB.*, AT.AbilityTypeName
                FROM Abilities AB
                INNER JOIN AbilityTypes AT ON AT.AbilityTypeID = AB.AbilityTypeID
                WHERE AB.CustomerGUID = @CustomerGUID
                ORDER BY AB.AbilityName";

        #endregion

        #region Customer Queries

        public static readonly string GetCustomer = @"SELECT CustomerGUID
                , CustomerEmail
                , EnableAutoLoopBack
                , NoPortForwarding
                FROM Customers
                WHERE CustomerGUID = @CustomerGUID";

        #endregion

        #region Character Queries

        public static readonly string AddCharacterToInstance = @"INSERT INTO CharOnMapInstance (CustomerGUID, MapInstanceID, CharacterID)
        VALUES (@CustomerGUID, @MapInstanceID, @CharacterID)";

        public static readonly string AddCharacterCustomDataField = @"INSERT INTO CustomCharacterData (CustomerGUID, CharacterID, CustomFieldName, FieldValue)
        VALUES (@CustomerGUID, @CharacterID, @CustomFieldName, @FieldValue)";

        public static readonly string AddDefaultCustomCharacterData = @"INSERT INTO CustomCharacterData (CustomerGUID, CharacterID, CustomFieldName, FieldValue)
                SELECT DCR.CustomerGUID, @CharacterID, DCCD.CustomFieldName, DCCD.FieldValue
                FROM DefaultCustomCharacterData DCCD
                INNER JOIN DefaultCharacterValues DCR
                    ON DCR.CustomerGUID = DCCD.CustomerGUID
                    AND DCR.DefaultCharacterValuesID = DCCD.DefaultCharacterValuesID
                WHERE DCR.CustomerGUID = @CustomerGUID
                    AND DCR.DefaultSetName = @DefaultSetName";

        public static readonly string DeleteCharacterInventoryItems = @"DELETE FROM CharInventoryItems
                WHERE CharInventoryItems.CharInventoryID = @CharInventoryID
                AND CharInventoryItems.CustomerGUID = @CustomerGUID";

        // Samsara saves levels as CharStats rows (the game's attribute names), not in Characters.CharacterLevel,
        // which nothing updates. The login screen cannot call the stats API, so the roster carries them.
        public static readonly string GetAllCharacters = @"SELECT WC.*,
                COALESCE(CL.ClassName,'') as ClassName,
                COALESCE((SELECT CS.Value FROM CharStats CS
                    WHERE CS.CustomerGUID = WC.CustomerGUID AND CS.CharacterID = WC.CharacterID
                      AND CS.StatIdentifier = 'BaseLevel'), 0) as BaseLevel,
                COALESCE((SELECT CS.Value FROM CharStats CS
                    WHERE CS.CustomerGUID = WC.CustomerGUID AND CS.CharacterID = WC.CharacterID
                      AND CS.StatIdentifier = 'BaseJobLevel'), 0) as BaseJobLevel
            FROM Characters WC
            INNER JOIN Users U
                ON U.UserGUID = WC.UserGUID
            INNER JOIN UserSessions US
                ON US.UserGUID = U.UserGUID
            LEFT JOIN Class CL
                ON CL.ClassID = WC.ClassID
            WHERE WC.CustomerGUID = @CustomerGUID
            AND US.UserSessionGUID = @UserSessionGUID";

        public static readonly string GetCharacterByName = @"SELECT *
                FROM Characters
                WHERE CustomerGUID = @CustomerGUID
                  AND CharName = @CharName";

        //Region is pinned on the account, not the character: the login-screen server selector runs
        //before a character is chosen, and new characters inherit the choice. NULL means the player
        //has never picked one and is resolved to ServerRegions.Default by the caller.
        public static readonly string GetJoinSettingsByUserGUID = @"SELECT PreferredServerRegion, IsInternalNetworkTestUser
                FROM Users
                WHERE CustomerGUID = @CustomerGUID
                  AND UserGUID = @UserGUID";

        public static readonly string UpdatePreferredServerRegion = @"UPDATE Users
                SET PreferredServerRegion = @PreferredServerRegion
                WHERE CustomerGUID = @CustomerGUID
                  AND UserGUID = @UserGUID";

        public static readonly string GetCharacterIDByName = @"SELECT CharacterID
                FROM Characters
                WHERE CustomerGUID = @CustomerGUID
                  AND CharName = @CharName";

        public static readonly string GetCharGuidByCharName = @"SELECT Convert(varchar(50), C.CharGuid) as CharGuid
                FROM Characters C 
                WHERE C.CharName = @CharName
                AND C.CustomerGUID = @CustomerGUID";

        public static readonly string GetCharStatsByCharName = @"SELECT CS.*
                FROM Characters C INNER JOIN CharStats CS
                ON C.CharName = @CharName
                  AND C.CustomerGUID = @CustomerGUID
                WHERE CS.CharacterID = C.CharacterID";

        // LEFT JOIN, not INNER: Samsara keeps quest definitions in a UE DataTable, so the Quest
        // registry table is empty and an inner join returned no saved quests for anyone - the journal
        // and task progress in CharQuests persisted fine and were never read back. QuestClassName is
        // optional on the UE side (it falls back to the DataTable's actor class).
        public static readonly string GetCharQuestsByCharName = @"SELECT CQ.* , COALESCE(Q.QuestClassName, '') AS QuestClassName
                FROM Characters C INNER JOIN CharQuests CQ
                ON C.CharName = @CharName
                AND C.CustomerGUID = @CustomerGUID
                LEFT JOIN Quest Q
                ON Q.CustomerGUID = @CustomerGUID
                AND Q.QuestIDTag = CQ.QuestIDTag
                WHERE CQ.CharacterID = C.CharacterID";

        public static readonly string GetCharInventoryIDByCharName = @"SELECT CI.CharInventoryID
                FROM Characters C INNER JOIN CharInventory CI
                ON C.CharName = @CharName
                AND C.CustomerGUID = @CustomerGUID
                WHERE CI.CharacterID = C.CharacterID
                AND CI.CustomerGUID = @CustomerGUID";
                
        public static readonly string GetCharInventoryByCharName = @"SELECT CII.ItemIDTag, CII.Quantity, CII.InSlotNumber, CII.CustomData
                FROM CharInventoryItems CII
                INNER JOIN Characters C ON C.CharName = @CharName
                INNER JOIN CharInventory CI ON CI.CharacterID = C.CharacterID
                WHERE CII.CharInventoryID = CI.CharInventoryID
                AND CII.CUstomerGUID = @CustomerGUID";

        public static readonly string GetCharByCharName = @"SELECT C.*, MI.Port, WS.ServerIP, CMI.MapInstanceID, COALESCE(CL.ClassName,'') AS ClassName
                FROM Characters C
                LEFT JOIN Class CL
                    ON CL.ClassID = C.ClassID
                LEFT JOIN CharOnMapInstance CMI
                    ON CMI.CharacterID = C.CharacterID
                LEFT JOIN MapInstances MI
                    ON MI.MapInstanceID = CMI.MapInstanceID
                LEFT JOIN WorldServers WS
                    ON WS.WorldServerID = MI.WorldServerID
                WHERE C.CharName = @CharName
                  AND C.CustomerGUID = @CustomerGUID
                ORDER BY MI.MapInstanceID DESC";

        public static readonly string GetCharacterAbilities = @"
                SELECT  CA.AbilityIDTag,
                        CA.CurrentAbilityLevel,
                        CA.ActualAbilityLevel,
                        CA.CustomData
                FROM    CharAbilities   CA
                JOIN    Characters      C   ON  C.CharacterID = CA.CharacterID
                                            AND C.CustomerGUID = CA.CustomerGUID
                WHERE   C.CustomerGUID = @CustomerGUID
                  AND   C.CharName     = @CharName;";

        public static readonly string GetCharacterAbilityByName = @"SELECT A.AbilityID, A.AbilityCustomJSON, A.AbilityName, A.AbilityTypeID, A.Class, A.CustomerGUID, A.Race, A.TextureToUseForIcon, A.GameplayAbilityClassName,
            CHA.CharHasAbilitiesID, CHA.AbilityLevel, CHA.CharHasAbilitiesCustomJSON, C.CharacterID, C.CharName 
                FROM CharHasAbilities CHA
                INNER JOIN Abilities A ON A.AbilityID = CHA.AbilityID AND A.CustomerGUID = CHA.CustomerGUID
                INNER JOIN Characters C ON C.CharacterID = CHA.CharacterID AND C.CustomerGUID = CHA.CustomerGUID
                WHERE C.CustomerGUID = @CustomerGUID
                  AND C.CharName = @CharacterName
                  AND A.AbilityName = @AbilityName";

        public static readonly string GetCharacterAbilityBars = @"SELECT CAB.AbilityBarName, CAB.CharAbilityBarID, COALESCE(CAB.CharAbilityBarsCustomJSON,'') as CharAbilityBarsCustomJSON, CAB.CharacterID, CAB.CustomerGUID, 
                CAB.MaxNumberOfSlots, CAB.NumberOfUnlockedSlots 
                FROM CharAbilityBars CAB
                INNER JOIN Characters C ON C.CharacterID = CAB.CharacterID AND C.CustomerGUID = CAB.CustomerGUID
                WHERE C.CustomerGUID = @CustomerGUID
                  AND C.CharName = @CharName";

        public static readonly string GetCharacterAbilityBarsAndAbilities = @"SELECT CAB.AbilityBarName, CAB.CharAbilityBarID, COALESCE(CAB.CharAbilityBarsCustomJSON,'') as CharAbilityBarsCustomJSON, CAB.CharacterID, CAB.CustomerGUID, CAB.MaxNumberOfSlots, CAB.NumberOfUnlockedSlots, 
                CHA.AbilityLevel, COALESCE(CHA.CharHasAbilitiesCustomJSON, '') as CharHasAbilitiesCustomJSON,
                AB.AbilityID, AB.AbilityName, AB.AbilityTypeID, AB.Class, AB.Race, AB.TextureToUseForIcon, AB.GameplayAbilityClassName, AB.AbilityCustomJSON,
                CABA.InSlotNumber
                FROM CharAbilityBars CAB
                INNER JOIN CharAbilityBarAbilities CABA ON CABA.CharAbilityBarID = CAB.CharAbilityBarID AND CABA.CustomerGUID = CAB.CustomerGUID
                INNER JOIN CharHasAbilities CHA ON CHA.CharHasAbilitiesID = CABA.CharHasAbilitiesID AND CHA.CustomerGUID = CABA.CustomerGUID
                INNER JOIN Abilities AB ON AB.AbilityID = CHA.AbilityID AND AB.CustomerGUID = CHA.CustomerGUID
                INNER JOIN Characters C ON C.CharacterID = CAB.CharacterID AND C.CustomerGUID = CAB.CustomerGUID
                WHERE C.CustomerGUID = @CustomerGUID
                  AND C.CharName = @CharName";

        public static readonly string GetCharacterCustomDataByName = @"SELECT *
                FROM CustomCharacterData CCD
                INNER JOIN Characters C ON C.CharacterID = CCD.CharacterID
                WHERE CCD.CustomerGUID = @CustomerGUID
                  AND C.CharName = @CharName";

        public static readonly string GetPlayerGroupIDByType = @"SELECT PG.PlayerGroupID
                FROM PlayerGroupMember PGM
                INNER JOIN PlayerGroup PG ON PG.PlayerGroupID = PGM.PlayerGroupID
                WHERE PGM.CustomerGUID = @CustomerGUID
                  AND PGM.CharacterID = @CharacterID
                  AND PG.PlayerGroupTypeID = @PlayerGroupType";

        public static readonly string GetUsers = @"SELECT UserGUID, FirstName, LastName, Email, CreateDate, LastAccess, Role
                FROM Users
                WHERE CustomerGUID = @CustomerGUID";

        // Bounded counterpart to GetUsers, which has no LIMIT and returns every row in the
        // customer. Matches email, either name, or SteamId. An empty search returns the first
        // page. Only the management console calls it.
        public static readonly string SearchUsers = @"SELECT U.UserGUID, U.FirstName, U.LastName, U.Email, U.SteamId,
                    U.CreateDate, U.LastAccess, U.Role,
                    (SELECT COUNT(*) FROM Characters C
                      WHERE C.CustomerGUID = U.CustomerGUID AND C.UserGUID = U.UserGUID) AS CharacterCount,
                    (SELECT COUNT(*) FROM Characters C
                      WHERE C.CustomerGUID = U.CustomerGUID AND C.UserGUID = U.UserGUID
                        AND C.IsInternalNetworkTestUser) AS NetworkTestCharacterCount
                FROM Users U
                WHERE U.CustomerGUID = @CustomerGUID
                  AND (LOWER(U.Email) LIKE @SearchPattern
                    OR LOWER(U.FirstName) LIKE @SearchPattern
                    OR LOWER(U.LastName) LIKE @SearchPattern
                    OR LOWER(COALESCE(U.SteamId, '')) LIKE @SearchPattern)
                ORDER BY U.Email
                OFFSET 0 ROWS FETCH NEXT 200 ROWS ONLY";

        // The account-level flag. A join checks it alongside Characters.IsInternalNetworkTestUser.
        public static readonly string UpdateNetworkTestFlagForUser = @"UPDATE Users
                SET IsInternalNetworkTestUser = @IsInternalNetworkTestUser
              WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID";

        public static readonly string UpdateUser = @"UPDATE Users
                SET FirstName = @FirstName
                  , LastName = @LastName
                  , Email = @Email
              WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID";

        public static readonly string UpdateUserRole = @"UPDATE Users
                SET Role = @Role
              WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID";

        // Admin console character lookups. Both are cross-user by design (the caller is an
        // operator, not a player), so they are only ever reached from OWSManagement.
        public static readonly string GetCharactersForUser = @"SELECT C.CharacterID, C.UserGUID, C.CharName, C.Email,
                    C.CharacterLevel, C.MapName, C.IsAdmin, C.IsModerator, C.IsInternalNetworkTestUser,
                    C.LastActivity, C.CreateDate,
                    COALESCE(CL.ClassName,'') AS ClassName
                FROM Characters C
                LEFT JOIN Class CL
                    ON CL.ClassID = C.ClassID
                WHERE C.CustomerGUID = @CustomerGUID
                  AND C.UserGUID = @UserGUID
                ORDER BY C.CharName";

        public static readonly string SearchCharacters = @"SELECT C.CharacterID, C.UserGUID, C.CharName, C.Email,
                    C.CharacterLevel, C.MapName, C.IsAdmin, C.IsModerator, C.IsInternalNetworkTestUser,
                    C.LastActivity, C.CreateDate,
                    COALESCE(CL.ClassName,'') AS ClassName
                FROM Characters C
                LEFT JOIN Class CL
                    ON CL.ClassID = C.ClassID
                WHERE C.CustomerGUID = @CustomerGUID
                  AND (LOWER(C.CharName) LIKE @SearchPattern OR LOWER(C.Email) LIKE @SearchPattern)
                ORDER BY C.CharName
                OFFSET 0 ROWS FETCH NEXT 200 ROWS ONLY";

        // COALESCE so a caller can send one flag without clobbering the others: a null parameter
        // leaves that column as it is. Callers that pass every flag still behave as before.
        public static readonly string UpdateCharacterFlags = @"UPDATE Characters
                SET IsAdmin = COALESCE(@IsAdmin, IsAdmin)
                  , IsModerator = COALESCE(@IsModerator, IsModerator)
                  , IsInternalNetworkTestUser = COALESCE(@IsInternalNetworkTestUser, IsInternalNetworkTestUser)
              WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID";

        public static readonly string HasCustomCharacterDataForField = @"SELECT 1
                FROM CustomCharacterData
                WHERE CustomerGUID = @CustomerGUID
                  AND CustomFieldName = @CustomFieldName
                  AND CharacterID = @CharacterID";

        public static readonly string RemoveAllCharactersFromAllInstancesByWorldID = @"DELETE FROM CharOnMapInstance 
                WHERE CustomerGUID = @CustomerGUID
                  AND MapInstanceID IN (SELECT MapInstanceID FROM MapInstances MI WHERE MI.WorldServerID = @WorldServerID)";

        public static readonly string RemoveCharacterFromAllInstances = @"DELETE FROM CharOnMapInstance 
                WHERE CustomerGUID = @CustomerGUID
                  AND CharacterID = @CharacterID";

        public static readonly string RemoveCharacterFromInstance = @"DELETE FROM CharOnMapInstance
                WHERE CustomerGUID = @CustomerGUID
                  AND CharacterID = @CharacterID
                  AND MapInstanceID = @MapInstanceID";

        public static readonly string RemoveCharacterFromInstances = @"DELETE FROM CharOnMapInstance WHERE CustomerGUID = @CustomerGUID AND MapInstanceID IN @MapInstances";

        public static readonly string UpdateCharacterCustomDataField = @"UPDATE CustomCharacterData
                SET FieldValue = @FieldValue
                WHERE CustomerGUID = @CustomerGUID
                  AND CustomFieldName = @CustomFieldName
                  AND CharacterID = @CharacterID";

        public static readonly string UpdateCharacterPosition = @"UPDATE Characters
                SET X = @X,
                    Y = @Y,
                    Z = @Z,
                    RX = @RX,
                    RY = @RY,
                    RZ = @RZ
                WHERE CharName = @CharName
                  AND CustomerGUID = @CustomerGUID";

        public static readonly string UpdateCharacterPositionAndMap = @"UPDATE Characters
                SET MapName = @MapName,
                    X = @X,
                    Y = @Y,
                    Z = @Z,
                    RX = @RX,
                    RY = @RY,
                    RZ = @RZ
                WHERE CharName = @CharName
                  AND CustomerGUID = @CustomerGUID";

        public static readonly string UpsertManyCharacterStats = @"
            WITH targ AS (
                SELECT characterid
                FROM characters
                WHERE charname     = @CharName
                  AND customerguid = @CustomerGUID
                LIMIT 1
            )
            INSERT INTO charstats (customerguid, characterid, statidentifier, value)
            SELECT  @CustomerGUID,
                    targ.characterid,
                    s.statidentifier,
                    s.statvalue
            FROM   targ,
                   unnest(@StatIdentifiers::varchar[], @StatValues::int[])
                        AS s(statidentifier, statvalue)
            ON CONFLICT (customerguid, characterid, statidentifier)
            DO UPDATE
            SET value = EXCLUDED.value;";


        public static readonly string UpdateCharacterQuest =
            @"IF EXISTS(
                SELECT * FROM Characters C INNER JOIN CharQuests CQ 
                ON C.CharName = @CharName
                AND C.CustomerGUID = @CustomerGUID    
                Where CQ.QuestIDTag = @QuestIDTag
                AND CQ.CharacterID = C.CharacterID
                AND CQ.CustomerGUID = @CustomerGUID
                )
                UPDATE CQ 
                SET QuestJournalTagContainer = @QuestJournalTagContainer,
                CustomData = @CustomData
                FROM Characters C INNER JOIN CharQuests CQ 
                ON C.CharName = @CharName
                AND C.CustomerGUID = @CustomerGUID
                Where CQ.QuestIDTag = @QuestIDTag
                AND CQ.CharacterID = C.CharacterID
                AND CQ.CustomerGUID = @CustomerGUID
            ELSE
                INSERT INTO CharQuests
                (
                CustomerGUID,
                CharacterID,
                QuestIDTag,
                QuestJournalTagContainer,
                CustomData
                )
                SELECT @CustomerGUID, CharacterID, @QuestIDTag, @QuestJournalTagContainer, @CustomData FROM Characters C
                WHERE C.CharName = @CharName
                AND C.CustomerGUID = @CustomerGUID";

        public static readonly string UpdateCharacterInventory =
            @"INSERT INTO CharInventoryItems
                (
                CustomerGUID,
                CharInventoryID,
                ItemIDTag,
                Quantity,
                InSlotNumber,
                CustomData
                )
                VALUES
                (
                    @CustomerGUID,
                    @CharInventoryID,
                    @ItemIDTag,
                    @Quantity,
                    @InSlotNumber,
                    @CustomData
                )";

        public static readonly string UpdateCharacterCurrency = @"UPDATE Characters
                SET Gold = @Gold
                WHERE CustomerGUID = @CustomerGUID
                  AND CharName = @CharName";

        // Resolves the class NAME to its ClassID and writes it to Characters.ClassID.
        // The EXISTS guard is deliberate: Characters.ClassID is a NOT NULL FK into Class, and an
        // unresolvable @ClassName would make the subselect return NULL. Guarding in the WHERE clause
        // makes an unknown class name affect 0 rows (a harmless no-op) instead of failing the write
        // or corrupting the FK. Callers that care can check the affected row count.
        public static readonly string UpdateCharacterClass = @"UPDATE Characters
                SET ClassID = (SELECT ClassID FROM Class
                               WHERE ClassName = @ClassName
                                 AND CustomerGUID = @CustomerGUID)
                WHERE CustomerGUID = @CustomerGUID
                  AND CharName = @CharName
                  AND EXISTS (SELECT 1 FROM Class
                              WHERE ClassName = @ClassName
                                AND CustomerGUID = @CustomerGUID)";

        public static readonly string UpdateCharacterZone = @"UPDATE Characters
                SET MapName = @ZoneName
                WHERE CharacterID = @CharacterID
                  AND CustomerGUID = @CustomerGUID";

        public static readonly string GetAllQuests = @"SELECT QuestIDTag, QuestOverview, QuestTasks, QuestClassName
                FROM Quest
                WHERE CustomerGUID = @CustomerGUID";

        #endregion

        #region World Queries

        public static readonly string GetWorldByID = @"SELECT *
                FROM WorldServers
                WHERE CustomerGUID = @CustomerGUID
                  AND WorldServerID = @WorldServerID";

        public static readonly string GetActiveWorldServersByLoad = @"SELECT WS.WorldServerID, WS.ServerIP, WS.InternalServerIP, WS.Port, WS.MaxNumberOfInstances, WS.StartingMapInstancePort
                FROM WorldServers WS
                LEFT JOIN MapInstances MI ON MI.WorldServerID = WS.WorldServerID AND MI.CustomerGUID = WS.CustomerGUID
                WHERE WS.CustomerGUID = @CustomerGUID
                  AND WS.ServerStatus = 1
                  AND WS.ActiveStartTime IS NOT NULL
                  --Region routing: only hosts tagged with the player's region are candidates. Fails
                  --closed (no rows) rather than falling back to another region's hosts.
                  AND WS.ServerRegion = @ServerRegion
                GROUP BY WS.WorldServerID, WS.ServerIP, WS.InternalServerIP, WS.Port, WS.MaxNumberOfInstances, WS.StartingMapInstancePort
                ORDER BY COALESCE (COUNT(MI.MapInstanceID),0)";

        public static readonly string GetPortsInUseByWorldServer = @"SELECT Port
                FROM MapInstances
                WHERE CustomerGUID = @CustomerGUID
                  AND WorldServerID = @WorldServerID
                ORDER BY Port";

        public static readonly string UpdateWorldServerStatus = @"UPDATE WorldServers
                    SET ActiveStartTime = NULL
                      , ServerStatus = @ServerStatus
                  WHERE CustomerGUID = @CustomerGUID
                    AND WorldServerID = @WorldServerID";

        #endregion

        #region Zone Queries

        public static readonly string GetMapInstance = @"SELECT *
                FROM MapInstances
                WHERE CustomerGUID = @CustomerGUID
                  AND MapInstanceID = @MapInstanceID";

        public static readonly string GetMapInstancesByIpAndPort = @"SELECT M.MapName, M.ZoneName, M.WorldCompContainsFilter, M.WorldCompListFilter, 
                MI.MapInstanceID, MI.Status, 
                WS.MaxNumberOfInstances, WS.ActiveStartTime, WS.ServerStatus, WS.InternalServerIP
                FROM MapInstances MI
                INNER JOIN Maps M ON M.MapID = MI.MapID AND M.CustomerGUID = MI.CustomerGUID
                INNER JOIN WorldServers WS ON WS.WorldServerID = MI.WorldServerID AND WS.CustomerGUID = MI.CustomerGUID
                WHERE WS.CustomerGUID = @CustomerGUID
                AND (WS.ServerIP = @ServerIP OR InternalServerIP = @ServerIP)
                AND MI.Port = @Port";

        public static readonly string GetMapInstanceStatus = @"SELECT Status
                FROM MapInstances
                WHERE CustomerGUID = @CustomerGUID
                  AND MapInstanceID = @MapInstanceID";

        public static readonly string GetMapByZoneName = @"SELECT *
                FROM Maps
                WHERE CustomerGUID = @CustomerGUID
                  AND ZoneName = @ZoneName";

        // Admin console zone management. Maps has no unique constraint on (CustomerGUID,
        // ZoneName) - the primary key is (CustomerGUID, MapID) over a sequence - so nothing in
        // the schema stops a second row with the same ZoneName, and the Instance Launcher
        // resolves zones by name. The repository therefore guards inserts and renames itself.
        public static readonly string GetZones = @"SELECT M.MapID, M.MapName, M.ZoneName, M.Width, M.Height,
                    M.WorldCompContainsFilter, M.WorldCompListFilter, M.MapMode,
                    M.SoftPlayerCap, M.HardPlayerCap, M.MinutesToShutdownAfterEmpty,
                    (SELECT COUNT(*) FROM MapInstances MI
                      WHERE MI.CustomerGUID = M.CustomerGUID AND MI.MapID = M.MapID) AS MapInstanceCount
                FROM Maps M
                WHERE M.CustomerGUID = @CustomerGUID
                ORDER BY M.ZoneName";

        // Pass @ExcludeMapID = 0 when inserting; no row can hold MapID 0, so nothing is excluded.
        // When renaming, pass the MapID being edited so a row never collides with itself.
        public static readonly string CountZonesWithZoneName = @"SELECT COUNT(*)
                FROM Maps
                WHERE CustomerGUID = @CustomerGUID
                  AND LOWER(ZoneName) = LOWER(@ZoneName)
                  AND MapID <> @ExcludeMapID";

        public static readonly string CountMapInstancesForMap = @"SELECT COUNT(*)
                FROM MapInstances
                WHERE CustomerGUID = @CustomerGUID
                  AND MapID = @MapID";

        public static readonly string DeleteZone = @"DELETE FROM Maps
                WHERE CustomerGUID = @CustomerGUID
                  AND MapID = @MapID";

        public static readonly string GetZoneName = @"SELECT M.ZoneName
                FROM Maps M
                INNER JOIN MapInstances MI ON MI.CustomerGUID = M.CustomerGUID
                                          AND MI.MapID = M.MapID
                WHERE M.CustomerGUID = @CustomerGUID
                  AND MI.MapInstanceID = @MapInstanceID";

        public static readonly string RemoveMapInstances = @"DELETE FROM MapInstances WHERE CustomerGUID = @CustomerGUID AND MapInstanceID IN @MapInstances";

        public static readonly string RemoveAllMapInstancesForWorldServer = @"DELETE FROM MapInstances WHERE CustomerGUID = @CustomerGUID AND WorldServerId = @WorldServerId";

        public static readonly string UpdateMapInstanceStatus = @"UPDATE MapInstances
                SET Status = @MapInstanceStatus
                WHERE CustomerGUID = @CustomerGUID
                  AND MapInstanceID = @MapInstanceID";

        #endregion

        #region Party Queries

        public static readonly string GetPartyId = @"SELECT PM.PartyID FROM PartyMember PM
                INNER JOIN Characters C ON C.CharacterID = PM.CharacterID 
                AND C.CharName = @CharName
                WHERE PM.CustomerGUID = @CustomerGUID";

        public static readonly string GetGuildId = @"SELECT GM.GuildID FROM GuildMember GM
                INNER JOIN Characters C ON C.CharacterID = GM.CharacterID 
                AND C.CharName = @CharName
                WHERE GM.CustomerGUID = @CustomerGUID";

        #endregion

        #region Global Data Queries

        public static readonly string AddGlobalData = @"INSERT INTO GlobalData (CustomerGUID, GlobalDataKey, GlobalDataValue)
        VALUES (@CustomerGUID, @GlobalDataKey, @GlobalDataValue)";

        public static readonly string GetGlobalDataByGlobalDataKey = @"SELECT CustomerGUID, GlobalDataKey, GlobalDataValue
            FROM GlobalData GD
            WHERE GD.CustomerGUID = @CustomerGUID
            AND GD.GlobalDataKey = @GlobalDataKey";

        public static readonly string UpdateGlobalData = @"UPDATE GlobalData
                SET GlobalDataValue = @GlobalDataValue
                WHERE CustomerGUID = @CustomerGUID
                  AND GlobalDataKey = @GlobalDataKey";

        #endregion

        #region User Queries

        public static readonly string Logout = @"DELETE FROM UserSessions WHERE CustomerGUID=@CustomerGuid AND UserSessionGUID=@UserSessionGUID";

        #endregion
    }
}
