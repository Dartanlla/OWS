using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class GenericQueries
    {
        #region Character Queries

        public static readonly string AddCharacterToInstance = @"INSERT INTO CharOnMapInstance (CustomerGUID, MapInstanceID, CharacterID)
		VALUES (@CustomerGUID, @MapInstanceID, @CharacterID)";
	    
        public static readonly string GetCharacterIDByName = @"SELECT CharacterID
				FROM Characters
				WHERE CustomerGUID = @CustomerGUID
				  AND CharName = @CharName";

        public static readonly string GetCharacterExtendedByName = @"SELECT C.*, MI.Port, WS.ServerIP, CMI.MapInstanceID, CL.ClassName
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
	    
        public static readonly string RemoveCharacterFromAllInstances = @"DELETE FROM CharOnMapInstance 
				WHERE CustomerGUID = @CustomerGUID
				  AND CharacterID = @CharacterID";
		
        public static readonly string UpdateCharacterZone = @"UPDATE Characters
				SET	MapName = @ZoneName
				WHERE CharacterID = @CharacterID
				  AND CustomerGUID = @CustomerGUID";

        #endregion
	    
        #region Zone Queries

        public static readonly string GetMapInstanceStatus = @"SELECT Status
				FROM MapInstances
				WHERE CustomerGUID = @CustomerGUID
				  AND MapInstanceID = @MapInstanceID";
	    
        public static readonly string GetZoneName = @"SELECT M.ZoneName
				FROM Maps M
				INNER JOIN MapInstances MI ON MI.CustomerGUID = M.CustomerGUID
				                          AND MI.MapID = M.MapID
				WHERE M.CustomerGUID = @CustomerGUID
				  AND MI.MapInstanceID = @MapInstanceID";

        #endregion
    }
}