## What changed

<!-- The change in a few bullets. Name the services touched (PublicAPI, CharacterPersistence, InstanceManagement, Management, Chat, Party, Launcher, ...). -->

## Why

<!-- The problem or goal. Link the issue, Sentry event, alert or Discord thread if there is one. -->

## How tested

<!-- What you ran and what you saw: unit tests, a local compose stack, a client login, a zone spin-up, ... Say what you did NOT test. -->

- [ ] `dotnet build src/OWS.sln -c Release` and `dotnet test src/OWS.sln -c Release` pass locally
- [ ] CI is green

## Deploy / migration notes

<!-- Delete what doesn't apply. -->

- Database migration: none / `Databases/Postgres/...` (run before or after the service rollout?)
- Config or `.env.hetzner-dev` changes: none / ...
- Services to rebuild or restart: none / ...
- Unreal client or server (OWSPlugin) change needed at the same time: no / yes, ...
- Rollback: revert the PR and redeploy / needs a down migration / ...

## Risk

<!-- Low / Medium / High, and why. What breaks for players if this is wrong, and how would we notice (logs, alerts, Grafana)? -->
