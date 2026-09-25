# Sentinel HUD 0.2.0.0 — Live Test

## Startup and basic modules

- [ ] Install or update Sentinel HUD from the Sentinel custom repository.
- [ ] `/shud` opens and closes configuration.
- [ ] Player module renders while logged in.
- [ ] Target module appears when a target is selected and disappears when cleared.
- [ ] Focus Target module appears and disappears correctly.
- [ ] Target-of-Target appears when the current target has a target.

## Granular display

- [ ] Player current HP, maximum HP and HP percentage can each be hidden independently.
- [ ] Target current HP, maximum HP and HP percentage can each be hidden independently.
- [ ] Target distance can be disabled and no distance remains in that module.
- [ ] Focus-target distance can be disabled independently.
- [ ] Cast name, bar and percentage toggles work independently during a cast.
- [ ] Shield information appears on a shielded player/target and disappears safely otherwise.
- [ ] Status summaries can be enabled without errors.

## Layout and persistence

- [ ] `/shud unlock` shows movable module windows, including placeholders for missing targets.
- [ ] Every module drags correctly.
- [ ] `/shud lock` removes title bars and prevents accidental movement/input capture.
- [ ] Per-module scale and opacity apply.
- [ ] Reset selected module moves only that module.
- [ ] Reset complete layout restores all default anchors.
- [ ] Positions and scaling survive plugin reload.
- [ ] Positions and scaling survive a complete FFXIV restart.
- [ ] Changing resolution/window size keeps modules reachable.

## Self Highlight

- [ ] Enable Self Highlight → Always.
- [ ] Highlight conforms to the actual character silhouette.
- [ ] No geometric box, trapezoid, construction lines or floating head circle appears.
- [ ] Highlight follows character animation and equipment/model shape.
- [ ] Yellow, Green and Blue apply their exact native colours.
- [ ] White reports and applies its documented nearest native palette fallback.
- [ ] Custom colour persists and reports its nearest native palette result.
- [ ] Combat Only and Duty Only activate only under their selected conditions.
- [ ] Hard targeting still works normally.
- [ ] Mouseover/soft targeting still works normally.
- [ ] Controller targeting still works normally.
- [ ] Tab, action and interaction targeting still work normally.
- [ ] Highlight persists through zone changes.
- [ ] Highlight follows the mounted model where supported.
- [ ] Disabling Self Highlight removes it immediately.

## Player Position Marker

- [ ] Enable the position marker independently of Self Highlight.
- [ ] Dot appears beneath the actual actor/world origin.
- [ ] Walk/run and confirm the dot follows continuously.
- [ ] Rotate the camera and zoom in/out.
- [ ] Mount and dismount.
- [ ] Stand on slopes and uneven ground; check the terrain-projected disc.
- [ ] Enter and leave combat.
- [ ] Change zones.
- [ ] Confirm the marker does not drift with character animation.
- [ ] Change Yellow → Green → Blue → White and test Custom.
- [ ] Change marker radius, opacity and border.
- [ ] Test marker-only, silhouette-only, both, and neither.
- [ ] Disable the marker and confirm it disappears immediately.

## Extended Camera

- [ ] With Extended Zoom off, stock camera behavior is unchanged.
- [ ] Enable Extended Zoom and zoom farther out than the normal limit.
- [ ] Adjust Maximum Zoom Distance and confirm the new limit.
- [ ] Disable Extended Zoom and confirm the normal limit and current zoom are restored.
- [ ] Reload/disable Sentinel HUD while extended and confirm camera restoration.
- [ ] Restart FFXIV and confirm the setting persists without a stale override while disabled.
- [ ] Enter and leave first person; Sentinel pauses and resumes safely.
- [ ] Enter and leave a duty.
- [ ] Check a cutscene where practical.
- [ ] Enter and leave GPose.
- [ ] Change zones while extended zoom is enabled.
- [ ] If Cammy or another listed camera plugin is loaded, confirm Sentinel reports a conflict and does not fight it.

## Transitions and cleanup

- [ ] Logging out and back in recovers all enabled features.
- [ ] Entering/exiting duties and PvP does not break the HUD.
- [ ] Hiding the FFXIV UI hides Sentinel visual overlays.
- [ ] Disabling Sentinel HUD removes panels, silhouette and marker and restores camera limits.
- [ ] Diagnostics reports module resolution, applied native highlight colour, marker projection and camera state without frame-by-frame log spam.
