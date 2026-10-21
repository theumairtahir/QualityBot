#!/usr/bin/env bash
# Post a pull request review with inline comments from a JSON payload.
#
#   post-review.sh <payload.json>
#
# payload: { "body": "markdown", "event": "COMMENT" | "REQUEST_CHANGES",
#            "comments": [ { "path": "development/src/X.cs", "line": 42, "side": "RIGHT", "body": "..." } ] }
# Lines must be part of the PR diff or GitHub rejects the review (422). APPROVE is refused on purpose.
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need gh; need jq

payload=${1:?usage: post-review.sh <payload.json>}
[ -f "$payload" ] || die "no such file: $payload"
jq -e . "$payload" >/dev/null || die "payload is not valid JSON"
: "${PR_NUMBER:?PR_NUMBER is required}"
: "${GITHUB_REPOSITORY:=$(gh repo view --json nameWithOwner -q .nameWithOwner)}"

event=$(jq -r '.event // "COMMENT"' "$payload")
case "$event" in
  COMMENT|REQUEST_CHANGES) ;;
  *) die "event must be COMMENT or REQUEST_CHANGES (got '$event')" ;;
esac

head_sha=$(gh pr view "$PR_NUMBER" --repo "$GITHUB_REPOSITORY" --json headRefOid -q .headRefOid)
tagged=$(mktemp)
jq --arg sha "$head_sha" --arg event "$event" '
  {
    commit_id: $sha,
    event: $event,
    body: ("<!-- qualitybot:review -->\n" + (.body // "")),
    comments: [ (.comments // [])[] | {path, line, side: (.side // "RIGHT"), body} ]
  }' "$payload" > "$tagged"

gh api -X POST "repos/$GITHUB_REPOSITORY/pulls/$PR_NUMBER/reviews" --input "$tagged" -q .html_url
log "posted review ($event) with $(jq '.comments|length' "$tagged") inline comments"
rm -f "$tagged"
