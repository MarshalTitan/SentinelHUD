# Changelog

## 0.8.7.2 - Lower arc text, split HP and target cast data

- Place resource rows near the lower end of each arc. Total HP is a separate upper outer readout; current HP is lower outer and HP percentage lower inner. Target mirrors Player. Current Only/maximum visibility omits the upper number; MP retains its existing number format.
- Add Curved bar text scale (0.5-2x) for HP/MP values and percentages without changing tube dimensions, names or levels. Text bounds grow/wrap as needed; standard text and cast scales remain independent.
- Use frame-local native Character cast snapshots through the existing cast formatter. Targeting self shares Player's snapshot, so independently enabled Player/Target cast modules can show the same cast. Other targets use their own valid cast data, including character wrappers without IBattleChara; null/invalid/cancelled casts clear safely.
- Arc pages expose their actual separate cast controls immediately after Bar style and hide unused attached-cast switches. Preserve each cast module's enable, visibility, fields, dimensions and position.
- Preserve liquid shading, smooth contours, flat caps, track-only input, Standard, currency changes and existing preferences. Schema stays 14; new text scale defaults 1x. Core remains 0.3.1.

## 0.8.7.1 - Currency search, catalog cleanup and right alignment

- Put search and its filtered scrolling checklist at the top of Currency Tracker. Add clear-search/no-results feedback and an action to remove saved unavailable selections.
- Use the game's active tomestone types (Poetics, Mnemonics and Mathematics) and exclude retired/exchange-only tomestones plus the shared Chocobo Training Manual icon group in every client language. Other existing currency choices remain available.
- Add Left/Right alignment. Right-aligned vertical entries share an icon column and right value edge; horizontal rows align to the right without reversing saved order. Remove currency window padding and add Move to right edge.
- Allow currency widths down to 64 px through both settings and editor resizing; ordinary actor modules retain their 180 px minimum. Wrapped full labels remain supported.
- Preserve schema 14 and all existing positions, selections, order, arc/cast and other preferences. New alignment defaults Left; Core stays 0.3.1.

## 0.8.7.0 - Polished side arcs and Currency Tracker

- Move HP/MP values to the outer side and percentages to the inner side, mirrored for Player/Target. Add Current Only/Current-Total and resource-percentage options without changing Standard settings or adding HP/MP labels.
- Add continuous alpha-fringed edges, shaded liquid fills and restrained tube highlights while preserving flat caps, cached bows, tight input regions, resource colours and independent casts.
- Add an opt-in Currency Tracker: game-data currency catalog and icons, selected balances at a 500 ms cadence, holding caps, rank-dependent company-seal caps, ordered multi-selection, compact/full and horizontal-wrapped/vertical layouts. The tracker is click-through during gameplay and uses the normal editor.
- Schema 14 adds only new defaults and a disabled currency module; existing positions/preferences remain intact. Core remains pinned at 0.3.1. Live balances/icons require in-game acceptance.

## 0.8.6.2 - Tight arc interactions and lower resource readouts

- Limit locked Player/Target Arc input to narrow bands along each visible HP/MP track. Text, empty interiors and unused module space stay click-through. Hide the rectangular hover outline for arcs; keep the full edit frame when unlocked.
- Mirror HP values at the bottom outside and HP percentage at the bottom inside. Put MP values below HP values, without literal HP/MP labels. Preserve resource colours, health gradients, flat ends and visible name/level metadata.
- Keep Standard clickable-area choices and rendering, independent cast modules, schema 13, saved positions and the Core 0.3.1 pin unchanged.
- Add native mouse-capture/click tests for bar regions versus empty space, mirrored text placement checks and bounded input-band coverage tests. Live actor/world clicks and restart acceptance remain in LIVE_TEST_CHECKLIST.md.

## 0.8.6.1 - Flat arc strips and resource-coloured readouts

- Replace stroked arc tubes with continuous flat-cut strip slices, including MP, partial HP fills and integrated shield segments. Retain cached bow geometry and mirrored orientation.
- Group wrapped name/level metadata and resource readouts beside each arc. HP values and percentage follow the resolved health colour/gradient; MP and shield readouts use their configured colours. Target distance joins the same group.
- Keep the header-only targeting region aligned with the integrated header. Arc previews use Full/Compact formatting and the configured health gradient.
- Preserve Standard, independent casts, all saved positions, schema 13 and the exact Core 0.3.1 pin. Add flat-cap geometry tests and native checks of actual HP/MP glyph vertex colours, opacity and readable bounds.

## 0.8.6.0 - Optional arcs and independent cast modules

- Add opt-in mirrored Player/Target HP/MP arcs using existing formatting, HP colour/gradient, integrated shield and interaction policies. Keep Standard as the default and preserve saved positions when switching.
- Add independent Player Cast and Target Cast modules using the existing live cast renderer, with their own visibility, display fields, position, width, height, scale and opacity. Arcs omit attached casts; Standard avoids duplicates when the separate bar is enabled.
- Extend the existing editor with arc-height resizing and independent cast previews. Cache bow geometry; do not rebuild paths for HP/MP changes.
- Migrate schema 12 to 13 without replacing old settings. Core remains pinned to 0.3.1. Add configuration, geometry, cast policy and native ImGui coverage; in-game acceptance remains required.

## 0.8.4.5 - Keep minimized HUD in Sentinel Modern

- Replaced 0.8.4.4's native minimized title strip with S Rank Sentinel's Core-header presentation. The same Modern header stays visible at its 56-logical-pixel height with icon, title, status, context, drag region and Core Expand/Close controls.
- Settings content, secondary categories and the action dock stay hidden while minimized. The header button, `/shud` and Dalamud Open Config restore the full dimensions without moving the window or changing its current page.
- Schema 12 adds only the minimized flag and expanded logical dimensions under Appearance. Minimize/restore saves them immediately so a header-sized ImGui record cannot erase the full size on reload. All schema-11 settings remain intact, with a schema-specific backup through the existing loader.
- Corrected logical outer constraints because Dalamud applies the UI scale: minimized height and the useful expanded minimum are scaled once.
- Added native ImGui geometry, mouse controls, keyboard/controller restore, minimized dragging, reload/reopen and Classic checks using the actual HUD presentation adapter and pinned Core shell, plus v11 migration preservation coverage.
- Sentinel Core stays exactly pinned to 0.3.1 / v0.3.1.0. Core source/packages and all HUD/gameplay services are unchanged.

## 0.8.4.4 - Working Modern minimize and restore

- Fixed the Modern header's minimize action: it previously requested native collapse while Core's expanded chrome set `NoTitleBar` and `NoCollapse`, so ImGui discarded the request.
- Queue collapse through Dalamud's top-level Window before Begin and use its native title strip only while minimized. The strip supports restoring, dragging and closing; the expanded window returns to its single Core Modern header.
- Apply collapse/expand requests once so the native restore arrow remains usable. `/shud` and Dalamud Open Config explicitly expand the same window; no size, position, configuration or HUD state is reset.
- Added repeated minimize/restore and queued-request regression coverage. Configuration schema 11 and the exact Sentinel Core 0.3.1 pin/packages remain unchanged; Sentinel Core itself was not changed.

## 0.8.4.3 - Complete responsive configuration flow

- Prepared long setting labels for the pinned Core row's measured text width, including labels without descriptions. Labels now contribute their full multiline height to the canonical row resolver.
- Routed Modern switches through those same responsive rows with a separate, keyboard-accessible On/Off control, preventing long toggle labels from covering the switch.
- Made Camera/Layout action pairs move to a following line when they do not fit; long individual action labels and diagnostic/anchor bullet rows wrap.
- Measured companion status-pill widths before allocating their columns and kept installed-but-disabled companions neutral. Corrected PvP Sentinel and Sentinel Profiles descriptions to match their current purpose.
- Added label/grapheme wrapping and action-flow cases, a wide/medium/narrow geometry matrix at three UI scales using the exact pinned Core source, and an entire customized schema-11 serialization/normalization preservation check.
- Hardened publication with explicit public-array parsing, uncached catalog requests, observed-version diagnostics, a bounded ten-minute verification deadline, and safe resume for a release already published by the same source commit.
- Preserved configuration schema 11, the five-companion Plugins implementation, all HUD/gameplay services and exact Core 0.3.1 pin/packages. Sentinel Core itself was not changed.

## 0.8.4.2 — Sentinel ecosystem status and responsive text

- Added a plug-icon **Plugins** destination to the Sentinel Modern primary rail for the five optional Dalamud companions: S Rank Sentinel, PvP Sentinel, Classy Sentinel, Sentinel Relay and Sentinel Profiles.
- Added an extensible central companion registry and public Dalamud status adapter that distinguishes Enabled, Installed/Disabled and Not Installed states and shows exposed installed versions.
- Refreshes companion state when configuration opens, when the Plugins page opens, after `ActivePluginsChanged`, and at a bounded three-second interval only while the page is visible; no plugin-manager internals or hard dependencies are used.
- Removed General's fixed-height outer child that clipped wrapped interaction text and rendered the interaction explanation through Core's measured responsive settings-row layout.
- Converted long muted helper/status copy throughout Player/Target/Focus/ToT, Awareness, Encounter, Camera, Convenience, Appearance, Layout and Diagnostics-adjacent sections to wrapped flow so narrow windows grow vertically instead of overflowing.
- Added tests for the Plugins navigation state, exact registry membership, case-insensitive installed matching, all three status states, installed versions and summary counts.
- Kept configuration schema 11, all saved settings/layouts and Classic rendering unchanged.

## 0.8.4.1 — Sentinel Modern 2 shell polish

- Hardened release publication so a verified release immediately dispatches the central Sentinel catalog generator and waits for the exact public catalog version and asset URLs; the hourly reconciliation remains a fallback.
- Updated the exact Sentinel Core pin to `v0.3.1.0` / `MarshalTitan.SentinelCore.UI` `0.3.1` and verified the required package hash.
- Replaced the duplicate native title strip in Modern mode with Core's single draggable custom header while keeping the native title bar unchanged in Classic.
- Adopted Core's unified full-bleed application surface, `0.9` procedural ambient intensity and reduced-motion-safe background treatment.
- Replaced letter placeholders with retained Font Awesome primary-rail icons and removed decorative letters from text-only secondary categories.
- Migrated shared combo, slider and colour controls to responsive Core settings rows so wrapped wording and controls stack instead of overlapping at narrow widths or larger UI scales.
- Added explicit reopen/expand handling for the custom collapse control while preserving window movement, resizing, close behavior and saved geometry.
- Kept configuration schema 11 and all gameplay/HUD defaults unchanged; Classic, module layouts, targeting, awareness, camera and quest convenience behavior are preserved.

## 0.8.4.0 — Sentinel Modern 2 reference release

- Replaced Sentinel HUD's local Modern preview renderer with the canonical `SentinelModernAppShell` from `MarshalTitan.SentinelCore.UI` 0.3.0.
- Added a compact Core header, six-item primary icon rail, conditional secondary category sidebars, Core page transitions and animated ambient treatment with Dalamud reduced-motion support.
- Adopted Core glass cards, settings rows, switches, status pills and an Appearance-only action dock; removed the local palette, card, navigation, toggle, ambient and motion implementation.
- Retained the persisted Modern theme selection without a configuration schema change and left the Classic renderer unchanged.
- Added retained and tested primary/category navigation state.
- Pinned Sentinel Core `v0.3.0.0` at commit `520f9b9837dc3286c24d6f812b8348a27f378a3e` and added CI verification for the `MarshalTitan.SentinelCore.UI` 0.3.0 package hash and packaged assembly version.
- Preserved all HUD modules, targeting interactions, editor/layout state, Awareness, camera, encounter, anti-AFK, quest convenience, commands, IPC and disposal behavior.
