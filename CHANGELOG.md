# Changelog

## 0.8.4.3 — Complete responsive configuration flow

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
