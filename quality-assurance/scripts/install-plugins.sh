#!/usr/bin/env bash
# Register the QA marketplace and superpowers at user scope and install the plugins the QA cycle uses.
# Idempotent: re-running updates the marketplaces and skips plugins already installed. The runner
# calls this on every job so a headless `claude -p` never has to prompt for a plugin install.
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need claude

QA_PLUGINS=(test-strategy functional-testing automation performance-testing api-testing quality-metrics accessibility-testing qa-toolkit)

add_or_update() { # name source
  # `add` fails when the marketplace is already registered; fall back to refreshing it.
  claude plugin marketplace add "$2" >/dev/null 2>&1 \
    || claude plugin marketplace update "$1" >/dev/null 2>&1 \
    || die "could not add or update marketplace $1 from $2"
}

install() { # plugin@marketplace
  if claude plugin install "$1" >/dev/null 2>&1; then log "installed $1"
  else claude plugin enable "$1" >/dev/null 2>&1 || warn "could not install or enable $1"; fi
}

log "QA marketplace from $QA_ROOT"
add_or_update quality-assurance "$QA_ROOT"
for p in "${QA_PLUGINS[@]}"; do install "$p@quality-assurance"; done

log "superpowers (code review, debugging, verification skills)"
add_or_update superpowers-marketplace obra/superpowers-marketplace
install superpowers@superpowers-marketplace

claude plugin list 2>/dev/null | sed 's/^/  /' || true
