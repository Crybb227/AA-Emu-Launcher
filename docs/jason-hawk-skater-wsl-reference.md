# JasonHawkSkater — legacy WSL integration reference (not the player release)

The initial supported platform is **Windows with WSL2 distro `dml-arch`, WSLg,
Vulkan Dozen, and the existing Arch runtime**. Native Windows benilla is experimental
upstream and has not been verified here. A clean game installation means a separate
game folder on this configured WSL host; it does not mean a clean Windows machine.

Open **JasonHawkSkater** in the launcher header. Choose an absolute WSL installation
folder, then a locally prepared release folder or an authenticated HTTPS manifest URL.
Install/update and Repair verify every distributable file's size and SHA-256. Progress
appears in the log and progress bar. The initial local package is
`/home/dml/games/hawkskater-release-2026.10.05.1`.

## Assets

The **Get WoW 1.12.1 assets** link opens the verified project page:
https://www.stonetavern.app/clients (checked 2026-10-04). This link is not permission to
redistribute its contents. Import your existing ZIP or Data folder. Import validates
the 12 required English MPQs, their headers, file sizes and complete build-5875 hashes
recorded in `HawkSkater/wow-5875.json`. These hashes were measured from the supplied
Stonetavern Enhanced 1.4 installation. Other locales and modified base/patch archives
are rejected until explicitly validated. Unverified extra patch MPQs are not imported.
No WoW executable is needed for validation.
ZIP import writes only MPQ basenames into a new asset generation; traversal, links,
duplicates, and oversized imports are rejected. The source remains read-only.

Skate 3 import takes the user's extracted Xbox 360 folder containing `default.xex`
and `data/`. Extract a user's own disc first if supplied as ISO. It runs the reference
converter and audio converter, validates their outputs, then activates a new asset
generation. On this initial deployment conversion uses the existing reference
`.setup/skate-engine`, its RefPack decoder, and `.setup/bin/vgmstream-cli`. Python
dependencies are installed into a game-local conversion venv from pinned binary
wheels (NumPy 2.5.3, Pillow 12.3.0, verified with Python 3.14). Conversion source is
pinned upstream to skate engine `cb79689` and converter revision
`adb684e78ec07654f00e2a9da88d32ae02cc68bd` by the reference setup.

There is no downloadable proprietary asset pack configured. WoW MPQs, Skate 3 source
files, and converted Skate 3 assets remain local. Asset repair revalidates WoW files;
restoring proprietary assets requires reimporting the user's source. Client Repair
restores distributable files from the configured release and preserves imported assets.

## Runtime audit and package

The audited Rust client reads `WOW_DATA` (MPQs), `WOW_SKATE_ASSETS`
(`skate-data/assets`, including private model/game/stock files and animation banks),
and `WOW_SKATE_AUDIO` (converted WAVs). It writes only `BENILLA_HOME`, set to the
installation's persistent `benilla-config`. Shaders and the client's own UI layer are
embedded in the binary. The package script copies an explicit allowlist:

- `bin/benilla` from the working `target/release/benilla`.
- WSL ALSA configuration and the Windows XInput forwarding / Linux evdev bridge.
- The project's audio conversion scripts and its licence/NOTICE files.

No `WoW.exe`, Stonetavern loader, DLLs, Interface addons, game assets, credentials,
account state, logs, or arbitrary files from the source ZIP are packaged.
`ldd` identified ALSA, udev, libgcc, libm, libc and the ELF loader as host dependencies.
WSLg/X11, Vulkan Dozen, ALSA Pulse plugin, evdev Python support and `/dev/uinput`
permissions are runtime prerequisites provided by the configured reference host.
The bridge currently uses the reference evdev venv and its existing passwordless
sudo permission; deployments must provision that narrowly scoped helper deliberately.
If the bridge cannot start, keyboard controls remain available; physical XInput input
on another machine has not been certified by the unattended tests.

Create a fresh package under WSL:

```sh
python3 scripts/package-hawkskater.py --output /path/to/new-release --version 2026.10.05.1 --flavor player
```

The manifest schema is 1, platform `wsl-dml-arch-x86_64`; each entry has relative path,
size, SHA-256, and executable flag. The player binary was built with
`cargo build --release -p benilla --no-default-features` and verified by the live
login/board test. The build completed with four existing unused-import warnings
in upstream source; no Rust source was changed.
The pinned mashup dependency has an Apache-2.0 licence at its repository root; the
package includes that licence and NOTICE plus dependency licence texts and metadata
(548 metadata records in this build). The original converter checkout has no root
licence file and is not included in the distributable package. It remains a locally
supplied conversion prerequisite. Do not upload proprietary assets or configure a
downloadable asset pack without confirmed redistribution rights.

Downloads use authenticated HTTPS byte ranges and persistent partial files. They are
verified before activation. Releases are staged into fresh immutable directories;
an atomic `current.json` replacement activates the complete generation. Old releases
remain available for diagnosis. Install/repair/launch take an exclusive process lock.
Settings, credentials and user assets are outside the release manifest allowlist.

Build/install the Windows launcher:

```powershell
dotnet build AAEmu.Launcher/AAEmu.Launcher.csproj -c Release --no-restore
.\scripts\Install-JasonGamesLauncher.ps1
```

The installer preserves existing launcher settings and excludes settings from used
build output. Newly installed defaults clear saved login fields and user history from
the repository's templates. Hawk settings live in `%LOCALAPPDATA%/JasonGamesLauncher/hawk.json`;
game account/password and launcher access token live in Windows Credential Manager. Secrets
reach WSL through process stdin and the child's environment, never shell arguments.
No repository credential is present in the launcher.

## Server lifecycle

Only the Discord helper may wake the server: use `/wake skatecraft` or its
“Start JasonHawkSkater” button first. The launcher never invokes Docker startup.
Without an HTTPS lifecycle URL, launch checks auth/world TCP readiness and
connects to `127.0.0.1:13724` (world `127.0.0.1:18085`), or reports that the server
must be started through Discord. This shared server remains running when the game exits. A local session record is renewed
every 20 seconds and removed on exit.

With an HTTPS lifecycle URL and launcher token, the launcher uses the adapter contract:

| Request | Payload | Response |
| --- | --- | --- |
| POST `/v1/attach` | `session_id` UUID | `ready`, `phase`, `auth_address`, `world_address` |
| POST `/v1/heartbeat` | same UUID | same readiness information |
| POST `/v1/release` | same UUID | acknowledgement |

All requests carry `Authorization: Bearer <launcher user token>`. Game passwords are
never sent to the lifecycle API. The implementation in `backend/lifecycle_api.py`
attaches only to an already-ready Compose project, stores per-user leases,
expires them after 90 seconds, and rechecks access revocation. It does not stop shared
servers. Configure the realm's advertised address for remote players on the server;
changing only the auth endpoint cannot rewrite the realm-list address.

Backend helpers bind loopback. Deploy them behind HTTPS with the desired user identity
provider, configure user token hashes and paths using `hawkskater-config.example.json`,
and point `HAWK_LIFECYCLE_CONFIG` at a private configuration file. No production domain
or user token is invented. Server failures surface clearly; failed release requests rely
on the server lease timeout. J summons/dismisses the board; K changes the camera;
Xbox-style XInput controllers provide Skate 3 input through the bridge.

The existing control plane at
`/home/dml/games/server-on-demand/skatecraft-on-demand/controller.py` implements this
attach/heartbeat/release contract, including renewing expired leases by attaching
again with the same UUID. Provision `can_launch: true` only on the dedicated Discord
identity, never on launcher identities. `/v1/launch` is reserved for Discord;
launcher `/v1/attach` cannot wake a stopped stack. Use its deployed HTTPS endpoint for remote hosting; its own
documentation covers player-protected idle shutdown, backups and migration. The
startup-only helper in this launcher repository is the local integration harness and
does not replace that controller or migrate its configuration.

## Restricted GitHub releases

Keep the release repository private. GitHub's release asset API supports server-side
GitHub App installation tokens or fine-grained tokens with **Contents: read** scoped
to that repository:
https://docs.github.com/en/rest/releases/assets#get-a-release-asset

`backend/download_broker.py` exposes `/v1/manifest` and `/v1/files/<allowlisted asset ID>`.
Set `HAWK_DOWNLOAD_CONFIG` to a private config and `HAWK_GITHUB_TOKEN` through the
backend's secret store. Map each manifest path to a release asset ID. It verifies
GitHub content against the manifest, strips repository authorization on GitHub storage
redirects, streams bytes itself, supports Range, and checks launcher user revocation
on requests and during streaming. It never sends the repository token or GitHub's
signed storage URL to the launcher. Remove/rotate a user token hash to revoke access.
Do not enable public caching on the proxy; backend caches remain inaccessible directly.
The supplied `hawkskater-download-broker.service` runs as a dedicated unprivileged user
with a private file-creation mask and a single writable cache directory. Provision its
environment file privately (mode 600) and configure the existing HTTPS reverse proxy;
do not commit the environment file or expose the loopback service directly.

Downloaded files can be inspected or copied by the person running the launcher.
Revocation can stop future downloads and sessions; it cannot erase bytes already
downloaded. Restricted distribution protects repository credentials and future access.

## Verification

Run `scripts/test-hawkskater.py` and `scripts/test-download-broker.py` under WSL;
run `scripts/Test-LauncherPresentation.ps1` on Windows. The local verification script
`scripts/verify-hawkskater-local.py` installs into a fresh folder, imports WoW assets,
converts Skate 3, creates a temporary certificate-verified HTTPS lifecycle API, requests
an already-running Compose server (start through Discord first), waits for readiness, logs in using only `.probe-identity`,
activates the board, verifies heartbeats and clean release, and retains client logs.
It uses an invisible Xvfb display, unattended/silent mode and GM-off checks.
The shared server is left running. Do not substitute a player's account.
The metadata-only inventory in `artifacts/JasonHawkSkater-runtime-asset-inventory.json`
lists all 2,198 reference runtime asset files by group, relative path, size and SHA-256.
It contains no asset bytes, settings, account files or credentials.

Remote deployment, private GitHub retrieval and a cold server start are not claimed
verified until a production endpoint, authorized release repository and permitted
release assets are provided. No assets have been uploaded.
