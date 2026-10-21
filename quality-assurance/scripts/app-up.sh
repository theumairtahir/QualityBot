#!/usr/bin/env bash
# Build and start an isolated copy of the application under test, wait until it is healthy, seed it.
# Project name comes from $QA_PROJECT so parallel/previous stacks never share networks or volumes.
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need docker; need curl

[ -f "$APP_COMPOSE" ] || die "no $APP_COMPOSE — the application under test is not in development/ yet"
: "${GH_PACKAGES_USER:?GH_PACKAGES_USER is required to restore TailwindUi.Blazor}"
: "${GH_PACKAGES_TOKEN:?GH_PACKAGES_TOKEN is required to restore TailwindUi.Blazor}"

log "starting app stack as compose project '$QA_PROJECT'"
app_compose up -d --build --wait --wait-timeout 420

log "waiting for API health at $QA_API_URL/health"
wait_for "$QA_API_URL/health" 180 Healthy || { app_compose logs --no-color api | tail -n 80 >&2; die "API did not become healthy"; }

log "waiting for web at $QA_WEB_URL"
wait_for "$QA_WEB_URL/" 60 || die "web did not answer"

"$QA_ROOT/scripts/db.sh" seed
app_compose ps
log "app is up: web $QA_WEB_URL · api $QA_API_URL"
