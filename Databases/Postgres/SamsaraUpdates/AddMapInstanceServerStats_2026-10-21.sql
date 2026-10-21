-- Zone server load, as each zone server reports it in its UpdateNumberOfPlayers heartbeat (every 10 s):
-- world-tick mean and worst in ms over the interval, memory used, uptime and the build changelist
-- (0 = an unstamped dev build). NULL until a server that sends them reports; OWSManagement's zone
-- instances grid reads them.
--
-- Idempotent: safe to run more than once.
ALTER TABLE MapInstances ADD COLUMN IF NOT EXISTS ServerFrameMsAvg REAL NULL;
ALTER TABLE MapInstances ADD COLUMN IF NOT EXISTS ServerFrameMsMax REAL NULL;
ALTER TABLE MapInstances ADD COLUMN IF NOT EXISTS MemoryUsedMB INT NULL;
ALTER TABLE MapInstances ADD COLUMN IF NOT EXISTS UptimeSeconds INT NULL;
ALTER TABLE MapInstances ADD COLUMN IF NOT EXISTS BuildCL INT NULL;
