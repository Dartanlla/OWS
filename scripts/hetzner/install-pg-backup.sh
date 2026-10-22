#!/usr/bin/env bash
# Install or update the hourly OWS Postgres backup and the daily zone log archive, as root: install-pg-backup.sh
# Settings live in /etc/default/ows-pg-backup; the first run writes a template there and stops.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)/pg-backup"
settings=/etc/default/ows-pg-backup
key=/root/.ssh/ows_backup_ed25519

command -v rsync >/dev/null || apt-get install -y -q rsync

if [ ! -f "$key" ]; then
  ssh-keygen -q -t ed25519 -N '' -C "ows-backup@$(hostname)" -f "$key"
fi

if [ ! -f "$settings" ]; then
  cat > "$settings" <<'EOF'
# Off-box copy: Hetzner Storage Box over rsync/SSH (port 23), e.g. u123456@u123456.your-storagebox.de:ows-backups
# 'none' keeps dumps on this host only.
OWS_BACKUP_REMOTE=
OWS_BACKUP_SSH_PORT=23
OWS_BACKUP_KEEP_DAYS=14
# Posted to when a backup fails (Discord #dev-observation webhook).
OWS_BACKUP_DISCORD_WEBHOOK_URL=
EOF
  chmod 600 "$settings"
  echo "Wrote $settings. Fill in OWS_BACKUP_REMOTE (and the webhook), then run this again."
  echo "Storage Box public key (add it under the box's SSH keys):"
  cat "$key.pub"
  exit 1
fi
chmod 600 "$settings"

install -m 755 "$here/ows-pg-backup.sh" /usr/local/sbin/ows-pg-backup
install -m 755 "$here/ows-pg-restore-check.sh" /usr/local/sbin/ows-pg-restore-check
install -m 755 "$here/ows-log-archive.sh" /usr/local/sbin/ows-log-archive
install -m 644 "$here/ows-pg-backup.service" "$here/ows-pg-backup.timer" \
  "$here/ows-log-archive.service" "$here/ows-log-archive.timer" /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now ows-pg-backup.timer ows-log-archive.timer

systemctl start ows-pg-backup.service ows-log-archive.service
journalctl -u ows-pg-backup.service -n 3 --no-pager
journalctl -u ows-log-archive.service -n 2 --no-pager
systemctl list-timers ows-pg-backup.timer ows-log-archive.timer --no-pager
