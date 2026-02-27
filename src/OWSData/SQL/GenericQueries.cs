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

		public static readonly string CreateCharacterSQL = @"WITH input AS (
            SELECT @CustomerGUID::UUID AS customerguid,
                   @UserSessionGUID::UUID AS usersessionguid,
                   @CharacterName AS charactername,
                   @ClassName AS classname
        ),
        support_unicode AS (
            SELECT C.SupportUnicode AS supportunicode
            FROM Customers C
            JOIN input I ON C.CustomerGUID = I.customerguid
        ),
        user_data AS (
            SELECT US.UserGUID AS userguid
            FROM UserSessions US
            JOIN input I ON US.CustomerGUID = I.customerguid AND US.UserSessionGUID = I.usersessionguid
        ),
        class_data AS (
            SELECT C.ClassID AS classid
            FROM Class C
            JOIN input I ON C.CustomerGUID = I.customerguid AND C.ClassName = I.classname
        ),
        char_count AS (
            SELECT COUNT(*)::INT AS count
            FROM Characters C
            JOIN input I ON C.CustomerGUID = I.customerguid AND C.CharName = I.charactername
        ),
        normalized AS (
            SELECT REPLACE(REPLACE(REPLACE(TRIM(BOTH FROM I.charactername), ' ', '<>'), '><', ''), '<>', ' ') AS clean_name,
                   COALESCE(LENGTH(ARRAY_TO_STRING(REGEXP_MATCHES(REPLACE(REPLACE(REPLACE(TRIM(BOTH FROM I.charactername), ' ', '<>'), '><', ''), '<>', ' '), '[^a-zA-Z0-9 ]'), '')), 0) AS invalid_count
            FROM input I
        ),
        errors AS (
            SELECT CASE
                WHEN (SELECT invalid_count FROM normalized) > 0
                    AND (SELECT supportunicode FROM support_unicode) = FALSE
                    THEN 'Character Name can only contain letters, numbers, and spaces'
                WHEN (SELECT userguid FROM user_data) IS NULL
                    THEN 'Invalid User Session'
                WHEN (SELECT classid FROM class_data) IS NULL
                    THEN 'Invalid Class Name'
                WHEN (SELECT count FROM char_count) > 0
                    THEN 'Invalid Character Name'
                ELSE NULL
            END AS errormessage
        ),
        class_inventory_count AS (
            SELECT COUNT(*)::INT AS count
            FROM ClassInventory CI
            JOIN input I ON CI.CustomerGUID = I.customerguid
            JOIN class_data CD ON CI.ClassID = CD.classid
        ),
        insert_character AS (
            INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, Email, CharName, MapName, X, Y, Z, Perception,
                                    Acrobatics, Climb, Stealth, ServerIP, LastActivity, RX, RY, RZ, Spirit, Magic,
                                    TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP, HitDie, Wounds,
                                    Size, Weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate,
                                    MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina,
                                    Stamina, StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength,
                                    Dexterity, Constitution, Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex,
                                    Willpower, BaseAttack, BaseAttackBonus, AttackPower, AttackSpeed, CritChance,
                                    CritMultiplier, Haste, SpellPower, SpellPenetration, Defense, Dodge, Parry, Avoidance,
                                    Versatility, Multishot, Initiative, NaturalArmor, PhysicalArmor, BonusArmor, ForceArmor,
                                    MagicArmor, Resistance, ReloadSpeed, Range, Speed, Silver, Copper, FreeCurrency,
                                    PremiumCurrency, Fame, Alignment, Description)
            SELECT I.customerguid,
                   CD.classid,
                   UD.userguid,
                   '',
                   N.clean_name,
                   CL.StartingMapName,
                   CL.X,
                   CL.Y,
                   CL.Z,
                   CL.Perception,
                   CL.Acrobatics,
                   CL.Climb,
                   CL.Stealth,
                   '',
                   NOW(),
                   CL.RX,
                   CL.RY,
                   CL.RZ,
                   CL.Spirit,
                   CL.Magic,
                   CL.TeamNumber,
                   CL.Thirst,
                   CL.Hunger,
                   CL.Gold,
                   CL.Score,
                   CL.CharacterLevel,
                   CL.Gender,
                   CL.XP,
                   CL.HitDie,
                   CL.Wounds,
                   CL.Size,
                   CL.Weight,
                   CL.MaxHealth,
                   CL.Health,
                   CL.HealthRegenRate,
                   CL.MaxMana,
                   CL.Mana,
                   CL.ManaRegenRate,
                   CL.MaxEnergy,
                   CL.Energy,
                   CL.EnergyRegenRate,
                   CL.MaxFatigue,
                   CL.Fatigue,
                   CL.FatigueRegenRate,
                   CL.MaxStamina,
                   CL.Stamina,
                   CL.StaminaRegenRate,
                   CL.MaxEndurance,
                   CL.Endurance,
                   CL.EnduranceRegenRate,
                   CL.Strength,
                   CL.Dexterity,
                   CL.Constitution,
                   CL.Intellect,
                   CL.Wisdom,
                   CL.Charisma,
                   CL.Agility,
                   CL.Fortitude,
                   CL.Reflex,
                   CL.Willpower,
                   CL.BaseAttack,
                   CL.BaseAttackBonus,
                   CL.AttackPower,
                   CL.AttackSpeed,
                   CL.CritChance,
                   CL.CritMultiplier,
                   CL.Haste,
                   CL.SpellPower,
                   CL.SpellPenetration,
                   CL.Defense,
                   CL.Dodge,
                   CL.Parry,
                   CL.Avoidance,
                   CL.Versatility,
                   CL.Multishot,
                   CL.Initiative,
                   CL.NaturalArmor,
                   CL.PhysicalArmor,
                   CL.BonusArmor,
                   CL.ForceArmor,
                   CL.MagicArmor,
                   CL.Resistance,
                   CL.ReloadSpeed,
                   CL.Range,
                   CL.Speed,
                   CL.Silver,
                   CL.Copper,
                   CL.FreeCurrency,
                   CL.PremiumCurrency,
                   CL.Fame,
                   CL.Alignment,
                   CL.Description
            FROM input I
            JOIN class_data CD ON TRUE
            JOIN user_data UD ON TRUE
            JOIN normalized N ON TRUE
            JOIN Class CL ON CL.ClassID = CD.classid AND CL.CustomerGUID = I.customerguid
            WHERE (SELECT errormessage FROM errors) IS NULL
            RETURNING CharacterID, CharName, MapName, X, Y, Z, RX, RY, RZ, TeamNumber, Gold, Silver, Copper, FreeCurrency,
                      PremiumCurrency, Fame, Alignment, Score, Gender, XP, Size, Weight, CharacterLevel
        ),
        insert_inventory_bag AS (
            INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize, InventoryWidth, InventoryHeight)
            SELECT I.customerguid, IC.CharacterID, 'Bag', 16, 4, 4
            FROM input I
            JOIN insert_character IC ON TRUE
            WHERE (SELECT errormessage FROM errors) IS NULL
              AND COALESCE((SELECT count FROM class_inventory_count), 0) < 1
            RETURNING 1
        ),
        insert_inventory_class AS (
            INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize, InventoryWidth, InventoryHeight)
            SELECT I.customerguid, IC.CharacterID, CI.InventoryName, CI.InventorySize, CI.InventoryWidth, CI.InventoryHeight
            FROM input I
            JOIN insert_character IC ON TRUE
            JOIN ClassInventory CI ON CI.CustomerGUID = I.customerguid
            JOIN class_data CD ON CI.ClassID = CD.classid
            WHERE (SELECT errormessage FROM errors) IS NULL
              AND COALESCE((SELECT count FROM class_inventory_count), 0) >= 1
            RETURNING 1
        )
        SELECT E.errormessage AS ErrorMessage,
               CASE WHEN E.errormessage IS NULL THEN IC.CharName ELSE '' END AS CharacterName,
               CASE WHEN E.errormessage IS NULL THEN I.classname ELSE '' END AS ClassName,
               CASE WHEN E.errormessage IS NULL THEN IC.CharacterLevel ELSE 0 END AS CharacterLevel,
               CASE WHEN E.errormessage IS NULL THEN IC.MapName ELSE '' END AS StartingMapName,
               CASE WHEN E.errormessage IS NULL THEN IC.X ELSE 0 END AS X,
               CASE WHEN E.errormessage IS NULL THEN IC.Y ELSE 0 END AS Y,
               CASE WHEN E.errormessage IS NULL THEN IC.Z ELSE 0 END AS Z,
               CASE WHEN E.errormessage IS NULL THEN IC.RX ELSE 0 END AS RX,
               CASE WHEN E.errormessage IS NULL THEN IC.RY ELSE 0 END AS RY,
               CASE WHEN E.errormessage IS NULL THEN IC.RZ ELSE 0 END AS RZ,
               CASE WHEN E.errormessage IS NULL THEN IC.TeamNumber ELSE 0 END AS TeamNumber,
               CASE WHEN E.errormessage IS NULL THEN IC.Gold ELSE 0 END AS Gold,
               CASE WHEN E.errormessage IS NULL THEN IC.Silver ELSE 0 END AS Silver,
               CASE WHEN E.errormessage IS NULL THEN IC.Copper ELSE 0 END AS Copper,
               CASE WHEN E.errormessage IS NULL THEN IC.FreeCurrency ELSE 0 END AS FreeCurrency,
               CASE WHEN E.errormessage IS NULL THEN IC.PremiumCurrency ELSE 0 END AS PremiumCurrency,
               CASE WHEN E.errormessage IS NULL THEN IC.Fame ELSE 0 END AS Fame,
               CASE WHEN E.errormessage IS NULL THEN IC.Alignment ELSE 0 END AS Alignment,
               CASE WHEN E.errormessage IS NULL THEN IC.Score ELSE 0 END AS Score,
               CASE WHEN E.errormessage IS NULL THEN IC.Gender ELSE 0 END AS Gender,
               CASE WHEN E.errormessage IS NULL THEN IC.XP ELSE 0 END AS XP,
               CASE WHEN E.errormessage IS NULL THEN IC.Size ELSE 0 END AS Size,
               CASE WHEN E.errormessage IS NULL THEN IC.Weight ELSE 0 END AS Weight
        FROM errors E
        CROSS JOIN input I
        LEFT JOIN insert_character IC ON TRUE;";

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

        public static readonly string GetPlayerGroupsCharacterIsIn = @"SELECT PG.PlayerGroupID,
				PG.CustomerGUID,
				PG.PlayerGroupName,
				PG.PlayerGroupTypeID,
				PG.ReadyState,
				PG.CreateDate,
				PGC.DateAdded,
				PGC.TeamNumber
			FROM PlayerGroupCharacters PGC
			INNER JOIN PlayerGroup PG
				ON PG.PlayerGroupID = PGC.PlayerGroupID
				AND PG.CustomerGUID = PGC.CustomerGUID
			INNER JOIN Characters C
				ON C.CharacterID = PGC.CharacterID
			INNER JOIN UserSessions US
				ON US.UserGUID = C.UserGUID
				AND US.CustomerGUID = C.CustomerGUID
			WHERE PGC.CustomerGUID = @CustomerGUID
			  AND (PG.PlayerGroupTypeID = @PlayerGroupTypeID OR @PlayerGroupTypeID = 0)
			  AND C.CharName = @CharName
			  AND C.CustomerGUID = @CustomerGUID";

        public static readonly string GetDefaultCustomCharacterDataByDefaultSetName = @"SELECT *
				FROM DefaultCustomCharacterData DCCD
				INNER JOIN DefaultCharacterValues DCV 
					ON DCCD.DefaultCharacterValuesId = DCV.DefaultCharacterValuesId
				WHERE DCV.DefaultSetName = @DefaultSetName
				AND DCCD.CustomerGUID = @CustomerGUID";


        public static readonly string GetPlayerGroupIDByType = @"SELECT COALESCE(PG.PlayerGroupID, 0)
				FROM PlayerGroupCharacters PGC
				INNER JOIN PlayerGroup PG ON PG.PlayerGroupID = PGC.PlayerGroupID
				WHERE PGC.CustomerGUID = @CustomerGUID
				  AND PGC.CharacterID = @CharacterID
				  AND PG.PlayerGroupTypeID = @PlayerGroupType";

        public static readonly string GetUsers = @"SELECT UserGUID, FirstName, LastName, Email, CreateDate, LastAccess, Role
				FROM Users
				WHERE CustomerGUID = @CustomerGUID";

        public static readonly string GetUser = @"SELECT *
				FROM Users
				WHERE CustomerGUID = @CustomerGUID
				  AND UserGUID = @UserGUID";

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

        public static readonly string GetUserSession = @"SELECT US.CustomerGUID,
				US.UserGUID,
				US.UserSessionGUID,
				US.LoginDate,
				US.SelectedCharacterName,
				U.Email,
				U.FirstName,
				U.LastName,
				U.CreateDate,
				U.LastAccess,
				U.Role,
				C.CharacterID,
				C.CharName,
				C.X,
				C.Y,
				C.Z,
				C.RX,
				C.RY,
				C.RZ,
				C.MapName AS ZoneName
			FROM UserSessions US
			INNER JOIN Users U
				ON U.UserGUID = US.UserGUID
			LEFT JOIN Characters C
				ON C.CustomerGUID = US.CustomerGUID
				AND C.CharName = US.SelectedCharacterName
				AND C.UserGUID = US.UserGUID
			WHERE US.CustomerGUID = @CustomerGUID
			  AND US.UserSessionGUID = @UserSessionGUID";

        public static readonly string PlayerLoginAndCreateSession = @"WITH user_row AS (
				SELECT U.UserGUID,
					(U.PasswordHash = crypt(@Password, U.PasswordHash)) AS PasswordCheck
				FROM Users U
				WHERE U.CustomerGUID = @CustomerGUID
				  AND U.Email = @Email
				  AND U.Role = 'Player'
			),
			auth AS (
				SELECT CASE
						WHEN (PasswordCheck OR @DontCheckPassword) THEN TRUE
						ELSE FALSE
					END AS Authenticated,
					UserGUID
				FROM user_row
			),
			deleted AS (
				DELETE FROM UserSessions US
				USING auth A
				WHERE A.Authenticated = TRUE
				  AND US.UserGUID = A.UserGUID
				RETURNING 1
			),
			ins AS (
				INSERT INTO UserSessions (CustomerGUID, UserSessionGUID, UserGUID, LoginDate)
				SELECT @CustomerGUID, gen_random_uuid(), A.UserGUID, NOW()
				FROM auth A
				WHERE A.Authenticated = TRUE
				RETURNING UserSessionGUID
			)
			SELECT COALESCE((SELECT Authenticated FROM auth), FALSE) AS Authenticated,
				(SELECT UserSessionGUID FROM ins) AS UserSessionGUID";

        public static readonly string UserSessionSetSelectedCharacter = @"UPDATE UserSessions
				SET SelectedCharacterName = @SelectedCharacterName
				WHERE CustomerGUID = @CustomerGUID
				  AND UserSessionGUID = @UserSessionGUID";

        public static readonly string AddUser = @"WITH new_user AS (
				SELECT gen_random_uuid() AS UserGUID,
					crypt(@Password, gen_salt('md5')) AS PasswordHash
			),
			ins AS (
				INSERT INTO Users (UserGUID, CustomerGUID, FirstName, LastName, Email, PasswordHash, CreateDate, LastAccess, Role)
				SELECT NU.UserGUID, @CustomerGUID, @FirstName, @LastName, @Email, NU.PasswordHash, NOW(), NOW(), @Role
				FROM new_user NU
				RETURNING UserGUID
			)
			SELECT UserGUID FROM ins";

        public static readonly string RemoveCharacter = @"WITH user_data AS (
				SELECT US.UserGUID
				FROM UserSessions US
				WHERE US.CustomerGUID = @CustomerGUID
				  AND US.UserSessionGUID = @UserSessionGUID
			),
			char_data AS (
				SELECT C.CharacterID
				FROM Characters C
				JOIN user_data UD ON C.UserGUID = UD.UserGUID
				WHERE C.CustomerGUID = @CustomerGUID
				  AND C.CharName = @CharacterName
			),
			del_char_ability_bar_abilities AS (
				DELETE FROM CharAbilityBarAbilities CABA
				USING CharAbilityBars CAB, char_data CD
				WHERE CABA.CustomerGUID = @CustomerGUID
				  AND CABA.CharAbilityBarID = CAB.CharAbilityBarID
				  AND CAB.CustomerGUID = @CustomerGUID
				  AND CAB.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_char_ability_bars AS (
				DELETE FROM CharAbilityBars CAB
				USING char_data CD
				WHERE CAB.CustomerGUID = @CustomerGUID
				  AND CAB.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_char_has_abilities AS (
				DELETE FROM CharHasAbilities CHA
				USING char_data CD
				WHERE CHA.CustomerGUID = @CustomerGUID
				  AND CHA.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_char_has_items AS (
				DELETE FROM CharHasItems CHI
				USING char_data CD
				WHERE CHI.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_char_inventory_items AS (
				DELETE FROM CharInventoryItems CII
				USING CharInventory CI, char_data CD
				WHERE CII.CustomerGUID = @CustomerGUID
				  AND CII.CharInventoryID = CI.CharInventoryID
				  AND CI.CustomerGUID = @CustomerGUID
				  AND CI.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_char_inventory AS (
				DELETE FROM CharInventory CI
				USING char_data CD
				WHERE CI.CustomerGUID = @CustomerGUID
				  AND CI.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_char_on_map_instance AS (
				DELETE FROM CharOnMapInstance COMI
				USING char_data CD
				WHERE COMI.CustomerGUID = @CustomerGUID
				  AND COMI.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_chat_group_users AS (
				DELETE FROM ChatGroupUsers CGU
				USING char_data CD
				WHERE CGU.CustomerGUID = @CustomerGUID
				  AND CGU.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_custom_character_data AS (
				DELETE FROM CustomCharacterData CCD
				USING char_data CD
				WHERE CCD.CustomerGUID = @CustomerGUID
				  AND CCD.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_player_group_characters AS (
				DELETE FROM PlayerGroupCharacters PGC
				USING char_data CD
				WHERE PGC.CustomerGUID = @CustomerGUID
				  AND PGC.CharacterID = CD.CharacterID
				RETURNING 1
			),
			del_characters AS (
				DELETE FROM Characters C
				USING char_data CD
				WHERE C.CustomerGUID = @CustomerGUID
				  AND C.CharacterID = CD.CharacterID
				RETURNING 1
			)
			SELECT 1";

        #endregion
    }
}
