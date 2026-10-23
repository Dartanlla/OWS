#!/usr/bin/env bash
# Delete the editor's throwaway PIE accounts (PurgePIEAccounts: @samsara.local, first name PIE) once they are
# OWS_PIE_PURGE_AGE old, with their characters and shops, plus rows of characters that no longer exist.
# Run by ows-pie-purge.timer (daily). Settings: /etc/default/ows-pg-backup.
set -euo pipefail
if [ -f /etc/default/ows-pg-backup ]; then . /etc/default/ows-pg-backup; fi

CONTAINER="${OWS_DB_CONTAINER:-ows-hetzner-dev-database-1}"
DB="${OWS_DB_NAME:-openworldserver}"
AGE="${OWS_PIE_PURGE_AGE:-6 hours}"
WEBHOOK="${OWS_BACKUP_DISCORD_WEBHOOK_URL:-}"

notify_failure() {
  local line="$1"
  echo "PIE purge FAILED at line $line" >&2
  if [ -n "$WEBHOOK" ]; then
    curl -fsS -m 10 -H 'Content-Type: application/json' \
      -d "{\"content\":\"OWS PIE account purge failed on $(hostname) (line $line). Check: journalctl -u ows-pie-purge\"}" \
      "$WEBHOOK" >/dev/null || true
  fi
}
trap 'notify_failure $LINENO' ERR

removed="$(docker exec "$CONTAINER" psql -U postgres -d "$DB" -v ON_ERROR_STOP=1 -At -F ': ' \
  -c "SELECT What, Removed FROM PurgePIEAccounts(INTERVAL '$AGE') WHERE Removed > 0")"
echo "PIE purge ok (older than $AGE): ${removed:-nothing to remove}" | tr '\n' ' '
echo

# The staff console's logs: game events after 180 days, chat and finished zone commands after 30.
purged="$(docker exec "$CONTAINER" psql -U postgres -d "$DB" -v ON_ERROR_STOP=1 -At -F ': ' \
  -c "SELECT What, Removed FROM PurgeSamsaraLogs() WHERE Removed > 0")"
echo "Console log purge ok: ${purged:-nothing to remove}" | tr '\n' ' '
echo
