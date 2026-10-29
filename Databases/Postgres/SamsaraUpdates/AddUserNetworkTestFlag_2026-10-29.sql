-- Users.IsInternalNetworkTestUser: the account-level LAN test flag. A join hands 127.0.0.1 to a character when this or
-- the character's own Characters.IsInternalNetworkTestUser is set, so new characters of a flagged account need no flag.
--
-- Idempotent: safe to run more than once.
ALTER TABLE Users ADD COLUMN IF NOT EXISTS IsInternalNetworkTestUser BOOLEAN NOT NULL DEFAULT FALSE;
