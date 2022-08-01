UPDATE OWSVersion
SET OWSDBVersion='20220801'
WHERE OWSDBVersion IS NOT NULL;

SELECT OWSDBVersion
FROM OWSVersion;

CREATE OR REPLACE PROCEDURE addnewcustomer(IN _customername character varying, IN _firstname character varying, IN _lastname character varying, IN _email character varying, IN _password character varying, IN _customerguid UUID)
    LANGUAGE plpgsql
AS
$$
DECLARE
    _UserGUID      UUID;
    _ClassID       INT;
    _CharacterName VARCHAR(50) := 'Test';
    _CharacterID   INT;
BEGIN
    IF _CustomerGUID IS NULL THEN
            _CustomerGUID := gen_random_uuid();
    END IF;

    IF NOT EXISTS(SELECT
              FROM Customers
              WHERE CustomerGUID = _CustomerGUID)
        THEN

        INSERT INTO Customers (CustomerGUID, CustomerName, CustomerEmail, CustomerPhone, CustomerNotes, EnableDebugLogging)
        VALUES (_CustomerGUID, _CustomerName, _Email, '', '', TRUE);

        INSERT INTO WorldSettings (CustomerGUID, StartTime)
        SELECT _CustomerGUID, CAST(EXTRACT(EPOCH FROM CURRENT_TIMESTAMP) AS BIGINT)
        FROM Customers C
        WHERE C.CustomerGUID = _CustomerGUID;

        SELECT UserGUID FROM AddUser(_CustomerGUID, _FirstName, _LastName, _Email, _Password, 'Developer') INTO _UserGUID;

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

        INSERT INTO CLASS (CustomerGUID, ClassName, StartingMapName, X, Y, Z, Perception, Acrobatics, Climb, Stealth, RX,
                           RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender, XP,
                           HitDie, Wounds, Size, weight, MaxHealth, Health, HealthRegenRate, MaxMana, Mana, ManaRegenRate,
                           MaxEnergy, Energy, EnergyRegenRate, MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina,
                           StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity, Constitution,
                           Intellect, Wisdom, Charisma, Agility, Fortitude, Reflex, Willpower, BaseAttack, BaseAttackBonus,
                           AttackPower, AttackSpeed, CritChance, CritMultiplier, Haste, SpellPower, SpellPenetration,
                           Defense, Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor,
                           PhysicalArmor, BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed, RANGE, Speed, Silver,
                           Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
        VALUES (_CustomerGUID, 'MaleWarrior', 'ThirdPersonExampleMap', 0, 0, 250, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                1, 1, 0, 10, 0, 1, 0, 100, 50, 1, 100, 0, 1, 100, 0, 5, 100, 0, 1, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10,
                0, 1, 1, 1, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                '');

        _ClassID := CURRVAL(PG_GET_SERIAL_SEQUENCE('class', 'classid'));

        INSERT INTO Characters (CustomerGUID, ClassID, UserGUID, Email, CharName, MapName, X, Y, Z, Perception, Acrobatics,
                                Climb, Stealth, ServerIP, LastActivity,
                                RX, RY, RZ, Spirit, Magic, TeamNumber, Thirst, Hunger, Gold, Score, CharacterLevel, Gender,
                                XP, HitDie, Wounds, Size, weight, MaxHealth, Health,
                                HealthRegenRate, MaxMana, Mana, ManaRegenRate, MaxEnergy, Energy, EnergyRegenRate,
                                MaxFatigue, Fatigue, FatigueRegenRate, MaxStamina, Stamina,
                                StaminaRegenRate, MaxEndurance, Endurance, EnduranceRegenRate, Strength, Dexterity,
                                Constitution, Intellect, Wisdom, Charisma, Agility, Fortitude,
                                Reflex, Willpower, BaseAttack, BaseAttackBonus, AttackPower, AttackSpeed, CritChance,
                                CritMultiplier, Haste, SpellPower, SpellPenetration, Defense,
                                Dodge, Parry, Avoidance, Versatility, Multishot, Initiative, NaturalArmor, PhysicalArmor,
                                BonusArmor, ForceArmor, MagicArmor, Resistance, ReloadSpeed,
                                RANGE, Speed, Silver, Copper, FreeCurrency, PremiumCurrency, Fame, ALIGNMENT, Description)
        SELECT _CustomerGUID,
               _ClassID,
               _UserGUID,
               '',
               _CharacterName,
               StartingMapName,
               X,
               Y,
               Z,
               Perception,
               Acrobatics,
               Climb,
               Stealth,
               '',
               NOW(),
               RX,
               RY,
               RZ,
               Spirit,
               Magic,
               TeamNumber,
               Thirst,
               Hunger,
               Gold,
               Score,
               CharacterLevel,
               Gender,
               XP,
               HitDie,
               Wounds,
               Size,
               weight,
               MaxHealth,
               Health,
               HealthRegenRate,
               MaxMana,
               Mana,
               ManaRegenRate,
               MaxEnergy,
               Energy,
               EnergyRegenRate,
               MaxFatigue,
               Fatigue,
               FatigueRegenRate,
               MaxStamina,
               Stamina,
               StaminaRegenRate,
               MaxEndurance,
               Endurance,
               EnduranceRegenRate,
               Strength,
               Dexterity,
               Constitution,
               Intellect,
               Wisdom,
               Charisma,
               Agility,
               Fortitude,
               Reflex,
               Willpower,
               BaseAttack,
               BaseAttackBonus,
               AttackPower,
               AttackSpeed,
               CritChance,
               CritMultiplier,
               Haste,
               SpellPower,
               SpellPenetration,
               Defense,
               Dodge,
               Parry,
               Avoidance,
               Versatility,
               Multishot,
               Initiative,
               NaturalArmor,
               PhysicalArmor,
               BonusArmor,
               ForceArmor,
               MagicArmor,
               Resistance,
               ReloadSpeed,
               RANGE,
               Speed,
               Silver,
               Copper,
               FreeCurrency,
               PremiumCurrency,
               Fame,
               ALIGNMENT,
               Description
        FROM CLASS
        WHERE ClassID = _ClassID;

        _CharacterID := CURRVAL(PG_GET_SERIAL_SEQUENCE('characters', 'characterid'));

        INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
        VALUES (_CustomerGUID, _CharacterID, 'Bag', 16);
    ELSE
        RAISE 'Duplicate Customer GUID: %', _CustomerGUID USING ERRCODE = 'unique_violation';
    END IF;
END
$$;

ALTER PROCEDURE addnewcustomer(VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR) OWNER TO ows;

