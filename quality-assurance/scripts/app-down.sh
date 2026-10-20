#!/usr/bin/env bash
# Destroy everything a QA cycle created: app containers, SonarQube containers, their networks and volumes.
# Safe to run when nothing is up. Saves container logs to .qa/ first.
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need docker

if [ -f "$APP_COMPOSE" ]; then
  app_compose logs --no-color --timestamps > "$QA_OUT/app-logs.txt" 2>/dev/null || true
  log "removing app stack '$QA_PROJECT'"
  app_compose down --volumes --remove-orphans --timeout 20 || warn "app down reported errors (ignored)"
fi

sonar_compose logs --no-color sonarqube > "$QA_OUT/sonar-logs.txt" 2>/dev/null || true
log "removing SonarQube stack '$QA_PROJECT-sonar'"
sonar_compose --profile scan down --volumes --remove-orphans --timeout 30 || warn "sonar down reported errors (ignored)"

log "done"
