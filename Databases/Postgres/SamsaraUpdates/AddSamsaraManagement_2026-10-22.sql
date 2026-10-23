-- Samsara Management, the staff console (OWSManagement).
--
-- AccountSanctions: bans, mutes, warnings and staff notes, on the account. Written by the console and by the in-game
--   staff commands (CharacterPersistence api/Sanctions). A ban or mute is active while RevokedAt is NULL and ExpiresAt is
--   NULL (permanent) or in the future. PublicAPI refuses a banned login; OWSChat drops a muted sender's messages.
-- ZoneCommands: console commands for zone servers (kick, mute, announce, unstuck, gifts). A zone server picks up its
--   commands, and a character's commands wherever that character is online, through CharacterPersistence api/ZoneSync.
-- GameEvents: the zone servers' SSEVENT lines (economy, progression, sessions), sent through api/ZoneSync.
-- ChatLog: every chat message OWSChat relays, and the ones a mute dropped.
-- ConsoleSettings: console-owned switches: motd, logins_open, region_status:<region>.
-- CharInventoryItems.InstanceId: each item's identity across saves, so the console can find duplicates.
--
-- Idempotent: safe to run more than once.
CREATE TABLE IF NOT EXISTS AccountSanctions
(
    SanctionID    BIGSERIAL    NOT NULL CONSTRAINT PK_AccountSanctions PRIMARY KEY,
    CustomerGUID  UUID         NOT NULL,
    UserGUID      UUID         NOT NULL,
    Kind          VARCHAR(8)   NOT NULL CONSTRAINT CK_AccountSanctions_Kind CHECK (Kind IN ('ban', 'mute', 'warn', 'note')),
    Reason        TEXT         NOT NULL DEFAULT '',
    CharacterName VARCHAR(50)  NULL,
    CreateDate    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    ExpiresAt     TIMESTAMPTZ  NULL,
    IssuedBy      VARCHAR(100) NOT NULL,
    IssuedFrom    VARCHAR(8)   NOT NULL CONSTRAINT CK_AccountSanctions_IssuedFrom CHECK (IssuedFrom IN ('console', 'zone')),
    RevokedAt     TIMESTAMPTZ  NULL,
    RevokedBy     VARCHAR(100) NULL
);

CREATE INDEX IF NOT EXISTS IX_AccountSanctions_User ON AccountSanctions (CustomerGUID, UserGUID, CreateDate DESC);
CREATE INDEX IF NOT EXISTS IX_AccountSanctions_Open ON AccountSanctions (CustomerGUID, Kind) WHERE RevokedAt IS NULL;

CREATE TABLE IF NOT EXISTS ZoneCommands
(
    CommandID                BIGSERIAL    NOT NULL CONSTRAINT PK_ZoneCommands PRIMARY KEY,
    CustomerGUID             UUID         NOT NULL,
    MapInstanceID            INT          NULL,
    CharacterName            VARCHAR(50)  NULL,
    Command                  VARCHAR(24)  NOT NULL,
    Args                     TEXT         NOT NULL DEFAULT '{}',
    CreateDate               TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CreatedBy                VARCHAR(100) NOT NULL,
    ExpiresAt                TIMESTAMPTZ  NOT NULL,
    DeliveredAt              TIMESTAMPTZ  NULL,
    DeliveredToMapInstanceID INT          NULL,
    CompletedAt              TIMESTAMPTZ  NULL,
    Result                   VARCHAR(16)  NULL,
    ResultDetail             TEXT         NULL,
    CONSTRAINT CK_ZoneCommands_Target CHECK (MapInstanceID IS NOT NULL OR CharacterName IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS IX_ZoneCommands_Pending ON ZoneCommands (CustomerGUID, ExpiresAt) WHERE CompletedAt IS NULL;
CREATE INDEX IF NOT EXISTS IX_ZoneCommands_CreateDate ON ZoneCommands (CustomerGUID, CreateDate DESC);

CREATE TABLE IF NOT EXISTS GameEvents
(
    GameEventID   BIGSERIAL   NOT NULL CONSTRAINT PK_GameEvents PRIMARY KEY,
    CustomerGUID  UUID        NOT NULL,
    EventGUID     UUID        NOT NULL CONSTRAINT AK_GameEvents_EventGUID UNIQUE,
    EventTime     TIMESTAMPTZ NOT NULL,
    ReceivedAt    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    MapInstanceID INT         NULL,
    MapName       VARCHAR(50) NULL,
    CharacterName VARCHAR(50) NULL,
    Event         VARCHAR(32) NOT NULL,
    Fields        JSONB       NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_GameEvents_Character ON GameEvents (CustomerGUID, CharacterName, EventTime DESC);
CREATE INDEX IF NOT EXISTS IX_GameEvents_Event ON GameEvents (CustomerGUID, Event, EventTime DESC);

CREATE TABLE IF NOT EXISTS ChatLog
(
    ChatLogID      BIGSERIAL   NOT NULL CONSTRAINT PK_ChatLog PRIMARY KEY,
    CustomerGUID   UUID        NOT NULL,
    CreateDate     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    Channel        VARCHAR(16) NOT NULL,
    SenderName     VARCHAR(50) NOT NULL,
    SenderUserGUID UUID        NULL,
    TargetName     VARCHAR(50) NULL,
    Message        TEXT        NOT NULL,
    Dropped        BOOLEAN     NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS IX_ChatLog_CreateDate ON ChatLog (CustomerGUID, CreateDate DESC);
CREATE INDEX IF NOT EXISTS IX_ChatLog_Sender ON ChatLog (CustomerGUID, SenderName, CreateDate DESC);

CREATE TABLE IF NOT EXISTS ConsoleSettings
(
    SettingKey   VARCHAR(64)  NOT NULL CONSTRAINT PK_ConsoleSettings PRIMARY KEY,
    SettingValue TEXT         NOT NULL,
    UpdatedAt    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UpdatedBy    VARCHAR(100) NOT NULL
);

ALTER TABLE CharInventoryItems ADD COLUMN IF NOT EXISTS InstanceId UUID NULL;
CREATE INDEX IF NOT EXISTS IX_CharInventoryItems_InstanceId ON CharInventoryItems (CustomerGUID, InstanceId) WHERE InstanceId IS NOT NULL;

-- The persistent character blob is client-written text; a row that is not valid JSON reads as NULL instead of failing
-- the whole console query.
CREATE OR REPLACE FUNCTION SamsaraTryJsonb(Value TEXT) RETURNS JSONB
    LANGUAGE plpgsql IMMUTABLE AS
$$
BEGIN
    RETURN Value::JSONB;
EXCEPTION WHEN OTHERS THEN
    RETURN NULL;
END
$$;

-- Run daily by ows-pie-purge.timer. Finished or expired commands go after 30 days, chat after 30, events after 180.
CREATE OR REPLACE FUNCTION PurgeSamsaraLogs()
    RETURNS TABLE (What TEXT, Removed BIGINT)
    LANGUAGE plpgsql AS
$$
DECLARE
    Count BIGINT;
BEGIN
    DELETE FROM GameEvents WHERE EventTime < NOW() - INTERVAL '180 days';
    GET DIAGNOSTICS Count = ROW_COUNT;
    What := 'game events'; Removed := Count; RETURN NEXT;

    DELETE FROM ChatLog WHERE CreateDate < NOW() - INTERVAL '30 days';
    GET DIAGNOSTICS Count = ROW_COUNT;
    What := 'chat lines'; Removed := Count; RETURN NEXT;

    DELETE FROM ZoneCommands
    WHERE CreateDate < NOW() - INTERVAL '30 days' AND (CompletedAt IS NOT NULL OR ExpiresAt < NOW());
    GET DIAGNOSTICS Count = ROW_COUNT;
    What := 'zone commands'; Removed := Count; RETURN NEXT;
END
$$;
