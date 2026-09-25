# Sentinel HUD 0.1.0.0 — First Live Test

## Startup and basic modules

- [ ] Install Sentinel HUD from the Sentinel custom repository.
- [ ] `/shud` opens and closes configuration.
- [ ] Player module renders while logged in.
- [ ] Target module appears when a target is selected and disappears when it is cleared.
- [ ] Focus Target module appears and disappears correctly.
- [ ] Target-of-Target appears when the current target has a target.

## Granular display

- [ ] Player current HP, maximum HP and HP percentage can each be hidden independently.
- [ ] Target current HP, maximum HP and HP percentage can each be hidden independently.
- [ ] Target distance can be disabled and no distance remains in that module.
- [ ] Focus-target distance can be disabled independently.
- [ ] Cast name, bar and percentage toggles work independently during a cast.
- [ ] Shield information appears on a shielded player/target and disappears safely when no shield exists.
- [ ] Status summaries can be enabled without errors.

## Layout and persistence

- [ ] `/shud unlock` shows movable module windows, including placeholders for missing targets.
- [ ] Every module drags correctly.
- [ ] `/shud lock` removes title bars and prevents accidental movement/input capture.
- [ ] Per-module scaling applies and remains readable.
- [ ] Per-module and global opacity apply.
- [ ] Reset selected module moves only that module.
- [ ] Reset complete HUD layout restores all default anchors.
- [ ] Positions and scaling survive plugin disable/re-enable or plugin reload.
- [ ] Positions and scaling survive a complete FFXIV game restart.
- [ ] Changing resolution/window size keeps modules reachable.

## Self highlight and targeting safety

- [ ] Self Highlight → Always displays the aura around the local character.
- [ ] The aura follows the character while moving and turning the camera.
- [ ] Yellow, Green, Blue and White presets visibly change the aura.
- [ ] Custom colour and intensity apply and persist after restart.
- [ ] Combat Only appears only while in combat.
- [ ] Duty Only appears only while bound by duty.
- [ ] Hard targeting behaves normally while highlight is active.
- [ ] Controller targeting behaves normally while highlight is active.
- [ ] Mouseover and soft targeting behave normally while highlight is active.
- [ ] Tab/action/interaction targeting behaves normally while highlight is active.

## Transitions and cleanup

- [ ] Changing zones does not leave stale target/focus data or break modules.
- [ ] Logging out and back in recovers normally.
- [ ] Entering and exiting duties does not break modules or conditional highlighting.
- [ ] Entering and leaving PvP does not break the HUD.
- [ ] Hiding the FFXIV UI hides Sentinel HUD overlays.
- [ ] Disabling Sentinel HUD removes all panels and the self highlight cleanly.
- [ ] Diagnostics shows resolved/visible states and no repeated frame-by-frame errors.
