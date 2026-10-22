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

## Admin Console

`owsmanagement` serves a REST API plus a Vue SPA for editing accounts and characters:
account role (`Users.Role`), and the per-character `IsAdmin` / `IsModerator` flags the game
client reads at login. No schema change is involved — every column it writes already exists.
Its Zone Instances page (`GET /api/Instances`) lists every `mapinstances` row with its region, status,
players, the characters on it, and the load its zone server last reported in the heartbeat (world-tick
ms average / worst, memory, uptime, build; `dev` is an unstamped build), and refreshes every 10 s.

It sits behind the `admin` compose profile, so a plain `up -d` never starts it. Two reasons:
the other services already reserve 3328 MB of a 4 GB host, and the console is occasional-use.

> **It has no login.** Its only credential is the `X-CustomerGUID` header, which
> `StoreCustomerGUIDMiddleware` checks for a parseable GUID and nothing more — and the game
> client already sends that same GUID on every public call. Anyone who can reach the port can
> edit every account in the customer. It is therefore bound to `127.0.0.1` with no
> `OWS_API_BIND` escape hatch. **Do not** add it to the Caddy proxy or move it off loopback
> until it has a real admin credential.

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
# open http://localhost:44410, Settings page first to set the CustomerGUID
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

Those four, plus SSH, are the allow list. After a compose or host change, this must print exactly
`22 44302 44306 44325 44364 50051`:

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
gives each zone instance its own file.

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

### Load dashboard

`scripts/hetzner/grafana/ows-load.json` is the "OWS load" dashboard for load tests and alpha sessions. Import it
once in Grafana (Dashboards, New, Import, upload the file; pick the stack's Prometheus and Loki data sources). It
shows players and logins, each zone server's heartbeat load, the box's CPU, memory and swap, per-service container
CPU and memory, Postgres connections (the server allows 40) and transactions, errors and warnings per service, and
health check times.

The zone and player panels read `ows_zone_*` (per instance: `players`, `frame_ms_avg`, `frame_ms_max`, `memory_mb`,
`uptime_seconds`, `heartbeat_age_seconds`, `status`; labels `zone`, `zone_instance`, `region`) and
`ows_online_characters_in_zones`, which a second Alloy postgres exporter (`ows_load`) reads from the game database
with the queries in `scripts/hetzner/alloy/ows-load-queries.yaml` (installed next to the config by
`install-alloy.sh`). The frame and memory columns stay empty until zone servers send the heartbeat stats. The
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
