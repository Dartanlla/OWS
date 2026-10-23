#!/usr/bin/env bash
# Install or update the staging loader for character restores, as root: install-staging.sh. It creates
# /var/lib/ows-staging (shared with the staff console), a staging database password in /etc/default/ows-staging,
# copies that password into the compose env file as OWS_STAGING_DB_PASSWORD, and enables ows-staging.path.
# Rebuild the console afterwards (admin-console.sh rebuild) so it reads the password.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)/staging"
settings=/etc/default/ows-staging
env_file="$(cd "$(dirname "$0")/../.." && pwd)/src/.env.hetzner-dev"

install -d -m 700 /var/lib/ows-staging

if [ ! -f "$settings" ]; then
  printf 'OWS_STAGING_DB_PASSWORD=%s\n' "$(openssl rand -hex 24)" > "$settings"
fi
chmod 600 "$settings"
# shellcheck disable=SC1090
. "$settings"

if [ -f "$env_file" ] && ! grep -q '^OWS_STAGING_DB_PASSWORD=' "$env_file"; then
  printf '\nOWS_STAGING_DB_PASSWORD=%s\n' "$OWS_STAGING_DB_PASSWORD" >> "$env_file"
fi

install -m 755 "$here/ows-stage-backup.sh" /usr/local/sbin/ows-stage-backup
install -m 644 "$here/ows-staging.path" "$here/ows-staging.service" /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now ows-staging.path
echo "Staging loader installed. Rebuild the console: scripts/hetzner/admin-console.sh rebuild"
