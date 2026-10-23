#!/usr/bin/env bash
# Start, stop or inspect Samsara Management (owsmanagement), the staff console, on the dev server.
#
# The console is behind the `admin` compose profile, so a plain `up -d` never starts it.
# It binds to 127.0.0.1 only and has no TLS: reach it with an SSH tunnel, never by opening the port.
#
# Deliberately uses `--no-deps` for the build/up path so it cannot recreate the `database`
# container. See the CAUTION in docs/hosting/hetzner-dev.md: recreating Postgres severs
# every service's pool and any container holding a stale password fails with 28P01.
#
# Usage (from anywhere on the dev server):
#   ./scripts/hetzner/admin-console.sh up       # build (if needed) and start
#   ./scripts/hetzner/admin-console.sh rebuild  # force a rebuild from current source, then start
#   ./scripts/hetzner/admin-console.sh down     # stop and remove the container
#   ./scripts/hetzner/admin-console.sh status   # show state and health
#   ./scripts/hetzner/admin-console.sh logs     # follow logs
set -euo pipefail

SRC_DIR="${OWS_SRC_DIR:-/opt/owsfork/src}"
ENV_FILE="${OWS_ENV_FILE:-.env.hetzner-dev}"
COMPOSE_FILE="${OWS_COMPOSE_FILE:-docker-compose.hetzner-dev.yml}"
SERVICE=owsmanagement
PORT="${OWS_MANAGEMENT_HOST_PORT:-44410}"

ACTION="${1:-up}"

cd "$SRC_DIR"
if [ ! -f "$ENV_FILE" ]; then echo "!! $ENV_FILE not found in $SRC_DIR" >&2; exit 2; fi
DC="docker compose --env-file $ENV_FILE -f $COMPOSE_FILE --profile admin"
export OWS_GIT_COMMIT="${OWS_GIT_COMMIT:-$(git rev-parse --short HEAD 2>/dev/null || true)}"

# The console is a fork-local addition; a server still on an older checkout would silently
# build nothing and leave a confusing "no such service" error.
if ! grep -q "^  ${SERVICE}:" "$COMPOSE_FILE"; then
  echo "!! $COMPOSE_FILE has no '$SERVICE' service." >&2
  echo "   The server is on an older checkout. Run: git pull --ff-only origin main" >&2
  exit 2
fi

wait_for_console() {
  echo "Waiting for the console to answer on 127.0.0.1:$PORT ..."
  for _ in $(seq 1 30); do
    if curl -fsS -o /dev/null "http://127.0.0.1:$PORT/api/System/Status" 2>/dev/null; then
      echo "Console is up."
      echo
      echo "From your PC:"
      echo "  ssh -L ${PORT}:127.0.0.1:${PORT} <user>@<this-host>"
      echo "  then open http://localhost:${PORT}"
      return 0
    fi
    sleep 2
  done
  echo "!! Console did not respond within 60s. Recent logs:" >&2
  $DC logs --tail=40 "$SERVICE" >&2
  return 1
}

case "$ACTION" in
  up)
    $DC up -d --no-deps "$SERVICE"
    wait_for_console
    ;;
  rebuild)
    $DC build "$SERVICE"
    $DC up -d --no-deps --force-recreate "$SERVICE"
    wait_for_console
    ;;
  down)
    $DC stop "$SERVICE"
    $DC rm -f "$SERVICE"
    echo "Console stopped."
    ;;
  status)
    $DC ps "$SERVICE"
    echo
    if curl -fsS -o /dev/null "http://127.0.0.1:$PORT/api/System/Status" 2>/dev/null; then
      echo "http://127.0.0.1:$PORT responds OK"
    else
      echo "http://127.0.0.1:$PORT is not responding"
    fi
    ;;
  logs)
    $DC logs -f --tail=100 "$SERVICE"
    ;;
  *)
    echo "Usage: $0 {up|rebuild|down|status|logs}" >&2
    exit 2
    ;;
esac
