# Native UI window occlusion - 0.8.9.1

**General > Respect Native Windows** defaults **On**, in Modern and Classic. Native FFXIV menus visually cover only the Sentinel gameplay pixels beneath their rectangular window bounds. Sentinel remains visible and interactive outside those bounds. Turn this setting Off to compare the previous rendering behavior. The configuration window is outside the clipping scope.

## Rendering decision

Dalamud's public IUiBuilder is an ImGui surface drawn on top of the game. Its background draw list is behind ImGui windows, not behind native FFXIV addons. The proposed native-overlay service (Dalamud PR 2874) was still open/unmerged when reviewed on 2026-11-02; it also requires native nodes rather than moving the current ImGui modules. Sentinel therefore uses overlap clipping on API 15 instead of introducing renderer hooks or rebuilding working modules.

Primary references: [IUiBuilder](https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/UiBuilder.cs), [native-overlay proposal](https://github.com/goatcorp/Dalamud/pull/2874), [addon lifecycle](https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Plugin/Services/IAddonLifecycle.cs), [AtkUnitBase](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Component/GUI/AtkUnitBase.cs), [AtkUnitManager](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Component/GUI/AtkUnitManager.cs), [ImVector](https://github.com/goatcorp/Dalamud/blob/master/imgui/Dalamud.Bindings.ImGui/ImVector.cs).

## Scope and implementation

- Gameplay modules keep their existing layout, geometry, textures, anti-aliasing, resource text and separate cast bars. Each affected draw command reuses its vertex/index range under disjoint GPU scissors obtained by subtracting the union of native window rectangles. Drawing is not replayed and the whole module is not hidden merely because a window opened. A fully covered module naturally has no visible portion.
- Applies to all module windows: Player/Target in Standard or Arc, Focus/ToT, the inline Focus Target's Target rows, Player/Target casts, Currency Tracker and Targeting Me. Shared background/foreground capture includes the position marker, supplementary stock-target HP text, actor hover outlines and editor chrome. Earlier/later drawing by other plugins or configuration is preserved, even if it shares an ImGui command with HUD drawing.
- Actor hit bands and editor surfaces are partitioned using the same rectangles. Covered areas have no Sentinel input island. Current native-window pointer positions cannot dispatch actor actions or editor drags. Entering a native window cancels an active edit drag; start a fresh drag in an uncovered area. Cutout edges do not become resize handles; only the original module edges do.
- The existing native actor context-menu opening/placement and short actor-input suspension remain. Menus are ordinary occluders; only their actual overlap is cut from HUD rendering. There is no module-hide workaround.
- Native game-rendered outlines, omen tinting and external Splatoon world-space rendering are not replaced or intercepted by this ImGui refinement. The 0.8.9.0 threat-history/Hub IPC behavior remains intact.

## Window detection and performance

Discovery reads the native manager's bounded loaded-unit list on addon PostSetup/PreFinalize invalidation, with a once-per-second fallback for already-loaded metadata. It selects native window/window-collision metadata plus known popup/tooltip types, excluding persistent HUD surfaces (underscore HUD names, chat log, nameplates, fly text, etc.). This is not a fixed inventory-only whitelist. Candidate IDs/names are cached; native pointers are resolved afresh and never retained across frames. Only candidate readiness, visibility, alpha, root visibility and native GetWindowBounds are refreshed each draw, so normal moves, resizing, hiding and closing do not wait for the discovery timer. Main viewport origin is added to client-space native bounds for windowed/multi-monitor play.

Native window rectangles are clipped to the viewport and combined without replacing overlapping windows with an oversized enclosing box. Rectangle/command/input buffers and generated fragment names reuse capacity. Unaffected module lists are skipped; repeated command scissors reuse their split result. No per-frame logs, texture captures, render hooks or world-object scans are added.

The mask uses window rectangles, not per-pixel native artwork: transparent margins inside native collision bounds also cover HUD pixels. An unusual addon without usable window/collision metadata may require a focused follow-up. Native data unavailable during transitions reports that state and leaves gameplay rendering usable rather than hiding arbitrary screen regions.

## Diagnostics and persistence

Open **Diagnostics > Native UI Occlusion** for the setting/state, visible blocking addon count, cached candidate count, addon names and bounds, discovery passes, clipped gameplay module count and shared-layer count. Shared layers combine marker/text/editor drawing, so their count is separate from module windows. No native pointers are shown or persisted.

Schema 16 adds only RespectNativeWindows=true. Existing false choices round-trip; all prior HUD positions/styles/scales, arcs/casts, currency selections, encounter colors, camera, convenience and plugin-window settings are preserved. The protected configuration store keeps its standard schema-v15 migration backup. Core stays exactly 0.3.1.

## Live acceptance checklist

Automated tests run synthetic native ImGui frames and test rectangle union/subtraction, untouched buffers, foreign draw prefixes/suffixes, tight arc input, visible/blocked input and editor drag/resize. They do not launch FFXIV. These game checks remain to be performed:

- [ ] Update from live 0.8.9.0 without deleting config. Confirm schema 16, backup, default On, old settings and saved positions. Test Modern/Classic General and Diagnostics. Toggle Off/On for comparison; restart/reload with each setting value.
- [ ] Open Chocobo Saddlebag across the middle of curved Player and Target bars. Only overlapping bar/text pixels are hidden, including a bar cut in two. Outside portions remain. Close Saddlebag and confirm full bars return on the next frame.
- [ ] Repeat with Inventory, Character, Achievements, Map, Duty Finder/duty window, Currency and quest windows. Note addon names/bounds in Diagnostics. Test overlapping native windows, side panels and nested popup menus.
- [ ] Move/resize native windows and test game UI scales, windowed/borderless, a secondary monitor/negative desktop origin and resolution changes. Window cutouts follow bounds without a one-second movement delay or persistent ghost gaps.
- [ ] Test Standard Player/Target, Focus, ToT and inline focus-target target; separate Player/Target casts; threat counter; compact/full Currency Tracker; all position-marker styles and supplementary stock-target text. Configuration stays a normal plugin window.
- [ ] Click native controls over HUD actor regions; left/right clicks must not target/open a Sentinel actor menu through a native panel. Click uncovered actor headers/arcs/inline rows and verify normal interactions. Empty arc interiors remain click-through.
- [ ] Right-click a Sentinel actor module. Native context menu stays visible and usable; only overlap is clipped. The originating module remains visible outside the menu; no whole-module hide occurs.
- [ ] Unlock HUD. Drag and resize uncovered portions; original edges remain resize handles. Native window interiors have no invisible editor rectangle. Entering a native window during a drag cancels the edit without moving/resizing through it. Close the panel and start a fresh drag normally.
- [ ] Nearby players/enemies/world geometry do not occlude Sentinel HUD. Persistent native HUD elements do not blank large screen areas. Log out/change territory/unload/reload and verify no stale cutouts or repeated errors.
- [ ] Verify unchanged encounter colors/overlay, cast information, curved liquid visuals, currency values, camera, PvP counter and Hub threat-death history. Confirm smooth frame pacing with several native windows open.
