param([Parameter(Mandatory=$true)][string]$Source, [Parameter(Mandatory=$true)][string]$CargoHome)
$ErrorActionPreference = 'Stop'
# Only the launcher's isolated, revision-pinned build checkout is patched.
function Replace-Once([string]$Path, [string]$Before, [string]$After) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    if (($text -replace '[\s,]+', '').Contains(($After -replace '[\s,]+', ''))) { return }
    if (-not $text.Contains($Before) -or $text.IndexOf($Before) -ne $text.LastIndexOf($Before)) {
        throw "Keyboard overlay does not match pinned source: $Path"
    }
    [IO.File]::WriteAllText($Path, $text.Replace($Before, $After), [Text.UTF8Encoding]::new($false))
}
$app = Join-Path $Source 'crates\benilla-app\src'
$keyboardSource = Join-Path $PSScriptRoot 'keyboard.rs'
$keyboardTarget = Join-Path $app 'skate\keyboard.rs'
if (-not (Test-Path -LiteralPath $keyboardTarget) -or (Get-FileHash -LiteralPath $keyboardSource).Hash -ne (Get-FileHash -LiteralPath $keyboardTarget).Hash) {
    Copy-Item -LiteralPath $keyboardSource -Destination $keyboardTarget -Force
}
Replace-Once (Join-Path $app 'local_state.rs') @'
/// `benilla-config/config.toml`: the CVar overrides, the `Config.wtf` analog.
pub(crate) fn config_path() -> Option<PathBuf> {
'@ @'
/// Fork-only keyboard settings; separate from the stock 1.12 CVar diff.
pub(crate) fn skate_keyboard_path() -> Option<PathBuf> {
    home().map(|h| h.join("skate-keyboard.toml"))
}

/// `benilla-config/config.toml`: the CVar overrides, the `Config.wtf` analog.
pub(crate) fn config_path() -> Option<PathBuf> {
'@
$mod = Join-Path $app 'skate\mod.rs'
if (-not ([IO.File]::ReadAllText($mod).Contains('mod keyboard;'))) {
    Replace-Once $mod 'mod export;' "mod export;`nmod keyboard;"
}
Replace-Once $mod '    Suspend,' "    Reset(u64, Vec3, f32),`n    Suspend,"
Replace-Once $mod '            Job::Suspend => {' @'
            Job::Reset(e, spawn, heading) => {
                if let Some(s) = session.as_mut().filter(|_| e == epoch) {
                    accumulated = 0.0;
                    let p = s.activate(spawn.to_array(), heading)?;
                    let _ = publish.send(Reply::Pose(epoch, frame(p)));
                }
            }
            Job::Suspend => {
'@
Replace-Once $mod '    keys: Res<ButtonInput<KeyCode>>,' "    keys: Res<ButtonInput<KeyCode>>,`n    mut keyboard: Local<keyboard::Keyboard>,"
Replace-Once $mod 'keys.just_pressed(KeyCode::KeyK)' 'keyboard.camera(&keys)'
Replace-Once $mod 'keys.just_pressed(KeyCode::KeyJ)' 'keyboard.board(&keys)'
Replace-Once $mod "    if !drive.active {`n        return;`n    }" "    if !drive.active {`n        keyboard.clear();`n        return;`n    }"
Replace-Once $mod '    let input = host.transport.lock().unwrap().poll();' @'
    let focused = windows.single().is_ok_and(|window| window.focused);
    let allowed = focused && !ui.typing;
    if allowed && keyboard.reset(&keys) {
        keyboard.clear();
        if let Some(send) = &host.send {
            // Fork recovery: reset at current feet, never teleport to a saved server position.
            let _ = send.send(Job::Reset(host.epoch, to_skate(drive.pos + Vec3::Y * 0.1, host.origin), drive.yaw + std::f32::consts::PI));
        }
    }
    let physical = host.transport.lock().unwrap().poll();
    let pad = physical.controller().is_some();
    let input = keyboard.sample(&keys, time.delta_secs(), allowed, physical);
'@
Replace-Once $mod '    let pad = input.controller().is_some();' '    // pad availability describes physical hardware, not the keyboard packet.'
Replace-Once $mod 'false => warn!("skate: no controller found; the board needs an Xbox-style pad"),' 'false => info!("skate: keyboard controls available; W/S push/brake, A/D steer, Space ollie, arrows tricks, R recover"),'
$engine = Join-Path $CargoHome 'git\checkouts\2010-rust-rewrite-mashup-f18be4cebdaada4c\2af4154\skate\crates\skate-host\src\physics\bridge.rs'
Replace-Once $engine "impl InputFrame {`n" @'
impl InputFrame {
    /// JasonHawkSkater fork: feed digital keyboard input through the same raw Pad converter.
    pub fn from_controls(controls: Controls, number: u32) -> Self {
        let mut samples = std::array::from_fn(|_| Err(crate::input::platform::DeviceError::Disconnected));
        samples[0] = Ok(crate::input::platform::DevicePacket {
            number, subtype: 1,
            state: skate_core::input::xbox::XboxState {
                buttons: controls.buttons, triggers: controls.triggers,
                left: controls.left, right: controls.right,
            },
        });
        Self { samples }
    }

'@
