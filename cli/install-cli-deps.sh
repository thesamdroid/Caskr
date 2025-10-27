#!/usr/bin/env bash
set -euo pipefail

NODE_VERSION="${NODE_VERSION:-20}"
PLAYWRIGHT_VERSION="${PLAYWRIGHT_VERSION:-1.48.2}"
export DEBIAN_FRONTEND=noninteractive

apt-get update
apt-get install -y --no-install-recommends \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg2 \
    unzip \
    wget

curl -fsSL "https://deb.nodesource.com/setup_${NODE_VERSION}.x" | bash -
apt-get install -y --no-install-recommends nodejs

npx --yes "playwright@${PLAYWRIGHT_VERSION}" install-deps chrome

wget -q https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
apt-get install -y --no-install-recommends ./google-chrome-stable_current_amd64.deb
rm google-chrome-stable_current_amd64.deb

apt-get clean
rm -rf /var/lib/apt/lists/*
rm -rf /root/.npm /root/.cache

if [ -f /opt/google/chrome/chrome-sandbox ]; then
    chmod 4755 /opt/google/chrome/chrome-sandbox || true
fi
ln -sf /usr/bin/google-chrome /usr/bin/chrome || true
