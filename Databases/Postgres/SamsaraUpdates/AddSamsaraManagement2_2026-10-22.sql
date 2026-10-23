-- Samsara Management, part 2 (docs/hosting/hetzner-dev.md, Admin Console).
--
-- PlayerReports: one player's report of another, filed by the zone server. ChatExcerpt is a copy of both players'
--   chat from the 10 minutes before the report, so the 30-day ChatLog purge does not remove it.
-- LoginQueue: sessions waiting to enter the world while the console's player cap is reached. A row that is not
--   asked for in 30 s no longer counts. AdmittedAt marks a session let in that has not reached a zone yet.
-- LivePositions: the latest position of each player in the world, sent by zone servers with each sync.
--
-- Idempotent: safe to run more than once.
CREATE TABLE IF NOT EXISTS PlayerReports
(
    ReportID         BIGSERIAL    NOT NULL CONSTRAINT PK_PlayerReports PRIMARY KEY,
    CustomerGUID     UUID         NOT NULL,
    CreateDate       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    ReporterName     VARCHAR(50)  NOT NULL,
    ReporterUserGUID UUID         NULL,
    TargetName       VARCHAR(50)  NOT NULL,
    TargetUserGUID   UUID         NULL,
    Reason           TEXT         NOT NULL,
    MapName          VARCHAR(50)  NULL,
    MapInstanceID    INT          NULL,
    Details          JSONB        NOT NULL DEFAULT '{}'::JSONB,
    ChatExcerpt      JSONB        NOT NULL DEFAULT '[]'::JSONB,
    ResolvedAt       TIMESTAMPTZ  NULL,
    ResolvedBy       VARCHAR(100) NULL,
    ResolutionNote   TEXT         NULL
);
CREATE INDEX IF NOT EXISTS IX_PlayerReports_Created ON PlayerReports (CustomerGUID, CreateDate DESC);
CREATE INDEX IF NOT EXISTS IX_PlayerReports_Target ON PlayerReports (CustomerGUID, LOWER(TargetName));
CREATE INDEX IF NOT EXISTS IX_PlayerReports_Reporter ON PlayerReports (CustomerGUID, LOWER(ReporterName));

CREATE TABLE IF NOT EXISTS LoginQueue
(
    CustomerGUID    UUID        NOT NULL,
    UserSessionGUID UUID        NOT NULL,
    UserGUID        UUID        NOT NULL,
    CharacterName   VARCHAR(50) NOT NULL,
    EnqueuedAt      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    LastSeenAt      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    AdmittedAt      TIMESTAMPTZ NULL,
    CONSTRAINT PK_LoginQueue PRIMARY KEY (CustomerGUID, UserSessionGUID)
);

CREATE TABLE IF NOT EXISTS LivePositions
(
    CustomerGUID  UUID        NOT NULL,
    CharacterName VARCHAR(50) NOT NULL,
    MapInstanceID INT         NOT NULL,
    MapName       VARCHAR(50) NULL,
    X             REAL        NOT NULL,
    Y             REAL        NOT NULL,
    Z             REAL        NOT NULL,
    UpdatedAt     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT PK_LivePositions PRIMARY KEY (CustomerGUID, CharacterName)
);
CREATE INDEX IF NOT EXISTS IX_LivePositions_Map ON LivePositions (CustomerGUID, MapName);

CREATE INDEX IF NOT EXISTS IX_GameEvents_Time ON GameEvents (CustomerGUID, EventTime DESC);

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

    DELETE FROM LoginQueue WHERE LastSeenAt < NOW() - INTERVAL '1 day';
    DELETE FROM LivePositions WHERE UpdatedAt < NOW() - INTERVAL '1 day';
    GET DIAGNOSTICS Count = ROW_COUNT;
    What := 'stale live positions'; Removed := Count; RETURN NEXT;
END
$$;
