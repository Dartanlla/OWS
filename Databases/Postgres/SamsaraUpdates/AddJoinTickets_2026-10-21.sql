-- JoinTickets: the zone server learns who joins from the backend, never from the client. GetServerToConnectTo
-- issues a single-use ticket for a character the session's user owns; the zone server redeems it once, in
-- PreLoginAsync, through OWSCharacterPersistence api/Characters/RedeemJoinTicket (service key).
--
-- StartMode: 0 = the character's stored position (read at redeem time), 1 = the X..RZ on the ticket (portal and
-- staff travel, set only by a zone server), 2 = the destination map's default start.
-- RX, RY, RZ are roll, pitch and yaw, as in Characters.
--
-- Idempotent: safe to run more than once.
CREATE TABLE IF NOT EXISTS JoinTickets
(
    TicketGUID      UUID        NOT NULL CONSTRAINT PK_JoinTickets PRIMARY KEY,
    CustomerGUID    UUID        NOT NULL,
    UserSessionGUID UUID        NOT NULL,
    UserGUID        UUID        NOT NULL,
    CharacterID     INT         NOT NULL,
    CharacterName   VARCHAR(50) NOT NULL,
    MapInstanceID   INT         NOT NULL,
    StartMode       SMALLINT    NOT NULL DEFAULT 0,
    X               FLOAT       NOT NULL DEFAULT 0,
    Y               FLOAT       NOT NULL DEFAULT 0,
    Z               FLOAT       NOT NULL DEFAULT 0,
    RX              FLOAT       NOT NULL DEFAULT 0,
    RY              FLOAT       NOT NULL DEFAULT 0,
    RZ              FLOAT       NOT NULL DEFAULT 0,
    CreatedAt       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ExpiresAt       TIMESTAMPTZ NOT NULL,
    RedeemedAt      TIMESTAMPTZ NULL,
    CONSTRAINT ck_jointickets_startmode CHECK (StartMode BETWEEN 0 AND 2)
);

CREATE INDEX IF NOT EXISTS ix_jointickets_expiresat ON JoinTickets (ExpiresAt);
