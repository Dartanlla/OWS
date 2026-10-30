-- Password hashes move from md5-crypt ($1$) to bcrypt ($2a$, cost 12).
--
-- AddUser and the Steam placeholder password hash with bcrypt from now on. Existing $1$ hashes keep verifying,
-- because pgcrypto's crypt() reads the algorithm from the stored hash, and PlayerLoginAndCreateSession rehashes
-- a $1$ hash to bcrypt on the first successful password login. Accounts that never log in again keep their
-- md5-crypt hash. Users.PasswordHash is VARCHAR(128); a bcrypt hash is 60 characters.
--
-- bcrypt reads only the first 72 bytes of a password, so AddUser rejects longer passwords rather than storing a
-- hash that silently ignores the rest.
--
-- Idempotent: safe to run more than once.

CREATE OR REPLACE FUNCTION AddUser(_CustomerGUID UUID,
                                   _FirstName VARCHAR(50),
                                   _LastName VARCHAR(50),
                                   _Email VARCHAR(256),
                                   _Password VARCHAR(256),
                                   _Role VARCHAR(10))
    RETURNS TABLE
            (
                UserGUID UUID
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _PasswordHash VARCHAR(128);
    _UserGUID     UUID;
BEGIN
    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        UserGUID UUID
    ) ON COMMIT DROP;

    IF (OCTET_LENGTH(_Password) > 72) THEN
        RAISE EXCEPTION 'Password must be 72 bytes or fewer';
    END IF;

    _PasswordHash = crypt(_Password, gen_salt('bf', 12));
    _UserGUID := gen_random_uuid();
    INSERT INTO temp_table (UserGUID) VALUES (_UserGUID);

    INSERT INTO Users (UserGUID, CustomerGUID, FirstName, LastName, Email, PasswordHash, CreateDate, LastAccess,
                       ROLE)
    VALUES (_UserGUID, _CustomerGUID, _FirstName, _LastName, TRIM(_Email), _PasswordHash, NOW(), NOW(),
            CASE LOWER(TRIM(_Role))
                WHEN 'player' THEN 'Player'
                WHEN 'moderator' THEN 'Moderator'
                WHEN 'mod' THEN 'Moderator'
                WHEN 'gamemaster' THEN 'GameMaster'
                WHEN 'gm' THEN 'GameMaster'
                WHEN 'admin' THEN 'Admin'
                ELSE TRIM(_Role)
            END);
    RETURN QUERY SELECT *
                 FROM temp_table;
END
$$;

CREATE OR REPLACE FUNCTION PlayerLoginAndCreateSession(_CustomerGUID UUID,
                                                       _Email VARCHAR(256),
                                                       _Password VARCHAR(256),
                                                       _DontCheckPassword BOOLEAN = FALSE
)
    RETURNS TABLE
            (
                Authenticated   BOOLEAN,
                UserSessionGUID UUID,
                ErrorMessage    VARCHAR(100)
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _Authenticated   BOOLEAN = FALSE;
    _PasswordCheck   BOOLEAN = FALSE;
    _PasswordHash    VARCHAR(128);
    _UserGUID        UUID;
    _UserSessionGUID UUID;
    _ErrorMessage    VARCHAR(100) = '';
    _MatchingUsers   INT = 0;
BEGIN

    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        Authenticated   BOOLEAN,
        UserSessionGUID UUID,
        ErrorMessage    VARCHAR(100)
    ) ON COMMIT DROP;

    SELECT COUNT(*)
    INTO _MatchingUsers
    FROM Users
    WHERE CustomerGUID = _CustomerGUID
      AND LOWER(TRIM(Email)) = LOWER(TRIM(_Email));

    IF (_MatchingUsers > 1) THEN
        _ErrorMessage := 'Account configuration error';
        INSERT INTO temp_table (Authenticated, UserSessionGUID, ErrorMessage)
        VALUES (_Authenticated, _UserSessionGUID, _ErrorMessage);
        RETURN QUERY SELECT * FROM temp_table;
        RETURN;
    ELSIF (_MatchingUsers = 0) THEN
        _ErrorMessage := 'Invalid email or password';
        INSERT INTO temp_table (Authenticated, UserSessionGUID, ErrorMessage)
        VALUES (_Authenticated, _UserSessionGUID, _ErrorMessage);
        RETURN QUERY SELECT * FROM temp_table;
        RETURN;
    END IF;

    SELECT (PasswordHash = crypt(_Password, PasswordHash)), PasswordHash, UserGUID
    INTO _PasswordCheck, _PasswordHash, _UserGUID
    FROM Users
    WHERE CustomerGUID = _CustomerGUID
      AND LOWER(TRIM(Email)) = LOWER(TRIM(_Email));

    IF (_PasswordCheck = TRUE OR _DontCheckPassword = TRUE) THEN
        _Authenticated := TRUE;

        -- Upgrade a legacy md5-crypt hash now that the plaintext is known to be right. Only a verified password
        -- is rehashed, never a _DontCheckPassword login.
        IF (_PasswordCheck = TRUE AND _PasswordHash LIKE '$1$%') THEN
            UPDATE Users
            SET PasswordHash = crypt(_Password, gen_salt('bf', 12))
            WHERE UserGUID = _UserGUID
              AND PasswordHash = _PasswordHash;
        END IF;

        DELETE FROM UserSessions WHERE UserGUID = _UserGUID;
        _UserSessionGUID := gen_random_uuid();
        INSERT INTO UserSessions (CustomerGUID, UserSessionGUID, UserGUID, LoginDate)
        VALUES (_CustomerGUID, _UserSessionGUID, _UserGUID, NOW());
    ELSE
        _ErrorMessage := 'Invalid email or password';
    END IF;
    INSERT INTO temp_table (Authenticated, UserSessionGUID, ErrorMessage)
    VALUES (_Authenticated, _UserSessionGUID, _ErrorMessage);

    RETURN QUERY SELECT * FROM temp_table;

END
$$;

CREATE OR REPLACE FUNCTION SteamLoginAndCreateSession(_CustomerGUID UUID,
                                                      _SteamId VARCHAR(20),
                                                      _PersonaName VARCHAR(50)
)
    RETURNS TABLE
            (
                Authenticated   BOOLEAN,
                UserSessionGUID UUID,
                ErrorMessage    VARCHAR(100)
            )
    LANGUAGE PLPGSQL
AS
$$
DECLARE
    _Authenticated   BOOLEAN = FALSE;
    _UserGUID        UUID;
    _UserSessionGUID UUID;
    _ErrorMessage    VARCHAR(100) = '';
    _MatchingUsers   INT = 0;
BEGIN

    CREATE TEMP TABLE IF NOT EXISTS temp_table
    (
        Authenticated   BOOLEAN,
        UserSessionGUID UUID,
        ErrorMessage    VARCHAR(100)
    ) ON COMMIT DROP;

    IF (_SteamId IS NULL OR TRIM(_SteamId) = '') THEN
        _ErrorMessage := 'Invalid SteamId';
        INSERT INTO temp_table (Authenticated, UserSessionGUID, ErrorMessage)
        VALUES (_Authenticated, _UserSessionGUID, _ErrorMessage);
        RETURN QUERY SELECT * FROM temp_table;
        RETURN;
    END IF;

    SELECT COUNT(*)
    INTO _MatchingUsers
    FROM Users
    WHERE CustomerGUID = _CustomerGUID
      AND SteamId = TRIM(_SteamId);

    IF (_MatchingUsers > 1) THEN
        _ErrorMessage := 'Account configuration error';
        INSERT INTO temp_table (Authenticated, UserSessionGUID, ErrorMessage)
        VALUES (_Authenticated, _UserSessionGUID, _ErrorMessage);
        RETURN QUERY SELECT * FROM temp_table;
        RETURN;
    ELSIF (_MatchingUsers = 0) THEN
        -- First login for this SteamId: JIT-create a player account.
        -- Synthetic email keeps the NOT NULL + unique-email constraints satisfied;
        -- random crypted password means the account can never password-login.
        _UserGUID := gen_random_uuid();
        INSERT INTO Users (UserGUID, CustomerGUID, FirstName, LastName, Email, PasswordHash, CreateDate, LastAccess,
                           Role, SteamId)
        VALUES (_UserGUID, _CustomerGUID, COALESCE(NULLIF(TRIM(_PersonaName), ''), 'SteamUser'), '',
                'steam_' || TRIM(_SteamId) || '@steam.samsarasaga.invalid',
                crypt(gen_random_uuid()::text, gen_salt('bf', 12)), NOW(), NOW(),
                'Player', TRIM(_SteamId));
    ELSE
        SELECT UserGUID
        INTO _UserGUID
        FROM Users
        WHERE CustomerGUID = _CustomerGUID
          AND SteamId = TRIM(_SteamId);

        UPDATE Users
        SET LastAccess = NOW()
        WHERE UserGUID = _UserGUID;
    END IF;

    _Authenticated := TRUE;
    DELETE FROM UserSessions WHERE UserGUID = _UserGUID;
    _UserSessionGUID := gen_random_uuid();
    INSERT INTO UserSessions (CustomerGUID, UserSessionGUID, UserGUID, LoginDate)
    VALUES (_CustomerGUID, _UserSessionGUID, _UserGUID, NOW());

    INSERT INTO temp_table (Authenticated, UserSessionGUID, ErrorMessage)
    VALUES (_Authenticated, _UserSessionGUID, _ErrorMessage);

    RETURN QUERY SELECT * FROM temp_table;

END
$$;
