#!/usr/bin/env bash
# Loads the hourly backup the staff console asks for into a staging database, for a character restore
# (docs/hosting/hetzner-dev.md, Restore a character). ows-staging.path runs it when the console writes
# /var/lib/ows-staging/request.json. The staging Postgres runs as container ows-staging-db on the compose network,
# with no published port, and is removed after 2 hours; `ows-stage-backup stop` removes it at once.
# Progress goes to /var/lib/ows-staging/status.json, which the console reads.
set -euo pipefail

dir=/var/lib/ows-staging
backups=/var/backups/ows
container=ows-staging-db
live=ows-hetzner-dev-database-1
network="${OWS_STAGING_NETWORK:-ows-hetzner-dev_default}"
# shellcheck disable=SC1091
. /etc/default/ows-staging

dump=""
stops_at=""

status() {
  local state="$1" error="${2:-}"
  printf '{"state":"%s","dump":"%s","at":"%s","stopsAt":"%s","error":"%s"}\n' \
    "$state" "$dump" "$(date -u +%FT%TZ)" "$stops_at" "$error" > "$dir/status.json.tmp"
  mv "$dir/status.json.tmp" "$dir/status.json"
}

remove_container() {
  docker rm -f "$container" >/dev/null 2>&1 || true
}

if [ "${1:-load}" = "stop" ]; then
  remove_container
  status stopped
  exit 0
fi

[ -f "$dir/request.json" ] || exit 0
dump="$(python3 -c 'import json, sys; print(json.load(open(sys.argv[1])).get("dump", ""))' "$dir/request.json" || true)"
rm -f "$dir/request.json"
if ! [[ "$dump" =~ ^openworldserver-[0-9A-Za-z-]+\.dump$ ]] || [ ! -f "$backups/$dump" ]; then
  status failed "unknown backup"
  exit 1
fi

trap 'status failed "the load failed; see $dir/restore.log"; remove_container' ERR
status loading
remove_container

image="$(docker inspect --format '{{.Config.Image}}' "$live")"
docker run -d --name "$container" --network "$network" --memory 384m \
  -e POSTGRES_PASSWORD="$OWS_STAGING_DB_PASSWORD" "$image" >/dev/null

# The image runs a temporary server while it initialises, so wait for its "init process complete" line first.
for _ in $(seq 1 120); do
  if docker logs "$container" 2>&1 | grep -q "PostgreSQL init process complete" \
    && docker exec "$container" pg_isready -U postgres -q; then
    break
  fi
  sleep 1
done

docker exec "$container" createdb -U postgres staging
docker exec -i "$container" pg_restore -U postgres --no-owner --no-privileges --exit-on-error -d staging \
  < "$backups/$dump" > "$dir/restore.log" 2>&1

systemctl stop ows-staging-expire.timer >/dev/null 2>&1 || true
systemctl reset-failed ows-staging-expire.timer ows-staging-expire.service >/dev/null 2>&1 || true
systemd-run --quiet --unit=ows-staging-expire --on-active=2h /usr/local/sbin/ows-stage-backup stop
stops_at="$(date -u -d '+2 hours' +%FT%TZ)"
status ready
