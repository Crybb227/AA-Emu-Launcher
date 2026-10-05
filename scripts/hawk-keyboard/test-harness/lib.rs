//! Isolated test harness for the exact production input module. It avoids
//! compiling unrelated WoW UI/network test code during input iteration.
#![allow(dead_code)] // This harness deliberately has no game event loop.
mod local_state {
    pub(crate) fn skate_keyboard_path() -> Option<std::path::PathBuf> {
        None
    }
    pub(crate) fn write_atomic(_: &std::path::Path, _: &str) -> std::io::Result<()> {
        panic!("isolated input tests must never write player settings")
    }
}
mod keyboard;
