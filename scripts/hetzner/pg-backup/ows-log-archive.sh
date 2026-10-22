#!/usr/bin/env bash
# Keep 90 days of archived zone server logs on this host and mirror them to the backups' off-box storage.
# Zone hosts upload one zip per host and UTC day to $DIR/<host>/zone-logs-<host>-<yyyymmdd>.zip
# (scripts/hetzner/Archive-ZoneLogs.ps1 on Windows). Run by ows-log-archive.timer (daily).
# Settings: /etc/default/ows-pg-backup; the remote defaults to the backups' Storage Box, folder ows-logs.
set -euo pipefail
if [ -f /etc/default/ows-pg-backup ]; then . /etc/default/ows-pg-backup; fi

DIR="${OWS_LOG_ARCHIVE_DIR:-/var/backups/ows-logs}"
KEEP_DAYS="${OWS_LOG_ARCHIVE_KEEP_DAYS:-90}"
BACKUP_REMOTE="${OWS_BACKUP_REMOTE:-none}"
REMOTE="${OWS_LOG_ARCHIVE_REMOTE:-${BACKUP_REMOTE%:*}:ows-logs}"
SSH_KEY="${OWS_BACKUP_SSH_KEY:-/root/.ssh/ows_backup_ed25519}"
SSH_PORT="${OWS_BACKUP_SSH_PORT:-23}"
WEBHOOK="${OWS_BACKUP_DISCORD_WEBHOOK_URL:-}"

notify_failure() {
  local line="$1"
  echo "log archive FAILED at line $line" >&2
  if [ -n "$WEBHOOK" ]; then
    curl -fsS -m 10 -H 'Content-Type: application/json' \
      -d "{\"content\":\"OWS zone log archive failed on $(hostname) (line $line). Check: journalctl -u ows-log-archive\"}" \
      "$WEBHOOK" >/dev/null || true
  fi
}
trap 'notify_failure $LINENO' ERR

mkdir -p "$DIR"
chmod 700 "$DIR"

# Age comes from the day in the name: an upload's file time is when it arrived, not the day it holds.
cutoff="$(date -u -d "-$KEEP_DAYS days" +%Y%m%d)"
find "$DIR" -type f -name 'zone-logs-*.zip' | while read -r zip; do
  day="$(basename "$zip" .zip)"
  day="${day##*-}"
  if [[ "$day" =~ ^[0-9]{8}$ && "$day" < "$cutoff" ]]; then rm -f "$zip"; fi
done
find "$DIR" -type f -name '*.partial' -mmin +120 -delete

if [ "$BACKUP_REMOTE" != none ]; then
  rsync -a --delete --include='*/' --include='zone-logs-*.zip' --exclude='*' \
    -e "ssh -p $SSH_PORT -i $SSH_KEY -o BatchMode=yes -o StrictHostKeyChecking=accept-new" \
    "$DIR/" "$REMOTE/"
fi

echo "log archive ok: $(find "$DIR" -name 'zone-logs-*.zip' | wc -l) archives, $(du -sh "$DIR" | cut -f1), remote: $REMOTE"
