# Sentinel HUD 0.3.0.0 — Live Test

## Startup and configuration migration

- [ ] Update Sentinel HUD from the Sentinel custom repository without deleting the existing configuration.
- [ ] Existing module positions, scale, colours, highlight, camera and enabled states remain intact.
- [ ] `/shud` opens and closes configuration.
- [ ] The normal title-bar collapse arrow is available.
- [ ] Collapse and reopen the configuration window successfully.

## Position Marker

- [ ] Off hides the marker immediately.
- [ ] Always shows the marker while logged in.
- [ ] Combat Only appears on entering combat and disappears afterward.
- [ ] Duty Only appears only while bound by duty.
- [ ] Marker and Self Highlight modes remain independent.
- [ ] Yellow, Green, Blue and White display correctly.
- [ ] Custom opens the colour picker and persists its colour.
- [ ] Set radius to 0.01, 0.02, 0.03, 0.04, 0.05 and 0.06 yalms.
- [ ] The marker remains tied to the actor/world origin while walking, running and animating.
- [ ] Rotate and zoom the camera, including extended zoom.
- [ ] Mount/dismount and test slopes or uneven ground.
- [ ] Enabling the thin border does not increase the marker's outside radius.
- [ ] Border Thickness remains subtle and is drawn inward.

## Integrated Shield

- [ ] Select Off, Text Only, Bar Only and Bar + Text.
- [ ] Gain a shield and confirm a blue portion appears inside the HP bar.
- [ ] Shield amount and segment update correctly as the shield changes.
- [ ] Shield disappearing restores the normal HP appearance.
- [ ] At full HP, the blue segment overlays the rightmost proportional part of the HP bar.
- [ ] Below full HP, shield first extends from the current HP endpoint into empty space.
- [ ] A shield larger than the empty space uses extension plus rightmost overlay without leaving the bar.
- [ ] Player, target and focus-target shield settings remain independent.

## Module width, height and persistence

- [ ] Make the Player panel wider.
- [ ] Make the Target panel wider.
- [ ] Make the Focus Target panel wider.
- [ ] Make the Target-of-Target panel wider.
- [ ] Text and icons remain normal and are not horizontally stretched.
- [ ] HP and cast bars fill the selected width.
- [ ] Test thin and thicker Bar Height values.
- [ ] Test Left, Center and Right HP/cast text alignment.
- [ ] Test Full and Compact HP number formatting.
- [ ] Width, bar height, alignment and number format survive plugin reload.
- [ ] Widths and all appearance settings survive a complete game restart.
- [ ] Copy selected appearance updates other modules without copying information visibility toggles.

## Compact Header

- [ ] Player name, job and level appear compactly on one line when width permits.
- [ ] A player-character target shows applicable job and level.
- [ ] An NPC target shows applicable name/level without an invented job.
- [ ] Narrow widths move metadata cleanly to another line rather than overlapping.

## Native Target HP Overlay

- [ ] Off hides the supplement.
- [ ] Always shows exact HP beneath/near the stock target HUD when a valid target exists.
- [ ] Combat Only appears and disappears with combat.
- [ ] Test Current / Maximum, Percentage and combined formats.
- [ ] Test Full and Compact number formatting.
- [ ] Adjust X/Y offsets.
- [ ] Move the stock FFXIV Target HUD element in HUD Layout and confirm the overlay follows.
- [ ] Change targets and confirm values update.
- [ ] Clear the target and confirm the overlay disappears.
- [ ] Hide the FFXIV UI and confirm the supplement disappears.

## Target and bar colours

- [ ] A hostile/enemy target HP bar is red.
- [ ] Friendly player/party/alliance targets are not incorrectly red.
- [ ] Neutral/noncombat targets use the neutral colour.
- [ ] Player HP uses the configured player colour.
- [ ] Shield uses the configured blue default colour.
- [ ] Appearance colour edits persist after restart.

## Module visibility and mouse behavior

- [ ] Test Always, Combat Only, Duty Only and Combat or Duty on each module.
- [ ] Disabled modules remain disabled.
- [ ] Unlocking temporarily exposes enabled modules/placeholders for editing.
- [ ] `/shud unlock` allows every module to drag.
- [ ] `/shud lock` removes title bars and prevents accidental movement.
- [ ] Locked panels do not intercept mouse clicks on nearby FFXIV UI.
- [ ] Positions survive plugin reload and complete game restart.
- [ ] Resolution/window changes keep modules reachable.

## Existing awareness and camera regression

- [ ] Native Self Highlight still follows the character silhouette with no geometric construction lines.
- [ ] Hard, mouseover, controller, tab, interaction and action targeting remain normal.
- [ ] Self Highlight survives zone changes and disappears when disabled.
- [ ] Extended Zoom still exceeds the normal limit when enabled.
- [ ] Disabling/reloading Sentinel HUD restores normal camera limits.
- [ ] First person, duty transitions, cutscenes and GPose remain safe.
- [ ] A known camera plugin conflict causes Sentinel HUD to yield.

## Cleanup and diagnostics

- [ ] Logging out and back in recovers all enabled features.
- [ ] Entering/exiting duties and PvP does not break the HUD.
- [ ] Disabling Sentinel HUD removes panels, native-target text, silhouette and marker and restores camera limits.
- [ ] Diagnostics reports module visibility, target resolution, highlight state, marker mode/projection, native-target anchor and camera state without frame-by-frame spam.
