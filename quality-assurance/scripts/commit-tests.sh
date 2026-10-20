#!/usr/bin/env bash
# Commit regression tests and seed files the agent wrote and push them to the PR branch.
# Only tests/ and seed/ under quality-assurance are committed. Skips (exit 0) for fork PRs, where the
# workflow token cannot push; the files still reach the team through the workflow artifact.
#
#   commit-tests.sh ["commit message"]
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need git

msg=${1:-"test(qa): add regression tests from QA cycle"}
: "${PR_BRANCH:?PR_BRANCH is required}"

cd "$REPO_ROOT"
if [ -n "${GITHUB_REPOSITORY:-}" ] && [ -n "${PR_HEAD_REPO:-}" ] && [ "$PR_HEAD_REPO" != "$GITHUB_REPOSITORY" ]; then
  warn "PR comes from fork $PR_HEAD_REPO — not pushing tests; they are in the workflow artifact"
  exit 0
fi

git add -A -- quality-assurance/tests quality-assurance/seed
if git diff --cached --quiet; then
  log "no test or seed changes to commit"
  exit 0
fi

# "[skip ci]" stops this push from starting another QA cycle (and Development CI) on the PR.
# The workflow's guard job is the second line of defence, keyed on this author e-mail.
git -c user.name=QualityBot -c user.email=qualitybot@users.noreply.github.com \
  commit -q -m "$msg [skip ci]" -m "Co-Authored-By: Claude <noreply@anthropic.com>"
log "committed: $(git log -1 --format=%h) $msg"

# The branch may have moved while the cycle ran; rebase once before giving up.
if ! git push -q origin "HEAD:refs/heads/$PR_BRANCH" 2>/dev/null; then
  warn "push rejected, rebasing onto origin/$PR_BRANCH"
  git fetch -q origin "$PR_BRANCH"
  git -c user.name=QualityBot -c user.email=qualitybot@users.noreply.github.com rebase -q FETCH_HEAD
  git push -q origin "HEAD:refs/heads/$PR_BRANCH"
fi
log "pushed to $PR_BRANCH ([skip ci] + the guard job keep this from starting another cycle)"
