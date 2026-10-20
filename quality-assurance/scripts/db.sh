#!/usr/bin/env bash
# psql against the test database inside the app's `db` container. The only DB access the agent has.
#
#   db.sh seed                 apply seed/*.sql in order (idempotent files only)
#   db.sh query "<sql>"        run one statement, print result
#   db.sh file <path.sql>      run a file
#   db.sh psql [args...]       raw psql, e.g. db.sh psql -c '\dt'
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need docker

: "${QA_DB_NAME:=sis}" "${QA_DB_USER:=sis}"

psql_in_db() { app_compose exec -T db psql -v ON_ERROR_STOP=1 -U "$QA_DB_USER" -d "$QA_DB_NAME" "$@"; }

cmd=${1:-}; shift || true
case "$cmd" in
  seed)
    shopt -s nullglob
    files=("$QA_ROOT"/seed/*.sql)
    [ ${#files[@]} -gt 0 ] || { log "no seed files"; exit 0; }
    for f in "${files[@]}"; do
      log "seed: $(basename "$f")"
      psql_in_db -q < "$f"
    done
    ;;
  query)
    [ -n "${1:-}" ] || die "usage: db.sh query \"<sql>\""
    psql_in_db -c "$1"
    ;;
  file)
    [ -f "${1:-}" ] || die "usage: db.sh file <path.sql>"
    psql_in_db < "$1"
    ;;
  psql)
    psql_in_db "$@"
    ;;
  *)
    sed -n '2,8p' "$0" >&2; exit 2
    ;;
esac
