using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class GenericQueries
    {

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

	    public static readonly string GetCharacterByName = @"SELECT *
				FROM Characters
				WHERE CustomerGUID = @CustomerGUID
				  AND CharName = @CharName";

	    public static readonly string GetCharacterIDByName = @"SELECT CharacterID
				FROM Characters
				WHERE CustomerGUID = @CustomerGUID
				  AND CharName = @CharName";

	    public static readonly string GetCharByCharName = @"SELECT C.*, MI.Port, WS.ServerIP, CMI.MapInstanceID, CL.ClassName
				FROM Characters C
				INNER JOIN Class CL
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

	    public static readonly string GetCharacterCustomDataByName = @"SELECT *
				FROM CustomCharacterData CCD
				INNER JOIN Characters C ON C.CharacterID = CCD.CharacterID
				WHERE CCD.CustomerGUID = @CustomerGUID
				  AND C.CharName = @CharName";

	    public static readonly string GetPlayerGroupIDByType = @"SELECT COALESCE(PG.PlayerGroupID, 0)
				FROM PlayerGroupCharacters PGC
				INNER JOIN PlayerGroup PG ON PG.PlayerGroupID = PGC.PlayerGroupID
				WHERE PGC.CustomerGUID = @CustomerGUID
				  AND PGC.CharacterID = @CharacterID
				  AND PG.PlayerGroupTypeID = @PlayerGroupType";

	    public static readonly string HasCustomCharacterDataForField = @"SELECT 1
				FROM CustomCharacterData
				WHERE CustomerGUID = @CustomerGUID
				  AND CustomFieldName = @CustomFieldName
				  AND CharacterID = @CharacterID";

	    public static readonly string RemoveCharacterFromAllInstances = @"DELETE FROM CharOnMapInstance 
				WHERE CustomerGUID = @CustomerGUID
				  AND CharacterID = @CharacterID";

	    public static readonly string RemoveCharacterFromInstances = @"DELETE FROM CharOnMapInstance WHERE CustomerGUID = @CustomerGUID AND MapInstanceID IN @MapInstances";

	    public static readonly string UpdateCharacterCustomDataField = @"UPDATE CustomCharacterData
				SET FieldValue = @FieldValue
				WHERE CustomerGUID = @CustomerGUID
				  AND CustomFieldName = @CustomFieldName
				  AND CharacterID = @CharacterID";

	    public static readonly string UpdateCharacterZone = @"UPDATE Characters
				SET	MapName = @ZoneName
				WHERE CharacterID = @CharacterID
				  AND CustomerGUID = @CustomerGUID";

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
				GROUP BY WS.WorldServerID, WS.ServerIP, WS.InternalServerIP, WS.Port, WS.MaxNumberOfInstances, WS.StartingMapInstancePort
				ORDER BY COALESCE (COUNT(MI.MapInstanceID),0)";

        public static readonly string GetPortsInUseByWorldServer = @"SELECT Port
				FROM MapInstances
				WHERE CustomerGUID = @CustomerGUID
				  AND WorldServerID = @WorldServerID
				ORDER BY Port";

        #endregion

        #region Zone Queries

        public static readonly string GetMapInstance = @"SELECT *
				FROM MapInstances
				WHERE CustomerGUID = @CustomerGUID
				  AND MapInstanceID = @MapInstanceID";

        public static readonly string GetMapInstancesByMapAndGroup = @"SELECT WS.ServerIP AS ServerIP
					, WS.InternalServerIP AS WorldServerIP
					, WS.Port AS WorldServerPort
					, MI.Port
     				, MI.MapInstanceID
     				, WS.WorldServerID
     				, MI.Status AS MapInstanceStatus
				FROM WorldServers WS
				LEFT JOIN MapInstances MI ON MI.WorldServerID = WS.WorldServerID AND MI.CustomerGUID = WS.CustomerGUID
				LEFT JOIN CharOnMapInstance CMI ON CMI.MapInstanceID = MI.MapInstanceID AND CMI.CustomerGUID = MI.CustomerGUID
				WHERE MI.MapID = @MapID
				AND WS.ActiveStartTime IS NOT NULL
				AND WS.CustomerGUID = @CustomerGUID
				AND MI.NumberOfReportedPlayers < @SoftPlayerCap 
				AND (MI.PlayerGroupID = @PlayerGroupID OR @PlayerGroupID = 0)
				AND MI.Status = 2
				GROUP BY MI.MapInstanceID, WS.ServerIP, MI.Port, WS.WorldServerID, WS.InternalServerIP, WS.Port, MI.Status
				ORDER BY COUNT(DISTINCT CMI.CharacterID)";

        public static readonly string GetMapInstanceStatus = @"SELECT Status
				FROM MapInstances
				WHERE CustomerGUID = @CustomerGUID
				  AND MapInstanceID = @MapInstanceID";

        public static readonly string GetMapByZoneName = @"SELECT *
				FROM Maps
				WHERE CustomerGUID = @CustomerGUID
				  AND ZoneName = @ZoneName";

		 public static readonly string GetZoneName = @"SELECT M.ZoneName
				FROM Maps M
				INNER JOIN MapInstances MI ON MI.CustomerGUID = M.CustomerGUID
				                          AND MI.MapID = M.MapID
				WHERE M.CustomerGUID = @CustomerGUID
				  AND MI.MapInstanceID = @MapInstanceID";

	        public static readonly string RemoveMapInstances = @"DELETE FROM MapInstances WHERE CustomerGUID = @CustomerGUID AND MapInstanceID IN @MapInstances";

        #endregion

		#region Global Data Queries

        public static readonly string GetGlobalDataByGlobalDataKey = @"SELECT CustomerGUID, GlobalDataKey, GlobalDataValue
			FROM GlobalData GD
			WHERE GD.CustomerGUID = @CustomerGUID
			AND GD.GlobalDataKey = @GlobalDataKey";

        #endregion
    }
}