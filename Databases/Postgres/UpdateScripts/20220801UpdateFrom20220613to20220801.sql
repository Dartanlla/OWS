UPDATE OWSVersion
SET OWSDBVersion='20220801'
WHERE OWSDBVersion IS NOT NULL;

SELECT OWSDBVersion
FROM OWSVersion;

CREATE OR REPLACE PROCEDURE AddNewCustomer(_CustomerName VARCHAR(50),
                                           _FirstName VARCHAR(50),
                                           _LastName VARCHAR(50),
                                           _Email VARCHAR(256),
                                           _Password VARCHAR(256),
                                           _CustomerGuid UUID)
    LANGUAGE PLPGSQL
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


CREATE OR REPLACE FUNCTION JoinMapByCharName(_CustomerGUID UUID,
                                             _CharName VARCHAR(50),
                                             _ZoneName VARCHAR(50),
                                             _PlayerGroupType INT)
    RETURNS TABLE
            (
                ServerIP           VARCHAR(50),
                WorldServerID      INT,
                WorldServerIP      VARCHAR(50),
                WorldServerPort    INT,
                Port               INT,
                MapInstanceID      INT,
                MapNameToStart     VARCHAR(50),
                MapInstanceStatus  INT,
                NeedToStartUpMap   BOOLEAN,
                EnableAutoLoopBack BOOLEAN,
                NoPortForwarding   BOOLEAN
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _MapID                     INT;
    _MapNameToStart            VARCHAR(50);
    _CharacterID               INT;
    _Email                     VARCHAR(255);
    _SoftPlayerCap             INT;
    _PlayerGroupID             INT;
    _ServerIP                  VARCHAR(50);
    _WorldServerID             INT;
    _WorldServerIP             VARCHAR(50);
    _WorldServerPort           INT;
    _Port                      INT;
    _MapInstanceID             INT;
    _MapInstanceStatus         INT;
    _NeedToStartUpMap          BOOLEAN;
    _EnableAutoLoopBack        BOOLEAN;
    _NoPortForwarding          BOOLEAN;
    _IsInternalNetworkTestUser BOOLEAN := FALSE;
    _ErrorRaised               BOOLEAN := FALSE;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        ServerIP           VARCHAR(50),
        WorldServerID      INT,
        WorldServerIP      VARCHAR(50),
        WorldServerPort    INT,
        Port               INT,
        MapInstanceID      INT,
        MapNameToStart     VARCHAR(50),
        MapInstanceStatus  INT,
        NeedToStartUpMap   BOOLEAN,
        EnableAutoLoopBack BOOLEAN,
        NoPortForwarding   BOOLEAN
    ) ON COMMIT DROP;

    --Run Cleanup here for now.  Later this can get moved to a scheduler to run periodically.
    CALL CleanUp(_CustomerGUID);

    INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
    VALUES (NOW(), 'JoinMapByCharName: ' || _ZoneName || ' - ' || _CharName, _CustomerGUID);

    SELECT M.MapID, M.MapName, M.SoftPlayerCap
    INTO _MapID, _MapNameToStart, _SoftPlayerCap
    FROM Maps M
    WHERE M.ZoneName = _ZoneName
      AND M.CustomerGUID = _CustomerGUID;

    SELECT C.CharacterID, C.IsInternalNetworkTestUser, C.Email
    INTO _CharacterID, _IsInternalNetworkTestUser, _Email
    FROM Characters C
    WHERE C.CharName = _CharName
      AND C.CustomerGUID = _CustomerGUID;

    IF (_CharacterID IS NULL) THEN
        INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
        VALUES (NOW(), 'JoinMapByCharName: CharacterID is NULL!', _CustomerGUID);

        _NeedToStartUpMap := 0;
        _ErrorRaised := TRUE;
    END IF;

    IF _ErrorRaised = FALSE THEN
        SELECT C.EnableAutoLoopBack, C.NoPortForwarding
        INTO _EnableAutoLoopBack, _NoPortForwarding
        FROM Customers C
        WHERE C.CustomerGUID = _CustomerGUID;
    END IF;

    IF _ErrorRaised = FALSE AND (_PlayerGroupType > 0) THEN
        SELECT COALESCE(PG.PlayerGroupID, 0)
        FROM PlayerGroupCharacters PGC
                 INNER JOIN PlayerGroup PG
                            ON PG.PlayerGroupID = PGC.PlayerGroupID
        WHERE PGC.CustomerGUID = _CustomerGUID
          AND PGC.CharacterID = _CharacterID
          AND PG.PlayerGroupTypeID = _PlayerGroupType
        INTO _PlayerGroupID;
    END IF;

    IF _ErrorRaised = FALSE THEN
        SELECT (CASE
                    WHEN _IsInternalNetworkTestUser = TRUE THEN WS.InternalServerIP
                    ELSE WS.ServerIP END) AS ServerIp,
               WS.InternalServerIP,
               WS.Port                    AS WSPort,
               MI.Port                    AS MIPort,
               MI.MapInstanceID,
               WS.WorldServerID,
               MI.Status
        INTO _ServerIP, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID, _WorldServerID, _MapInstanceStatus
        FROM WorldServers WS
                 LEFT JOIN MapInstances MI
                           ON MI.WorldServerID = WS.WorldServerID
                               AND MI.CustomerGUID = WS.CustomerGUID
                 LEFT JOIN CharOnMapInstance CMI
                           ON CMI.MapInstanceID = MI.MapInstanceID
                               AND CMI.CustomerGUID = MI.CustomerGUID
        WHERE MI.MapID = _MapID
          AND WS.ActiveStartTime IS NOT NULL
          AND WS.CustomerGUID = _CustomerGUID
          AND MI.NumberOfReportedPlayers < _SoftPlayerCap
          AND (MI.PlayerGroupID = _PlayerGroupID OR COALESCE(_PlayerGroupID,0) = 0) --Only lookup map instances that match the player group fro this Player Group Type or lookup all if zero
          AND MI.Status = 2
        GROUP BY MI.MapInstanceID, WS.ServerIP, MI.Port, WS.WorldServerID, WS.InternalServerIP, WS.Port, MI.Status
        ORDER BY COUNT(DISTINCT CMI.CharacterID);


        --There is a map already running to connect to
        IF _MapInstanceID IS NOT NULL THEN
            /*IF (POSITION('\@localhost' IN _Email) > 0) THEN
                _ServerIP := '127.0.0.1';
            END IF;*/

            _NeedToStartUpMap := FALSE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(), 'Joined Existing Map: ' || COALESCE(_ZoneName, '') || ' - ' || COALESCE(_CharName, '') ||
                           ' - ' || COALESCE(_ServerIP, ''),
                    _CustomerGUID);
        ELSE --Spin up a new map

            SELECT *
            FROM SpinUpMapInstance(_CustomerGUID, _ZoneName, _PlayerGroupID)
            INTO _ServerIP , _WorldServerID , _WorldServerIP , _WorldServerPort , _Port, _MapInstanceID;

            /*IF (POSITION('@localhost' IN _Email) > 0 OR _IsInternalNetworkTestUser = TRUE) THEN
                _ServerIP := '127.0.0.1';
            END IF;*/

            _NeedToStartUpMap := TRUE;

            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    'SpinUpMapInstance returned: ' || COALESCE(_ZoneName, '') || ' CharName: ' ||
                    COALESCE(_CharName, '') || ' ServerIP: ' ||
                    COALESCE(_ServerIP, '') ||
                    ' WorldServerPort: ' || CAST(COALESCE(_WorldServerPort, -1) AS VARCHAR), _CustomerGUID);


            INSERT INTO DebugLog (DebugDate, DebugDesc, CustomerGUID)
            VALUES (NOW(),
                    'JoinMapByCharName returned: ' || COALESCE(_MapNameToStart, '[NoMapName]') || ' MapInstanceID: ' ||
                    CAST(COALESCE(_MapInstanceID, -1) AS VARCHAR) || ' MapInstanceStatus: ' ||
                    CAST(COALESCE(_MapInstanceStatus, -1) AS VARCHAR) || ' NeedToStartUpMap: ' ||
                    CAST(_NeedToStartUpMap AS VARCHAR) || ' EnableAutoLoopBack: ' ||
                    CAST(_EnableAutoLoopBack AS VARCHAR) ||
                    ' ServerIP: ' || COALESCE(_ServerIP, '') || ' WorldServerIP: ' || COALESCE(_WorldServerIP, ''),
                    _CustomerGUID);
        END IF;
    END IF;
    INSERT INTO temp_table(ServerIP, WorldServerID, WorldServerIP, WorldServerPort, Port, MapInstanceID, MapNameToStart,
                           MapInstanceStatus, NeedToStartUpMap, EnableAutoLoopBack, NoPortForwarding)
    VALUES (_ServerIP, _WorldServerID, _WorldServerIP, _WorldServerPort, _Port, _MapInstanceID, _MapNameToStart,
            _MapInstanceStatus, _NeedToStartUpMap, _EnableAutoLoopBack, _NoPortForwarding);
    RETURN QUERY SELECT * FROM temp_table;
END;
$$;
