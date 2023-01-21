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
	    
	    public static readonly string AddCharacterCustomDataField = @"INSERT INTO CustomCharacterData (CustomerGUID, CharacterID, CustomFieldName, FieldValue)
		VALUES (@CustomerGUID, @CharacterID, @CustomFieldName, @FieldValue)";
	    
	    public static readonly string GetCharacterIDFromName = @"SELECT CharacterID
				FROM Characters
				WHERE CustomerGUID = @CustomerGUID
				  AND CharName = @CharName";

	    public static readonly string GetCharacterCustomDataByName = @"SELECT *
				FROM CustomCharacterData CCD
				INNER JOIN Characters C ON C.CharacterID = CCD.CharacterID
				WHERE CCD.CustomerGUID = @CustomerGUID
				  AND C.CharName = @CharName";
	    
	    public static readonly string HasCustomCharacterDataForField = @"SELECT 1
				FROM CustomCharacterData
				WHERE CustomerGUID = @CustomerGUID
				  AND CustomFieldName = @CustomFieldName
				  AND CharacterID = @CharacterID";
	    
	    public static readonly string RemoveCharacterFromAllInstances = @"DELETE FROM CharOnMapInstance 
				WHERE CustomerGUID = @CustomerGUID
				  AND CharacterID = @CharacterID";
		
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

		#region Global Data Queries

        public static readonly string GetGlobalDataByGlobalDataKey = @"SELECT CustomerGUID, GlobalDataKey, GlobalDataValue  
			FROM GlobalData GD
			WHERE GD.CustomerGUID=@CustomerGUID
			AND GD.GlobalDataKey=@GlobalDataKey";

        #endregion

        #region Zone Queries

        public static readonly string GetZoneName = @"SELECT M.ZoneName
				FROM Maps M
				INNER JOIN MapInstances MI ON MI.CustomerGUID = M.CustomerGUID
				                          AND MI.MapID = M.MapID
				WHERE MI.MapInstanceID = @MapInstanceID";

	    #endregion
    }
}
