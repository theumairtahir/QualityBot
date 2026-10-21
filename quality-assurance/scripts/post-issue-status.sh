#!/usr/bin/env bash
# Report the cycle's verdict on the linked issue: one upserted status comment plus a qa:passed /
# qa:failed label, so the issue shows QA state without opening the PR. The workflow runs it after
# the agent; it is safe to re-run. Closing the issue is left to the PR merge ("closes #N").
#
# Inputs: .qa/issue.json (from collect-context.sh), .qa/verdict.json (from the agent; missing →
# "incomplete"), optional .qa/issue-status.md (agent-written per-criterion table, included verbatim).
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need gh; need jq

: "${PR_NUMBER:?PR_NUMBER is required}"
: "${GITHUB_REPOSITORY:=$(gh repo view --json nameWithOwner -q .nameWithOwner)}"
R=$GITHUB_REPOSITORY
PR_URL=${PR_URL:-https://github.com/$R/pull/$PR_NUMBER}
MARK="<!-- qualitybot:issue-status -->"

issue=$(jq -r '.number // empty' "$QA_OUT/issue.json" 2>/dev/null || true)
[ -n "$issue" ] || { log "no linked issue — nothing to report"; exit 0; }

if [ -s "$QA_OUT/verdict.json" ] && jq -e .verdict "$QA_OUT/verdict.json" >/dev/null 2>&1; then
  verdict=$(jq -r .verdict "$QA_OUT/verdict.json")
  counts=$(jq -r '"Critical \(.critical // 0) · High \(.high // 0) · Medium \(.medium // 0) · Low \(.low // 0) · regression tests added \(.tests_added // 0)"' "$QA_OUT/verdict.json")
else
  verdict=incomplete
  counts="The QA cycle did not finish; see the workflow run."
fi
case "$verdict" in
  pass) headline="✅ QA passed";     add=qa:passed; remove=qa:failed ;;
  fail) headline="❌ QA failed";     add=qa:failed; remove=qa:passed ;;
  *)    headline="⚠️ QA incomplete"; add="";        remove="" ;;
esac

summary_url=$(gh api --paginate "repos/$R/issues/$PR_NUMBER/comments" \
  | jq -r '.[] | select(.body | contains("<!-- qualitybot:summary -->")) | .html_url' | head -n 1)

body=$(mktemp)
{
  echo "$MARK"
  echo "## $headline — [PR #$PR_NUMBER]($PR_URL)"
  echo
  echo "$counts"
  echo
  if [ -s "$QA_OUT/issue-status.md" ]; then cat "$QA_OUT/issue-status.md"; echo; fi
  echo "Full report: ${summary_url:-$PR_URL} · [Workflow run](${QA_RUN_URL:-$PR_URL}) · $(date -u '+%Y-%m-%d %H:%M') UTC"
} > "$body"

existing=$(gh api --paginate "repos/$R/issues/$issue/comments" \
  | jq -r --arg m "$MARK" '.[] | select(.body | contains($m)) | .id' | head -n 1)
if [ -n "$existing" ]; then
  gh api -X PATCH "repos/$R/issues/comments/$existing" -F body=@"$body" -q .html_url
  log "issue #$issue: updated status comment ($headline)"
else
  gh api -X POST "repos/$R/issues/$issue/comments" -F body=@"$body" -q .html_url
  log "issue #$issue: posted status comment ($headline)"
fi
rm -f "$body"

if [ -n "$add" ]; then
  # --force makes create idempotent (updates colour/description if the label exists).
  gh label create qa:passed --repo "$R" --color 0E8A16 --description "QualityBot: latest QA cycle passed" --force >/dev/null 2>&1 || true
  gh label create qa:failed --repo "$R" --color D93F0B --description "QualityBot: latest QA cycle found Critical/High bugs" --force >/dev/null 2>&1 || true
  gh issue edit "$issue" --repo "$R" --remove-label "$remove" >/dev/null 2>&1 || true
  gh issue edit "$issue" --repo "$R" --add-label "$add" >/dev/null 2>&1 || warn "could not add label $add to #$issue"
  log "issue #$issue: label $add"
fi
