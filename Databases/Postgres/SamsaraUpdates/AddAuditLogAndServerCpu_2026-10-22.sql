-- AuditLog: a permanent record of who did what. Writers: OWSPublicAPI (logins, logouts, zone joins),
-- OWSManagement (every admin console edit, with the caller's IP), and zone servers through
-- OWSCharacterPersistence api/Audit/Add (staff/GM actions, player trades). Rows are never updated or deleted
-- by the services. Detail is a JSON object whose shape depends on Action.
--
-- MapInstances.ServerCpuPct: the zone server process's CPU over its last heartbeat interval, in percent of one
-- core (a server busy on two cores reads 200). NULL until a server that sends it reports.
--
-- Idempotent: safe to run more than once.
CREATE TABLE IF NOT EXISTS AuditLog
(
    AuditLogID   BIGSERIAL    NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
    CustomerGUID UUID         NOT NULL,
    CreateDate   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    Source       VARCHAR(32)  NOT NULL,
    Actor        VARCHAR(100) NOT NULL,
    Action       VARCHAR(64)  NOT NULL,
    Target       VARCHAR(200) NULL,
    Detail       TEXT         NULL,
    ClientIP     VARCHAR(64)  NULL
);

CREATE INDEX IF NOT EXISTS IX_AuditLog_CustomerGUID_CreateDate ON AuditLog (CustomerGUID, CreateDate DESC);

ALTER TABLE MapInstances ADD COLUMN IF NOT EXISTS ServerCpuPct REAL NULL;
