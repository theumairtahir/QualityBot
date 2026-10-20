#!/usr/bin/env bash
# SonarQube for one QA cycle, fully in Docker.
#
#   sonar.sh up      start SonarQube, wait for UP, create an analysis token (.qa/sonar.env)
#   sonar.sh scan    build the scanner image and analyse ../development
#   sonar.sh issues  wait for the analysis to be processed, write .qa/sonar-issues.json + sonar-summary.md
#   sonar.sh down    destroy the stack
#   sonar.sh all     up → scan → issues → down (down runs even when a step fails)
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need docker; need curl; need jq

: "${SONAR_PROJECT_KEY:=qualitybot}"
: "${SONAR_PROJECT_NAME:=QualityBot}"
: "${SONAR_BRANCH_LABEL:=${PR_BRANCH:-local}}"
ENV_FILE="$QA_OUT/sonar.env"

load_env() { [ -f "$ENV_FILE" ] || die "no $ENV_FILE — run 'sonar.sh up' first"; source "$ENV_FILE"; }
api() { curl -fsS -u "admin:$SONAR_ADMIN_PASSWORD" "$SONAR_URL/api/$1" "${@:2}"; }

up() {
  log "starting SonarQube as compose project '$QA_PROJECT-sonar' on $SONAR_URL"
  SONAR_TOKEN=unused sonar_compose up -d sonarqube
  log "waiting for SonarQube (this takes 1–2 minutes)"
  wait_for "$SONAR_URL/api/system/status" 300 '"UP"' || { sonar_compose logs --no-color sonarqube | tail -n 60 >&2; die "SonarQube did not start"; }

  # Fresh container: rotate the default admin password, then mint a token for the scanner.
  # Policy (SonarQube 2025+): 12+ chars with upper, lower, digit and special — the prefix guarantees it.
  local pw; pw="Qa!9$(head -c 24 /dev/urandom | base64 | tr -d '/+=' | head -c 20)"
  curl -fsS -u admin:admin -X POST "$SONAR_URL/api/users/change_password" \
    --data-urlencode login=admin --data-urlencode previousPassword=admin --data-urlencode "password=$pw" >/dev/null
  local token
  token=$(curl -fsS -u "admin:$pw" -X POST "$SONAR_URL/api/user_tokens/generate" \
    --data-urlencode name=qa-cycle --data-urlencode type=GLOBAL_ANALYSIS_TOKEN | jq -r .token)
  [ -n "$token" ] && [ "$token" != null ] || die "could not create a SonarQube token"
  umask 077
  printf 'SONAR_ADMIN_PASSWORD=%q\nSONAR_TOKEN=%q\n' "$pw" "$token" > "$ENV_FILE"
  log "SonarQube is up"
}

scan() {
  load_env
  [ -n "$(find "$APP_DIR" -maxdepth 1 \( -name '*.slnx' -o -name '*.sln' \) 2>/dev/null)" ] \
    || { warn "no solution in $APP_DIR — skipping scan"; return 3; }
  log "building scanner image"
  SONAR_TOKEN=$SONAR_TOKEN sonar_compose --profile scan build -q scanner
  log "analysing $APP_DIR as '$SONAR_PROJECT_KEY' (version label: $SONAR_BRANCH_LABEL)"
  if ! APP_DIR=$APP_DIR SONAR_TOKEN=$SONAR_TOKEN SONAR_PROJECT_KEY=$SONAR_PROJECT_KEY \
       SONAR_PROJECT_NAME=$SONAR_PROJECT_NAME SONAR_BRANCH_LABEL=$SONAR_BRANCH_LABEL \
       sonar_compose --profile scan run --rm scanner > "$QA_OUT/sonar-scan.log" 2>&1; then
    grep -iE 'error|fail|scan:' "$QA_OUT/sonar-scan.log" | tail -n 40 >&2
    die "scanner failed — full log in $QA_OUT/sonar-scan.log"
  fi
  grep -E '^scan:' "$QA_OUT/sonar-scan.log" >&2 || true
}

issues() {
  load_env
  log "waiting for the analysis to be processed"
  local i status
  for ((i = 0; i < 90; i++)); do
    status=$(api "ce/component?component=$SONAR_PROJECT_KEY" | jq -r 'if (.queue|length) > 0 then "PENDING" else (.current.status // "NONE") end')
    case "$status" in
      SUCCESS) break ;;
      FAILED|CANCELED) die "SonarQube background task $status" ;;
      *) sleep 2 ;;
    esac
  done
  [ "$status" = SUCCESS ] || die "analysis not processed in time (status: $status)"

  local page=1 total=0 tmp; tmp=$(mktemp)
  : > "$tmp"
  while :; do
    api "issues/search?components=$SONAR_PROJECT_KEY&resolved=false&ps=500&p=$page&additionalFields=rules" > "$tmp.page"
    jq -c '.issues[]' "$tmp.page" >> "$tmp"
    total=$(jq '.paging.total' "$tmp.page")
    [ $((page * 500)) -ge "$total" ] && break
    page=$((page + 1))
  done

  jq -n \
    --arg key "$SONAR_PROJECT_KEY" --arg url "$SONAR_URL" --arg label "$SONAR_BRANCH_LABEL" \
    --slurpfile issues "$tmp" \
    --argjson rules "$(jq '.rules // []' "$tmp.page")" \
    --argjson hotspots "$(api "hotspots/search?project=$SONAR_PROJECT_KEY&ps=500" | jq '.hotspots')" \
    --argjson measures "$(api "measures/component?component=$SONAR_PROJECT_KEY&metricKeys=bugs,vulnerabilities,code_smells,security_hotspots,duplicated_lines_density,ncloc,cognitive_complexity" | jq '[.component.measures[] | {(.metric): .value}] | add')" \
    '{
       project: $key, label: $label, generated_at: (now | todate),
       note: "SonarQube ran in a throwaway container; the server URL will not be reachable later.",
       measures: $measures,
       issues: [$issues[] | {
         key, rule, severity, type, status,
         component: (.component | sub("^[^:]*:"; "")), line, message, effort, tags,
         impacts, cleanCodeAttribute
       }],
       hotspots: [$hotspots[] | {
         key, ruleKey, vulnerabilityProbability, status,
         component: (.component | sub("^[^:]*:"; "")), line, message
       }],
       rules: [$rules[] | {key, name, lang}]
     }' > "$QA_OUT/sonar-issues.json"
  rm -f "$tmp" "$tmp.page"

  jq -r '
    "## SonarQube — \(.label)",
    "",
    "Lines: \(.measures.ncloc // "?") · Bugs: \(.measures.bugs // 0) · Vulnerabilities: \(.measures.vulnerabilities // 0) · Code smells: \(.measures.code_smells // 0) · Hotspots: \(.measures.security_hotspots // 0) · Duplication: \(.measures.duplicated_lines_density // 0)%",
    "",
    "| Severity | Count |", "| --- | --- |",
    (.issues | group_by(.severity) | map("| \(.[0].severity) | \(length) |") | .[]),
    "",
    "| Severity | Rule | File:line | Message |", "| --- | --- | --- | --- |",
    (.issues | sort_by(.severity) | .[] | "| \(.severity) | `\(.rule)` | `\(.component):\(.line // "-")` | \(.message | gsub("\\|"; "\\\\|")) |")
  ' "$QA_OUT/sonar-issues.json" > "$QA_OUT/sonar-summary.md"

  log "$(jq '.issues|length' "$QA_OUT/sonar-issues.json") issues, $(jq '.hotspots|length' "$QA_OUT/sonar-issues.json") hotspots → $QA_OUT/sonar-issues.json"
}

down() {
  log "removing SonarQube stack '$QA_PROJECT-sonar'"
  SONAR_TOKEN=unused sonar_compose --profile scan down --volumes --remove-orphans --timeout 30 || true
  rm -f "$ENV_FILE"
}

case "${1:-}" in
  up) up ;;
  scan) scan ;;
  issues) issues ;;
  down) down ;;
  all)
    trap down EXIT
    up
    if scan; then issues; else rc=$?; [ $rc -eq 3 ] && exit 0 || exit $rc; fi
    ;;
  *) sed -n '2,9p' "$0" >&2; exit 2 ;;
esac
