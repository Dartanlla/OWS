#!/usr/bin/env bash
# Restore rehearsal: load a dump into a throwaway Postgres (same image, no network) and compare
# every table's row count with the live database. Run it right after a backup for matching counts.
# Usage: ows-pg-restore-check.sh [dump]   (default: the newest dump in OWS_BACKUP_DIR)
# Exit 1 if the restore fails or a live table is missing from the restore.
set -euo pipefail
export LC_ALL=C # sort and join must agree on order
if [ -f /etc/default/ows-pg-backup ]; then . /etc/default/ows-pg-backup; fi

CONTAINER="${OWS_DB_CONTAINER:-ows-hetzner-dev-database-1}"
DB="${OWS_DB_NAME:-openworldserver}"
DIR="${OWS_BACKUP_DIR:-/var/backups/ows}"

dump="${1:-$(ls -1t "$DIR"/openworldserver-*.dump 2>/dev/null | head -1)}"
[ -f "$dump" ] || { echo "No dump found (looked in $DIR)" >&2; exit 1; }

image="$(docker inspect -f '{{.Config.Image}}' "$CONTAINER")"
scratch="ows-restore-check-$$"
docker run -d --rm --name "$scratch" --network none -e POSTGRES_PASSWORD=restorecheck "$image" >/dev/null
trap 'docker rm -f "$scratch" >/dev/null 2>&1 || true' EXIT

# The image runs setup.sql on a temporary server first; wait for the real one.
for _ in $(seq 1 120); do
  if docker logs "$scratch" 2>&1 | grep -q 'PostgreSQL init process complete' \
     && docker exec "$scratch" pg_isready -U postgres -q; then
    break
  fi
  sleep 1
done
docker exec "$scratch" pg_isready -U postgres -q

docker exec "$scratch" createdb -U postgres restorecheck
docker exec -i "$scratch" pg_restore -U postgres --no-owner --no-privileges --exit-on-error -d restorecheck < "$dump"

count_sql="select string_agg(format('select %L, count(*) from %I.%I', table_schema || '.' || table_name, table_schema, table_name), ' union all ')
  from information_schema.tables where table_type = 'BASE TABLE' and table_schema not in ('pg_catalog', 'information_schema')"
counts() {
  local query
  query="$(docker exec "$1" psql -U postgres -d "$2" -Atc "$count_sql")"
  docker exec "$1" psql -U postgres -d "$2" -At -F ' ' -c "$query" | sort
}

echo "dump: $dump"
printf '%-50s %10s %10s\n' table live restored
join -a 1 -a 2 -e MISSING -o 0,1.2,2.2 <(counts "$CONTAINER" "$DB") <(counts "$scratch" restorecheck) \
  | awk '{ flag = ($2 != $3) ? "  <- differs" : ""; printf "%-50s %10s %10s%s\n", $1, $2, $3, flag; if ($3 == "MISSING") missing = 1 }
         END { exit missing }'
echo "restore check ok"
