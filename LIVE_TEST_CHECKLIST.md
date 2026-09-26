# Sentinel HUD 0.6.0.0 — Live Test

## Startup and configuration migration

- [ ] Update Sentinel HUD from the Sentinel custom repository without deleting the existing configuration.
- [ ] Existing module positions, widths, scale, bar heights, colours, field toggles, visibility modes, HP/MP/shield/cast settings, Awareness settings, marker radius/opacity, camera settings and native-target offsets remain intact.
- [ ] Diagnostics reports that schema 5 was loaded/migrated and shows a one-time `SentinelHUD.schema-v5.backup.json` path.
- [ ] Reload again and confirm the schema backup is not replaced or multiplied.
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

## Lock/unlock drift and alignment

- [ ] Place Player and Target panels on exactly the same horizontal line.
- [ ] Unlock the HUD, then lock it without moving either panel.
- [ ] Repeat unlock → lock at least five times; neither panel moves or accumulates downward drift.
- [ ] Unlock, move both panels, lock, and confirm their visible module bodies remain aligned.
- [ ] Reload Sentinel HUD and confirm exact alignment persists.
- [ ] Restart FFXIV and confirm exact alignment persists.

## MP bar

- [ ] Player MP supports Off, Text Only, Bar Only and Bar + Text.
- [ ] Player MP text and bar align correctly and use the expected blue MP colour.
- [ ] Target and Focus Target MP modes render for actors that expose meaningful maximum MP.
- [ ] Actors without meaningful MP do not leave an empty bar or blank gap.
- [ ] Module Width changes the MP bar length without stretching its text.
- [ ] Bar Height, Full/Compact number mode and Left/Center/Right alignment apply to MP bars.
- [ ] MP modes and colour persist through plugin reload and complete game restart.

## HP colour mode

- [ ] Player Static / Role-Based mode still uses the configured player HP colour.
- [ ] Target Static / Role-Based mode keeps hostile targets red and friendly targets non-red.
- [ ] Enable Player Health-State Gradient and confirm near-full HP is green.
- [ ] Around 60–50% HP, confirm the bar transitions smoothly through yellow.
- [ ] At 35% HP and below, confirm the bar is red.
- [ ] Enable Target / Focus / ToT Health-State Gradient and confirm hostile targets follow health state instead of remaining fixed red.
- [ ] Player and Target colour modes remain independent and persist after restart.

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
- [ ] Test the normal combined Target Info layout.
- [ ] Test the separated Target Info layout; exact HP anchors to the visible Main Target HP bar.
- [ ] Diagnostics identifies the detected layout/addon, HP-gauge or root anchor source, screen position/size and visibility state.
- [ ] Enter and leave combat in Combat Only mode.
- [ ] Change targets and confirm values update.
- [ ] Clear the target and confirm the overlay disappears.
- [ ] No stale overlay remains after clearing or changing target.
- [ ] Hide the FFXIV UI and confirm the supplement disappears.

## Player Cast Bar

- [ ] Cast a spell with a cast time and confirm the cast name appears.
- [ ] The cast bar advances smoothly and obeys Player width, scale, bar height and text alignment.
- [ ] Cast percentage updates through completion.
- [ ] Enable remaining time and confirm it counts down accurately.
- [ ] Independently disable name, bar, percentage and remaining time.
- [ ] Interrupt or cancel a cast and confirm the row disappears immediately.
- [ ] Finish a cast and confirm no permanent empty row remains.

## Focus Target's Target

- [ ] Set another player as Focus Target and have them target an enemy.
- [ ] Their target's configured name, HP, HP %, job and level fields appear when applicable.
- [ ] Have the Focus Target change targets; Sentinel updates without retaining the previous actor.
- [ ] Have the Focus Target clear target; the row disappears without stale information.
- [ ] Focus an enemy that targets the player and confirm its target resolves when exposed by the client.
- [ ] NPCs do not receive a fabricated player job.

## Module targeting

- [ ] Lock the HUD and click Player; the normal hard target becomes the local player.
- [ ] Click Target; it reselects the actor currently represented by that module.
- [ ] Click Target-of-Target; the normal hard target becomes that represented actor.
- [ ] Click Focus Target; the focus actor becomes the normal hard target.
- [ ] Focus a player, let them target an enemy, and click the Focus Target's Target row; that child actor—not the parent focus actor—is targeted.
- [ ] Change the Focus Target's target and repeat; the newly resolved actor is targeted.
- [ ] Whole module mode accepts clicks across the visible panel and shows subtle hover feedback.
- [ ] Header / name only mode accepts clicks only over the header/name region.
- [ ] Disable Click to Target independently on every module; each disabled region becomes mouse-pass-through.
- [ ] Normal world/FFXIV UI mouse interaction works everywhere outside enabled visible regions.
- [ ] Unlock the HUD; all click-to-target actions stop and clicks edit the layout instead.
- [ ] Let an actor leave the object table before clicking; targeting fails safely and Diagnostics explains the result.

## Visual editor and direct resize

- [ ] Unlock the HUD; subtle borders, labels and right-edge resize grips appear without title bars.
- [ ] Drag Player and Target by their module bodies.
- [ ] Drag the right edge of Player and Target to resize them horizontally.
- [ ] Resize Focus Target and Target-of-Target the same way.
- [ ] Bars reflow to the new width; fonts and icons are not stretched.
- [ ] Precision Module Width sliders reflect the mouse-resized values.
- [ ] Lock the HUD; all editor chrome disappears and the exact visual origins remain unchanged.
- [ ] Repeat unlock → lock at least five times; no panel drifts in any direction.
- [ ] Reload the plugin and restart FFXIV; positions and mouse-resized widths persist.

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
- [ ] `/shud unlock` allows every module to drag and resize from its right edge.
- [ ] `/shud lock` removes editor borders/grips and prevents accidental movement.
- [ ] Locked panels intercept only explicitly enabled click-to-target regions.
- [ ] Positions survive plugin reload and complete game restart.
- [ ] Resolution/window changes keep modules reachable.

## Existing awareness and camera regression

- [ ] Native Self Highlight still follows the character silhouette with no geometric construction lines.
- [ ] Select Yellow; the silhouette is yellow.
- [ ] Select Green; the silhouette is green.
- [ ] Select Blue; the silhouette is blue.
- [ ] White and Custom are not presented as working native choices.
- [ ] If upgrading an old White/Custom selection, the highlight remains off with an explicit unsupported warning—it does not silently render Yellow.
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
- [ ] Diagnostics reports module visibility, target/focus/Focus-ToT resolution, highlight state, marker mode/projection, native-target variants/anchor and camera state without frame-by-frame spam.
