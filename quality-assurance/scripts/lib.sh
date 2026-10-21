#!/usr/bin/env bash
# Shared helpers for the QA scripts. Source it; do not run it.
set -euo pipefail

QA_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_ROOT="$(cd "$QA_ROOT/.." && pwd)"
APP_DIR="${APP_DIR:-$REPO_ROOT/development}"
APP_COMPOSE="$APP_DIR/docker-compose.yml"
QA_OUT="$QA_ROOT/.qa"

# Compose project name; the workflow sets qa-pr-<N>-<attempt> so every cycle is isolated.
: "${QA_PROJECT:=qa-local}"
: "${QA_WEB_URL:=http://localhost:8080}"
: "${QA_API_URL:=http://localhost:8081}"
: "${SONAR_PORT:=9000}"
: "${SONAR_URL:=http://localhost:$SONAR_PORT}"

log()  { printf '\033[1;34m[qa]\033[0m %s\n' "$*" >&2; }
warn() { printf '\033[1;33m[qa] %s\033[0m\n' "$*" >&2; }
die()  { printf '\033[1;31m[qa] %s\033[0m\n' "$*" >&2; exit 1; }

need() { command -v "$1" >/dev/null 2>&1 || die "missing tool: $1"; }

app_compose()   { docker compose -p "$QA_PROJECT" -f "$APP_COMPOSE" "$@"; }
sonar_compose() { docker compose -p "$QA_PROJECT-sonar" -f "$QA_ROOT/sonarqube/docker-compose.yml" "$@"; }

# Poll a URL until it answers 2xx (optionally containing a string). wait_for URL SECONDS [GREP]
wait_for() {
  local url=$1 secs=$2 pattern=${3:-} i
  for ((i = 0; i < secs; i += 2)); do
    if body=$(curl -fsS --max-time 5 "$url" 2>/dev/null); then
      if [ -z "$pattern" ] || grep -q -- "$pattern" <<<"$body"; then return 0; fi
    fi
    sleep 2
  done
  return 1
}

mkdir -p "$QA_OUT"
