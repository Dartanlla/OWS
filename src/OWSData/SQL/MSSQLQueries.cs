using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class MSSQLQueries
    {

	    #region To Refactor

        public static readonly string AddOrUpdateWorldServerSQL = @"MERGE WorldServers AS tbl
				USING (SELECT @CustomerGUID AS CustomerGUID, 
					@ServerIP AS ServerIP, 
					@MaxNumberOfInstances AS MaxNumberOfInstances,
					8081 as Port,
					0 as ServerStatus,
					@InternalServerIP as InternalServerIP,
					@StartingMapInstancePort as StartingMapInstancePort,
					@ZoneServerGUID as ZoneServerGUID) AS row
					ON tbl.CustomerGUID = Row.CustomerGUID AND tbl.ZoneServerGUID = row.ZoneServerGUID
				WHEN NOT MATCHED THEN
					INSERT(CustomerGUID, ServerIP, MaxNumberOfInstances, Port, ServerStatus, InternalServerIP, StartingMapInstancePort, ZoneServerGUID)
					VALUES(row.CustomerGUID, row.ServerIP, row.MaxNumberOfInstances, row.Port, row.ServerStatus, row.InternalServerIP, row.StartingMapInstancePort, row.ZoneServerGUID)
				WHEN MATCHED THEN
					UPDATE SET
					tbl.MaxNumberOfInstances = row.MaxNumberOfInstances,
					tbl.Port = row.Port,
					tbl.ServerStatus = row.ServerStatus,
					tbl.InternalServerIP = row.InternalServerIP,
					tbl.StartingMapInstancePort = row.StartingMapInstancePort,
					tbl.ZoneServerGUID = row.ZoneServerGUID;";

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

		public static readonly string UpdateNumberOfPlayersSQL = @"UPDATE MI
				SET NumberOfReportedPlayers=@NumberOfReportedPlayers,
				LastUpdateFromServer=GETDATE(),
				LastServerEmptyDate=(CASE WHEN @NumberOfReportedPlayers = 0 AND NumberOfReportedPlayers > 0 THEN GETDATE() ELSE (CASE WHEN NumberOfReportedPlayers = 0 AND @NumberOfReportedPlayers > 0 THEN NULL ELSE LastServerEmptyDate END) END),
				[Status]=2
				FROM MapInstances MI
				WHERE CustomerGUID=@CustomerGUID
					AND MapInstanceID=@ZoneInstanceID";

		public static readonly string UpdateWorldServerSQL = @"UPDATE WorldServers
				SET ActiveStartTime=GETDATE(),
				ServerStatus=1
				WHERE CustomerGUID=@CustomerGUID 
				AND WorldServerID=@WorldServerID";

		#endregion

		#region Character Queries

		public static readonly string AddAbilityToCharacter = @"INSERT INTO CharHasAbilities (CustomerGUID, CharacterID, AbilityID, AbilityLevel, CharHasAbilitiesCustomJSON)
				SELECT @CustomerGUID, 
					(SELECT TOP 1 C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName AND C.CustomerGUID = @CustomerGUID ORDER BY C.CharacterID),
					(SELECT TOP 1 A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName AND A.CustomerGUID = @CustomerGUID ORDER BY A.AbilityID),
					@AbilityLevel,
					@CharHasAbilitiesCustomJSON";

		public static readonly string AddCharacterUsingDefaultCharacterValues = @"INSERT INTO Characters (CustomerGUID, UserGUID, Email, CharName, MapName, X, Y, Z, RX, RY, RZ, Perception, Acrobatics, Climb, Stealth, ClassID)
				OUTPUT inserted.CharacterID
				SELECT @CustomerGUID, @UserGUID, '', @CharacterName, DCR.StartingMapName, DCR.X, DCR.Y, DCR.Z, DCR.RX, DCR.RY, DCR.RZ, 0, 0, 0, 0, 0
				FROM DefaultCharacterValues DCR 
				WHERE DCR.CustomerGUID = @CustomerGUID 
					AND DCR.DefaultSetName = @DefaultSetName";

        public static readonly string RemoveAbilityFromCharacter = @"DELETE FROM CharHasAbilities
				WHERE CustomerGUID = @CustomerGUID
					AND CharacterID = (SELECT TOP 1 C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName ORDER BY C.CharacterID)
					AND AbilityID = (SELECT TOP 1 A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName ORDER BY A.AbilityID)";

		public static readonly string RemoveCharactersFromAllInactiveInstances = @"DELETE FROM CharOnMapInstance
                WHERE CustomerGUID = @CustomerGUID
                AND CharacterID IN (
                    SELECT C.CharacterID
                      FROM Characters C
                     INNER JOIN Users U ON U.CustomerGUID = C.CustomerGUID AND U.UserGUID = C.UserGUID
                     WHERE U.LastAccess < DATEADD(minute, @CharacterMinutes, GETDATE()) AND C.CustomerGUID = @CustomerGUID)";

		public static readonly string UpdateAbilityOnCharacter = @"UPDATE CharHasAbilities
				SET AbilityLevel = @AbilityLevel,
				CharHasAbilitiesCustomJSON = @CharHasAbilitiesCustomJSON
				WHERE CustomerGUID = @CustomerGUID
					AND CharacterID = (SELECT TOP 1 C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName ORDER BY C.CharacterID)
					AND AbilityID = (SELECT TOP 1 A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName ORDER BY A.AbilityID)";

		#endregion

		#region User Queries

		public static readonly string UpdateUserLastAccess = @"UPDATE Users
				SET LastAccess = GETDATE()
                WHERE CustomerGUID = @CustomerGUID
                AND UserGUID IN (
                    SELECT C.UserGUID
                      FROM Characters C
                      WHERE C.CustomerGUID = @CustomerGUID AND C.CharName = @CharName)";

		#endregion

		#region Zone Queries

		public static readonly string AddMapInstance = @"INSERT INTO MapInstances (CustomerGUID, WorldServerID, MapID, Port, Status, PlayerGroupID, LastUpdateFromServer)
		OUTPUT inserted.MapInstanceID
		VALUES (@CustomerGUID, @WorldServerID, @MapID, @Port, 1, @PlayerGroupID, GETDATE())";

		public static readonly string GetAllInactiveMapInstances = @"SELECT MapInstanceID
                FROM MapInstances
                WHERE LastUpdateFromServer < DATEADD(minute, @MapMinutes, GETDATE()) AND CustomerGUID = @CustomerGUID";

		public static readonly string GetMapInstancesByWorldServerID = @"SELECT MI.*, M.SoftPlayerCap, M.HardPlayerCap, M.MapName, M.MapMode, M.MinutesToShutdownAfterEmpty, 
				DateDiff(minute, ISNULL(MI.LastServerEmptyDate, GETDATE()), GETDATE()) as MinutesServerHasBeenEmpty,
				DateDiff(minute, ISNULL(MI.LastUpdateFromServer, GETDATE()), GETDATE()) as MinutesSinceLastUpdate
				FROM Maps M
				INNER JOIN MapInstances MI ON MI.MapID = M.MapID
				WHERE M.CustomerGUID = @CustomerGUID
				AND MI.WorldServerID = @WorldServerID";

        public static readonly string GetZoneInstancesByZoneAndGroup = @"SELECT TOP 1
					  WS.ServerIP AS ServerIP
					, WS.InternalServerIP AS WorldServerIP
					, WS.Port AS WorldServerPort
					, MI.Port
     				, MI.MapInstanceID
     				, WS.WorldServerID
     				, MI.Status AS MapInstanceStatus
				FROM WorldServers WS
				LEFT JOIN MapInstances MI 
					ON MI.WorldServerID = WS.WorldServerID 
					AND MI.CustomerGUID = WS.CustomerGUID
				LEFT JOIN CharOnMapInstance CMI 
					ON CMI.MapInstanceID = MI.MapInstanceID 
					AND CMI.CustomerGUID = MI.CustomerGUID
				WHERE MI.MapID = @MapID
				AND WS.ActiveStartTime IS NOT NULL
				AND WS.CustomerGUID = @CustomerGUID
				AND MI.NumberOfReportedPlayers < @SoftPlayerCap 
				AND (MI.PlayerGroupID = @PlayerGroupID OR @PlayerGroupID = 0)
				AND MI.Status = 2
				GROUP BY MI.MapInstanceID, WS.ServerIP, MI.Port, WS.WorldServerID, WS.InternalServerIP, WS.Port, MI.Status
				ORDER BY COUNT(DISTINCT CMI.CharacterID)";

		#endregion

    }
}
