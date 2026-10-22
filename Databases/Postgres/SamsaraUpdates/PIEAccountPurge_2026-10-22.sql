-- RemoveCharacter also clears the Samsara tables it missed (stats, abilities, quests, party and player-group
-- membership, join tickets), which left a row trail behind every deleted character. Player shops stay: closing a
-- real character's shop with pending deliveries is an economy decision, not a delete.
--
-- PurgePIEAccounts removes the throwaway accounts the editor's PIE character provisioning registers
-- (email @samsara.local, first name PIE) older than _OlderThan, with their characters, shops, sessions and audit
-- rows, then sweeps rows whose character no longer exists and parties left without members. One transaction.
-- Run nightly by ows-pie-purge.timer on the API box; returns what it removed.
--
-- Idempotent: safe to run more than once.
CREATE OR REPLACE PROCEDURE RemoveCharacter(_CustomerGUID UUID, _UserSessionGUID UUID, _CharacterName VARCHAR)
    LANGUAGE plpgsql
AS
$$
DECLARE
    _UserGUID    UUID;
    _CharacterID INT;
BEGIN
    SELECT US.UserGUID
    INTO _UserGUID
    FROM UserSessions US
    WHERE US.CustomerGUID = _CustomerGUID
      AND US.UserSessionGUID = _UserSessionGUID;
    IF _UserGUID IS NOT NULL THEN
        SELECT CharacterID
        INTO _CharacterID
        FROM Characters C
        WHERE C.CustomerGUID = _CustomerGUID
          AND C.UserGUID = _UserGUID
          AND C.CharName = _CharacterName;
        DELETE
        FROM CharAbilityBarAbilities
        WHERE CustomerGUID = _CustomerGUID
          AND CharAbilityBarID
            IN (SELECT CharAbilityBarID
                FROM CharAbilityBars
                WHERE CustomerGUID = _CustomerGUID
                  AND CharacterID = _CharacterID);
        DELETE FROM CharAbilityBars WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharHasAbilities WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharHasItems WHERE CharacterID = _CharacterID;
        DELETE
        FROM CharInventoryItems
        WHERE CustomerGUID = _CustomerGUID
          AND CharInventoryID
            IN (SELECT CharInventoryID
                FROM CharInventory
                WHERE CustomerGUID = _CustomerGUID
                  AND CharacterID = _CharacterID);
        DELETE FROM CharInventory WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharOnMapInstance WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM ChatGroupUsers WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CustomCharacterData WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM PlayerGroupCharacters WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM PlayerGroupMember WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM PartyMember WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharStats WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharAbilities WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM CharQuests WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM JoinTickets WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
        DELETE FROM Characters WHERE CustomerGUID = _CustomerGUID AND CharacterID = _CharacterID;
    END IF;
END
$$;

CREATE OR REPLACE FUNCTION PurgePIEAccounts(_OlderThan INTERVAL DEFAULT INTERVAL '6 hours')
    RETURNS TABLE (What TEXT, Removed BIGINT)
    LANGUAGE plpgsql
AS
$$
DECLARE
    _Count BIGINT;
BEGIN
    PERFORM set_config('client_min_messages', 'warning', true);
    DROP TABLE IF EXISTS PIEUsers, PIEChars, PIEShops;
    CREATE TEMP TABLE PIEUsers ON COMMIT DROP AS
    SELECT U.CustomerGUID, U.UserGUID, U.Email
    FROM Users U
    WHERE U.Email LIKE '%@samsara.local'
      AND U.FirstName = 'PIE'
      AND U.CreateDate < LOCALTIMESTAMP - _OlderThan;

    CREATE TEMP TABLE PIEChars ON COMMIT DROP AS
    SELECT C.CustomerGUID, C.CharacterID
    FROM Characters C
             JOIN PIEUsers P ON P.CustomerGUID = C.CustomerGUID AND P.UserGUID = C.UserGUID;

    CREATE TEMP TABLE PIEShops ON COMMIT DROP AS
    SELECT S.PlayerShopID
    FROM PlayerShops S
             JOIN PIEUsers P ON P.CustomerGUID = S.CustomerGUID AND P.UserGUID = S.OwnerUserGUID;

    DELETE FROM PlayerShopPurchases
    WHERE PlayerShopID IN (SELECT PlayerShopID FROM PIEShops)
       OR (CustomerGUID, BuyerCharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM PlayerShopItems WHERE PlayerShopID IN (SELECT PlayerShopID FROM PIEShops);
    DELETE FROM PlayerShops WHERE PlayerShopID IN (SELECT PlayerShopID FROM PIEShops);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'player shops'; Removed := _Count; RETURN NEXT;

    DELETE
    FROM CharAbilityBarAbilities
    WHERE (CustomerGUID, CharAbilityBarID) IN
          (SELECT B.CustomerGUID, B.CharAbilityBarID
           FROM CharAbilityBars B
                    JOIN PIEChars P ON P.CustomerGUID = B.CustomerGUID AND P.CharacterID = B.CharacterID);
    DELETE FROM CharAbilityBars WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM CharHasAbilities WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM CharHasItems WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE
    FROM CharInventoryItems
    WHERE (CustomerGUID, CharInventoryID) IN
          (SELECT I.CustomerGUID, I.CharInventoryID
           FROM CharInventory I
                    JOIN PIEChars P ON P.CustomerGUID = I.CustomerGUID AND P.CharacterID = I.CharacterID);
    DELETE FROM CharInventory WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM CharOnMapInstance WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM ChatGroupUsers WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM CustomCharacterData WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM PlayerGroupCharacters WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM PlayerGroupMember WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM PartyMember WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM CharStats WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM CharAbilities WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM CharQuests WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    DELETE FROM JoinTickets WHERE (CustomerGUID, UserGUID) IN (SELECT CustomerGUID, UserGUID FROM PIEUsers);
    DELETE FROM Characters WHERE (CustomerGUID, CharacterID) IN (SELECT CustomerGUID, CharacterID FROM PIEChars);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'characters'; Removed := _Count; RETURN NEXT;

    DELETE FROM UserSessions WHERE (CustomerGUID, UserGUID) IN (SELECT CustomerGUID, UserGUID FROM PIEUsers);
    DELETE FROM UsersInQueue WHERE (CustomerGUID, UserGUID) IN (SELECT CustomerGUID, UserGUID FROM PIEUsers);
    DELETE FROM AuditLog WHERE (CustomerGUID, Actor) IN (SELECT CustomerGUID, Email FROM PIEUsers);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'audit rows'; Removed := _Count; RETURN NEXT;
    DELETE FROM Users WHERE (CustomerGUID, UserGUID) IN (SELECT CustomerGUID, UserGUID FROM PIEUsers);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'accounts'; Removed := _Count; RETURN NEXT;

    -- Rows of characters deleted before RemoveCharacter cleared these tables, by anyone.
    DELETE FROM CharStats X WHERE NOT EXISTS (SELECT 1 FROM Characters C WHERE C.CharacterID = X.CharacterID);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'orphaned stats'; Removed := _Count; RETURN NEXT;
    DELETE FROM CharAbilities X WHERE NOT EXISTS (SELECT 1 FROM Characters C WHERE C.CharacterID = X.CharacterID);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'orphaned abilities'; Removed := _Count; RETURN NEXT;
    DELETE FROM CharQuests X WHERE NOT EXISTS (SELECT 1 FROM Characters C WHERE C.CharacterID = X.CharacterID);
    DELETE FROM PartyMember X WHERE NOT EXISTS (SELECT 1 FROM Characters C WHERE C.CharacterID = X.CharacterID);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'orphaned party members'; Removed := _Count; RETURN NEXT;
    DELETE FROM PlayerGroupMember X WHERE NOT EXISTS (SELECT 1 FROM Characters C WHERE C.CharacterID = X.CharacterID);
    DELETE FROM CharInventoryItems X WHERE NOT EXISTS (SELECT 1 FROM CharInventory I WHERE I.CharInventoryID = X.CharInventoryID);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'orphaned inventory items'; Removed := _Count; RETURN NEXT;

    DELETE
    FROM Party P
    WHERE P.CreatedAt < NOW() - _OlderThan
      AND NOT EXISTS (SELECT 1 FROM PartyMember M WHERE M.CustomerGUID = P.CustomerGUID AND M.PartyID = P.PartyID);
    GET DIAGNOSTICS _Count = ROW_COUNT;
    What := 'empty parties'; Removed := _Count; RETURN NEXT;
END
$$;
