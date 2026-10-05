#!/usr/bin/env bash
# Publisher-owned deployment only. No game checkout, Discord helper or Docker writes.
set -euo pipefail
repo=$(realpath "$1")
runtime=$(realpath "$2")
content=$(realpath "$3")
test -f "$repo/backend/download_broker.py"
test -f "$runtime/manifest.json"
test -f "$content/manifest.json"
test -s /home/dml/.config/jason-games/github-read.token
sudo -n true
if ! id hawkskater-download >/dev/null 2>&1; then
    sudo useradd --system --no-create-home --shell /usr/bin/nologin hawkskater-download
fi
sudo install -d -m 0755 /opt/hawkskater-download-broker
sudo install -d -m 0750 -o root -g hawkskater-download /etc/hawkskater /var/lib/hawkskater /var/lib/hawkskater/release /var/lib/hawkskater/skate-content
sudo install -d -m 0700 -o hawkskater-download -g hawkskater-download /var/lib/hawkskater/download-cache /var/lib/hawkskater/sessions
for file in download_broker.py discord_login.py lifecycle_api.py bounded_http.py; do
    sudo install -m 0644 "$repo/backend/$file" "/opt/hawkskater-download-broker/$file"
done
sudo install -m 0640 -o root -g hawkskater-download "$runtime/manifest.json" /var/lib/hawkskater/release/manifest.json
sudo install -m 0640 -o root -g hawkskater-download "$content/manifest.json" /var/lib/hawkskater/skate-content/manifest.json
if test -f /etc/hawkskater/config.json; then
    echo 'Existing publisher config preserved; review changes before rerunning.' >&2
    exit 1
fi
sudo install -m 0640 -o root -g hawkskater-download "$repo/backend/hawkskater-config.example.json" /etc/hawkskater/config.json
sudo install -m 0644 "$repo/backend/hawkskater-download-broker.env" /etc/hawkskater-download-broker.env
sudo chmod 600 /home/dml/.config/jason-games/github-read.token
for unit in hawkskater-download-broker.service hawkskater-session.service; do
    sudo install -m 0644 "$repo/backend/$unit" "/etc/systemd/system/$unit"
done
sudo systemctl daemon-reload
sudo systemctl enable --now hawkskater-session.service hawkskater-download-broker.service
systemctl is-active hawkskater-session.service hawkskater-download-broker.service
echo 'Public downloads and attach-only sessions installed on loopback. Discord helper unchanged.'
