#!/usr/bin/env bash
# Install or update Grafana Alloy with one config from ./alloy, as root: install-alloy.sh api-box.alloy
set -euo pipefail

config="$(cd "$(dirname "$0")" && pwd)/alloy/${1:?usage: install-alloy.sh <config>.alloy}"
[ -f "$config" ] || { echo "No config at $config" >&2; exit 1; }

if ! command -v alloy >/dev/null; then
  mkdir -p /etc/apt/keyrings
  wget -q -O - https://apt.grafana.com/gpg.key | gpg --dearmor > /etc/apt/keyrings/grafana.gpg
  echo "deb [signed-by=/etc/apt/keyrings/grafana.gpg] https://apt.grafana.com stable main" \
    > /etc/apt/sources.list.d/grafana.list
  apt-get update -q
  apt-get install -y -q alloy
fi

# cAdvisor reads /var/lib/docker and the containerd socket, which are root-only. The docker group the
# socket needs is root-equivalent anyway.
if getent group docker >/dev/null; then
  mkdir -p /etc/systemd/system/alloy.service.d
  printf '[Service]\nUser=root\n' > /etc/systemd/system/alloy.service.d/10-root.conf
  systemctl daemon-reload
fi

missing=0
for name in $(grep -o 'sys.env("[A-Z0-9_]*")' "$config" | sed 's/sys.env("\(.*\)")/\1/' | sort -u); do
  if ! grep -q "^${name}=" /etc/default/alloy; then
    echo "Set ${name} in /etc/default/alloy" >&2
    missing=1
  fi
done
[ "$missing" -eq 0 ] || exit 1
chmod 640 /etc/default/alloy
chgrp alloy /etc/default/alloy

alloy fmt "$config" >/dev/null
install -m 644 "$config" /etc/alloy/config.alloy
# Files the configs read by path, such as the postgres exporter's custom queries.
for extra in "$(dirname "$config")"/*.yaml; do
  if [ -e "$extra" ]; then install -m 644 "$extra" /etc/alloy/; fi
done
systemctl enable --now alloy
systemctl restart alloy
sleep 5
systemctl is-active alloy
alloy --version | head -1
