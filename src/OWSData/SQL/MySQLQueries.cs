using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class MySQLQueries
    {

	    #region To Refactor

	    public static readonly string AddAbilityToCharacterSQL = @"INSERT INTO CharHasAbilities (CustomerGUID, CharacterID, AbilityID, AbilityLevel, CharHasAbilitiesCustomJSON)
				SELECT @CustomerGUID AS CustomerGUID , 
					(SELECT C.CharacterID FROM Characters C WHERE C.CharName=@CharacterName AND C.CustomerGUID=@CustomerGUID LIMIT 1),
					(SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName=@AbilityName AND A.CustomerGUID=@CustomerGUID LIMIT 1),
					@AbilityLevel,
					@CharHasAbilitiesCustomJSON
				WHERE NOT EXISTS (SELECT 1 FROM CharHasAbilities CHA 
									INNER JOIN Characters C 
										ON C.CharacterID=CHA.CharacterID 
										AND C.CustomerGUID=CHA.CustomerGUID
									INNER JOIN Abilities A 
										ON A.AbilityID=CHA.AbilityID 
										AND A.CustomerGUID=CHA.CustomerGUID
									WHERE CHA.CustomerGUID=@CustomerGUID 
										AND C.CharName=@CharacterName 
										AND A.AbilityName=@AbilityName)";

	    public static readonly string AddOrUpdateWorldServerSQL = @"call AddOrUpdateWorldServer(
            @CustomerGUID,
            @ZoneServerGUID,
            @ServerIP,
            @MaxNumberOfInstances,
            @InternalServerIP,
            @StartingMapInstancePort)";

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
	            WHERE US.CustomerGUID=@CustomerGUID
	            AND US.UserSessionGUID=@UserSessionGUID";

		public static readonly string GetUserSessionOnlySQL = @"SELECT US.CustomerGUID, US.UserGUID, US.UserSessionGUID, US.LoginDate, US.SelectedCharacterName
	            FROM UserSessions US
	            WHERE US.CustomerGUID=@CustomerGUID
	            AND US.UserSessionGUID=@UserSessionGUID";

		public static readonly string GetUserSQL = @"SELECT U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role
	            FROM Users U
	            WHERE U.CustomerGUID=@CustomerGUID
	            AND U.UserGUID=@UserGUID";

		public static readonly string GetUserFromEmailSQL = @"SELECT U.Email, U.FirstName, U.LastName, U.CreateDate, U.LastAccess, U.Role
	            FROM Users U
	            WHERE U.CustomerGUID=@CustomerGUID
	            AND U.Email=@Email";

		public static readonly string GetCharacterByNameSQL = @"SELECT C.CharacterID, C.CharName, C.X, C.Y, C.Z, C.RX, C.RY, C.RZ, C.MapName as ZoneName
	            FROM Characters C
	            WHERE C.CustomerGUID=@CustomerGUID
	            AND C.CharName=@CharacterName";

		public static readonly string GetWorldServerSQL = @"SELECT WorldServerID
				FROM WorldServers 
				WHERE CustomerGUID=@CustomerGUID
				AND ZoneServerGUID=@ZoneServerGUID";

		public static readonly string RemoveAbilityFromCharacterSQL = @"DELETE FROM CharHasAbilities
				WHERE CustomerGUID=@CustomerGUID
					AND CharacterID=(SELECT C.CharacterID FROM Characters C WHERE C.CharName=@CharacterName LIMIT 1)
					AND AbilityID=(SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName=@AbilityName LIMIT 1)";

		public static readonly string UpdateAbilityOnCharacterSQL = @"UPDATE CharHasAbilities
				SET AbilityLevel = @AbilityLevel,
				CharHasAbilitiesCustomJSON = @CharHasAbilitiesCustomJSON
				WHERE CustomerGUID=@CustomerGUID
					AND CharacterID=(SELECT C.CharacterID FROM Characters C WHERE C.CharName=@CharacterName LIMIT 1)
					AND AbilityID=(SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName=@AbilityName LIMIT 1)";

		public static readonly string UpdateNumberOfPlayersSQL = @"UPDATE MapInstances
				SET NumberOfReportedPlayers=@NumberOfReportedPlayers,
				LastUpdateFromServer=NOW(),
				LastServerEmptyDate=(CASE WHEN @NumberOfReportedPlayers = 0 AND NumberOfReportedPlayers > 0 THEN NOW() ELSE (CASE WHEN NumberOfReportedPlayers = 0 AND @NumberOfReportedPlayers > 0 THEN NULL ELSE LastServerEmptyDate END) END),
				Status=2
				WHERE CustomerGUID=@CustomerGUID
					AND MapInstanceID=@ZoneInstanceID";

		public static readonly string UpdateWorldServerSQL = @"UPDATE WorldServers
				SET ActiveStartTime=NOW(),
				ServerStatus=1
				WHERE CustomerGUID=@CustomerGUID
				AND WorldServerID=@WorldServerID";

		#endregion

		#region Character Queries

		public static readonly string RemoveCharactersFromAllInactiveInstances = @"DELETE FROM CharOnMapInstance
                WHERE CustomerGUID = @CustomerGUID
                AND CharacterID IN (
                    SELECT C.CharacterID
                      FROM Characters C
                     INNER JOIN Users U ON U.CustomerGUID = C.CustomerGUID AND U.UserGUID = C.UserGUID
                     WHERE U.LastAccess < DATE_SUB(NOW(), INTERVAL @CharacterMinutes MINUTE) AND C.CustomerGUID = @CustomerGUID)";

		#endregion

		#region Zone Queries

		public static readonly string AddMapInstance = @"INSERT INTO MapInstances (CustomerGUID, WorldServerID, MapID, Port, Status, PlayerGroupID, LastUpdateFromServer)
		VALUES (@CustomerGUID, @WorldServerID, @MapID, @Port, 1, @PlayerGroupID, NOW());
		SELECT LAST_INSERT_ID();";

		public static readonly string GetAllInactiveMapInstances = @"SELECT MapInstanceID
                FROM MapInstances
                WHERE LastUpdateFromServer < DATE_SUB(NOW(), INTERVAL @MapMinutes MINUTE) AND CustomerGUID = @CustomerGUID";

		#endregion
    }
}
