//! JasonHawkSkater fork extension: stock WoW input is unchanged off the board.
//! Digital keys produce Xbox packets for the existing Skate engine, not new physics.
use bevy::prelude::*;
use serde::{Deserialize, Serialize};
use skate_host::bridge::{Controls, InputFrame};

#[derive(Deserialize, Serialize)]
#[serde(default, deny_unknown_fields)]
struct Bindings {
    push: String,
    brake: String,
    left: String,
    right: String,
    trick_left: String,
    trick_right: String,
    trick_up: String,
    trick_down: String,
    ollie: String,
    reset: String,
    board: String,
    camera: String,
}
impl Default for Bindings {
    fn default() -> Self {
        Self {
            push: "KeyW".into(),
            brake: "KeyS".into(),
            left: "KeyA".into(),
            right: "KeyD".into(),
            trick_left: "ArrowLeft".into(),
            trick_right: "ArrowRight".into(),
            trick_up: "ArrowUp".into(),
            trick_down: "ArrowDown".into(),
            ollie: "Space".into(),
            reset: "KeyR".into(),
            board: "KeyJ".into(),
            camera: "KeyK".into(),
        }
    }
}
fn key(name: &str) -> Option<KeyCode> {
    Some(match name {
        "KeyA" => KeyCode::KeyA,
        "KeyB" => KeyCode::KeyB,
        "KeyC" => KeyCode::KeyC,
        "KeyD" => KeyCode::KeyD,
        "KeyE" => KeyCode::KeyE,
        "KeyF" => KeyCode::KeyF,
        "KeyG" => KeyCode::KeyG,
        "KeyH" => KeyCode::KeyH,
        "KeyI" => KeyCode::KeyI,
        "KeyJ" => KeyCode::KeyJ,
        "KeyK" => KeyCode::KeyK,
        "KeyL" => KeyCode::KeyL,
        "KeyM" => KeyCode::KeyM,
        "KeyN" => KeyCode::KeyN,
        "KeyO" => KeyCode::KeyO,
        "KeyP" => KeyCode::KeyP,
        "KeyQ" => KeyCode::KeyQ,
        "KeyR" => KeyCode::KeyR,
        "KeyS" => KeyCode::KeyS,
        "KeyT" => KeyCode::KeyT,
        "KeyU" => KeyCode::KeyU,
        "KeyV" => KeyCode::KeyV,
        "KeyW" => KeyCode::KeyW,
        "KeyX" => KeyCode::KeyX,
        "KeyY" => KeyCode::KeyY,
        "KeyZ" => KeyCode::KeyZ,
        "ArrowLeft" => KeyCode::ArrowLeft,
        "ArrowRight" => KeyCode::ArrowRight,
        "ArrowUp" => KeyCode::ArrowUp,
        "ArrowDown" => KeyCode::ArrowDown,
        "Space" => KeyCode::Space,
        "ShiftLeft" => KeyCode::ShiftLeft,
        "ControlLeft" => KeyCode::ControlLeft,
        _ => return None,
    })
}
impl Bindings {
    fn codes(&self) -> Result<[KeyCode; 12], String> {
        let names = [
            &self.push,
            &self.brake,
            &self.left,
            &self.right,
            &self.trick_left,
            &self.trick_right,
            &self.trick_up,
            &self.trick_down,
            &self.ollie,
            &self.reset,
            &self.board,
            &self.camera,
        ];
        let mut codes = [KeyCode::Space; 12];
        for (i, name) in names.iter().enumerate() {
            codes[i] = key(name).ok_or_else(|| format!("unknown key {name}"))?;
            if codes[..i].contains(&codes[i]) {
                return Err(format!("duplicate key {name}"));
            }
        }
        Ok(codes)
    }
}
pub(super) struct Keyboard {
    codes: [KeyCode; 12],
    number: u32,
    charged: bool,
    flick: f32,
    steering: f32,
    owned: bool,
    blocked: bool,
}
impl Default for Keyboard {
    fn default() -> Self {
        let defaults = Bindings::default();
        let codes = crate::local_state::skate_keyboard_path()
            .and_then(|path| {
                if !path.exists() {
                    if let Ok(text) = toml::to_string_pretty(&defaults) {
                        if let Err(e) = crate::local_state::write_atomic(&path, &text) {
                            warn!("skate keyboard: cannot save bindings: {e}");
                        }
                    }
                    return None;
                }
                match std::fs::read_to_string(&path)
                    .map_err(|e| e.to_string())
                    .and_then(|s| toml::from_str::<Bindings>(&s).map_err(|e| e.to_string()))
                    .and_then(|b| b.codes())
                {
                    Ok(codes) => Some(codes),
                    Err(e) => {
                        warn!("skate keyboard: {e}; using defaults (file preserved)");
                        None
                    }
                }
            })
            .unwrap_or_else(|| defaults.codes().expect("valid built-in keyboard bindings"));
        Self {
            codes,
            number: 0,
            charged: false,
            flick: 0.,
            steering: 0.,
            owned: false,
            blocked: false,
        }
    }
}
impl Keyboard {
    pub(super) fn board(&self, keys: &ButtonInput<KeyCode>) -> bool {
        keys.just_pressed(self.codes[10])
    }
    pub(super) fn camera(&self, keys: &ButtonInput<KeyCode>) -> bool {
        keys.just_pressed(self.codes[11])
    }
    pub(super) fn reset(&self, keys: &ButtonInput<KeyCode>) -> bool {
        keys.just_pressed(self.codes[9])
    }
    pub(super) fn clear(&mut self) {
        self.charged = false;
        self.flick = 0.;
        self.steering = 0.;
        self.owned = false;
        self.blocked = true;
    }
    pub(super) fn sample(
        &mut self,
        keys: &ButtonInput<KeyCode>,
        dt: f32,
        allowed: bool,
        physical: InputFrame,
    ) -> InputFrame {
        let pressed = self.codes[..10].iter().any(|k| keys.pressed(*k));
        if !allowed {
            self.clear();
            return InputFrame::neutral();
        }
        // Focus/chat dismissal cannot launch an ollie or restart a held push.
        if self.blocked {
            if pressed {
                return InputFrame::neutral();
            }
            self.blocked = false;
        }
        let down = |i| keys.pressed(self.codes[i]);
        let dt = dt.clamp(0., 0.1);
        let target = i32::from(down(3)) - i32::from(down(2));
        let step = 6. * dt;
        self.steering += ((target as f32) - self.steering).clamp(-step, step);
        if self.charged && !down(8) {
            self.flick = 0.10;
        }
        self.charged = down(8);
        let mut controls = Controls::default();
        if down(0) && !down(1) {
            controls.buttons |= 0x1000;
        } // Xbox A: right-foot push
        if down(1) {
            controls.buttons |= 0x2000;
        } // Xbox B: foot brake (X is the alternate-foot push)
        controls.left[0] = (self.steering * 32767.).round() as i16;
        controls.right[0] = ((i32::from(down(5)) - i32::from(down(4))) * 32767) as i16;
        controls.right[1] = if self.charged {
            -32767
        } else if self.flick > 0. {
            32767
        } else {
            ((i32::from(down(6)) - i32::from(down(7))) * 32767) as i16
        };
        self.flick = (self.flick - dt).max(0.);
        let active = pressed || self.steering.abs() > 0.001 || controls.right[1] != 0;
        // Physical transport remains untouched while keyboard is idle. One neutral
        // keyboard packet releases its buttons before handing ownership back.
        if !active && !self.owned && physical.controller().is_some() {
            return physical;
        }
        self.owned = active;
        self.number = self.number.wrapping_add(1);
        InputFrame::from_controls(controls, self.number)
    }
}
#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    #[ignore = "requires the explicitly supplied authorized converted Skate pack"]
    fn engine_push_and_ollie() {
        use skate_host::bridge::Session;
        let root = std::path::PathBuf::from(
            std::env::var_os("SKATE_KEYBOARD_TEST_ASSETS").expect("explicit asset path"),
        );
        let floor = vec![
            [
                [-1000., 0., -1000.],
                [-1000., 0., 1000.],
                [1000., 0., 1000.],
            ],
            [
                [-1000., 0., -1000.],
                [1000., 0., 1000.],
                [1000., 0., -1000.],
            ],
        ];
        let mut session = Session::new(&root, floor, vec![], [0., 0.1, 0.], 0.).unwrap();
        session.activate([0., 0.1, 0.], 0.).unwrap();
        let mut k = keyboard();
        let mut keys = ButtonInput::default();
        let mut tick = |session: &mut Session, keys: &ButtonInput<KeyCode>| {
            let dt = session.period();
            session.collect(k.sample(keys, dt, true, InputFrame::neutral()), dt);
            session.advance().unwrap();
            assert!(session.pose().root.is_finite());
        };
        for _ in 0..120 {
            tick(&mut session, &keys);
        }
        let start = session.pose().root.w_axis.truncate();
        keys.press(KeyCode::KeyW);
        for _ in 0..360 {
            tick(&mut session, &keys);
        }
        let pushed = session.pose();
        eprintln!(
            "keyboard engine push: speed={} distance={} state={}",
            pushed.velocity.length(),
            pushed.root.w_axis.truncate().distance(start),
            pushed.state
        );
        assert!(
            pushed.velocity.length() > 0.5,
            "W must push the actual engine"
        );
        assert!(
            pushed.root.w_axis.truncate().distance(start) > 1.,
            "W must move the skater"
        );
        keys.release(KeyCode::KeyW);
        keys.press(KeyCode::KeyS);
        for _ in 0..180 {
            tick(&mut session, &keys);
        }
        let braked = session.pose().velocity.length();
        eprintln!(
            "keyboard engine brake: before={} after={}",
            pushed.velocity.length(),
            braked
        );
        assert!(
            braked < pushed.velocity.length() * 0.5,
            "S must brake the actual engine"
        );
        keys.release(KeyCode::KeyS);
        keys.press(KeyCode::KeyW);
        for _ in 0..240 {
            tick(&mut session, &keys);
        }
        keys.release(KeyCode::KeyW);
        keys.press(KeyCode::Space);
        for _ in 0..60 {
            tick(&mut session, &keys);
        }
        let base = session.pose().root.w_axis.y;
        keys.release(KeyCode::Space);
        let mut peak = base;
        for _ in 0..180 {
            tick(&mut session, &keys);
            peak = peak.max(session.pose().root.w_axis.y);
        }
        eprintln!("keyboard engine ollie: rise={}", peak - base);
        assert!(
            peak - base > 0.15,
            "Space release must produce an actual ollie"
        );
        let turn_start = session.pose().root.w_axis.truncate();
        keys.press(KeyCode::KeyW);
        keys.press(KeyCode::KeyD);
        for _ in 0..120 {
            tick(&mut session, &keys);
        }
        let turned = session.pose().root.w_axis.truncate();
        eprintln!(
            "keyboard engine steering: lateral travel={}",
            turned.x - turn_start.x
        );
        assert!(
            (turned.x - turn_start.x).abs() > 0.5,
            "D must steer the actual engine"
        );
        let recovered = session.activate(turned.to_array(), 0.).unwrap();
        eprintln!(
            "keyboard engine recovery: velocity={:?} state={}",
            recovered.velocity, recovered.state
        );
        assert!(recovered.root.is_finite());
        // activate advances four physics ticks: gravity/contact may change Y,
        // but a recovered board must not retain its prior horizontal momentum.
        assert!(
            recovered.velocity.x.hypot(recovered.velocity.z) < 0.1,
            "recovery must reset horizontal board velocity"
        );
        assert_eq!(recovered.state, "PhysicsGround");
    }
    #[test]
    fn board_camera_and_recovery_edges() {
        let k = Keyboard::default();
        let mut keys = ButtonInput::default();
        keys.press(KeyCode::KeyJ);
        assert!(k.board(&keys));
        keys.clear();
        assert!(!k.board(&keys));
        keys.press(KeyCode::KeyK);
        assert!(k.camera(&keys));
        keys.press(KeyCode::KeyR);
        assert!(k.reset(&keys));
    }
    fn keyboard() -> Keyboard {
        Keyboard {
            codes: Bindings::default().codes().unwrap(),
            number: 0,
            charged: false,
            flick: 0.,
            steering: 0.,
            owned: false,
            blocked: false,
        }
    }
    #[test]
    fn defaults_and_invalid_bindings() {
        assert!(Bindings::default().codes().is_ok());
        let mut b = Bindings {
            push: "Bogus".into(),
            ..Default::default()
        };
        assert!(b.codes().is_err());
        b.push = b.brake.clone();
        assert!(b.codes().is_err());
        assert!(toml::from_str::<Bindings>("unexpected = 'KeyA'").is_err());
    }
    #[test]
    fn push_brake_and_opposites() {
        let mut k = keyboard();
        let mut keys = ButtonInput::default();
        keys.press(KeyCode::KeyW);
        assert_eq!(
            k.sample(&keys, 0.016, true, InputFrame::neutral())
                .buttons(),
            0x1000
        );
        keys.press(KeyCode::KeyS);
        assert_eq!(
            k.sample(&keys, 0.016, true, InputFrame::neutral())
                .buttons(),
            0x2000
        );
        keys.press(KeyCode::KeyA);
        keys.press(KeyCode::KeyD);
        k.sample(&keys, 0.1, true, InputFrame::neutral());
        assert_eq!(k.steering, 0.);
    }
    #[test]
    fn charge_release_and_focus_cancellation() {
        let mut k = keyboard();
        let mut keys = ButtonInput::default();
        keys.press(KeyCode::Space);
        k.sample(&keys, 0.016, true, InputFrame::neutral());
        assert!(k.charged);
        keys.release(KeyCode::Space);
        k.sample(&keys, 0.016, true, InputFrame::neutral());
        assert!(k.flick > 0.);
        k.sample(&keys, 0.016, false, InputFrame::neutral());
        assert_eq!(k.flick, 0.);
        keys.press(KeyCode::KeyW);
        k.sample(&keys, 0.016, true, InputFrame::neutral());
        assert!(!k.owned);
        keys.release(KeyCode::KeyW);
        k.sample(&keys, 0.016, true, InputFrame::neutral());
        keys.press(KeyCode::KeyW);
        assert_eq!(
            k.sample(&keys, 0.016, true, InputFrame::neutral())
                .buttons(),
            0x1000
        );
    }
    #[test]
    fn controller_passthrough_and_keyboard_release() {
        let mut k = keyboard();
        let mut keys = ButtonInput::default();
        let pad = || {
            InputFrame::from_controls(
                Controls {
                    buttons: 0x2000,
                    ..Default::default()
                },
                9,
            )
        };
        assert_eq!(k.sample(&keys, 0.016, true, pad()).buttons(), 0x2000);
        keys.press(KeyCode::KeyW);
        assert_eq!(k.sample(&keys, 0.016, true, pad()).buttons(), 0x1000);
        keys.release(KeyCode::KeyW);
        assert_eq!(k.sample(&keys, 0.016, true, pad()).buttons(), 0);
        assert_eq!(k.sample(&keys, 0.016, true, pad()).buttons(), 0x2000);
    }
}
