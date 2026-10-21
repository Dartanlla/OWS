#!/usr/bin/env bash
# Dump the OWS database, keep 14 days of dumps on this host, and mirror them to off-box storage.
# Run by ows-pg-backup.timer (hourly); run it by hand before and after a test session.
# Settings: /etc/default/ows-pg-backup (see install-pg-backup.sh).
set -euo pipefail
if [ -f /etc/default/ows-pg-backup ]; then . /etc/default/ows-pg-backup; fi

CONTAINER="${OWS_DB_CONTAINER:-ows-hetzner-dev-database-1}"
DB="${OWS_DB_NAME:-openworldserver}"
DIR="${OWS_BACKUP_DIR:-/var/backups/ows}"
KEEP_DAYS="${OWS_BACKUP_KEEP_DAYS:-14}"
REMOTE="${OWS_BACKUP_REMOTE:-}"
SSH_KEY="${OWS_BACKUP_SSH_KEY:-/root/.ssh/ows_backup_ed25519}"
SSH_PORT="${OWS_BACKUP_SSH_PORT:-23}"
WEBHOOK="${OWS_BACKUP_DISCORD_WEBHOOK_URL:-}"

notify_failure() {
  local line="$1"
  echo "backup FAILED at line $line" >&2
  if [ -n "$WEBHOOK" ]; then
    curl -fsS -m 10 -H 'Content-Type: application/json' \
      -d "{\"content\":\"OWS database backup failed on $(hostname) (line $line). Check: journalctl -u ows-pg-backup\"}" \
      "$WEBHOOK" >/dev/null || true
  fi
}
trap 'notify_failure $LINENO' ERR

if [ -z "$REMOTE" ]; then
  echo "Set OWS_BACKUP_REMOTE in /etc/default/ows-pg-backup (user@host:path, or 'none' for local only)" >&2
  false
fi

mkdir -p "$DIR"
chmod 700 "$DIR"
out="$DIR/openworldserver-$(date -u +%Y%m%dT%H%M%SZ).dump"

# A dump only gets its final name once pg_restore can read its table of contents (a truncated archive fails).
docker exec "$CONTAINER" pg_dump -U postgres -Fc "$DB" > "$out.partial"
docker exec -i "$CONTAINER" pg_restore --list < "$out.partial" > /dev/null
mv "$out.partial" "$out"

find "$DIR" -name 'openworldserver-*.dump' -mtime +"$KEEP_DAYS" -delete
find "$DIR" -name '*.partial' -mmin +60 -delete

if [ "$REMOTE" != none ]; then
  rsync -a --delete --include='openworldserver-*.dump' --exclude='*' \
    -e "ssh -p $SSH_PORT -i $SSH_KEY -o BatchMode=yes -o StrictHostKeyChecking=accept-new" \
    "$DIR/" "$REMOTE/"
fi

echo "backup ok: $out ($(du -h "$out" | cut -f1)), remote: $REMOTE"
