# Native keyboard skating extension

This is the JasonHawkSkater fork extension, not a change to stock WoW 1.12
CVars or the working WSL checkout. `Build-HawkWindows.ps1` copies the keyboard
module into its isolated source archive and applies checked substitutions to
the pinned Skate engine checkout in that build's isolated Cargo home. Unexpected
source changes fail closed; rerunning after formatting is supported.

Engine input remains `InputFrame -> Session.collect -> Session.advance` for
both physical XInput and synthesized keyboard packets. No replacement physics
or controller driver is installed. Keyboard activity takes precedence; an idle
keyboard passes physical input through unchanged, including a neutral release
packet when handing control back. Unfocused/typing input cancels any pending
ollie and waits for keys to be released before resuming.

Defaults: W push (Xbox A), S foot brake (Xbox B), A/D left stick, arrows right
stick, Space held right-stick down then a 100 ms upward flick on release,
J board, K camera, R recover using the engine's existing activation path at
the current feet. R does not teleport to a saved server position.

Bindings are a distinct `benilla-config/skate-keyboard.toml`, never written to
the WoW install or stock CVar configuration. Updates preserve that directory.
The UI describes defaults; remapping takes effect at the next game launch.

Tests live beside the implementation. Run the normal workspace tests without
WoW data, then the explicitly ignored `engine_push_and_ollie` test with
`SKATE_KEYBOARD_TEST_ASSETS` pointing to an authorized converted pack. That test
feeds the real engine the same generated packets and measures movement and
ollie height on a synthetic floor; it neither connects to nor starts a server.
Native world-entry/session tests remain a separate acceptance check. Physical
keyboard/controller feel still needs player acceptance testing.

These extension files use the launcher's MIT license. Preserve the upstream
client and Skate engine license notices; release packaging includes this source.
