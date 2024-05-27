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

        public static readonly string GetAllCharacters = @"SELECT WC.*,
				COALESCE(CL.ClassName,'') as ClassName
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

	    public static readonly string GetCharacterIDByName = @"SELECT CharacterID
				FROM Characters
				WHERE CustomerGUID = @CustomerGUID
				  AND CharName = @CharName";

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

	    public static readonly string GetCharacterAbilities = @"SELECT A.AbilityID, A.AbilityCustomJSON, A.AbilityName, A.AbilityTypeID, A.Class, A.CustomerGUID, A.Race, A.TextureToUseForIcon, A.GameplayAbilityClassName,
			CHA.CharHasAbilitiesID, CHA.AbilityLevel, CHA.CharHasAbilitiesCustomJSON, C.CharacterID, C.CharName 
				FROM CharHasAbilities CHA
				INNER JOIN Abilities A ON A.AbilityID = CHA.AbilityID AND A.CustomerGUID = CHA.CustomerGUID
				INNER JOIN Characters C	ON C.CharacterID = CHA.CharacterID AND C.CustomerGUID = CHA.CustomerGUID
				WHERE C.CustomerGUID = @CustomerGUID
				  AND C.CharName = @CharName";

	    public static readonly string GetCharacterAbilityByName = @"SELECT A.AbilityID, A.AbilityCustomJSON, A.AbilityName, A.AbilityTypeID, A.Class, A.CustomerGUID, A.Race, A.TextureToUseForIcon, A.GameplayAbilityClassName,
			CHA.CharHasAbilitiesID, CHA.AbilityLevel, CHA.CharHasAbilitiesCustomJSON, C.CharacterID, C.CharName 
				FROM CharHasAbilities CHA
				INNER JOIN Abilities A ON A.AbilityID = CHA.AbilityID AND A.CustomerGUID = CHA.CustomerGUID
				INNER JOIN Characters C	ON C.CharacterID = CHA.CharacterID AND C.CustomerGUID = CHA.CustomerGUID
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

	    public static readonly string GetPlayerGroupIDByType = @"SELECT COALESCE(PG.PlayerGroupID, 0)
				FROM PlayerGroupCharacters PGC
				INNER JOIN PlayerGroup PG ON PG.PlayerGroupID = PGC.PlayerGroupID
				WHERE PGC.CustomerGUID = @CustomerGUID
				  AND PGC.CharacterID = @CharacterID
				  AND PG.PlayerGroupTypeID = @PlayerGroupType";

		public static readonly string GetUsers = @"SELECT UserGUID, FirstName, LastName, Email, CreateDate, LastAccess, Role
				FROM Users
				WHERE CustomerGUID = @CustomerGUID";

		public static readonly string UpdateUser = @"UPDATE Users
				SET FirstName = @FirstName
				  , LastName = @LastName
				  , Email = @Email
			  WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID";

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
				SET	MapName = @MapName,
					X = @X,
					Y = @Y,
					Z = @Z,
					RX = @RX,
					RY = @RY,
					RZ = @RZ
				WHERE CharName = @CharName
				  AND CustomerGUID = @CustomerGUID";

	    public static readonly string UpdateCharacterStats = @"UPDATE Characters
				SET	CharacterLevel = @CharacterLevel,
					Gender = @Gender,
					Weight = @Weight,
					Size = @Size,
					Fame = @Fame,
					Alignment = @Alignment,
					Description = @Description,
					XP = @XP,
					X = @X,
					Y = @Y,
					Z = @Z,
					RX = @RX,
					RY = @RY,
					RZ = @RZ,
					TeamNumber = @TeamNumber,
					HitDie = @HitDie,
					Wounds = @Wounds,
					Thirst = @Thirst,
					Hunger = @Hunger,
					MaxHealth = @MaxHealth,
					Health = @Health,
					HealthRegenRate = @HealthRegenRate,
					MaxMana = @MaxMana,
					Mana = @Mana,
					ManaRegenRate = @ManaRegenRate,
					MaxEnergy = @MaxEnergy,
					Energy = @Energy,
					EnergyRegenRate = @EnergyRegenRate,
					MaxFatigue = @MaxFatigue,
					Fatigue = @Fatigue,
					FatigueRegenRate = @FatigueRegenRate,
					MaxStamina = @MaxStamina,
					Stamina = @Stamina,
					StaminaRegenRate = @StaminaRegenRate,
					MaxEndurance = @MaxEndurance,
					Endurance = @Endurance,
					EnduranceRegenRate = @EnduranceRegenRate,
					Strength = @Strength,
					Dexterity = @Dexterity,
					Constitution = @Constitution,
					Intellect = @Intellect,
					Wisdom = @Wisdom,
					Charisma = @Charisma,
					Agility = @Agility,
					Spirit = @Spirit,
					Magic = @Magic,
					Fortitude = @Fortitude,
					Reflex = @Reflex,
					Willpower = @Willpower,
					BaseAttack = @BaseAttack,
					BaseAttackBonus = @BaseAttackBonus,
					AttackPower = @AttackPower,
					AttackSpeed = @AttackSpeed,
					CritChance = @CritChance,
					CritMultiplier = @CritMultiplier,
					Haste = @Haste,
					SpellPower = @SpellPower,
					SpellPenetration = @SpellPenetration,
					Defense = @Defense,
					Dodge = @Dodge,
					Parry = @Parry,
					Avoidance = @Avoidance,
					Versatility = @Versatility,
					Multishot = @Multishot,
					Initiative = @Initiative,
					NaturalArmor = @NaturalArmor,
					PhysicalArmor = @PhysicalArmor,
					BonusArmor = @BonusArmor,
					ForceArmor = @ForceArmor,
					MagicArmor = @MagicArmor,
					Resistance = @Resistance,
					ReloadSpeed = @ReloadSpeed,
					Range = @Range,
					Speed = @Speed,
					Gold = @Gold,
					Silver = @Silver,
					Copper = @Copper,
					FreeCurrency = @FreeCurrency,
					PremiumCurrency = @PremiumCurrency,
					Perception = @Perception,
					Acrobatics = @Acrobatics,
					Climb = @Climb,
					Stealth = @Stealth,
					Score = @Score
				WHERE CharName = @CharName
				  AND CustomerGUID = @CustomerGUID";

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
