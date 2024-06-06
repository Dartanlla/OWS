﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.SQL
{
    public static class PostgresQueries
    {

	    #region To Refactor

	    public static readonly string AddOrUpdateWorldServerSQL = @"INSERT INTO WorldServers (CustomerGUID, ServerIP, MaxNumberOfInstances, Port, ServerStatus, InternalServerIP,
                          StartingMapInstancePort, ZoneServerGUID)
    (SELECT @CustomerGUID::UUID           AS CustomerGUID,
            @ServerIP                     AS ServerIP,
            @MaxNumberOfInstances         AS MaxNumberOfInstances,
            8081                          AS Port,
            0                             AS ServerStatus,
            @InternalServerIP             AS InternalServerIP,
            @StartingMapInstancePort      AS StartingMapInstancePort,
            @ZoneServerGUID::UUID         AS ZoneServerGUID)
ON CONFLICT ON CONSTRAINT ak_zoneservers
    DO UPDATE SET MaxNumberOfInstances    = @MaxNumberOfInstances,
                  Port                    = 8081,
                  ServerStatus            = 0,
                  InternalServerIP        = @InternalServerIP,
                  StartingMapInstancePort = @StartingMapInstancePort,
                  ZoneServerGUID          = @ZoneServerGUID::UUID;";

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
	            AND U.Email=@Email";

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
				LastUpdateFromServer=NOW(),
				LastServerEmptyDate=(CASE WHEN @NumberOfReportedPlayers = 0 AND NumberOfReportedPlayers > 0 THEN NOW() ELSE (CASE WHEN NumberOfReportedPlayers = 0 AND @NumberOfReportedPlayers > 0 THEN NULL ELSE LastServerEmptyDate END) END),
				Status=2
				WHERE CustomerGUID=@CustomerGUID
					AND MapInstanceID=@ZoneInstanceID";

		public static readonly string UpdateWorldServerSQL = @"UPDATE WorldServers
				SET ActiveStartTime=NOW(),
				ServerStatus=1
				WHERE CustomerGUID=@CustomerGUID::UUID 
				AND WorldServerID=@WorldServerID";

		#endregion

		#region Character Queries

		public static readonly string AddAbilityToCharacter = @"INSERT INTO CharHasAbilities (CustomerGUID, CharacterID, AbilityID, AbilityLevel, CharHasAbilitiesCustomJSON)
				SELECT @CustomerGUID, 
					(SELECT C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName AND C.CustomerGUID = @CustomerGUID ORDER BY C.CharacterID LIMIT 1),
					(SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName AND A.CustomerGUID = @CustomerGUID ORDER BY A.AbilityID LIMIT 1),
					@AbilityLevel,
					@CharHasAbilitiesCustomJSON";

		public static readonly string AddCharacterUsingClassID = @"INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, Email, CharName, MapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, ServerIP, LastActivity, 
				RX, RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP, HitDie, Wounds, Size, weight, MaxHealth, Health, 
				HealthRegenRate, MaxMana, Mana, ManaRegenRate, MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina, 
				StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution, Intellect, Wisdom, Charisma, Agility, Fortitude, 
				Reflex, Willpower, BaseAttack, BaseAttackBonus, AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration, Defense, 
				Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor, PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, 
				Range, Speed, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, Alignment, Description)
				SELECT @CustomerGUID, @ClassID, @UserGUID, '', @CharName, StartingMapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, '', NOW(), 
				RX, RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP, HitDie, Wounds, Size, weight, MaxHealth, Health, 
				HealthRegenRate, MaxMana, Mana, ManaRegenRate, MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina, 
				StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution, Intellect, Wisdom, Charisma, Agility, Fortitude, 
				Reflex, Willpower, BaseAttack, BaseAttackBonus, AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration, Defense, 
				Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor, PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, 
				Range, Speed, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, Alignment, Description 
				FROM Class 
				WHERE ClassID = @ClassID
				AND CustomerGUID = @CustomerGUID
				RETURNING characterid";

		public static readonly string AddCharacterUsingDefaultCharacterValues = @"INSERT INTO Characters (CustomerGUID, UserGUID, Email, CharName, MapName, X, Y, Z, RX, RY, RZ, Perception, Acrobatics, Climb, Stealth, ClassID)
				SELECT @CustomerGUID, @UserGUID, '', @CharacterName, DCR.StartingMapName, DCR.X, DCR.Y, DCR.Z, DCR.RX, DCR.RY, DCR.RZ, 0, 0, 0, 0, 0
				FROM DefaultCharacterValues DCR 
				WHERE DCR.CustomerGUID = @CustomerGUID 
					AND DCR.DefaultSetName = @DefaultSetName
				RETURNING characterid";

		public static readonly string RemoveAbilityFromCharacter = @"DELETE FROM CharHasAbilities
				WHERE CustomerGUID = @CustomerGUID
					AND CharacterID = (SELECT C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName  ORDER BY C.CharacterID LIMIT 1)
					AND AbilityID = (SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName ORDER BY A.AbilityID LIMIT 1)";

		public static readonly string RemoveCharactersFromAllInactiveInstances = @"DELETE FROM CharOnMapInstance
                WHERE CustomerGUID = @CustomerGUID
                AND CharacterID IN (
                    SELECT C.CharacterID
                      FROM Characters C
                     INNER JOIN Users U ON U.CustomerGUID = C.CustomerGUID AND U.UserGUID = C.UserGUID
                     WHERE U.LastAccess < CURRENT_TIMESTAMP - (@CharacterMinutes || ' minutes')::INTERVAL AND C.CustomerGUID = CharOnMapInstance.CustomerGUID)";

		public static readonly string RemoveCharacterFromInstances = @"DELETE FROM CharOnMapInstance WHERE CustomerGUID = @CustomerGUID AND MapInstanceID = ANY(@MapInstances)";

		public static readonly string UpdateAbilityOnCharacter = @"UPDATE CharHasAbilities
				SET AbilityLevel = @AbilityLevel,
				CharHasAbilitiesCustomJSON = @CharHasAbilitiesCustomJSON
				WHERE CustomerGUID = @CustomerGUID
					AND CharacterID = (SELECT C.CharacterID FROM Characters C WHERE C.CharName = @CharacterName ORDER BY C.CharacterID LIMIT 1)
					AND AbilityID = (SELECT A.AbilityID FROM Abilities A WHERE A.AbilityName = @AbilityName ORDER BY A.AbilityID LIMIT 1)";

		#endregion

		#region User Queries

		public static readonly string AddUser = @"INSERT INTO Users (CustomerGUID, UserGUID, FirstName, LastName, Email, PasswordHash, Salt, CreateDate, LastAccess, Role)
		VALUES (@CustomerGUID, @UserGUID, @FirstName, @LastName, @Email, @PasswordHash, @Salt, NOW(), NOW(), @Role)";

		public static readonly string AddUserSession = @"INSERT INTO UserSessions (CustomerGUID, UserSessionGUID, UserGUID, LoginDate)
		VALUES (@CustomerGUID, @UserSessionGUID, @UserGUID, NOW())";

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
		       FLOOR(EXTRACT(EPOCH FROM NOW()::TIMESTAMP - MI.LastServerEmptyDate) / 60)  AS MinutesServerHasBeenEmpty,
		       FLOOR(EXTRACT(EPOCH FROM NOW()::TIMESTAMP - MI.LastUpdateFromServer) / 60) AS MinutesSinceLastUpdate
				FROM Maps M
				INNER JOIN MapInstances MI ON MI.MapID = M.MapID
				WHERE M.CustomerGUID = @CustomerGUID
				AND MI.WorldServerID = @WorldServerID";

        public static readonly string GetZoneInstancesByZoneAndGroup = @"SELECT WS.ServerIP AS ServerIP
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
				ORDER BY COUNT(DISTINCT CMI.CharacterID)
				LIMIT 1";

		public static readonly string RemoveMapInstances = @"DELETE FROM MapInstances WHERE CustomerGUID = @CustomerGUID AND MapInstanceID = ANY(@MapInstances)";

		#endregion
	}
}
