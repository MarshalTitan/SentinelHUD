# Sentinel HUD 0.8.4.2 — Live Test

## Startup and configuration migration

- [ ] Update Sentinel HUD from the Sentinel custom repository without deleting the existing configuration.
- [ ] Existing module positions, widths, scale, bar heights, colours, field toggles, visibility modes, HP/MP/shield/cast settings, Awareness settings, marker radius/opacity, camera settings and native-target offsets remain intact.
- [ ] Diagnostics reports configuration schema 11 and, when upgrading schema 10, shows a one-time `SentinelHUD.schema-v10.backup.json` path.
- [ ] Reload again and confirm the schema backup is not replaced or multiplied.
- [ ] `/shud` opens and closes configuration.
- [ ] Modern mode has no native `Sentinel HUD Configuration` title strip above the application header.
- [ ] The custom header collapse and close controls work; `/shud` or Dalamud reopening expands a collapsed window successfully.

## Sentinel Modern 2

- [ ] `/shud` opens in Sentinel Modern 2 after updating from 0.8.3.3 without asking you to reselect the theme.
- [ ] Confirm the midnight-navy canvas, electric-blue accents and violet/teal ambient rings are visible and restrained rather than distracting.
- [ ] Confirm the primary icon rail uses recognizable crisp icons rather than letters, remains fixed on the far left and exposes General, HUD, Awareness, Systems, Plugins, Appearance and Diagnostics through its tooltips.
- [ ] Confirm HUD's secondary sidebar opens Player, Target, Focus Target, Target-of-Target and Layout.
- [ ] Confirm Awareness's secondary sidebar opens Personal Awareness and Encounter Awareness.
- [ ] Confirm Systems' secondary sidebar opens Camera and Questing / Convenience.
- [ ] Confirm secondary categories are text-only with no `P/T/F/2/L/A/E/C/Q` letter placeholders.
- [ ] Confirm General, Plugins, Appearance and Diagnostics do not show a redundant secondary sidebar.
- [ ] Confirm the compact header has no internal scrollbar and shows icon, `Sentinel HUD`, status pill, page context, collapse and close controls on one row.
- [ ] Drag unused space in the custom header; confirm the complete configuration window moves and its new position survives reopen/restart.
- [ ] Confirm the header, rail, optional sidebar and content read as one continuous surface without heavy boxed pane divisions.
- [ ] Confirm every page title and its short explanation appear only in the right content area.
- [ ] Confirm General uses compact Core glass settings rows without a fixed-height outer card.
- [ ] Confirm General has one Lock HUD switch and no duplicate lower Unlock HUD button.
- [ ] Confirm Appearance opens directly at Bar Colours without a redundant Configuration Window section.
- [ ] Confirm page changes use a subtle fade/slide and the rail selection indicator interpolates smoothly.
- [ ] Enable Dalamud reduced motion; confirm page/rail changes become immediate and ambient movement becomes static without changing inputs.
- [ ] Toggle several modern switches and confirm each changes only its labelled setting.
- [ ] Confirm combos, sliders, colour pickers, collapsible sections and buttons remain readable and functional.
- [ ] Narrow the window and test at 1.25/1.5 UI scale; confirm long labels/descriptions wrap and controls stack below them before any overlap occurs.
- [ ] Unlock and lock the HUD; confirm the header status changes between `EDIT MODE` and `HUD LOCKED` without moving any HUD module.
- [ ] At UI scales 1.0, 1.25 and 1.5, resize to the minimum and substantially enlarge the window; confirm header, rail, secondary sidebar and Appearance action dock remain fixed while content alone scrolls.
- [ ] Confirm navigation never moves between the page heading and its settings and page content never overlaps the Appearance action dock.
- [ ] Collapse from the custom header, reopen through `/shud`, and confirm the same saved size/position returns without an empty native-title strip.
- [ ] Open Appearance, click its single `Use Classic Theme` action and confirm the original tab layout returns without changing gameplay or layout settings.
- [ ] Use the compact Classic-header action and confirm Sentinel Modern returns immediately.
- [ ] Reload the plugin and restart FFXIV; confirm the selected configuration theme persists.
- [ ] Confirm all pre-update module positions, sizes, colours and gameplay settings remain unchanged.

## Sentinel ecosystem Plugins page

- [ ] Open Plugins from the plug icon in the primary rail; confirm the page matches the Sentinel Modern surface, typography, cards and status pills.
- [ ] Confirm the summary counts installed, enabled, disabled and absent companions accurately without any `Required` warning.
- [ ] With S Rank Sentinel enabled, confirm its card reports **Enabled** and shows the installed version.
- [ ] Disable S Rank Sentinel through Dalamud; confirm the card changes to **Installed · Disabled** rather than **Not Installed**.
- [ ] Re-enable it and confirm the status returns to **Enabled** without restarting Sentinel HUD.
- [ ] Confirm an uninstalled listed companion reports **Not Installed**.
- [ ] Repeat status checks for PvP Sentinel, Classy Sentinel, Sentinel Relay and Sentinel Profiles where installed.
- [ ] Confirm Sentinel HUD, SentinelCore and MINION Frontline Sentinel do not appear as companion cards.
- [ ] Confirm Sentinel HUD modules and all other features work normally with every companion missing or disabled.

## Responsive configuration text

- [ ] At a wide configuration width, confirm setting rows remain compact and each control stays in its reserved right column.
- [ ] Narrow to approximately the attached medium-width example; confirm the General interaction description wraps and the row grows instead of clipping its second line.
- [ ] Resize to the minimum supported width; confirm controls stack below wording where needed and remain usable.
- [ ] Audit General, Player, Target, Focus Target, Target-of-Target, Awareness, Encounter Awareness, Camera, Questing / Convenience, Appearance, Layout, Diagnostics and Plugins.
- [ ] Confirm long descriptions and muted runtime/helper text wrap naturally, move later content downward and never render under a slider, combo, colour control, switch or status pill.
- [ ] Confirm Plugins cards grow for wrapped descriptions/version text without horizontal overflow.
- [ ] Repeat at Dalamud UI scales 1.0, 1.25 and 1.5; confirm header and rail remain fixed while page content scrolls independently when necessary.

## Targeting Me Counter

- [ ] In PvE and outside a PvP duty, the counter remains inactive and Diagnostics explains why.
- [ ] In Frontline Only mode, enter Frontline and confirm the counter activates; enter another PvP duty and confirm it remains inactive.
- [ ] In All PvP Duties mode, confirm the counter activates in supported non-Frontline PvP.
- [ ] With no enemy hard-targeting the local player, Hide When Zero hides the counter; disabling it displays a normal-colour `0`.
- [ ] Have one enemy player hard-target the local player; the count becomes `1` promptly.
- [ ] Have several enemies hard-target the local player; the count matches the visible current hard targeters and warning colour changes at the documented threat levels.
- [ ] Enemy soft target, mouseover or nearby presence alone does not increment the count.
- [ ] Show Jobs lists each observed targeter's current job abbreviation without changing the count.
- [ ] Show Targeter Details lists current name/job/distance rows and removes them immediately when the targeter changes target or disappears.
- [ ] In Frontline, Diagnostics reports a local Battalion of 0, 1 or 2 and `BattalionTeam` as authoritative; same-Battalion allies never count and other valid Battalions can count.
- [ ] If Battalion classification is unavailable, Diagnostics reports the conservative hostile fallback; party, alliance and roster allies are still excluded.
- [ ] A dead, untargetable, despawned or out-of-object-table actor is removed without a stale row or count.
- [ ] Local death, logout, zone change and duty exit clear the snapshot safely.
- [ ] Unlock, drag and horizontally resize the counter; lock it and confirm no drift.
- [ ] Adjust scale, width, opacity, number/job/detail sizes and warning colours; reload and restart FFXIV and confirm persistence.
- [ ] Enable existing HUD, Awareness, Camera, Encounter and Convenience systems and confirm the counter remains independent.
- [ ] Run without PvP Sentinel installed; the counter works. Install/enable PvP Sentinel separately and confirm neither plugin is a hard dependency of the other.
- [ ] Diagnostics shows PvP mode, classification source, observed players/enemies, nearby enemies/allies, target count, threat level and a bounded targeter list without frame-by-frame log spam.

## Position Marker

- [ ] Off hides the marker immediately.
- [ ] Always shows the marker while logged in.
- [ ] Combat Only appears on entering combat and disappears afterward.
- [ ] Duty Only appears only while bound by duty.
- [ ] Marker and Self Highlight modes remain independent.
- [ ] Yellow, Green, Blue and White display correctly.
- [ ] Custom opens the colour picker and persists its colour.
- [ ] In Camera Facing, set size to 0.01, 0.02, 0.03, 0.04, 0.05 and 0.06; confirm each remains compact and selectable.
- [ ] In Ground Projected, confirm those values retain their yalm-radius meaning.
- [ ] Camera Facing is selected after update and remains tied to the exact actor world origin while walking, running and animating.
- [ ] Stand on an upper platform above another walkable floor; Camera Facing remains on the upper level and does not snap to the floor below.
- [ ] Fly upward on a mount; Camera Facing follows the actor vertically instead of remaining on the terrain below.
- [ ] Tilt the camera to a shallow angle; Camera Facing remains circular and does not collapse into a line.
- [ ] Zoom out normally and with Extended Zoom; Camera Facing remains readable and centred without drift.
- [ ] Switch to Ground Projected; the original terrain-plane disc remains available and follows perspective.
- [ ] Switch repeatedly between both styles; position, visibility, colour, radius, opacity, border and danger settings remain unchanged.
- [ ] Reload the plugin and restart FFXIV; the selected style persists.
- [ ] Mount/dismount and test slopes or uneven ground.
- [ ] Enabling the thin border does not increase the marker's outside radius.
- [ ] Border Thickness remains subtle and is drawn inward.

## Position Marker Danger

- [ ] Enable Encounter Awareness and **Change Position Marker colour when unsafe**.
- [ ] The marker uses its normal configured colour while safe.
- [ ] Stand inside a supported currently casting hostile telegraph; the exact-position dot changes to the configured Danger colour.
- [ ] Move the actor origin outside the hazard; the normal colour returns immediately.
- [ ] Test Red, Orange, Yellow and a visibly different Custom danger colour.
- [ ] Character animation/model movement does not change the danger result when the actor origin stays still.
- [ ] Disabling Player Danger leaves the marker at its normal colour even while a provider reports danger.

## Encounter Awareness

- [ ] With General disabled, Diagnostics reports no active hazards and the marker never changes colour.
- [ ] Enable Native Detection and test a standard visible hostile circle/cone/line where practical.
- [ ] Unsupported or scripted mechanics do not create guessed/stale hazards.
- [ ] Diagnostics reports Native provider status, territory, hazard count and current unsafe state without log spam.
- [ ] Without Splatoon installed, enabling its provider reports Not Installed and the rest of Sentinel HUD works normally.
- [ ] With Splatoon installed, Diagnostics reports Installed/Connected without requiring a hard dependency.
- [ ] With the default fail-closed setting, unclassified Splatoon drawings do not change the marker.
- [ ] Opt in to treating unclassified visible Splatoon geometry as danger and verify the warning and marker response; remember that safe-zone artwork can also be classified as danger in this mode.
- [ ] Change zone/duty and confirm cached hazards clear immediately.

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
- [ ] Specific regression: Focus Target `Rocky Bear`, have Rocky Bear target `Market Board`, click `Target: Market Board`, and confirm Market Board becomes the normal hard target while Rocky Bear remains the Focus Target.
- [ ] Have the Focus Target switch between a player, enemy, NPC and targetable world object; each currently displayed entry targets where FFXIV permits it.
- [ ] Change or clear the Focus Target's target immediately before clicking; stale text clears/updates and no old object is targeted.

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

## Right-click native actor context menus

- [ ] Lock the HUD and right-click Player; an FFXIV-built actor context menu opens.
- [ ] Right-click another player represented by Target and verify only currently valid game actions appear.
- [ ] Right-click Target-of-Target and Focus Target.
- [ ] Right-click Focus Target's Target; the child actor receives the menu, not the focus parent.
- [ ] NPC/enemy actors do not receive player-only options such as Send Tell or Friend Request.
- [ ] Existing left-click targeting still works independently.
- [ ] Disable **Right-click native context menu** on a module; right-click becomes pass-through there.
- [ ] Select Header / name only; transparent space outside that region remains pass-through.
- [ ] Unlock the HUD; neither left-click targeting nor right-click menus fire while moving/resizing.
- [ ] Change/clear an actor before interacting; no stale actor menu opens.
- [ ] The originating module remains continuously visible with its HP/MP/cast bars unchanged.
- [ ] The native menu opens adjacent to the module and remains completely readable.
- [ ] Select an option and confirm normal menu input works without Sentinel stealing the click.
- [ ] Dismiss the menu by clicking outside; the continuously visible module remains in exactly the same position and size.
- [ ] Repeat from Player, Target, Target-of-Target, Focus Target and Focus Target's Target.
- [ ] After the menu closes, left-click actor targeting still works normally.
- [ ] Unlock the HUD and confirm normal editing still takes precedence without opening an actor menu.

## Visual editor and full drag resize

- [ ] Unlock the HUD; subtle borders, labels and edge/corner resize grips appear without title bars.
- [ ] Drag Player and Target by their module bodies.
- [ ] Drag left/right edges to resize width.
- [ ] Drag top/bottom edges to resize bar height and compact row geometry.
- [ ] Drag each corner to change width and bar height together.
- [ ] Resize Player, Target, Focus Target and Target-of-Target.
- [ ] Bars reflow to the new width; fonts and icons are not stretched.
- [ ] Vertical resizing changes HP/MP/cast bar height without scaling fonts.
- [ ] Precision Module Width and Bar Height sliders reflect mouse-resized values.
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

## Self Highlight and camera regression

- [ ] Native Self Highlight still follows the character silhouette with no geometric construction lines.
- [ ] Select Yellow; the silhouette is yellow.
- [ ] Select Green; the silhouette is green.
- [ ] Select Blue; the silhouette is blue.
- [ ] Select Custom black, orange and pink/purple; the UI reports the actual closest native palette colour and persists the picker value.
- [ ] Selecting White leaves the outline off with an explicit unsupported warning; it never silently renders Yellow.
- [ ] Custom never claims arbitrary-RGB accuracy when the applied native palette entry differs.
- [ ] Hard, mouseover, controller, tab, interaction and action targeting remain normal.
- [ ] Self Highlight survives zone changes and disappears when disabled.
- [ ] Extended Zoom still exceeds the normal limit when enabled.
- [ ] Set a clearly extended current zoom, die, and confirm the same distance is restored instead of remaining at FFXIV's stock death zoom.
- [ ] Respawn and confirm the pre-death current zoom remains restored while the configured maximum is unchanged.
- [ ] Manually adjust zoom after the death transition and confirm Sentinel does not continuously fight the new input.
- [ ] Disabling/reloading Sentinel HUD restores normal camera limits.
- [ ] First person, duty transitions, cutscenes and GPose remain safe.
- [ ] A known camera plugin conflict causes Sentinel HUD to yield.

## Prevent AFK Disconnect

- [ ] Default is Off and Diagnostics reports Off.
- [ ] Enable it while logged in; Diagnostics reports Active after the first update cycle.
- [ ] Keyboard, mouse and controller input remain normal; no movement, chat or visible key presses occur.
- [ ] Change zones and confirm the service resumes cleanly.
- [ ] Disable and re-enable it; normal timer accumulation resumes while disabled.
- [ ] Reload/unload Sentinel HUD with it enabled; disposal is clean and no background worker remains.

## Skip Dialogue

- [ ] Leave Skip Dialogue Off and confirm ordinary dialogue remains fully manual.
- [ ] Enable Skip Dialogue and confirm ordinary NPC/quest `Talk` text advances.
- [ ] Reach a multiple-response dialogue prompt; Sentinel pauses and waits for manual input.
- [ ] Reach a Yes/No prompt; Sentinel does not choose either answer.
- [ ] Close/end dialogue and confirm automation stops without interacting with gameplay UI.
- [ ] Open a quest reward window with Reward Selection set to Manual; dialogue skipping does not select or confirm a reward.

## Skip Cutscenes

- [ ] Leave Skip Cutscenes Off and confirm cutscenes retain normal manual behavior.
- [ ] Enable it, start an FC submarine/voyage cutscene and confirm the list-style `Skip cutscene? / Yes. / No.` prompt is answered with Yes automatically.
- [ ] Confirm the submarine interaction resumes normally after the cutscene closes.
- [ ] Enter another normally skippable cutscene; FFXIV's skip flow is requested and confirmed.
- [ ] Enter an unskippable/protected cutscene; Sentinel leaves it playing and Diagnostics reports that the game did not permit skipping.
- [ ] After a skipped cutscene, open an unrelated Yes/No prompt and confirm Sentinel does not answer it.
- [ ] Manually dismiss/cancel a skip prompt before Sentinel confirms it where practical; confirm no later unrelated prompt is accepted.
- [ ] Turn Skip Cutscenes Off while a request is pending; confirm pending state clears and subsequent cutscenes behave normally.
- [ ] With AutoRetainer disabled, deploy an FC submarine and confirm `Skip cutscene?` is accepted without a crash.
- [ ] With AutoRetainer enabled and its skipper active, repeat once; confirm whichever plugin acts first closes the prompt and Sentinel fails closed without a crash or unrelated confirmation.
- [ ] After a submarine cutscene, open an unrelated SelectString or Yes/No prompt and confirm Sentinel does not select it.
- [ ] Change territory or log out while pending where practical; confirm pending state clears.
- [ ] In Diagnostics, confirm the progression reports skip request, waiting, detected addon, Yes selected and completion without frame-by-frame log spam.
- [ ] Finish or leave the cutscene and confirm the service resets cleanly for the next one.

## Quest Rewards — Manual

- [ ] Set Quest Reward Selection to Manual and open a choose-one reward screen.
- [ ] Sentinel makes no selection and does not click Complete.
- [ ] Guaranteed EXP, gil, items and unlocks remain unaffected.

## Quest Rewards — First Reward

- [ ] Open a reward screen with multiple selectable choices.
- [ ] The first selectable reward is selected.
- [ ] The quest completes through the enabled Complete button.
- [ ] Change/close the reward window during the flow and confirm no stale action reaches a later window.

## Quest Rewards — Current Job

- [ ] On WAR, open a reward with tank/WAR-compatible equipment; the appropriate item is selected.
- [ ] Change to another combat job and confirm the compatible reward changes.
- [ ] Test shared role gear and confirm it is recognized from ClassJobCategory data.
- [ ] Test a crafting or gathering job with applicable gear where practical.
- [ ] When two compatible rewards exist, confirm narrower job/role compatibility wins, then higher item level, then first source order.
- [ ] When no compatible equipment exists, the first selectable reward is used and Diagnostics reports `No Job Match -> First Reward`.

## Quest Rewards — Allagan Piece

- [ ] With an Allagan Piece offered, confirm it is selected by stable item data rather than localized display text.
- [ ] If multiple supported pieces are offered, confirm the greatest `vendor value × quantity` wins.
- [ ] With no Allagan Piece offered, the first selectable reward is used and Diagnostics reports `No Allagan Piece -> First Reward`.
- [ ] Confirm the selected reward name/ID, current job, window detection and selection reason appear in Diagnostics without frame-by-frame log spam.

## Cleanup and diagnostics

- [ ] Logging out and back in recovers all enabled features.
- [ ] Entering/exiting duties and PvP does not break the HUD.
- [ ] Disabling Sentinel HUD removes panels, native-target text, silhouette and marker and restores camera limits.
- [ ] Diagnostics reports module visibility, target/focus/Focus-ToT resolution, actor menu result, highlight state, marker mode/projection/danger, encounter providers, native-target variants/anchor, camera and Anti-AFK state without frame-by-frame spam.
