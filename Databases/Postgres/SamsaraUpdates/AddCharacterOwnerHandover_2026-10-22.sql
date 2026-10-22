-- Safe handover of a character that a live session still holds (CharacterOwners, AddCharacterOwners_2026-10-22.sql).
--
-- CheckOwners stamps LastConfirmedAt on every owner it reports current, and a zone server lets a character trade or
-- drop items only for 8 s after its last confirmation. A login claim that replaces an owner confirmed in the last
-- 8 s keeps that owner's token valid for saves until PreviousOwnerUntil (20 s later), and the claiming zone server
-- loads the character only after it. So whatever the old copy did before it stopped is saved before the new copy
-- loads. A second claim while a handover runs is refused.
--
-- A travel claim needs no handover when it replaces the owner whose zone server asked for its ticket
-- (JoinTickets.HandoffOwnerToken): travel froze that copy and saved it first. PlayerLogout with the current token
-- clears LastConfirmedAt, so a login right after a logout does not wait.
--
-- Idempotent: safe to run more than once.
ALTER TABLE CharacterOwners ADD COLUMN IF NOT EXISTS LastConfirmedAt TIMESTAMPTZ NULL;
ALTER TABLE CharacterOwners ADD COLUMN IF NOT EXISTS PreviousOwnerToken UUID NULL;
ALTER TABLE CharacterOwners ADD COLUMN IF NOT EXISTS PreviousOwnerUntil TIMESTAMPTZ NULL;
ALTER TABLE JoinTickets ADD COLUMN IF NOT EXISTS HandoffOwnerToken UUID NULL;
