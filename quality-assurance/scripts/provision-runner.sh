#!/usr/bin/env bash
# One-time setup of the self-hosted runner VM (Ubuntu 22.04/24.04). Run as root:
#   sudo bash quality-assurance/scripts/provision-runner.sh [runner-user]
#
# Assumes Docker Engine, gh and the GitHub Actions runner service are already installed (see README).
# Installs: kernel limits for SonarQube, jq/gettext, Node.js 22, Playwright's Chromium + OS deps,
# Claude Code for the runner user. Prints the manual steps that remain (auth + secrets).
set -euo pipefail
RUNNER_USER=${1:-runner}
NODE_MAJOR=22
PLAYWRIGHT_VERSION=1.64.1
[ "$(id -u)" -eq 0 ] || { echo "run as root"; exit 1; }
id "$RUNNER_USER" >/dev/null 2>&1 || { echo "user $RUNNER_USER does not exist"; exit 1; }
export DEBIAN_FRONTEND=noninteractive

echo "== kernel limits for SonarQube/Elasticsearch"
cat > /etc/sysctl.d/99-sonarqube.conf <<'EOF'
vm.max_map_count=524288
fs.file-max=131072
EOF
sysctl -q --system

echo "== base packages"
apt-get update -qq
apt-get install -y -qq curl ca-certificates git jq gettext-base unzip >/dev/null

echo "== Node.js $NODE_MAJOR"
if ! command -v node >/dev/null || [ "$(node -v | cut -d. -f1 | tr -d v)" -lt "$NODE_MAJOR" ]; then
  curl -fsSL "https://deb.nodesource.com/setup_${NODE_MAJOR}.x" | bash - >/dev/null
  apt-get install -y -qq nodejs >/dev/null
fi
node -v; npm -v

echo "== Playwright Chromium (OS deps as root, browser for $RUNNER_USER)"
npx --yes "playwright@$PLAYWRIGHT_VERSION" install-deps chromium >/dev/null
su - "$RUNNER_USER" -c "npx --yes playwright@$PLAYWRIGHT_VERSION install chromium" >/dev/null

echo "== Claude Code for $RUNNER_USER"
su - "$RUNNER_USER" -c 'curl -fsSL https://claude.ai/install.sh | bash' >/dev/null
su - "$RUNNER_USER" -c 'grep -q ".local/bin" ~/.bashrc || echo "export PATH=\$HOME/.local/bin:\$PATH" >> ~/.bashrc'
# Jobs get PATH from the runner's .path file, not from .bashrc.
runner_path="/home/$RUNNER_USER/actions-runner/.path"
if [ -f "$runner_path" ] && ! grep -q ".local/bin" "$runner_path"; then
  sed -i "1s#^#/home/$RUNNER_USER/.local/bin:#" "$runner_path"
fi
su - "$RUNNER_USER" -c '~/.local/bin/claude --version'

echo "== docker access"
usermod -aG docker "$RUNNER_USER"
su - "$RUNNER_USER" -c 'docker ps >/dev/null' && echo "docker ok"

cat <<EOF

Provisioning done. Remaining manual steps:

1. Claude auth on the runner (pick one):
     - API key: add repository secret ANTHROPIC_API_KEY.
     - Subscription: as $RUNNER_USER run  ~/.local/bin/claude setup-token  and add the result as
       repository secret CLAUDE_CODE_OAUTH_TOKEN.
2. Repository secret GH_PACKAGES_TOKEN (PAT with read:packages for NimbleLoop-Technologies), so the
   app and the Sonar scanner can restore TailwindUi.Blazor. Optional variable GH_PACKAGES_USER.
3. Restart the runner service so it picks up PATH and group changes:
     systemctl restart 'actions.runner.*.service'
4. Disk: SonarQube + .NET images + browsers want ~15 GB free; prune with  docker system prune -af  if tight.
EOF
