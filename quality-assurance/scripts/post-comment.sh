#!/usr/bin/env bash
# Upsert a PR comment identified by a marker, so re-runs update instead of duplicating.
#
#   post-comment.sh <marker> <file.md>      marker e.g. "summary" or "bug:login-empty-password"
#
# The marker becomes the hidden first line `<!-- qualitybot:<marker> -->`. If a comment with that
# marker exists it is edited in place; otherwise a new comment is created.
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need gh; need jq

marker=${1:?usage: post-comment.sh <marker> <file.md>}
file=${2:?usage: post-comment.sh <marker> <file.md>}
[ -f "$file" ] || die "no such file: $file"
: "${PR_NUMBER:?PR_NUMBER is required}"
: "${GITHUB_REPOSITORY:=$(gh repo view --json nameWithOwner -q .nameWithOwner)}"
R=$GITHUB_REPOSITORY
tag="<!-- qualitybot:$marker -->"

body=$(mktemp)
if grep -qF "$tag" "$file"; then cp "$file" "$body"; else { echo "$tag"; cat "$file"; } > "$body"; fi

existing=$(gh api --paginate "repos/$R/issues/$PR_NUMBER/comments" \
  | jq -r --arg tag "$tag" '.[] | select(.body | contains($tag)) | .id' | head -n 1)

if [ -n "$existing" ]; then
  gh api -X PATCH "repos/$R/issues/comments/$existing" -F body=@"$body" -q .html_url
  log "updated comment [$marker]"
else
  gh api -X POST "repos/$R/issues/$PR_NUMBER/comments" -F body=@"$body" -q .html_url
  log "created comment [$marker]"
fi
rm -f "$body"
