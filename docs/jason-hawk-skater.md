# JasonHawkSkater player launcher

The player runtime is native **Windows x64**, installed automatically into
`%LOCALAPPDATA%\JasonGamesLauncher\Games\JasonHawkSkater`. Players do not need WSL,
Rust, Python, Docker, a developer checkout, release URLs or manually entered tokens.
The [legacy WSL integration](jason-hawk-skater-wsl-reference.md) is not the player release.

## Playing

1. Select **JasonHawkSkater** in the normal launcher game header and click its sidebar
   **Install / Play** button. The game uses the same in-place dashboard/location
   pattern as JasonWoW; no separate game window or publisher fields are opened.
2. First use downloads Stonetavern Classic 1.8 directly from the verified publisher
   link, checks its pinned 5,321,185,674-byte size and SHA-256, and imports only the
   required WoW MPQs. The archive is cached for resumable retries. Import your own
   required Skate content automatically from the publisher pack. Optional own-file
   import/conversion remains available in Settings.
3. Start the server through `/wake skatecraft` or **Start JasonHawkSkater** in the
   Discord helper. **Play cannot wake the server**; it attaches to a ready/booting
   server, maintains your session, and releases it when the client exits.
4. Afterwards, **Play** checks updates and launches the game. **J** toggles the board,
   **K** changes camera. Keyboard skating uses **W/S** to push/brake, **A/D** to
   steer, **Space** held then released to ollie, arrow keys for trick-stick input,
   and **R** to recover at the current position after a bail. Xbox/XInput
   controllers still use native Windows input. Keyboard skating pauses while
   typing and while the window is unfocused; release held keys before resuming.

Keyboard bindings live in `<installation>/benilla-config/skate-keyboard.toml`
and are created on first launch. Edit while the game is closed, then restart.
Use `KeyA`–`KeyZ`, `ArrowLeft`, `ArrowRight`, `ArrowUp`, `ArrowDown`, `Space`,
`ShiftLeft`, or `ControlLeft`; each action must have a distinct key. Invalid
settings produce a log warning and defaults without overwriting the file.
The fork extension and pinned engine-input adaptation are reproducible through
`scripts/hawk-keyboard/Apply-Keyboard.ps1`; the working WSL reference is unchanged.

Settings holds the optional Windows install-location picker, Repair, asset replacement,
the WoW download page, and launcher sign-out. Game account optionally saves the game
login in Credential Manager; game passwords are never sent to the download/sign-in
gateway. Windows preferences use `hawk-windows.json`, leaving the WSL profile and
existing game credentials intact. Changing location leaves the previous installation
untouched; it does not silently move or delete it.

## Assets and runtime

The verified WoW page is https://www.stonetavern.app/clients. Install / Play downloads
https://downloads.stonetavern.app/clients/classic-1.12.1/all/1.8/Stonetavern-Classic-1.12.1-v1.8.zip
with published SHA-256 `0ebb9b5f0386d547e0715fdae57955743dd06e991b507ca5a7729d14af0479fc`.
The download is not the native Rust client and does not contain Skate 3 assets. Never
send launcher access tokens to this public asset mirror. You can also import an existing
ZIP or Data folder. Only the 12 English 1.12.1/build-5875 MPQs in `wow-5875.json` are copied,
after exact size/SHA-256 verification. The original remains read-only. No WoW executable,
source-ZIP DLLs, addons or unverified extra patch MPQs are copied. Traversal, duplicate
MPQs, links and oversized ZIP entries are rejected. The audited Classic 1.8 patch-2.MPQ
size/hash is allowlisted alongside the previous Enhanced 1.4 variant so older imported
assets remain valid. Other MPQs match the reference hashes exactly.

Skate 3 import accepts an extracted Xbox 360 folder with `default.xex` and `data/`,
or the user's converted `skate-data` and `skate-audio` folders. The standalone Windows
converter includes its Python/numpy/Pillow runtime and native RefPack decoder; it never
consults a developer installation. It converts the client-required character,
animation/state/input/physics data and WoW-mode audio. The client derives board/rig
JSON from the imported GLB on first activation. Extract your own disc before importing;
ISO extraction is not integrated into this launcher yet.

The converter uses SK8-ENGINE/skate-3-rust-engine source pinned to
`4488651c35c44365faa1ba38eed5758b6ebde714`, with its explicit GPL-3.0-only licence.
The old unlicensed converter checkout is not shipped. Conversion source/build scripts,
vendor notices and Python/Rust dependency licences accompany the package. vgmstream
r2117 codec binaries are unmodified upstream tools, not DLLs from the user's game ZIP.
Final third-party codec/source obligations must be reviewed before distribution.
The publisher confirmed redistribution rights for necessary Skate 3 content on
2026-10-05. `package-skate-content.py` selects 67 files (38,286,919 uncompressed bytes):
the embedded skater/board GLB, game manifest, two animation banks/state graphs,
physics collections, input/gesture mapping, camera graph/shakes and board audio.
No WoW files, raw Xbox executable, unused world maps or user configuration are included.
The manifest records the reason, size and SHA-256 for each file. This records the
publisher's confirmation, not an independent legal determination.

Imported generations carry local per-file integrity metadata. Repair restores
distributable runtime and publisher Skate files, and checks user-imported assets.
User imports require reimport if damaged and are never replaced by publisher packs.
Skate updates use staged generations and atomic `skate.json` activation; each Play
checks the authenticated `skateManifestUrl`, or the bundled pack in local previews.
Updates preserve assets, credentials and `benilla-config`.
Downloaded files are inspectable/copyable locally. Revocation prevents future access;
it cannot erase bytes already downloaded or remove GPL recipients' licence rights.

## Updates

Manifest schema `1`, platform `windows-x86_64`, version and explicit file
paths/sizes/SHA-256 hashes. Only `bin/`, `runtime/`, `tools/`, and `licenses/` are allowed.
Traversal, reserved Windows names, alternate streams, duplicates, links and sizes are
checked. Launch also requires an x64 Windows PE client.

HTTPS downloads use persistent partial files and byte ranges. Ignored
Range safely restarts the file. Checksums precede activation; only same-origin redirects
may retain user authorization. Immutable staged releases become active through atomic
Windows `File.Replace` of `current.json`. A process lock excludes concurrent install,
repair and play. Valid unchanged releases are not recopied on each Play. Old generations
remain for recovery; the installer never silently prunes a player's installation.

## Publisher configuration, not player fields

The intended repository **Crybb227/skatecraft** was verified private and accessible
through publisher access. The audited Skate ZIP is uploaded to the private prerelease
[skate-content-2026.10.05.1](https://github.com/Crybb227/skatecraft/releases/tag/skate-content-2026.10.05.1).
Asset ID `613491723`, archive size `20281425`, SHA-256
`c54cf099dd95b7317977d1a8f5174f2edd91019c0be68d2886fd925600828833`.
GitHub metadata and an actual download through the broker fetch code both matched.
The repository remained private before/after upload. WoW assets were not uploaded.
The keyboard-enabled native runtime is in private prerelease
`client-windows-2026.10.05.6`, asset ID `613729452`, 88,194,187 archive bytes,
SHA-256 `1487942b011a1a3dcd30883e7aa86b62f4342a123370cf45f5906361a8c4b491`.

The broker exposes `/v1/skate/manifest` and hash-allowlisted `/v1/skate/files/<sha256>`.
`skateManifest`/`skateArchive` are separate from the native runtime release, so no
Skate or WoW bytes are added to its `bin/runtime/tools/licenses` manifest.
Production GitHub access uses a fine-grained Contents:read credential restricted to
this private repository, saved by the publisher in
`/home/dml/.config/jason-games/github-read.token` (directory 0700, file 0600), passed
to the broker via systemd `LoadCredential`. It is never shipped or passed to players.
Publisher upload/readback tests use existing Git Credential Manager access transiently;
that broader publisher credential is not deployed to the backend.

The publisher chose **everyone may download** on 2026-10-05. `publicDownloads: true`
serves only allowlisted files without a player token; the GitHub repository and its
credentials remain private. This is public distribution of selected files, not
per-user restricted distribution. Set it false to disable anonymous future downloads;
already downloaded files cannot be revoked or hidden. Optional authenticated mode
remains available, but Discord OAuth is not required/configured for this deployment.

The broker and separate attach-only session service are installed in WSL as
`hawkskater-download-broker.service` (127.0.0.1:18766) and `hawkskater-session.service`
(127.0.0.1:18765), using an unprivileged account with no Docker group membership.
TCP readiness probes never run Docker/start commands. Public sessions use an
unguessable per-launch ownership secret, expire after 90 seconds without heartbeats,
and cannot be renewed/released with another secret. There is a session-count cap,
64-worker bound, socket timeouts and systemd resource limits. The broker proxies only
attach/heartbeat/release to the fixed session service, never to the Discord controller.
The actual scoped credential passed private release readback and checksum checks.

`backend/deployment.production.example.json` uses **games-api.jmservers.com**.
The Cloudflare hostname now serves the public manifests over HTTPS. Trusted HTTPS
protects the download/session traffic. It does not replace the
game ports 13724/18085. The server must advertise a remotely reachable world address;
the launcher cannot rewrite the realm list.

Ship reviewed manifest/lifecycle/auth URLs and an optional Discord invite in
`HawkSkater/deployment.json`. The checked-in default now uses the verified public
download/attach gateway with no player sign-in. The configured game addresses are
`24.16.12.90:13724` and `:18085`; the server is woken only through Discord.
Launcher v0.6.0.2 is the publisher-authorized launcher release. External-PC,
physical-controller/audio testing and the publisher's final third-party
distribution review remain outstanding; release publication is not evidence
that these acceptance checks have passed.

The player HTTPS gateway exposes `/v1/attach`, `/v1/heartbeat`, `/v1/release`, and public
allowlisted downloads. Wake/admin routes are not exposed. In the companion controller,
only the dedicated Discord identity has `can_launch: true`; launcher identities never
receive it. Expired sessions reattach, not wake. Discord startup and production idle
shutdown remain the existing controller's responsibility.

### Cloudflare hosting

The publisher manages `jmservers.com` through Cloudflare. Run the tunnel connector
inside the same `dml-arch` instance as the broker/controller. The publisher-only
`backend/cloudflared.example.yml` exposes exactly the player routes and returns 404
for other paths, including wake/admin. It needs no publicly opened API/router port.
Cloudflare provides public HTTPS; the connector forwards to loopback services.

For the already running remotely managed tunnel, add a published application route:
hostname `games-api.jmservers.com`, service type HTTP, URL `127.0.0.1:18766`.
The broker itself is path-restricted and contains no server-start route. Keep tunnel
credentials private and out of the launcher/repository. DNS authorization is a
publisher action, not a player setup step. Do not change the existing Discord helper.

Use Cloudflare cache-bypass rules for all `games-api.jmservers.com` paths and rate-limit
download/session requests. Do not put an interactive Cloudflare Access login in front
of native requests: this deployment intentionally permits everyone to download.
Do not expose the WSL filesystem or SSH. Game TCP ports remain separate from
this HTTPS tunnel; a Windows game client must not require cloudflared itself.

Official setup: https://developers.cloudflare.com/tunnel/features/locally-managed-tunnels/create-local-tunnel/

The publisher created the remotely managed `jason-games` tunnel in Cloudflare.
Its connector was installed on 2026-10-05 as `jason-games-tunnel.service` in
`dml-arch`, using official cloudflared 2026.10.0 with verified upstream SHA-256.
The unprivileged service reads its token through systemd `LoadCredential`; no
token is stored in the service command, repository or launcher. The original token
file is mode 600 inside a mode-700 `.cloudflared` directory. Loopback connector
readiness returned HTTP 200 with four active Cloudflare connections.

For this remotely managed tunnel, configure the ingress in Cloudflare, not the
locally managed YAML template. Route the hostname only to the path-restricted broker
on port 18766, never to the Discord controller on port 18990.
The connector's metrics endpoint is private at `127.0.0.1:20243`. No hostname DNS
route was enabled during connector installation. The system service starts with
the WSL distro; availability still depends on this computer and WSL remaining up.

### Optional authenticated mode (disabled for public downloads)

`backend/discord_login.py` implements an application-specific device handoff around
Discord's authorization-code flow, not Discord's native OAuth device grant:

- POST `/v1/auth/device` returns a five-minute random device code and browser link.
- Browser authorization requests `identify guilds.members.read`. A secure HttpOnly
  cookie binds the browser to single-use OAuth state. OAuth secrets stay server-side.
- The callback checks guild membership, optional allowed roles and an explicit allowed
  Discord user. It grants no server wake/admin permission.
- POST `/v1/auth/token` polls with the secret device code and returns that user's narrow
  launcher token once. Discord and GitHub tokens are never returned to the player.

Register `https://games-api.jmservers.com/v1/auth/callback` in the Discord application.
Set `HAWK_DISCORD_CLIENT_SECRET` in the broker's protected environment file. Configure
`discordClientId`, `discordGuildId`, optional `discordRequiredRoles` and `discordUsers`
in private backend config. Each allowed Discord ID maps to `{user, token_file}`.
Generate separate random user tokens, keep files mode 600 on the server, and provision
their SHA-256 hashes in broker `users` and controller `identities`, without `can_launch`.
Never ship bot, OAuth, GitHub or per-user secrets in a launcher/repository.

Revoke access in both the broker users/Discord allowlist and controller identities,
or rotate the token/hash in both. Broker authorization is reloaded for each request
and streaming chunk. Reload/restart the companion controller as its configuration
requires. Set proxy rate limits for device issuance/polling. Never log OAuth query codes.

### Private download service

`download_broker.py` binds loopback behind HTTPS. Set `HAWK_GITHUB_TOKEN` to a server-side
GitHub App or fine-grained Contents:read credential scoped to this repository alone.
Map each manifest file to its allowlisted release asset ID. The broker verifies upstream
sizes/hashes, caches privately, strips repository authorization on GitHub storage
redirects and serves authenticated resumable bytes itself. Deploy `discord_login.py`
alongside it. No repository credential or signed GitHub storage URL reaches players.

For one packaged runtime ZIP, set `archive` to `{asset_id, size, sha256}` and leave
`assets` empty. The broker downloads/verifies that allowlisted ZIP once and exposes
each manifest file by hash. It rejects unlisted entries, duplicates, traversal and
links, and verifies each selected file before caching it; it never uses extractall.
This avoids uploading thousands of individual licence/runtime files as release assets.

Use `hawkskater.nginx.example.conf` and the service/config examples as reviewed templates,
not automatic production installation. The publisher confirmed that hosting is on this
Windows computer, in `dml-arch`; players must not access its WSL filesystem. The existing
Discord helper and on-demand controller are running there. The public HTTPS gateway
still requires DNS/certificate setup and a secure tunnel or router forwarding to this
computer, Discord OAuth application/guild configuration and a server-only GitHub
credential. No DNS, router rules or public gateway have been changed.

## Build and tests

```powershell
.\scripts\Build-HawkWindows.ps1
.\scripts\Build-HawkConverter.ps1
.\scripts\Test-HawkNative.ps1
python scripts/test-discord-login.py
python scripts/test-download-broker.py
```

Build workspaces are isolated under `%LOCALAPPDATA%\JasonGamesLauncher\Build`; no user
PATH changes are made. The native client uses an asset-free source archive of the audited
reference and `cargo build --locked --release -p benilla --no-default-features` with MSVC.

`package-hawk-windows.py` prepares the versioned native runtime/converter manifest.
`Publish-HawkLauncher.ps1 -ClientRelease <folder> -SkateContent <audited-folder>`
bundles a local test runtime and authorized Skate pack;
`-DeploymentFile <reviewed-json>` supplies production addresses. New installer defaults
clear saved logins; reinstall preserves existing settings. Outputs remain local until
release approval.

Native tests cover clean install, corruption/atomic pointer, settings preservation,
unsafe paths/reserved names/case collisions, wrong platform/assets, download resume,
ignored Range and credential redirect refusal. Form tests cover Credential Manager and
absence of developer fields. OAuth tests cover browser binding, single use, role denial,
expiry and revocation. Do not claim clean-second-PC, physical-controller or deployed
private-service acceptance until those tests run against real infrastructure/hardware.

### Local native acceptance (2026-10-05)

The complete Windows package installed in a fresh test-owned directory. The standalone
Windows converter processed the owner's extracted Skate 3 files. Imports validated
the required WoW MPQs and copied the converted content; neither originals nor player
settings were modified. Long runtime and owned-asset paths have regression coverage.

The native client authenticated and entered the world on the actual server started
through Discord, then activated the board. It used the NVIDIA GeForce RTX 3080 Ti
Laptop GPU via native Vulkan, without WSL graphics/input bridges. A loopback HTTPS
test-only attach service recorded heartbeats and clean release, with no sessions or
client lease left behind. This verifies the launcher protocol, not deployment of the
public gateway or the companion controller's private authentication mapping.

Run `scripts/Test-HawkNativeLive.ps1 -ClientRelease <native-release-folder>` for a
fresh test. Its declared probe account is read only by the test harness, never bundled
or stored in player credentials. Detailed logs remain in the test-owned NativeLive
workspace. The test closes only its own game client and never wakes/stops the server.

### Reduced pack acceptance (2026-10-05)

The actual 67-file publisher pack and native Windows binary passed clean installation,
per-file verification, atomic asset repair and settings preservation. A live private
GitHub download then passed through an authenticated, pinned-certificate loopback TLS
broker into a fresh native installer, including repair. Run
`python scripts/test-private-skate-install.py --app <published-App> --receipt <publisher-receipt>`
for this publisher-only test; its transient GitHub credential is never deployed or
bundled. Download authorization/ranges/revocation and in-place dashboard tests passed.

The reduced 67-file pack subsequently passed native world entry and board activation
against the actual Discord-started server, with one attach, three heartbeats, one
release and no remaining sessions/client lease in the pinned-certificate test gateway.
The production broker and session services are running, and the public HTTPS manifests
respond without player authentication. Preview 2026.10.05.9 uses these remote URLs
without bundled runtime/assets or a sign-in prompt. A fresh native installation through
the actual public HTTPS gateway passed all runtime/Skate checksums, atomic repair and
settings preservation. That downloaded client entered the world using the audited
Classic 1.8 MPQs, activated the board, maintained its production HTTPS session and
exited cleanly with no local lease or remaining gateway session. The packaged launcher
installer also passed a clean destination/configuration check. Physical controllers,
audible output, a second external PC and other GPUs are not yet acceptance-tested.

### Keyboard test candidate

Launcher `0.6.0.2-rc1` is a local hands-on candidate, not a published launcher
tag/release. It bundles native runtime `2026.10.05.5` with keyboard skating;
its preview-only configuration selects that local runtime while keeping the
production HTTPS Skate downloads and attach-only sessions. The normal public
runtime manifest remains unchanged until player acceptance.

Five isolated tests of the exact keyboard module passed: binding validation,
push/brake arbitration, charge/release cancellation, physical-controller
handoff, and board/camera/recovery edges. The authorized-pack real-engine test
measured 39.94 m travel at 8.50 m/s under W, braking to 0.025 m/s under S,
a 1.17 m ollie, 3.85 m lateral travel while steering, and recovery to
`PhysicsGround` with horizontal momentum reset. Vertical gravity/contact
velocity after recovery is expected; the test does not require gravity to stop.

The corrected native binary then passed actual server world entry, board
activation, session heartbeats and clean release in the pinned-certificate
acceptance gateway, after the publisher woke the server externally in Discord.
Installer tests also verified custom `skate-keyboard.toml` survives updates.
Native key-event feel, typing/focus interaction and physical controller feel
still need hands-on player acceptance. The publisher subsequently authorized
the launcher `v0.6.0.2` tag/release without claiming these checks had passed.
