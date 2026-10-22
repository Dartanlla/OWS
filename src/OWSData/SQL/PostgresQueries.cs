using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class PostgresQueries
    {

	    #region To Refactor

	    public static readonly string AddOrUpdateWorldServerSQL = @"INSERT INTO WorldServers (CustomerGUID, ServerIP, MaxNumberOfInstances, Port, ServerStatus, InternalServerIP,
                          StartingMapInstancePort, ZoneServerGUID, ServerRegion)
    (SELECT @CustomerGUID::UUID           AS CustomerGUID,
            @ServerIP                     AS ServerIP,
            @MaxNumberOfInstances         AS MaxNumberOfInstances,
            8081                          AS Port,
            0                             AS ServerStatus,
            @InternalServerIP             AS InternalServerIP,
            @StartingMapInstancePort      AS StartingMapInstancePort,
            @ZoneServerGUID::UUID         AS ZoneServerGUID,
            @ServerRegion                 AS ServerRegion)
ON CONFLICT ON CONSTRAINT ak_zoneservers
    DO UPDATE SET ServerIP                = @ServerIP,
                  MaxNumberOfInstances    = @MaxNumberOfInstances,
                  Port                    = 8081,
                  ServerStatus            = 0,
                  InternalServerIP        = @InternalServerIP,
                  StartingMapInstancePort = @StartingMapInstancePort,
                  ZoneServerGUID          = @ZoneServerGUID::UUID,
                  ServerRegion            = @ServerRegion;";

	    public static readonly string GetAbilities = @"SELECT AB.*, AT.AbilityTypeName
				FROM Abilities AB
				INNER JOIN AbilityTypes AT
					ON AT.AbilityTypeID=AB.AbilityTypeID
				WHERE AB.CustomerGUID=@CustomerGUID
				ORDER BY AB.AbilityName";

		public static readonly string GetUserSessionSQL = @"SELECT US.CustomerGUID, US.UserGUID, US.UserSessionGUID, US.LoginDate, US.SelectedCharacterName,
	            U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role,
	            C.CharacterID, C.CharName, C.X, C.Y, C.Z, C.RX, C.RY, C.RZ, C.MapName as ZoneName
	            FROM UserSessions US
	            INNER JOIN Users U
		            ON U.UserGUID=US.UserGUID
	            LEFT JOIN Characters C
		            ON C.CustomerGUID=US.CustomerGUID
		            AND C.CharName=US.SelectedCharacterName
	            WHERE US.CustomerGUID=@CustomerGUID::UUID
	            AND US.UserSessionGUID=@UserSessionGUID::UUID";

        public static readonly string GetUserSessionOnlySQL = @"SELECT US.CustomerGUID, US.UserGUID, US.UserSessionGUID, US.LoginDate, US.SelectedCharacterName
	            FROM UserSessions US
	            WHERE US.CustomerGUID=@CustomerGUID::UUID
	            AND US.UserSessionGUID=@UserSessionGUID";

        public static readonly string GetUserSQL = @"SELECT U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role
	            FROM Users U
	            WHERE U.CustomerGUID=@CustomerGUID::UUID
	            AND U.UserGUID=@UserGUID";

        public static readonly string GetUserFromEmailSQL = @"SELECT U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role
	            FROM Users U
	            WHERE U.CustomerGUID=@CustomerGUID::UUID
	            AND LOWER(TRIM(U.Email))=LOWER(TRIM(@Email))";

        public static readonly string GetCharacterByNameSQL = @"SELECT C.CharacterID, C.CharName, C.X, C.Y, C.Z, C.RX, C.RY, C.RZ, C.MapName as ZoneName
	            FROM Characters C
	            WHERE C.CustomerGUID=@CustomerGUID::UUID
	            AND C.CharName=@CharacterName";

		public static readonly string GetWorldServerSQL = @"SELECT WorldServerID
				FROM WorldServers
				WHERE CustomerGUID=@CustomerGUID::UUID
				AND ZoneServerGUID=@ZoneServerGUID::UUID";

		public static readonly string UpdateNumberOfPlayersSQL = @"UPDATE MapInstances
				SET NumberOfReportedPlayers = @NumberOfReportedPlayers,
				ServerFrameMsAvg = @ServerFrameMsAvg,
				ServerFrameMsMax = @ServerFrameMsMax,
				ServerCpuPct = @ServerCpuPct,
				MemoryUsedMB = @MemoryUsedMB,
				UptimeSeconds = @UptimeSeconds,
				BuildCL = @BuildCL,
				LastUpdateFromServer=NOW(),
				LastServerEmptyDate=(CASE WHEN @NumberOfReportedPlayers = 0 AND (NumberOfReportedPlayers > 0 OR LastServerEmptyDate IS NULL) THEN NOW() ELSE (CASE WHEN NumberOfReportedPlayers = 0 AND @NumberOfReportedPlayers > 0 THEN NULL ELSE LastServerEmptyDate END) END),
				Status=2
				WHERE CustomerGUID=@CustomerGUID
					AND MapInstanceID=@ZoneInstanceID
					AND Status <> 3";

		public static readonly string AcquireMapAllocationLock = @"SELECT pg_advisory_xact_lock(hashtextextended(CAST(@CustomerGUID AS text), 0))";

		public static readonly string UpdateWorldServerSQL = @"UPDATE WorldServers
				SET ActiveStartTime=NOW(),
				ServerStatus=1
				WHERE CustomerGUID=@CustomerGUID::UUID
				AND WorldServerID=@WorldServerID";

        #endregion

        #region Character Queries

        public static readonly string AddAbilityToCharacter = @"
        INSERT INTO CharHasAbilities (CustomerGUID, CharacterID, AbilityID, AbilityLevel, CharHasAbilitiesCustomJSON)
        SELECT @CustomerGUID::UUID,
            (SELECT C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName AND C.CustomerGUID = @CustomerGUID::UUID ORDER BY C.CharacterID LIMIT 1),
            (SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName AND A.CustomerGUID = @CustomerGUID::UUID ORDER BY A.AbilityID LIMIT 1),
            @AbilityLevel,
            @CharHasAbilitiesCustomJSON";

        public static readonly string AddCharacterUsingDefaultCharacterValues = @"
        INSERT INTO Characters (CustomerGUID, UserGUID, Email, CharName, MapName, X, Y, Z, RX, RY, RZ, Perception, Acrobatics, Climb, Stealth, ClassID)
        SELECT @CustomerGUID::UUID, @UserGUID::UUID, '', @CharacterName, DCR.StartingMapName, DCR.X, DCR.Y, DCR.Z, DCR.RX, DCR.RY, DCR.RZ, 0, 0, 0, 0, 0
        FROM DefaultCharacterValues DCR
        WHERE DCR.CustomerGUID = @CustomerGUID::UUID
            AND DCR.DefaultSetName = @DefaultSetName
        RETURNING CharacterID";

        public static readonly string RemoveAbilityFromCharacter = @"
        DELETE FROM CharHasAbilities
        WHERE CustomerGUID = @CustomerGUID::UUID
            AND CharacterID = (SELECT C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName ORDER BY C.CharacterID LIMIT 1)
            AND AbilityID = (SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName ORDER BY A.AbilityID LIMIT 1)";

        public static readonly string RemoveCharactersFromAllInactiveInstances = @"
        DELETE FROM CharOnMapInstance
        WHERE CustomerGUID = @CustomerGUID::UUID
        AND CharacterID IN (
            SELECT C.CharacterID
            FROM Characters C
            INNER JOIN Users U ON U.CustomerGUID = C.CustomerGUID AND U.UserGUID = C.UserGUID
            WHERE U.LastAccess < NOW() - (@CharacterMinutes || ' minutes')::INTERVAL AND C.CustomerGUID = @CustomerGUID::UUID
        )";

        public static readonly string UpdateAbilityOnCharacter = @"
        INSERT INTO CharAbilities (
            CustomerGUID,
            CharacterID,
            AbilityIDTag,
            CurrentAbilityLevel,
            ActualAbilityLevel,
            CustomData
        )
        SELECT
            @CustomerGUID::UUID,
            C.CharacterID,
            @AbilityIDTag,
            @CurrentAbilityLevel,
            @ActualAbilityLevel,
            @CustomData
        FROM Characters C
        WHERE C.CharName = @CharName
        AND C.CustomerGUID = @CustomerGUID::UUID
        ON CONFLICT (CustomerGUID, CharacterID, AbilityIDTag)
        DO UPDATE SET
            CurrentAbilityLevel = EXCLUDED.CurrentAbilityLevel,
            ActualAbilityLevel = EXCLUDED.ActualAbilityLevel,
            CustomData = EXCLUDED.CustomData";

        public const string UpsertCharacterAbilitiesJson = @"
        WITH abil AS (
            SELECT
                @CustomerGUID                             ::uuid   AS customerguid,
                C.characterid                                       AS characterid,
                J->>'abilityIdTag'                                  AS abilityidtag,
                (J->>'currentAbilityLevel')::int4                   AS currentabilitylevel,
                (J->>'actualAbilityLevel') ::int4                   AS actualabilitylevel,
                COALESCE(J->>'CustomData','')                       AS customdata
            FROM   jsonb_array_elements(@AbilitiesJson::jsonb) J
            JOIN   characters C
                   ON  C.customerguid = @CustomerGUID
                   AND C.charname     = @CharName
        )
        INSERT INTO charabilities
               (customerguid,
                characterid,
                abilityidtag,
                currentabilitylevel,
                actualabilitylevel,
                customdata)
        SELECT  *
        FROM   abil
        ON CONFLICT (customerguid, characterid, abilityidtag)
        DO UPDATE
           SET currentabilitylevel = EXCLUDED.currentabilitylevel,
               actualabilitylevel  = EXCLUDED.actualabilitylevel,
               customdata          = EXCLUDED.customdata;";

        public static readonly string AddQuestToDatabase = @"
        INSERT INTO Quest (
            CustomerGUID,
            QuestIDTag,
            QuestOverview,
            QuestTasks,
            QuestClassName,
            CustomData
        )
        SELECT
            @CustomerGUID::UUID,
            @QuestIDTag,
            @QuestOverview,
            @QuestTasks,
            @QuestClassName,
            @CustomData
        WHERE NOT EXISTS (
            SELECT 1 FROM Quest
            WHERE Quest.QuestIDTag = @QuestIDTag
            AND Quest.CustomerGUID = @CustomerGUID::UUID
        )";

        // Postgres form of GenericQueries.UpdateCharacterQuest, which is T-SQL (IF EXISTS / UPDATE alias
        // FROM) and errors out here. Upserts on the CharQuests primary key, so repeat saves cannot
        // duplicate rows.
        public static readonly string UpdateCharacterQuest = @"
        INSERT INTO CharQuests (
            CustomerGUID,
            CharacterID,
            QuestIDTag,
            QuestJournalTagContainer,
            CustomData
        )
        SELECT
            @CustomerGUID::UUID,
            C.CharacterID,
            @QuestIDTag,
            @QuestJournalTagContainer,
            @CustomData
        FROM Characters C
        WHERE C.CharName = @CharName
        AND C.CustomerGUID = @CustomerGUID::UUID
        ON CONFLICT (CustomerGUID, CharacterID, QuestIDTag)
        DO UPDATE SET
            QuestJournalTagContainer = EXCLUDED.QuestJournalTagContainer,
            CustomData               = EXCLUDED.CustomData";

        #endregion

        #region User Queries

        public static readonly string UpdateUserLastAccess = @"UPDATE Users
				SET LastAccess = NOW()
                WHERE CustomerGUID = @CustomerGUID
                AND UserGUID IN (
                    SELECT C.UserGUID
                      FROM Characters C
                      WHERE C.CustomerGUID = @CustomerGUID AND C.CharName = @CharName)";

		#endregion

		#region Zone Queries

		public static readonly string AddMapInstance = @"INSERT INTO MapInstances (CustomerGUID, WorldServerID, MapID, Port, Status, PlayerGroupID, LastUpdateFromServer)
		VALUES (@CustomerGUID, @WorldServerID, @MapID, @Port, 1, @PlayerGroupID, NOW())
		RETURNING mapinstanceid";

		public static readonly string GetAllInactiveMapInstances = @"SELECT MapInstanceID
                FROM MapInstances
                WHERE LastUpdateFromServer < CURRENT_TIMESTAMP - (@MapMinutes || ' minutes')::INTERVAL AND CustomerGUID = @CustomerGUID";

		public static readonly string GetMapInstancesByWorldServerID = @"SELECT MI.*, M.SoftPlayerCap, M.HardPlayerCap, M.MapName, M.MapMode, M.MinutesToShutdownAfterEmpty,
		       COALESCE(FLOOR(EXTRACT(EPOCH FROM NOW()::TIMESTAMP - MI.LastServerEmptyDate) / 60), 0)::INT  AS MinutesServerHasBeenEmpty,
		       COALESCE(FLOOR(EXTRACT(EPOCH FROM NOW()::TIMESTAMP - MI.LastUpdateFromServer) / 60), 0)::INT AS MinutesSinceLastUpdate
				FROM Maps M
				INNER JOIN MapInstances MI ON MI.MapID = M.MapID
				WHERE M.CustomerGUID = @CustomerGUID
				AND MI.WorldServerID = @WorldServerID";

        public static readonly string GetLiveZoneInstances = @"SELECT MI.MapInstanceID,
                    MI.WorldServerID,
                    WS.ServerRegion,
                    M.ZoneName,
                    MI.Port,
                    MI.Status,
                    MI.NumberOfReportedPlayers,
                    MI.LastUpdateFromServer,
                    MI.ServerFrameMsAvg,
                    MI.ServerFrameMsMax,
                    MI.ServerCpuPct,
                    MI.MemoryUsedMB,
                    MI.UptimeSeconds,
                    MI.BuildCL,
                    (SELECT STRING_AGG(C.CharName, ', ' ORDER BY C.CharName)
                        FROM CharOnMapInstance CMI
                        INNER JOIN Characters C ON C.CharacterID = CMI.CharacterID AND C.CustomerGUID = CMI.CustomerGUID
                        WHERE CMI.CustomerGUID = MI.CustomerGUID AND CMI.MapInstanceID = MI.MapInstanceID) AS OnlineCharacters
                FROM MapInstances MI
                INNER JOIN Maps M ON M.MapID = MI.MapID AND M.CustomerGUID = MI.CustomerGUID
                INNER JOIN WorldServers WS ON WS.WorldServerID = MI.WorldServerID AND WS.CustomerGUID = MI.CustomerGUID
                WHERE MI.CustomerGUID = @CustomerGUID
                ORDER BY M.ZoneName, MI.MapInstanceID";

        public static readonly string GetZoneInstancesOfZone = @"SELECT M.MapID,
                    M.MapName,
                    M.ZoneName,
                    M.WorldCompContainsFilter,
                    M.WorldCompListFilter,
                    CAST(M.MapMode AS VARCHAR) AS MapMode,
                    M.SoftPlayerCap,
                    M.HardPlayerCap,
                    M.MinutesToShutdownAfterEmpty,
                    MI.MapInstanceID,
                    MI.WorldServerID,
                    MI.Port,
                    MI.Status,
                    MI.PlayerGroupID,
                    MI.NumberOfReportedPlayers,
                    MI.LastUpdateFromServer,
                    MI.LastServerEmptyDate
                FROM Maps M
                LEFT JOIN MapInstances MI ON MI.MapID = M.MapID AND MI.CustomerGUID = M.CustomerGUID
                WHERE M.CustomerGUID = @CustomerGUID
                  AND M.ZoneName = @ZoneName
                ORDER BY MI.MapInstanceID";

        public static readonly string GetZoneInstancesByZoneAndGroup = @"SELECT WS.ServerIP AS ServerIP
					, WS.InternalServerIP AS WorldServerIP
					, WS.Port AS WorldServerPort
					, MI.Port
     				, MI.MapInstanceID
     				, WS.WorldServerID
     				, MI.Status AS MapInstanceStatus
				FROM WorldServers WS
				INNER JOIN MapInstances MI
					ON MI.WorldServerID = WS.WorldServerID
					AND MI.CustomerGUID = WS.CustomerGUID
				LEFT JOIN CharOnMapInstance CMI
					ON CMI.MapInstanceID = MI.MapInstanceID
					AND CMI.CustomerGUID = MI.CustomerGUID
				WHERE MI.MapID = @MapID
				AND WS.ActiveStartTime IS NOT NULL
				AND WS.CustomerGUID = @CustomerGUID
				--Region routing: an instance already running on another region's host must not be reused,
				--or this path would silently bypass the region filter that spin-up applies.
				AND WS.ServerRegion = @ServerRegion
				AND MI.Status IN (1, 2)
				--Ready servers heartbeat every ~10s (UpdateNumberOfPlayers, which is also what sets Status=2), so a
				--quiet ready row is a frozen server; without this filter players are routed to it until the 2-minute
				--CleanUpInstances sweep deletes the row. Spawning rows (Status 1) have no heartbeat yet and pass through.
				AND (MI.Status <> 2 OR MI.LastUpdateFromServer >= NOW() - INTERVAL '30 seconds')
				AND (MI.PlayerGroupID = @PlayerGroupID OR @PlayerGroupID = 0)
				GROUP BY MI.MapInstanceID, WS.ServerIP, MI.Port, WS.WorldServerID, WS.InternalServerIP, WS.Port, MI.Status, MI.NumberOfReportedPlayers
				HAVING GREATEST(COALESCE(MI.NumberOfReportedPlayers, 0), COUNT(DISTINCT CMI.CharacterID)) < @HardPlayerCap
					OR COUNT(DISTINCT CASE WHEN CMI.CharacterID = @CharacterID THEN CMI.CharacterID END) > 0
				ORDER BY CASE WHEN MI.Status = 2 THEN 0 ELSE 1 END,
					GREATEST(COALESCE(MI.NumberOfReportedPlayers, 0), COUNT(DISTINCT CMI.CharacterID)),
					MI.MapInstanceID
				LIMIT 1";

		//Postgres needs = ANY(@MapInstances), not the GenericQueries "IN @MapInstances" form. Dapper only
		//text-expands an enumerable parameter into "(@p1,@p2,...)" for providers without array support; for
		//Npgsql it binds the list as a single native array and leaves the SQL untouched, so "IN @MapInstances"
		//reaches Postgres verbatim as "IN $1" and fails to parse.
		public static readonly string RemoveCharacterFromInstances = @"DELETE FROM CharOnMapInstance WHERE CustomerGUID = @CustomerGUID AND MapInstanceID = ANY(@MapInstances)";

		public static readonly string RemoveMapInstances = @"DELETE FROM MapInstances WHERE CustomerGUID = @CustomerGUID AND MapInstanceID = ANY(@MapInstances)";

        public static readonly string GetZoneInstanceStatusForShutdown = @"SELECT Status
                FROM MapInstances
                WHERE CustomerGUID = @CustomerGUID
                  AND MapInstanceID = @ZoneInstanceID
                FOR UPDATE";

        public static readonly string AddZone = @"INSERT INTO Maps
                (CustomerGUID, MapName, MapData, Width, Height, ZoneName, WorldCompContainsFilter, WorldCompListFilter, SoftPlayerCap, HardPlayerCap, MapMode, MinutesToShutdownAfterEmpty)
                VALUES
                (@CustomerGUID, @MapName, @MapData, 1, 1, @ZoneName, COALESCE(@WorldCompContainsFilter, ''), COALESCE(@WorldCompListFilter, ''), @SoftPlayerCap, @HardPlayerCap, @MapMode, @MinutesToShutdownAfterEmpty)";

        public static readonly string UpdateZone = @"UPDATE Maps
                SET MapName = @MapName,
                    MapData = @MapData,
                    ZoneName = @ZoneName,
                    WorldCompContainsFilter = COALESCE(@WorldCompContainsFilter, ''),
                    WorldCompListFilter = COALESCE(@WorldCompListFilter, ''),
                    SoftPlayerCap = @SoftPlayerCap,
                    HardPlayerCap = @HardPlayerCap,
                    MapMode = @MapMode,
                    MinutesToShutdownAfterEmpty = @MinutesToShutdownAfterEmpty
                WHERE CustomerGUID = @CustomerGUID
                  AND MapID = @MapID";

        public static readonly string RemoveCharactersFromZoneInstance = @"DELETE FROM CharOnMapInstance
                WHERE CustomerGUID = @CustomerGUID
                  AND MapInstanceID = @ZoneInstanceID
                  AND EXISTS (
                      SELECT 1
                      FROM MapInstances
                      WHERE CustomerGUID = @CustomerGUID
                        AND MapInstanceID = @ZoneInstanceID
                        AND Status = 3
                  )";

        public static readonly string RemoveZoneInstance = @"DELETE FROM MapInstances
                WHERE CustomerGUID = @CustomerGUID
                  AND MapInstanceID = @ZoneInstanceID
                  AND Status = 3";

		#endregion

        #region Action House

        public static readonly string GetActionHousePlayerItems = @"WITH character_check AS (
            SELECT CharacterID,
                CASE
                    WHEN CharacterID IS NULL THEN 'Character not found'
                    ELSE NULL
                END AS ErrorMessage
            FROM Characters
            WHERE CustomerGUID = @CustomerGUID AND CharName = @CharacterName
        ),
        action_house_items AS (
            SELECT
                AHPI.SlotIndex,
                AHPI.ItemIDTag,
                AHPI.InProgressQuantity,
                AHPI.TotalQuantity,
                AHPI.SetPrice,
                AHPI.TotalItemQuantityInStorage,
                AHPI.TotalCurrencyInStorage,
                AHPI.ActionHouseActionID
            FROM ActionHousePlayerItem AHPI
            JOIN character_check cc ON AHPI.CharacterID = cc.CharacterID
            WHERE AHPI.CustomerGUID = @CustomerGUID
        )
        SELECT
            CASE
                WHEN cc.ErrorMessage IS NOT NULL THEN cc.ErrorMessage
                WHEN NOT EXISTS (SELECT 1 FROM action_house_items) THEN 'No items found'
                ELSE NULL
            END AS ErrorMessage,
            CASE
                WHEN cc.ErrorMessage IS NULL AND EXISTS (SELECT 1 FROM action_house_items) THEN 'Items retrieved successfully'
                ELSE NULL
            END AS SuccessMessage,
            ahi.*
        FROM character_check cc
        LEFT JOIN action_house_items ahi ON 1=1;";

        public static string IsCharacterOwnedByOtherZoneInstance = @"
            SELECT EXISTS (
                SELECT 1
                FROM CharOnMapInstance COMI
                INNER JOIN Characters C
                    ON C.CharacterID = COMI.CharacterID
                   AND C.CustomerGUID = COMI.CustomerGUID
                WHERE C.CustomerGUID = @CustomerGUID
                  AND C.CharName = @CharName
                  AND COMI.MapInstanceID <> @CallerZoneInstanceID
            );";
        }

        #endregion
}
