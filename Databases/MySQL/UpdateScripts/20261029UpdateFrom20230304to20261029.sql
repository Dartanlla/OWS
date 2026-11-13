DROP PROCEDURE IF EXISTS AddNewCustomer;

DELIMITER //

CREATE PROCEDURE AddNewCustomer(IN _CustomerName VARCHAR(50), IN _FirstName VARCHAR(50),
                                               IN _LastName VARCHAR(50), IN _Email VARCHAR(256),
                                               IN _Password VARCHAR(256), IN _CustomerGUID VARCHAR(36))
BEGIN
    DECLARE _UserGUID, _Exists CHAR(36);
    DECLARE _CharacterID, _ClassID INT;
    DECLARE _CharacterName VARCHAR(50);

    SET _CharacterName = 'Test';
    IF _CustomerGUID IS NULL THEN
        SET _CustomerGUID = UUID();
    END IF;

    SELECT CustomerGUID
    INTO _Exists
    FROM Customers C
    WHERE C.CustomerGUID = _CustomerGUID;

    IF _Exists IS NULL THEN

        INSERT INTO Customers (CustomerGUID, CustomerName, CustomerEmail, CustomerPhone, CustomerNotes, EnableDebugLogging)
        VALUES (_CustomerGUID, _CustomerName, _Email, '', '', TRUE);

        INSERT INTO WorldSettings (CustomerGUID, StartTime)
        SELECT _CustomerGUID, CAST(UNIX_TIMESTAMP() AS INTEGER)
        FROM Customers C
        WHERE C.CustomerGUID = _CustomerGUID;

        SET _UserGUID = AddUser(_CustomerGUID, _FirstName, _LastName, _Email, _Password, 'Developer');

        INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
        VALUES (_CustomerGUID, 'ThirdPersonExampleMap', 'ThirdPersonExampleMap', NULL, 1, 1);
        INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
        VALUES (_CustomerGUID, 'Map2', 'Map2', NULL, 1, 1);
        INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
        VALUES (_CustomerGUID, 'DungeonMap', 'DungeonMap', NULL, 1, 1);
        INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
        VALUES (_CustomerGUID, 'FourZoneMap', 'Zone1', NULL, 1, 1);
        INSERT INTO Maps (CustomerGUID, MapName, ZoneName, MapData, Width, Height)
        VALUES (_CustomerGUID, 'FourZoneMap', 'Zone2', NULL, 1, 1);

        INSERT INTO Class (CustomerGUID, ClassName, StartingMapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, RX,
                           RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP,
                           HitDie, Wounds, Size, weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate,
                           MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina,
                           StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution,
                           Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex, Willpower, BaseAttack, BaseAttackBonus,
                           AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration,
                           Defense, Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor,
                           PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, `Range`, Speed,
                           Silver,
                           Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
        VALUES (_CustomerGUID, 'MaleWarrior', 'ThirdPersonExampleMap', 0, 0, 250, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                1, 1, 0, 10, 0, 1, 0, 100, 50, 1, 100, 0, 1, 100, 0, 5, 100, 0, 1, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10,
                0, 1, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                '');

        SET _ClassID = LAST_INSERT_ID();

        INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, CharName, MapName, X, Y, Z, ServerIP, LastActivity, RX, RY, RZ)
        SELECT _CustomerGUID, _ClassID, _UserGUID, _CharacterName, StartingMapName, X, Y, Z, '', NOW(), RX, RY, RZ
            FROM Class
        WHERE ClassID = _ClassID;

        SET _CharacterID = LAST_INSERT_ID();

        INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
        VALUES (_CustomerGUID, _CharacterID, 'Bag', 16);
    ELSE
        SELECT 'Customer with specified GUID already exists.';
    END IF;
END;

//
DELIMITER ;

SET SESSION group_concat_max_len = 65535;
SELECT CONCAT('ALTER TABLE Characters ', GROUP_CONCAT(CONCAT('DROP COLUMN `', COLUMN_NAME, '`') SEPARATOR ', '))
INTO @ows_character_drop_sql
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Characters'
  AND COLUMN_NAME IN ('Email', 'IsAdmin', 'IsModerator', 'Perception', 'Acrobatics', 'Climb', 'Stealth', 'Spirit', 'Magic', 'TeamNumber', 'Thirst', 'Hunger', 'Gold', 'Score', 'CharacterLevel', 'Gender', 'Xp', 'HitDie', 'Wounds', 'Size', 'Weight', 'MaxHealth', 'Health', 'HealthRegenRate', 'MaxMana', 'Mana', 'ManaRegenRate', 'MaxEnergy', 'Energy', 'EnergyRegenRate', 'MaxFatigue', 'Fatigue', 'FatigueRegenRate', 'MaxStamina', 'Stamina', 'StaminaRegenRate', 'MaxEndurance', 'Endurance', 'EnduranceRegenRate', 'Strength', 'Dexterity', 'Constitution', 'Intellect', 'Wisdom', 'Charisma', 'Agility', 'Fortitude', 'Reflex', 'Willpower', 'BaseAttack', 'BaseAttackBonus', 'AttackPower', 'AttackSpeed', 'CritChance', 'CritMultiplier', 'Haste', 'SpellPower', 'SpellPenetration', 'Defense', 'Dodge', 'Parry', 'Avoidance', 'Versatility', 'Multishot', 'Initiative', 'NaturalArmor', 'PhysicalArmor', 'BonusArmor', 'ForceArmor', 'MagicArmor', 'Resistance', 'ReloadSpeed', 'Range', 'Speed', 'Silver', 'Copper', 'FreeCurrency', 'PremiumCurrency', 'Fame', 'Alignment', 'Description');
SET @ows_character_drop_sql = COALESCE(@ows_character_drop_sql, 'SELECT 1');
PREPARE ows_character_drop FROM @ows_character_drop_sql;
EXECUTE ows_character_drop;
DEALLOCATE PREPARE ows_character_drop;
UPDATE OWSVersion SET OWSDBVersion = '20261029';
