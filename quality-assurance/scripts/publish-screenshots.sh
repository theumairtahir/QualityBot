#!/usr/bin/env bash
# GitHub has no API for attaching images to comments, so push the screenshots to an orphan branch
# (qa-artifacts) and hand back URLs that render inline in PR markdown for anyone with repo access.
#
#   publish-screenshots.sh [dir]     default dir: .qa/screenshots → writes .qa/screenshot-urls.txt (name=url)
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
need git

SRC=${1:-$QA_OUT/screenshots}
: "${PR_NUMBER:?PR_NUMBER is required}"
: "${GITHUB_REPOSITORY:=$(gh repo view --json nameWithOwner -q .nameWithOwner)}"
: "${GITHUB_RUN_ID:=local-$(date +%Y%m%d%H%M%S)}"
BRANCH=qa-artifacts
DEST="pr-$PR_NUMBER/run-$GITHUB_RUN_ID"
OUT="$QA_OUT/screenshot-urls.txt"

shopt -s nullglob
files=("$SRC"/*.png "$SRC"/*.jpg "$SRC"/*.jpeg)
[ ${#files[@]} -gt 0 ] || { warn "no screenshots in $SRC"; : > "$OUT"; exit 0; }

tmp=$(mktemp -d)
cleanup() { git -C "$REPO_ROOT" worktree remove --force "$tmp" 2>/dev/null || rm -rf "$tmp"; }
trap cleanup EXIT

cd "$REPO_ROOT"
if git fetch -q origin "$BRANCH" 2>/dev/null; then
  git worktree add -q --detach "$tmp" FETCH_HEAD
else
  log "creating orphan branch $BRANCH"
  git worktree add -q --detach "$tmp"
  git -C "$tmp" checkout -q --orphan "$BRANCH"
  git -C "$tmp" rm -rfq . 2>/dev/null || true
  git -C "$tmp" clean -fdxq
  printf '# QA artifacts\n\nScreenshots published by QualityBot. One folder per PR and run.\n' > "$tmp/README.md"
fi

mkdir -p "$tmp/$DEST"
cp "${files[@]}" "$tmp/$DEST/"
git -C "$tmp" add -Af .
git -C "$tmp" -c user.name=QualityBot -c user.email=qualitybot@users.noreply.github.com \
  commit -qm "QA screenshots for PR #$PR_NUMBER (run $GITHUB_RUN_ID) [skip ci]"
git -C "$tmp" push -q origin "HEAD:refs/heads/$BRANCH"

: > "$OUT"
for f in "${files[@]}"; do
  name=$(basename "$f")
  echo "$name=https://github.com/$GITHUB_REPOSITORY/blob/$BRANCH/$DEST/$name?raw=true" >> "$OUT"
done
log "published ${#files[@]} screenshots → $OUT"
cat "$OUT"
