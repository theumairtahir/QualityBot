#!/usr/bin/env bash
# Gather everything the agent needs to understand the PR into .qa/ so it spends its turns on testing,
# not on gh plumbing. Needs PR_NUMBER, GH_TOKEN and GITHUB_REPOSITORY (owner/repo).
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need gh; need jq

: "${PR_NUMBER:?PR_NUMBER is required}"
: "${GITHUB_REPOSITORY:=$(gh repo view --json nameWithOwner -q .nameWithOwner)}"
R=$GITHUB_REPOSITORY
# Fresh cycle: the runner reuses its work directory, so drop anything a previous run left behind.
rm -rf "${QA_OUT:?}"/* "${QA_OUT:?}"/.[!.]* 2>/dev/null || true
mkdir -p "$QA_OUT/screenshots"

log "PR #$PR_NUMBER metadata"
gh pr view "$PR_NUMBER" --repo "$R" --json \
  number,title,body,url,author,isDraft,state,headRefName,baseRefName,headRepository,headRepositoryOwner,labels,files,closingIssuesReferences,additions,deletions,createdAt,updatedAt \
  > "$QA_OUT/pr.json"

log "PR comments, review comments and reviews"
jq -n \
  --slurpfile issue    <(gh api --paginate "repos/$R/issues/$PR_NUMBER/comments" | jq -c '.[]') \
  --slurpfile review   <(gh api --paginate "repos/$R/pulls/$PR_NUMBER/comments"  | jq -c '.[]') \
  --slurpfile reviews  <(gh api --paginate "repos/$R/pulls/$PR_NUMBER/reviews"   | jq -c '.[]') \
  '{
     issue_comments:  [$issue[]   | {id, author: .user.login, created_at, updated_at, body, url: .html_url}],
     review_comments: [$review[]  | {id, author: .user.login, created_at, path, line, side, body, url: .html_url, in_reply_to_id}],
     reviews:         [$reviews[] | {id, author: .user.login, submitted_at, state, body, url: .html_url}]
   }' > "$QA_OUT/pr-comments.json"

log "linked issue"
issue_num=$(jq -r '.closingIssuesReferences[0].number // empty' "$QA_OUT/pr.json")
if [ -z "$issue_num" ]; then
  # Fall back to the first "#123" / "fixes #123" style reference in the PR body that is not the PR itself.
  issue_num=$(jq -r '.body // ""' "$QA_OUT/pr.json" | grep -oE '#[0-9]+' | tr -d '#' | grep -vx "$PR_NUMBER" | head -n 1 || true)
fi
if [ -n "$issue_num" ] && gh issue view "$issue_num" --repo "$R" --json number,title,body,url,state,labels,comments > "$QA_OUT/issue.json" 2>/dev/null; then
  log "linked issue: #$issue_num"
else
  echo null > "$QA_OUT/issue.json"
  warn "no linked issue — the agent will infer scope from the diff"
fi

log "diff"
gh pr diff "$PR_NUMBER" --repo "$R" > "$QA_OUT/diff.patch"
jq -r '.files[].path' "$QA_OUT/pr.json" > "$QA_OUT/changed-files.txt"

log "context written to $QA_OUT: $(wc -l < "$QA_OUT/changed-files.txt") changed files, $(jq '.issue_comments|length' "$QA_OUT/pr-comments.json") comments"
