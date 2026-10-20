#!/usr/bin/env bash
# Entry point of the scanner container: copy the read-only source to /work, build it under the
# SonarScanner for .NET and upload the result. Build output never touches the host checkout.
set -euo pipefail

: "${SONAR_HOST_URL:?}" "${SONAR_TOKEN:?}" "${SONAR_PROJECT_KEY:?}"

sln=$(find /src -maxdepth 1 \( -name '*.slnx' -o -name '*.sln' \) | head -n 1)
if [ -z "$sln" ]; then
  echo "scan: no solution file in /src — nothing to analyse" >&2
  exit 3
fi

echo "scan: copying source to /work"
rm -rf /work && mkdir -p /work
# Skip build output and node_modules; keep everything else, hidden files included.
tar -C /src --exclude='./**/bin' --exclude='./**/obj' --exclude='./**/node_modules' -cf - . | tar -C /work -xf -
cd /work
sln="/work/$(basename "$sln")"

echo "scan: sonarscanner begin ($SONAR_PROJECT_KEY)"
dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /n:"${SONAR_PROJECT_NAME:-$SONAR_PROJECT_KEY}" \
  /v:"${SONAR_BRANCH_LABEL:-local}" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.exclusions="**/wwwroot/**,**/Migrations/**,**/*.min.js,**/*.min.css" \
  /d:sonar.scanner.scanAll=false \
  /d:sonar.qualitygate.wait=false

echo "scan: dotnet build"
dotnet build "$sln" -c Release --nologo -clp:ErrorsOnly

echo "scan: sonarscanner end"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
echo "scan: done"
