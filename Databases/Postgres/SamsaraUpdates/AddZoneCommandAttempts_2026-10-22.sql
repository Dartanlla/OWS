-- ZoneCommands.DeliveryAttempt counts the deliveries of a command. A zone server's retry names the delivery it got, so a
-- retry report that arrives late, or twice, cannot release a later delivery of the same gift.
--
-- Idempotent: safe to run more than once.
ALTER TABLE ZoneCommands ADD COLUMN IF NOT EXISTS DeliveryAttempt INT NOT NULL DEFAULT 0;
