-- CharacterOwners: one owning session per character. A zone server claims the character when a player joins
-- (OWSCharacterPersistence api/Characters/ClaimCharacter, service key) and gets a fresh OwnerToken; every save it
-- sends carries that token. Once a character has a row, a save with any other token is refused (stale_owner), so a
-- second login or a finished zone travel silences the old copy. A character with no row still takes token-less
-- saves (game builds from before the owner token). Logout keeps the row, so the owner's last saves still land.
--
-- ClaimKind: 1 = login (the old copy is told "logged in somewhere else"), 2 = zone travel (the old copy leaves
-- quietly).
--
-- JoinTickets gains ClaimedAt and CancelledAt: claim and cancel each update the ticket row in one statement, and
-- each requires the other to be unset, so exactly one of them wins.
--
-- Idempotent: safe to run more than once.
CREATE TABLE IF NOT EXISTS CharacterOwners
(
    CustomerGUID  UUID        NOT NULL,
    CharacterID   INT         NOT NULL,
    OwnerToken    UUID        NOT NULL,
    MapInstanceID INT         NOT NULL,
    ClaimKind     SMALLINT    NOT NULL,
    ClaimedAt     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT PK_CharacterOwners PRIMARY KEY (CustomerGUID, CharacterID),
    CONSTRAINT FK_CharacterOwners_Characters FOREIGN KEY (CustomerGUID, CharacterID)
        REFERENCES Characters (CustomerGUID, CharacterID) ON DELETE CASCADE,
    CONSTRAINT ck_characterowners_claimkind CHECK (ClaimKind BETWEEN 1 AND 2)
);

ALTER TABLE JoinTickets ADD COLUMN IF NOT EXISTS ClaimedAt TIMESTAMPTZ NULL;
ALTER TABLE JoinTickets ADD COLUMN IF NOT EXISTS CancelledAt TIMESTAMPTZ NULL;
