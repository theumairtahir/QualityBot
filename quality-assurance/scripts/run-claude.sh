#!/usr/bin/env bash
# Run the headless QA cycle. Claude's permissions come from .claude/settings.json (allow/deny lists),
# the browser from .mcp.json, the instructions from CLAUDE.md, the task from prompts/qa-cycle.md.
# Writes .qa/claude-transcript.jsonl (every message) and .qa/claude-summary.md (final answer).
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need claude; need jq; need envsubst

: "${PR_NUMBER:?PR_NUMBER is required}"
: "${PR_URL:=https://github.com/${GITHUB_REPOSITORY:-}/pull/$PR_NUMBER}"
: "${QA_RUN_URL:=local}"
[ -n "${ANTHROPIC_API_KEY:-}${CLAUDE_CODE_OAUTH_TOKEN:-}" ] || claude auth status >/dev/null 2>&1 \
  || die "Claude is not authenticated: set ANTHROPIC_API_KEY or CLAUDE_CODE_OAUTH_TOKEN"

cd "$QA_ROOT"
export PR_NUMBER PR_URL QA_WEB_URL QA_API_URL QA_RUN_URL

# Headless Claude ignores .claude/settings.json (permissions, additionalDirectories) until the
# workspace is trusted, and the trust dialog cannot be shown here. Trust is keyed by the git root.
trust_workspace() {
  local cfg="$HOME/.claude.json" tmp
  [ -s "$cfg" ] || echo '{}' > "$cfg"
  tmp=$(mktemp)
  jq --arg root "$REPO_ROOT" --arg qa "$QA_ROOT" '
      .projects[$root] = ((.projects[$root] // {}) + {hasTrustDialogAccepted: true})
    | .projects[$qa]   = ((.projects[$qa]   // {}) + {hasTrustDialogAccepted: true})' "$cfg" > "$tmp" \
    && mv "$tmp" "$cfg"
}
trust_workspace
prompt=$(envsubst '$PR_NUMBER $PR_URL $QA_WEB_URL $QA_API_URL $QA_RUN_URL' < prompts/qa-cycle.md)

args=(
  -p "$prompt"
  --output-format stream-json --verbose
  --mcp-config .mcp.json --strict-mcp-config
  --add-dir "$APP_DIR"
)
[ -n "${QA_MODEL:-}" ] && args+=(--model "$QA_MODEL")

log "starting QA cycle for PR #$PR_NUMBER (cwd $QA_ROOT)"
set +e
claude "${args[@]}" 2> "$QA_OUT/claude-stderr.log" \
  | tee "$QA_OUT/claude-transcript.jsonl" \
  | jq -R -r --unbuffered '
      fromjson? | select(.type == "assistant") | .message.content[]? |
      if .type == "text" then "\n" + .text
      elif .type == "tool_use" then "→ " + .name + " " + ((.input | tostring) | .[0:160])
      else empty end'
rc=${PIPESTATUS[0]}
set -e

# The transcript may contain non-JSON warning lines from Claude; -R + fromjson? skips them.
result() { jq -R "fromjson? | select(.type == \"result\") | $1" "$QA_OUT/claude-transcript.jsonl" 2>/dev/null; }
result -r '.result // empty' > "$QA_OUT/claude-summary.md" || true
if result -e '.is_error == true' >/dev/null; then
  warn "Claude reported an error — see $QA_OUT/claude-stderr.log and the transcript"
  rc=1
fi
if [ -n "${ANTHROPIC_API_KEY:-}" ]; then
  result -r '"turns: \(.num_turns // "?") · duration: \((.duration_ms // 0) / 1000 | floor)s · API cost: $\(.total_cost_usd // 0)"' | tail -n 1 >&2 || true
else
  # Subscription login: nothing is billed per run; total_cost_usd would only be an API-price estimate.
  result -r '"turns: \(.num_turns // "?") · duration: \((.duration_ms // 0) / 1000 | floor)s · tokens out: \(.usage.output_tokens // 0), cache read: \(.usage.cache_read_input_tokens // 0) (subscription, not billed per run)"' | tail -n 1 >&2 || true
fi

[ -f "$QA_OUT/verdict.json" ] || warn "no .qa/verdict.json was written"
exit "$rc"
