# Hetzner Dev Hosting

This profile hosts a lean OWS backend for dev/testing while Unreal Engine runs locally.
It is an add-on deployment configuration only; it does not change the base OWS API code.

## What Runs

- OWSPublicAPI
- OWSCharacterPersistence
- OWSInstanceManagement
- OWSGlobalData
- OWSChat
- OWSParty
- Postgres
- RabbitMQ

- OWSManagement (admin console) — **opt-in**, see below

The profile intentionally omits ELK, OWSGuild, OWSActionHouse, and UE dedicated servers.

## Recommended Host

Use a 4 GB Hetzner cloud server for the first pass. Add a 2 GB swapfile on small hosts:

```bash
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

## Current Dev Server

SSH from Windows:

```powershell
ssh -F NUL -i $env:USERPROFILE\.ssh\id_ed25519_codex_20260509 root@87.99.150.89
```

The repo is checked out at:

```bash
cd /opt/owsfork/src
```

Check the running stack:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml ps
```

## Deploy

From `src/`:

```bash
cp .env.hetzner-dev.example .env.hetzner-dev
```

Edit `.env.hetzner-dev` and set a unique Postgres password. Keep
`DATABASE_CONNECTION_STRING` in sync with `DATABASE_PASSWORD`.

RabbitMQ uses the existing repo config at `.docker/rabbitmq/rabbitmq.conf`, which
defaults to `dev` / `test`. The broker ports are bound to `127.0.0.1` on the host
and are only used internally by OWS services in this profile.

Additional env vars for production hardening (set in `.env.hetzner-dev`):

- `OWS_SAMSARA_SERVICE_KEY` — server-to-server key for the PlayerShops endpoints.
  **Required outside Development**: the API fails closed (401) when unset. Set the
  same value in the environment of the machine running the UE dedicated servers
  (the UE zone server reads the same variable and sends it as `X-Samsara-Service-Key`).
- `OWS_PUBLIC_API_DOMAIN` / `OWS_GLOBAL_DATA_DOMAIN` — hostnames for the built-in
  Caddy TLS proxy (e.g. `api.example.com`). Point A records at this host before
  first start; Caddy provisions Let's Encrypt certificates automatically. When
  unset, the proxy boots with `.localhost` placeholders and internal certs.
- `OWS_MIGRATIONS_BASELINE` — see Database Migrations below.
- `OWS_DB_MAX_POOL_SIZE` / `OWS_DB_MIN_POOL_SIZE` — per-service Npgsql pool bounds. Six
  services share one Postgres (`max_connections=40`), so the default cap is 5 (6×5=30).
  Raise once Postgres has more connections or PgBouncer is in front.
- `UserSessionCacheValkeyPassword` / `OWS_SESSION_CACHE_ENABLED` — the Valkey session cache
  (loopback-bound `usersessioncache` service). OWSPublicAPI reads sessions through it with a
  short TTL and DB fallback; a Valkey outage degrades to Postgres via a circuit breaker, so
  auth never depends on the cache. Set `OWS_SESSION_CACHE_ENABLED=false` to bypass it.
- `OWS_GRPC_MAX_CONNECTIONS` / `OWS_GRPC_MSGS_PER_WINDOW` / `OWS_GRPC_RATE_WINDOW_SECONDS` —
  Chat/Party connection cap and per-connection message rate limit (always enforced).
- `OWS_GRPC_REQUIRE_SESSION_AUTH` — gRPC session validation. Keep `false` until the UE client
  sends `customerguid`/`usersessionguid` gRPC metadata; enabling it before then rejects all
  streams. See `docs/hosting/scaling-roadmap.md` (4.1).

Start the lean stack:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml up --build -d
```

Check status and logs:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml ps
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml logs -f owspublicapi
```

## Admin Console (Samsara Management)

`owsmanagement` serves Samsara Management, the staff console: a REST API plus a Vue SPA. Pages:

| Page | What it shows and does |
| --- | --- |
| Overview | Players online, zones up, today's logins and new characters, 24 h signals (failed saves, rejected requests, large juri changes, zone crashes, duplicate items), each zone server's heartbeat load and build; maintenance, rate event, player cap, players in line, open reports |
| Players | Search accounts and characters; account page (sanctions, notes, logins, role); character page (items with refine and cards, ledger of game events, chat, quests, trades and trade partners, reports, zone commands, stats, a Sentry search link, restore from backup) |
| Moderation | Player reports (with the chat around them), active bans and mutes, in-game staff commands, chat search |
| Economy | Juri held and its flows by source, item census, possible duplicates, player shop health, trade pairs and one-way trades, drop rates against the configured chance |
| Insight | New players by signup day (level 5 and 10, day 1 and day 7 return), where new characters stopped, quest funnel, outliers |
| World | Broadcast to every zone, message of the day, logins open or closed, scheduled maintenance, rate event, player cap, live map, region status, instances, zone config |
| Audit, System | `AuditLog` with filters; service health, newest backup, migrations |

**Sign-in.** Staff sign in through Steam (OpenID 2.0, checked with Steam's `check_authentication`). The Steam ID must
match `Users.SteamId` of an account whose `Users.Role` is Moderator, GameMaster or Admin; the role is re-read every
60 s. Actions follow the in-game command roles: Moderator reads everything, kicks, warns, mutes and adds notes;
GameMaster also gives items and juri, unsticks and broadcasts; Admin also bans, sets roles, closes logins, stops
instances and edits zones. Every write needs the `X-Samsara-Console` header and writes an `admin_change` audit row
with the staff member's name and Steam ID.

**Bans and mutes** live in `AccountSanctions`, on the account. PublicAPI refuses a banned login (Steam and
password) and `GetServerToConnectTo`; OWSChat drops a muted sender's messages (needs `OWS_GRPC_REQUIRE_SESSION_AUTH`).
The in-game `/ban`, `/mute` and `/warn` write the same table through CharacterPersistence `api/Sanctions`.

**Zone commands.** Kick, mute, warn, announce, unstuck and gifts go into `ZoneCommands`. Each zone server picks up
its own, and those for characters online on it, every 5 s through CharacterPersistence `api/ZoneSync` (service key),
which also takes the zone's game events into `GameEvents` and returns the message of the day. A gift is delivered at
most once: if its zone stops before reporting, the console marks it unconfirmed, and staff check the ledger before
giving it again.

**Switches** (`ConsoleSettings`): `motd`, `logins_open` (`false` refuses logins below Moderator, and registration) and
`region_status:<Region>`, which overrides that region's status on the login screen's server list.

**Reports.** Any player types `/report <name> <reason>` (or clicks a name in chat). The zone posts it to
CharacterPersistence `api/Reports/Add` with both positions; the backend copies the last 10 minutes of both players'
chat into `PlayerReports`. One report a minute and five an hour per player in game, 20 a day on the backend.

**Live operations** (`ConsoleSettings`, returned by every `ZoneSync`):
- `maintenance` `{StartsAt, Message}`: zones warn at 15, 5 and 1 minutes and kick players below Moderator at the
  start; from then `LoginGate` refuses logins and first entries below Moderator. "End maintenance" deletes it.
- `rate_event` `{Name, Exp, Drop, StartsAt, EndsAt}`: zones scale kill EXP and drop chances (1 to 5, drops capped at
  100%) and announce the start and end.
- `player_cap`: above 0, a first entry below Moderator waits in `LoginQueue` while the players in the world plus the
  live entries ahead reach the cap. The game asks again every 5 s; an entry not asked about for 30 s drops out; a
  character that left in the last 90 s skips the line. Zone changes are never held.

Zones also send their players' positions (`LivePositions`, the World page's map) and, every 5 minutes, loot roll
counts per mob (`loot_stats` events, the Economy page's drop check). Map images are the minimap bakes, exported from
the editor into `src/OWSManagement/wwwroot/public/maps/` with `maps.json`; a map without one shows a grid.

**Staff alerts.** CharacterPersistence checks every 60 s and posts to `OWS_STAFF_DISCORD_WEBHOOK_URL` (unset = off):
new reports, bans and unbans, large juri gifts and any item gift, new duplicate items, unexpected zone exits, more
than 5 failed saves in 10 minutes, and maintenance and rate events starting and ending. One message per check;
cursors in `ConsoleSettings` (`alerts_cursor:<kind>`) keep a restart from posting again. The box uses the ops webhook
of the backup scripts; copy it without printing it, then recreate `owscharacterpersistence`:

```bash
. /etc/default/ows-pg-backup && grep -q '^OWS_STAFF_DISCORD_WEBHOOK_URL=' src/.env.hetzner-dev \
  || printf '\nOWS_STAFF_DISCORD_WEBHOOK_URL=%s\n' "$OWS_BACKUP_DISCORD_WEBHOOK_URL" >> src/.env.hetzner-dev
```

**Restore a character** (Admin, character page). The console writes `/var/lib/ows-staging/request.json`;
`ows-staging.path` runs `ows-stage-backup`, which loads that hourly dump into the container `ows-staging-db`
(compose network, no published port, 384 MB) and removes it after 2 hours. The console then shows what differs and,
with the character offline and its name typed, copies its rows from staging in one transaction. Install once:
`scripts/hetzner/install-staging.sh`, then `admin-console.sh rebuild`.

**Retention**: `PurgeSamsaraLogs()` (run by `ows-pie-purge.timer`) keeps game events 180 days, chat 30 days and
finished zone commands 30 days, and clears login queue entries and live positions older than a day.

It sits behind the `admin` compose profile, so a plain `up -d` never starts it: the other services already reserve
3328 MB of a 4 GB host. It binds `127.0.0.1`; staff reach it through the SSH tunnel, or over HTTPS at
`https://87-99-150-89.sslip.io` while the Caddy proxy runs (`OWS_CONSOLE_DOMAIN=87-99-150-89.sslip.io` in
`.env.hetzner-dev`, then `$C --profile tls up -d --no-deps proxy`). Caddy gets a Let's Encrypt certificate for the
name and passes `X-Forwarded-Proto`, so the Steam return URL is the HTTPS one. Optional `.env.hetzner-dev` keys: `OWS_CUSTOMER_GUID` (else the only `Customers` row),
`OWS_GRAFANA_URL` and `OWS_SENTRY_URL` (links on the System page). `admin-console.sh` passes the checkout's commit
as `OWS_GIT_COMMIT`, the version the System page shows.

From your Windows PC — starts it, tunnels, opens the browser, and stops it again on Ctrl+C.
Double-click one of these in `scripts/hetzner/`:

```text
Update-AdminConsole.cmd     first run, and after pushing any console change
Start-AdminConsole.cmd      every time after that
```

Both wrap `Start-AdminConsole.ps1` and pass extra arguments through
(`-KeepRunning`, `-Port`, `-Server`, `-User`, `-KeyPath`, `-NoBrowser`).

Or drive it from the box directly:

```bash
./scripts/hetzner/admin-console.sh up       # start (never touches the database container)
./scripts/hetzner/admin-console.sh rebuild  # rebuild from current source, then start
./scripts/hetzner/admin-console.sh status
./scripts/hetzner/admin-console.sh down
```

Then tunnel by hand if you are not using the PowerShell helper:

```bash
ssh -L 44410:127.0.0.1:44410 root@SERVER_IP
# open http://localhost:44410 and sign in through Steam
```

Optional `.env.hetzner-dev` keys: `OWS_MANAGEMENT_HOST_PORT` (default 44410) and
`OWS_MANAGEMENT_MEM_LIMIT` (default 256m).

## Database Migrations

Schema scripts are applied automatically by the `migrations` service (DbUp) before the
API services start. It runs the scripts listed in `Databases/Postgres/migration-manifest.txt`
top to bottom, journaling each in the `schemaversions` table so every script runs exactly
once. Append new scripts to the bottom of the manifest; never reorder applied entries.

**One-time adoption on a database that already has the schema** (i.e. the current dev
server): the migrator refuses to run against an existing schema with no journal. Baseline
it once, marking every manifest script as applied without executing anything:

```bash
OWS_MIGRATIONS_BASELINE=true docker compose --env-file .env.hetzner-dev \
  -f docker-compose.hetzner-dev.yml run --rm migrations
```

After that, plain `up` runs pending migrations automatically. To apply migrations
manually without starting anything else:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml run --rm migrations
```

## Updating The Server

The server should usually follow `origin/main`:

```bash
cd /opt/owsfork/src
git fetch origin
git pull --ff-only origin main
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml up --build -d
```

Keep `.env.hetzner-dev` local to the server. Do not commit it.

Region routing (`Users.PreferredServerRegion` / `WorldServers.ServerRegion`) touches OWSData SQL
and adds two columns, so it deploys as schema-first, then only the two services that execute the
changed code:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml run --rm migrations
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml up -d --no-deps --build owspublicapi owsinstancemanagement
```

Zone server load (`AddMapInstanceServerStats`: five nullable `mapinstances` columns that
`UpdateNumberOfPlayersSQL` writes) deploys the same way, with the console rebuilt if it is in use:
migrations, then `up -d --no-deps --build owspublicapi owsinstancemanagement`, then
`./scripts/hetzner/admin-console.sh rebuild`. Zone servers from before it send no stats (the columns
stay NULL), and a launcher from before it never reports exits.

The `WorldServers.ServerRegion` column default covers the window between the two commands, in
which an `owsinstancemanagement` image predating the change can still register a launcher. The
`OWS_SERVER_SEA_*` keys below control the second entry of the login screen's server list; editing
them needs `up -d --no-deps --force-recreate owspublicapi` (no rebuild). See
`docs/hosting/thailand-demo.md`.

> **CAUTION — `--build` also recreates the `database` container.** The `database`
> service has a `build:` context, so `up --build` rebuilds and **recreates** it, which
> severs every service's live Postgres connection pool. On reconnect, any service still
> holding a **stale DB password** (e.g. `.env.hetzner-dev` was rotated but that container
> was never recreated) fails with `Npgsql 28P01: password authentication failed for user
> "postgres"` — which surfaces to players as **"login service unavailable"** from
> `owspublicapi`. This is a latent trap: a long-running container keeps working on its old
> pooled credentials until something forces a reconnect.
>
> Safer patterns:
> - **App-code change only (no DB image change):** rebuild just that service and don't touch
>   the DB — `... up -d --no-deps --build <service>`.
> - **After any DB restart/recreate:** force every app service to reconnect with the current
>   env — `... up -d --no-deps --force-recreate owspublicapi owscharacterpersistence owsinstancemanagement owsglobaldata owschat owsparty`.
> - **Verify no drift first:** run `scripts/hetzner/check-db-password-drift.sh` (compares each
>   running container's DB password to `.env.hetzner-dev`). A green `docker compose exec
>   database psql -U postgres` does NOT prove network creds are valid — that path uses local
>   trust auth and never checks the password.

## Troubleshooting

### "Login service unavailable" / `owspublicapi` returns HTTP 500

Check its logs for a Postgres auth error:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml logs --tail=40 owspublicapi | grep -i 28P01
```

If present, a container is on a stale DB password (see the CAUTION above). Fix by recreating
it with the current env (DB left alone):

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml up -d --no-deps --force-recreate owspublicapi
```

Then confirm: a request to `/api/Users/GetServerToConnectTo` returns 400 (business error), not
500 (DB error), and the logs show a clean start with no `28P01`.

### Idle zone servers never shut down / port 7778 reused

Zone (dedicated) server processes run on the **dev PC**, not this box; the launcher decides to
shut them down from the `mapinstances` rows in this DB. Symptoms of a leak: `mapinstances` is
empty (or has stale rows) while `SamsaraSagaServer.exe` processes keep running, and every new
instance is assigned the base port (`StartingInstancePort`, 7778) because the port is derived
from existing rows — so fresh servers collide on 7778 with a lingering one and die.

Inspect from this box:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml exec -T database \
  psql -U postgres -d openworldserver -c \
  "SELECT mapinstanceid, mapid, port, status, numberofreportedplayers, lastserveremptydate, lastupdatefromserver FROM mapinstances ORDER BY mapinstanceid;"
```

On the dev PC, list/kill orphaned processes (verify no player connections first — an idle
server's only established connection is outbound to the Party gRPC service on `:44364`):

```powershell
Get-CimInstance Win32_Process -Filter "Name='SamsaraSagaServer.exe'" | Select ProcessId, CommandLine
# after confirming zero inbound player connections:
Stop-Process -Id <pid> -Force
```

## Public Endpoints

There are two exposure modes, controlled by `OWS_API_BIND` and the `tls` compose profile.

**Current mode — direct IP access (no TLS).** `OWS_API_BIND=0.0.0.0`, deployed without the
`tls` profile (Caddy does not start). The client-facing APIs are served directly:

- Public API: `http://SERVER_IP:44302` (still rate-limited per client IP; `/api/Users` tighter)
- Global Data API: `http://SERVER_IP:44325`
- Party REST/gRPC: `http://SERVER_IP:44306` / `SERVER_IP:44364`
- Chat gRPC: `SERVER_IP:50051`

Character Persistence and Instance Management stay bound to `127.0.0.1` (SSH-tunnel only), as
before. Postgres, RabbitMQ, and Valkey are loopback-only.

Those four, plus SSH, are the allow list, plus 80 and 443 while the Caddy proxy serves the console. After a compose
or host change, this must print exactly `22 44302 44306 44325 44364 50051` (`22 80 443 ...` with the proxy):

```bash
ss -tlnH | awk '$4 !~ /^(127\.|\[::1\])/ {sub(/.*:/, "", $4); print $4}' | sort -un | tr '\n' ' '
```

Chat and Party gRPC read the caller's session from call metadata (`customerguid`, `usersessionguid`).
With `OWS_GRPC_REQUIRE_SESSION_AUTH=true` in `.env.hetzner-dev`, a stream must name a live session and
speaks as that session's selected character; a game client that sends no session loses chat. Turn it on
only when the live Steam build sends the session (the client keeps it on the game instance from login),
then rebuild and recreate both services:

```bash
cd /opt/owsfork/src
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml up -d --no-deps --build owschat owsparty
```

> **TODO — move to HTTPS via domains.** This box currently serves plaintext over IP because no
> domains/DNS are set up. To switch: set `OWS_PUBLIC_API_DOMAIN`/`OWS_GLOBAL_DATA_DOMAIN`, point
> their A-records at this host, set `OWS_API_BIND=127.0.0.1`, and deploy with `--profile tls`.
> Caddy (`src/.docker/caddy/Caddyfile`) then terminates Let's Encrypt TLS in front of the APIs
> and the UE client/launcher config moves from `http://SERVER_IP:port` to the `https://` domains.
> Chat gRPC (`:50051`) is direct in both modes; fronting it with TLS is a further follow-up.

## Logging

This profile does not run ELK. The OWS API services write one JSON object per line to stdout
(Serilog `RenderedCompactJsonFormatter`): `@t` is the time, `@m` the message, `@l` the level, and
`Application` names the service. `@l` is absent on Information events, so treat a missing `@l` as
Information. Only the one-shot `migrations` job still writes plain text. Docker rotates
each container's log at 10 MB and keeps 5 files (`x-logging` in the compose file).

Event lines to filter on (the first word of `@m`; the named values are also top-level JSON fields):

- `owspublicapi`: `user_login` (`Method` steam or password, `Result` ok / refused / steam_rejected / banned /
  unavailable, `SteamId`, `PersonaName`, `UserSessionGuid`; no email), `user_logout` (`UserSessionGuid`), and
  `zone_join` (`CharacterName`, `ZoneName`, `MapInstanceId`, `Port`) for every successful
  `GetServerToConnectTo`, so a login and each later zone travel. In Grafana:
  `{service="owspublicapi"} |= "user_login" | json`.
- `owsinstancemanagement`: `zone_process_exited` (`ZoneInstanceId`, `MapName`, `Port`, `ExitCode`, `Expected`),
  reported by the launcher when a zone server process it started exits. `Expected` is true when the
  instance's row was already shutting down (status 3) or gone, as every deliberate stop does first; an
  unexpected exit (a crash, or a kill by hand) logs at Warning.

Use Docker logs for hosted dev:

```bash
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml logs -f
```

Zone server logs: the launcher replaces `{zoneinstanceid}` and `{port}` in
`OWSInstanceLauncherOptions.OtherCustomFlags`, so `-AbsLog=<dir>/UEServer-{zoneinstanceid}-{port}.log`
gives each zone instance its own file. Alloy on the zone host ships part of each file (Zone hosts, below).

### Audit log

`AuditLog` in the game database is the permanent record of who did what; Loki keeps logs about 14 days. The
services only add rows. Columns: `CreateDate`, `Source`, `Actor`, `Action`, `Target`, `Detail` (a JSON object
whose shape depends on `Action`), and `ClientIP`, which only the admin console fills: no player IP is stored.

| Source | Action | Actor | Target | Detail |
| --- | --- | --- | --- | --- |
| `publicapi` | `user_login` | Steam persona name (else Steam id), or the email for a password login | `steam:<id>` or `password` | `method`, `result` (as the `user_login` log line), `userSessionGuid` |
| `publicapi` | `user_logout` | the account's first name | the selected character | `userSessionGuid` |
| `publicapi` | `zone_join` | character | zone | `mapInstanceId`, `port` |
| `management` | `console_login` | `Name (steam:<id>)`, or `steam:<id>` when refused | `ok` or `refused` | `role`, or `reason` (`no_account`, `not_staff`) |
| `management` | `admin_change` | `Name (steam:<id>)` of the signed-in staff member | `METHOD /api/path` | the call's arguments without any password field, and `outcome` |
| `instancemanagement` | `zone_exit` | `launcher` | map name | `zoneInstanceId`, `port`, `exitCode`. Unexpected exits only (a crash, not a shutdown the launcher asked for) |
| `zone` | `staff_action` | staff character | action id | `role`, `source`, `scope`, `result`, `detail`, `raw` (the typed command), `userSession`. Staff callers only; a player's attempt stays in the zone log |
| `zone` | `trade_completed` | the trade's first player | the second player | `actorGave` / `targetGave` (item tag, quantity, refine, instanceId) and `actorJuri` / `targetJuri` |

Zone servers write through `OWSCharacterPersistence` `POST api/Audit/Add` with the service key. A write that fails
is logged as `audit_write_failed` (zone log, or the service's log) and is not retried. Read it on the console's
Audit Log page, or in SQL:

```sql
SELECT CreateDate, Source, Actor, Action, Target, Detail FROM AuditLog ORDER BY AuditLogID DESC LIMIT 50;
```

## Monitoring (Grafana Cloud)

Grafana Alloy runs on the host as a systemd service (not in compose) and ships every container's
logs to Loki (labels `host`, `service`, and `level` on OWS services) plus host, container (cAdvisor)
and Postgres metrics. The config is `scripts/hetzner/alloy/api-box.alloy`. Install or update, as root:

```bash
bash /opt/owsfork/scripts/hetzner/install-alloy.sh api-box.alloy
```

The script installs Alloy from `apt.grafana.com` and refuses to start until `/etc/default/alloy` sets
every `sys.env(...)` name the config reads: `GRAFANA_CLOUD_TOKEN` (an access policy token with
logs:write and metrics:write), `GRAFANA_CLOUD_LOKI_URL`/`_USER`, `GRAFANA_CLOUD_PROM_URL`/`_USER`,
`ALLOY_INSTANCE`, and `POSTGRES_MONITOR_DSN`. The DSN uses a `pg_monitor` role, created once:

```sql
CREATE ROLE alloy_monitor LOGIN PASSWORD '<random>'; GRANT pg_monitor TO alloy_monitor;
```

Health: `curl -s localhost:12345/api/v0/web/components` lists each component's state, and
`journalctl -u alloy` shows push errors. The metric job names match the Grafana Cloud Linux Server,
Docker and PostgreSQL integrations, so their prebuilt dashboards work without edits.

SSH logins and sudo use come from the systemd journal as `{job="auth"}`, labelled `identifier` (`sshd` or `sudo`).
Of sshd's lines only accepted logins (with the key fingerprint and source IP) and session opens and closes are kept;
the scanners' failed attempts are dropped. In Grafana: `{job="auth"} |= "Accepted"`.

### Zone hosts

A machine that runs zone servers through the instance launcher runs Alloy too: `scripts/hetzner/alloy/zone-logs.alloy`
ships the zone server logs as `{job="zone_server"}` (labels `host`, `zone_instance`, `level`), keeping only game
events (`SSEVENT`), `StaffActionAudit` lines, warnings, errors and fatals, at most 20 lines/s per host. The host config
adds the machine's CPU, memory, disk and network, and per-process CPU and memory of the zone servers
(`SamsaraSagaServer*`):

- Windows (Julian's PC, today): `zone-host-windows.alloy`, job `integrations/windows_exporter`, per-process
  `windows_process_*`. From an elevated PowerShell in the repo:
  `powershell -ExecutionPolicy Bypass -File scripts\hetzner\Install-ZoneHostAlloy.ps1` (`-LogDir`, default
  `C:/OWS/Logs`; `-Instance`, default the computer name). It installs Alloy if needed, copies the Grafana Cloud
  settings from this box's `/etc/default/alloy` over SSH into the service environment without showing them, installs
  both files and restarts the `Alloy` service. Run it again after a config change. The token is then in
  `HKLM\SOFTWARE\GrafanaLabs\Alloy` (`Environment`), which every local user can read.
- Linux (a regional zone server): `zone-host-linux.alloy`, job `integrations/node_exporter`, per-process
  `namedprocess_namegroup_*{groupname="zone_server"}`. As root: `bash scripts/hetzner/install-alloy.sh
  zone-host-linux.alloy`, after `/etc/default/alloy` sets the `GRAFANA_CLOUD_*` values, `ALLOY_INSTANCE` and
  `OWS_ZONE_LOG_DIR`. `install-alloy.sh` also installs the modules a config imports from `/etc/alloy`. The `alloy`
  user must be able to read the log folder (or run Alloy as root, as on this box).

Grafana alerts in folder `OWS zone servers`, rule group `zone-servers` (every 60 s), post to Discord
`#dev-observation` through the same contact point as the service health alert. They live only in Grafana and are
editable there:

| Alert | Fires when | Source |
| --- | --- | --- |
| Zone server CPU high | a zone server process stays above 80 % of one core for 5 minutes | `windows_process_cpu_time_total` (Windows zone hosts) |
| Zone server memory high | a zone server process's private working set stays above 3 GB for 5 minutes | `windows_process_working_set_private_bytes` |
| Zone frame time over budget | a zone's average world tick stays above 33 ms for 5 minutes | `ows_zone_frame_ms_avg` (heartbeat) |
| Zone server exited unexpectedly | the launcher reports an exit that was not a deliberate stop | `zone_process_exited` at Warning in `owsinstancemanagement` |

The CPU and memory alerts read Windows metrics only. On a Linux zone host the process exporter groups every zone
server into one `zone_server` series, so extend them (per-process grouping or the heartbeat's `ows_zone_cpu_pct` and
`ows_zone_memory_mb`) when that host gets Alloy.

### Service health

Every app service answers `GET /health` with `Healthy` (200) or `Unhealthy` (503). The check logs in to
Postgres fresh with the service's own connection string, so a stale password fails even while pooled
connections still work; InstanceManagement also opens a RabbitMQ connection. Results are cached for 10 s,
so the public `/health` cannot flood the database. `docker ps` shows each container `(healthy)` or
`(unhealthy)`. Chat serves `/health` on loopback port `44351`, because cleartext 50051 is HTTP/2 only.

Alloy probes each `/health` every 15 s (`probe_success{service="..."}`). The Grafana alert
`OWS service unhealthy` fires when a service's `probe_success` stays 0 for 1 minute, or when the probes
stop arriving (the box or Alloy is down), and posts to Discord `#dev-observation`. It lives only in Grafana: folder
`OWS API box`, rule group `ows-health` (evaluated every 30 s), contact point `discord-dev-observation` (group wait
10 s). Tested 2026-10-21: a wrong DB password in `owsmanagement` alerted after 89 s and reached Discord at 120 s
(before the group wait went from 30 s to 10 s).

### Refused logins and API box host alerts

Health checks stay green while chat or party refuses every stream, as in the 2026-10-18 chat outage. With
`OWS_GRPC_REQUIRE_SESSION_AUTH=true`, each refused stream logs `grpc_session_rejected {Service} {Reason}` at Warning:
`no_credentials` means the client sent no session (a client bug); `invalid_session` means the session is unknown,
expired, or bound to another character. The log line has no session GUID and no IP.

More alerts in folder `OWS API box`, same contact point. They live only in Grafana:

| Alert | Fires when | Source |
| --- | --- | --- |
| Chat or party refusing logins | more than 10 refusals for one service and reason in 10 minutes (group `ows-health`) | `grpc_session_rejected` in `owschat` / `owsparty` |
| API box CPU high | CPU stays above 85 % for 10 minutes (group `api-box-host`) | `node_cpu_seconds_total` |
| API box memory low | memory stays above 90 % used for 5 minutes | `node_memory_MemAvailable_bytes` |
| API box disk filling | the root disk stays above 85 % full for 10 minutes | `node_filesystem_avail_bytes` |

A refused game client stops retrying (game CL 1468); an older client retries every second, so one player of an old
build fires the refusal alert. Tested 2026-10-25: 12 unauthenticated chat streams from the box fired it. The disk was
72 % full that day, mostly docker build cache (`docker system df`: about 40 GB reclaimable with `docker builder prune`).

### Load dashboard

`scripts/hetzner/grafana/ows-load.json` is the "OWS load" dashboard for load tests and alpha sessions. Import it
once in Grafana (Dashboards, New, Import, upload the file; pick the stack's Prometheus and Loki data sources). It
shows players and logins, each zone server's heartbeat load and CPU, every host's CPU, zone log warnings and errors,
the box's CPU, memory and swap, per-service container CPU and memory, Postgres connections (the server allows 40) and
transactions, errors and warnings per service, and health check times.

The zone and player panels read `ows_zone_*` (per instance: `players`, `frame_ms_avg`, `frame_ms_max`, `cpu_pct`,
`memory_mb`, `uptime_seconds`, `heartbeat_age_seconds`, `status`; labels `zone`, `zone_instance`, `region`) and
`ows_online_characters_in_zones`, which a second Alloy postgres exporter (`ows_load`) reads from the game database
with the queries in `scripts/hetzner/alloy/ows-load-queries.yaml` (installed next to the config by
`install-alloy.sh`). A column stays empty until zone servers built with that heartbeat field report (`cpu_pct`, in
percent of one core, came after the others). The
monitor role needs read access to those columns only, granted once, as `postgres`, in `openworldserver`:

```sql
GRANT SELECT ON mapinstances TO alloy_monitor;
GRANT SELECT (mapid, customerguid, zonename) ON maps TO alloy_monitor;
GRANT SELECT (worldserverid, customerguid, serverregion) ON worldservers TO alloy_monitor;
GRANT SELECT (mapinstanceid) ON charonmapinstance TO alloy_monitor;
```

## Persistence And Backups

Postgres and RabbitMQ data are stored in named Docker volumes:

- `ows-hetzner-dev-postgres-data`
- `ows-hetzner-dev-rabbitmq-data`

### Scheduled backups

`ows-pg-backup.timer` runs `/usr/local/sbin/ows-pg-backup` hourly: a `pg_dump -Fc` of `openworldserver` into
`/var/backups/ows/openworldserver-<UTC time>.dump`, checked with `pg_restore --list`, kept 14 days, and mirrored
with `rsync --delete` to a Hetzner Storage Box. A failed run posts to the Discord webhook in the settings.

Install or update (from `/opt/owsfork`, as root):

```bash
bash scripts/hetzner/install-pg-backup.sh
```

The first run writes `/etc/default/ows-pg-backup`, prints the backup SSH public key, and stops. Add that key to
the Storage Box from this host (SSH support on; it asks for the Storage Box password, and works with External
Reachability off): `cat /root/.ssh/ows_backup_ed25519.pub | ssh -p 23 u123456@u123456.your-storagebox.de
install-ssh-key`. Set `OWS_BACKUP_REMOTE` to `u123456@u123456.your-storagebox.de:ows-backups` and
`OWS_BACKUP_DISCORD_WEBHOOK_URL`, then run the installer again. It enables the timer and takes the first
backup (rsync creates the remote folder). `OWS_BACKUP_REMOTE=none` keeps dumps on this host only. Turn on the Storage Box's automatic
snapshots too: `rsync --delete` mirrors this host, so a deletion here reaches the box on the next run.

By hand, before and after a test session or a destructive host change:

```bash
ows-pg-backup                               # one backup now
journalctl -u ows-pg-backup -n 20           # last runs
systemctl list-timers ows-pg-backup.timer   # next run
```

### Restore rehearsal

`ows-pg-restore-check [dump]` restores a dump (default: the newest) into a throwaway container of the same
image with no network, and prints every table's row count next to the live count. Run it right after a backup,
when the counts should match; it fails if the restore fails or a table is missing:
`ows-pg-backup && ows-pg-restore-check`.

### Restoring for real

Take a fresh `ows-pg-backup` first, so the restore itself can be undone. Then stop the app services so nothing
writes, restore into the live database, and start them again:

```bash
cd /opt/owsfork/src
DC="docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml"
APPS="owspublicapi owscharacterpersistence owsinstancemanagement owsglobaldata owschat owsparty"
$DC stop $APPS
docker exec -i ows-hetzner-dev-database-1 pg_restore -U postgres --clean --if-exists --no-owner \
  -d openworldserver < /var/backups/ows/openworldserver-<time>.dump
$DC start $APPS
```

A dump on the Storage Box comes back with
`rsync -e 'ssh -p 23 -i /root/.ssh/ows_backup_ed25519' <remote>/<file> /var/backups/ows/`.
Point-in-time recovery (WAL-G or pgBackRest) is not set up.

### Log archive

Raw zone server logs, with their `SSEVENT` event lines, are kept 90 days (Loki keeps about 14). Each zone host
uploads one zip per UTC day to `/var/backups/ows-logs/<host>/zone-logs-<host>-<yyyymmdd>.zip`; a log belongs to the
day of its last write, so a day is uploaded once it is over. `ows-log-archive.timer` (daily, 18:00 UTC) runs
`/usr/local/sbin/ows-log-archive`: it deletes zips whose day is more than 90 days old and mirrors the folder with
`rsync --delete` to the backups' Storage Box, folder `ows-logs` (`OWS_LOG_ARCHIVE_REMOTE` overrides it). A failed run
posts to the backup webhook. `install-pg-backup.sh` installs it with the backup.

- Windows zone host (Julian's PC): `scripts\hetzner\Archive-ZoneLogs.ps1` zips every `*.log` in `C:\OWS\Logs` (zone
  servers, their `-backup-` copies, the launcher's daily log) by day, skips days already on the box, and uploads with
  the same SSH key as the admin console. `-Register` adds the daily task `OWS zone log archive` (10:00, or at the next
  logon when the PC was off). Local logs are not deleted.
- Linux zone host: not written yet; it needs the same upload from the deploy bundle (a daily timer with `zip` and
  `scp`, or `rsync` of finished days).

Read an archived day on the box (no `unzip` there): `python3 -m zipfile -l <zip>` lists it, `python3 -m zipfile -e <zip>
<dir>` extracts it. From the Storage Box, fetch it with the same `rsync -e 'ssh -p 23 -i /root/.ssh/ows_backup_ed25519'`
as a dump, from `<storage box>:ows-logs/<host>/`.

### PIE account purge

The editor's PIE character provisioning registers a new account for each PIE client on every run
(`pie_player_<run>_<n>@samsara.local`, first name `PIE`) and removes only its character at the end. The
`PurgePIEAccounts(age)` function (migration `PIEAccountPurge_2026-10-22.sql`) deletes accounts with an
`@samsara.local` email and first name `PIE` older than `age`, with their characters and everything under them, the
shops they own, their sessions, queue entries, join tickets and audit rows. It also deletes stats, abilities, quests,
party and group memberships and inventory items of characters that no longer exist, and parties with no members. One
transaction; real accounts (Steam, other emails) are never matched, whatever their character names.
`ows-pie-purge.timer` (daily, 08:00 UTC) runs `/usr/local/sbin/ows-pie-purge` with `OWS_PIE_PURGE_AGE` (default
`6 hours`, so a PIE session running at the time keeps its account) and logs what it removed; a failed run posts to the
backup webhook. `install-pg-backup.sh` installs it. By hand: `ows-pg-backup && ows-pie-purge`.

## Smoke Tests

```bash
curl https://$OWS_PUBLIC_API_DOMAIN/api/system/status
curl https://$OWS_GLOBAL_DATA_DOMAIN/api/system/status
# or from the box itself, bypassing the proxy:
curl http://127.0.0.1:44302/api/system/status
curl http://127.0.0.1:44325/api/system/status
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml logs --tail=100 owschat
docker compose --env-file .env.hetzner-dev -f docker-compose.hetzner-dev.yml logs --tail=100 owsparty
```

Then test account creation, login, character persistence, chat connection, and party registration/invite flow from your local UE client.

## Local UE Instance Launcher

If UE dedicated server instances run on your PC, run OWSInstanceLauncher on your
PC too. Keep RabbitMQ and internal APIs private by using an SSH tunnel:

```bash
ssh -N \
  -L 56720:127.0.0.1:5672 \
  -L 18028:127.0.0.1:44328 \
  -L 18023:127.0.0.1:44323 \
  -L 18025:127.0.0.1:44325 \
  -L 18002:127.0.0.1:44302 \
  -L 18006:127.0.0.1:44306 \
  -L 18064:127.0.0.1:44364 \
  root@SERVER_IP
```

Party (REST 44306, gRPC 44364), Global Data (44325), and Public API (44302) are now
loopback-only on the host, so server-side callers must use the tunnel (or the public
HTTPS domains for Public API / Global Data). Zone servers hold an outbound connection
to Party gRPC — update their config to the tunneled port.

Point the local Instance Launcher at:

- RabbitMQ host: `127.0.0.1`
- RabbitMQ port: `56720`
- Instance Management URL: `http://127.0.0.1:18028/`
- Character Persistence URL: `http://127.0.0.1:18023/`
- Global Data URL: `http://127.0.0.1:18025/`
- Public API URL: `http://127.0.0.1:18002/`
- Party REST URL: `http://127.0.0.1:18006/`
- Party gRPC: `127.0.0.1:18064`

For UE config while using the tunnel, use the same internal URLs for
Instance Management and Character Persistence.

On Windows, `scripts/hetzner/Start-OWSTunnelTray.cmd` starts a tray helper that
keeps the tunnel alive and lets you copy the UE and Instance Launcher settings.
