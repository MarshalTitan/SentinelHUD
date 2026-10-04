# Sentinel HUD

Sentinel HUD is a modular Dalamud enhancement layer for the normal FFXIV HUD. It provides compact player, target, focus-target and target-of-target panels whose modules and individual fields can be enabled independently. It does not attempt to replace the full native HUD.

Current release: **0.8.3.0** · Dalamud API **15** · .NET **10**

## Features

- Sentinel Modern configuration theme preview: midnight-navy surfaces, electric-blue controls, restrained violet/teal ambient rings, rounded cards, grouped sidebar navigation, modern switches and clear lock/edit status. Classic remains available from Appearance or the sidebar without changing HUD gameplay presentation.
- Player: compact name/job/level header, independently selectable HP values, MP text/bar modes, integrated shield display, own-cast name/bar/percentage/remaining time and a compact status summary.
- Target: compact player/NPC-aware header, independently selectable HP values, optional MP text/bar modes, distance, integrated shield display, cast information and statuses.
- Focus target: name, HP, optional MP text/bar modes, distance, shield and cast information, plus a compact Focus Target's Target row with safe click-to-target.
- Target-of-target: an independent small module with name, HP and optional distance.
- Visual layout editor: titleless drift-free geometry, whole-panel mouse dragging, all-edge/corner resizing for independent width and bar height, per-module scale, precision controls, background/border appearance, one-module reset, appearance copy and complete layout reset.
- Per-module visibility conditions: Always, Combat Only, Duty Only or Combat or Duty. In locked gameplay mode, each actor module can optionally target its represented actor with left-click and open FFXIV's context-sensitive native actor menu with right-click. Whole-module and header/name-only regions are available; all unrelated space remains click-through.
- Custom compact HP/MP/cast bars with Left/Center/Right text alignment, Full/Compact numbers, optional health-state gradients, hostile-red static disposition colouring and a blue shield segment inside the HP bar.
- Optional exact-HP supplement anchored read-only beneath the visible native target HP gauge. It supports both combined `_TargetInfo` and split `_TargetInfoMainTarget` layouts, including Always/Combat Only modes and X/Y fine tuning.
- Resolution-safe normalized positions with screen-boundary protection and delayed persistent saves.
- Native, model-conforming self silhouette with Off, Always, Combat Only and Duty Only modes. It follows the animated model, equipment, weapons, mount and ornament without changing any target state.
- Independent Player Position Marker centred on the local actor's exact terrain-resolved origin. Camera Facing (default) stays circular and readable at shallow camera angles and extended zoom; Ground Projected retains the original terrain-plane presentation. Both support Off/Always/Combat Only/Duty Only modes, independent colour, 0.01–0.60 size control, opacity and inward border controls. Encounter Awareness changes either style to a separately configured danger colour when that exact point intersects a trusted hazard.
- Encounter Awareness providers: conservative current hostile-cast geometry for standard visible circle/donut/rectangle/cone/line/cross actions, plus optional fail-closed Splatoon geometry IPC. Unknown and encounter-specific mechanics are not guessed.
- Independent PvP Targeting Me Counter with Frontline-only or all-PvP visibility, optional zero hiding, optional job/detail rows, scalable crisp digits and configurable normal/moderate/high/extreme colours. It requires no PvP Sentinel installation.
- Optional extended third-person zoom with a configurable 20–100-yalm maximum, automatic restoration, special-camera safeguards and known camera-plugin conflict handling.
- Optional Prevent AFK Disconnect convenience switch, default Off, which periodically resets current client inactivity timers without movement, chat, key presses or controller input.
- Independent quest convenience controls, all defaulting to Off/Manual: ordinary-dialogue advancement that pauses for choices, game-permitted cutscene skipping through FFXIV's normal skip dialog, and validated choose-one reward selection by first option, current-job equipment compatibility or stable Allagan Piece IDs/value.
- Compact state diagnostics without frame-by-frame logging.

## Installation

Add the Sentinel custom repository to Dalamud:

```text
https://raw.githubusercontent.com/MarshalTitan/Sentinel/main/repo.json
```

Install **Sentinel HUD** from the plugin installer. No other Sentinel plugin is required.

## Commands

| Command | Result |
|---|---|
| `/shud` | Toggle configuration |
| `/shud lock` | Enter gameplay mode; optional actor targeting regions become active |
| `/shud unlock` | Show drag/resize editor affordances and disable target clicks |
| `/shud reset` | Reset all module positions |
| `/shud enable` | Enable overlays |
| `/shud disable` | Remove overlays while keeping configuration available |
| `/shud status` | Print a compact runtime state |
| `/shud help` | List commands |

## Configuration

Sentinel Modern organizes General, actor modules, Awareness, Encounter Awareness, Camera, Questing / Convenience, Appearance, Layout and Diagnostics through a persistent grouped sidebar and a bordered content card. Module pages retain collapsible Visibility, Information, Size/Layout and Appearance groups. The Appearance page can switch back to the original Classic tab layout at any time; the modern sidebar also has a one-click Classic fallback. The configuration window retains the standard ImGui collapse and close controls.

HP presentation is controlled by independent current, maximum and percentage switches, allowing number-only, percentage-only, current/maximum or combined formats. Full numbers remain the default; Compact displays values such as `295.9k` and `12.48m`. Shield Display supports Off, Text Only, Bar Only and Bar + Text.

MP Display supports Off, Text Only, Bar Only and Bar + Text. Player defaults to Bar + Text; Target and Focus Target default Off and omit the resource when the current actor has no meaningful maximum MP. The MP bar uses an FFXIV-style blue and obeys the module's independent width, bar height, number format and text alignment.

Player casting can independently show the current action name, progress bar, percentage and remaining time. It uses the Player module's width, scale, bar height and text alignment and occupies no space while the player is not casting.

Player, Target, Focus Target and Target-of-Target each have independent **Click to Target**, **Right-click native context menu** and **Clickable Area** settings. Direct actors are represented by object ID and re-resolved from the current object table at interaction time. Left-click assigns the current result through Dalamud's supported hard-target property. Right-click passes it to FFXIV's own HUD context-menu path, so the game decides which actions are valid for that actor and situation. Sentinel requests an adjacent on-screen popup position through the current `AgentContext` position facility, then refines it from the visible native `ContextMenu` addon's actual size. HUD modules, bars and awareness visuals continue drawing unchanged; only the transparent actor-input regions pause until the native menu closes. Unlocking removes all gameplay interaction regions and activates the move/resize editor instead.

Focus Target's Target is a compact child row in the Focus Target module. It is resolved fresh from the current Focus Target ID and current object table on every draw; absent actors are hidden rather than retained. On click, the shared actor resolver repeats the complete `current Focus Target → current target ID → current object-table object` lookup before setting the hard target. The full visible row is interactive and wins overlapping input over the parent Focus panel, so player, enemy, NPC and targetable world-object entries use the same final hard-target path as the working Target-of-Target module without retaining a child pointer.

### Configuration persistence

Schema 11 performs stepwise migrations and changes only fields introduced by the applicable schema. Schema 8 added marker Style; schema 9 added Skip Dialogue, Skip Cutscenes and Quest Reward Selection with Off/Off/Manual migration defaults; schema 10 added the Targeting Me Counter; schema 11 adds only the configuration-window theme selector and chooses Sentinel Modern for the preview. Existing position, visibility, colour, camera, awareness and convenience values remain untouched. The earlier reset was traced to the former generic configuration adapter living in `SentinelCore.Dalamud.dll`: current Dalamud discovers the live-update-safe config type from the calling assembly, so it could not see `SentinelHUD.Configuration` and could fall through to the legacy assembly-type-metadata path. A null result then reached the coordinator, which immediately saved fresh defaults. The replacement loader lives in `SentinelHUD.dll`, where typed discovery finds the stable configuration type. Before an older file is migrated, it creates a one-time `SentinelHUD.schema-vN.backup.json` beside Dalamud's normal configuration file, and it safely retries the same JSON without obsolete assembly type metadata if a live update still returns null or throws. An unreadable file is backed up and logged before defaults may be saved; if the backup itself cannot be created, writes are refused rather than overwriting the only recoverable copy. Diagnostics shows the load path and backup location.

### Integrated health bars

Health, shield, MP and cast bars use the module's independent Width and Bar Height rather than stretching the complete UI. At less than full HP, shield extends from the current HP endpoint into unused bar space. Any shield beyond that space overlays the rightmost part of the HP fill; at full HP the blue segment overlays the rightmost proportional part of the bar. Static / Role-Based mode keeps hostile characters red by default. The optional Health-State Gradient smoothly blends red at 35% and below through yellow at mid health to green from 80% upward; separate Player and Target/Focus/ToT modes prevent the two groups from being coupled.

### Native Target HP Overlay

The optional overlay probes the current combined `_TargetInfo` and split `_TargetInfoMainTarget` stock layouts, selects the visible variant, and reads the actual HP gauge node's screen bounds every frame. It uses node 19 for combined Target Info and node 13 for split Main Target, with read-only addon-root bounds as a safe fallback. Moving the stock target element through FFXIV HUD Layout therefore moves the supplement with it. Sentinel HUD never inserts nodes into or writes to the native addon. Diagnostics reports each variant's availability/visibility, selected addon, anchor source, position, size and any failure reason.

## Awareness implementation and safety

### Self Highlight

Version 0.2.0.0 removed the former projected circle/line/trapezoid prototype from user-facing rendering. `NativeSelfHighlightService` calls the current FFXIVClientStructs `GameObject.Highlight` render function for the local actor. FFXIV applies the outline to the actor's real draw object and propagates it to weapons, mounts and ornaments. The service never reads or writes hard target, soft target, mouseover target, nameplate mouseover, controller target, tab target, interaction target or action target.

The current FFXIVClientStructs renderer exposes a fixed `ObjectHighlightColor` palette rather than arbitrary RGB: Red, Green, Blue, Yellow, Orange, Magenta and Black. Sentinel HUD exposes Yellow/Green/Blue directly and restores a Custom picker that selects the closest real palette entry while showing the actually applied colour. White is not present in the documented palette, so a White request now fails closed and never silently renders Yellow. Native opacity/intensity is not exposed. A true arbitrary-colour silhouette would require a supported model mask/depth path; Sentinel HUD does not spoof target state, try unknown palette values, inject a graphics hook, or return to primitive geometry.

### Player Position Marker

The marker begins at the local actor's real world origin and casts a short downward collision ray using current FFXIVClientStructs terrain collision. Neither style derives its centre from the animated model, head, feet or screen-space bounds. **Camera Facing** projects that exact resolved point once and draws a compact screen-space circle, so camera pitch cannot compress it into a line and extended zoom cannot make it unreadably small. The existing 0.01–0.60 radius setting maps deterministically to a 1.0–19.2 px outside radius in this mode. **Ground Projected** retains the 24-point terrain-plane disc for users who prefer a perspective-grounded marker.

This presentation follows the current Avarice player-dot principle (project the actor position and draw a screen-space filled circle) while keeping Sentinel's independent terrain-resolution, conditional visibility, border and danger-provider systems; no Avarice source or runtime dependency is included. The optional contrasting border is drawn inward in both styles, so it never enlarges the configured outside radius. Danger changes only the colour. If collision data is temporarily unavailable, the renderer fails safely to the actor origin and reports that fallback and active style in Diagnostics.

### Encounter Awareness

`EncounterAwarenessService` combines independent cached danger providers and performs allocation-free point-in-shape tests against the local player's world origin. `NativeCastDangerProvider` samples the object table at a bounded interval and only publishes hostile casts with an action omen and a supported standard shape. Unsupported, hidden, scripted and boss-specific mechanics are withheld rather than inferred.

`SplatoonDangerProvider` uses Splatoon's current optional `Splatoon.GetActiveDrawGeometryV1` IPC; no Splatoon code is copied and Sentinel HUD works normally without it. Geometry IPC v1 does not label every active drawing as danger, safe or informational. Sentinel therefore ignores unclassified drawings by default. An explicit advanced opt-in can treat all supported visible Splatoon shapes as danger, but it can also flag safe-zone artwork. Encounter-specific scripts and presets remain in Splatoon or a future separately maintained Sentinel encounter-resource repository, not in the HUD core assembly. Current Avarice was also reviewed for future hitbox/melee/positional concepts; no Avarice code or dependency is included in this release.

Before creating any Sentinel-owned encounter definition, check Splatoon's maintained [official presets and scripts](https://github.com/PunishXIV/Splatoon/tree/main/Presets). Existing vetted upstream resources should be preferred; a future Sentinel resource repository is for uncovered encounters or Sentinel-specific metadata, keyed by territory/content/actor IDs rather than localized display names.

### Targeting Me Counter

The counter is a standalone Sentinel HUD awareness module; it does not load, call or require PvP Sentinel, MINION, RSR or another targeting plugin. `PvPThreatTracker` samples the current object table at a bounded 125 ms cadence and never retains game-object pointers. In Frontline it treats native zero-based Battalion values `0`, `1` and `2` as authoritative teams. If authoritative team data is unavailable, the counter alone falls back conservatively to the current hostile flag while excluding the local player, party, alliance and roster allies.

Only live, targetable, non-dead enemy player characters currently present in the client actor set are eligible. A player counts only when their current hard-target object ID equals the local player's object ID; soft targets, mouseover, queued/future actions and opponents outside the client actor set are not observable and are not guessed. The default borderless presentation can hide zero, render a large dedicated digit font, and optionally list jobs or targeter name/job/distance. Warning colour derives from the same targeter/nearby-density policy as PvP Sentinel, while a zero count always remains visually normal. Diagnostics exposes PvP scope, local Battalion, classification source/authority, observed player/enemy and nearby ally/enemy counts, current targeters and the reason when the module is inactive.

### Prevent AFK Disconnect

The opt-in convenience service checks the current FFXIVClientStructs inactivity timer module every ten seconds and resets positive timers only after 30 seconds. It does not create a worker thread, synthesize keyboard/controller input, move the actor or send chat. Disabling or unloading stops resets immediately, after which the client resumes normal timer accumulation.

### Questing / Convenience

`QuestConvenienceService` is owned directly by the plugin lifecycle and does not run through `HudRenderer`. **Skip Dialogue** touches only the visible `Talk` addon at a bounded cadence. It explicitly pauses while `SelectString`, `SelectIconString` or `SelectYesno` is visible, so it never chooses branching responses or Yes/No answers.

**Skip Cutscenes** asks the current `AgentCutscene` to open FFXIV's own skip dialog using the live cutscene callback. A successful request creates a five-second confirmation token. While that token is active, Sentinel resolves the exact skip addon ID published by `AgentCutscene` (with the dedicated `CutSceneSelectString` name as a setup-frame fallback), validates its list structure and executes option zero through the addon's callback. This covers the list-style `Skip cutscene? / Yes. / No.` prompt used by FC submarine interactions. Merely highlighting a list item is not treated as confirmation. If the callback is absent, the request is rejected, the dedicated prompt is dismissed or the token expires, Sentinel fails closed and clears the token. It never confirms generic `SelectYesno`/`SelectString` prompts, patches the skippable check or synthesizes Escape/confirm input.

Reward automation activates only for a visible `JournalResult` with positively identified choose-one entries. Each action re-resolves the addon and fingerprints item IDs, quantities and indices. Selection and completion are separate, throttled steps; completion requires the same fingerprint and the game's enabled Complete button. Manual mode never acts, and windows containing only guaranteed rewards are left untouched. Current Job uses `Item`, `EquipSlotCategory`, `ClassJobCategory`, current `ClassJob` and item-level game data—not localized item names—and ranks narrower compatible categories before item level and source order. Allagan mode recognizes stable item rows 5824–5827 and ranks their `PriceLow × quantity`, falling back to the first selectable reward when required.

### Extended camera zoom

The camera service was checked against Cammy commit `c9895b2ca7a4d285aa967be46a2963cbaddd7282` (2026-05-04, API 15). Sentinel HUD uses only the narrow current/max-zoom camera fields needed for ordinary third-person zoom; it does not copy Cammy's free-camera, FoV, collision, or camera hooks. The service restores the captured normal maximum when disabled or unloaded, clamps an over-limit current zoom during restoration, and pauses in first person, GPose, cutscenes and territory transitions.

If Cammy, EasyZoom, EasyZoomReborn, ZoomTilt or PyonCam is loaded, Sentinel HUD does not write camera state. It also detects an unexpected camera-limit writer and pauses instead of continually fighting it.

## Sentinel Core adoption

Sentinel HUD is the first substantial Sentinel Core consumer. The repository pins Sentinel Core at commit `bef05184e357474216b26dd2865549d9c8b401a7` as a Git submodule and consumes:

- configuration coordination and delayed/final saves;
- bounded diagnostics and changed/throttled state gates;
- safe lifecycle/disposal utilities;
- dynamic job/role metadata;
- Dalamud configuration/logging adapters;
- the shared Sentinel palette and balanced ImGui style scopes;
- common identity/versioning.

The selected Core assemblies are packaged inside Sentinel HUD. Users do not install Sentinel Core separately, and Sentinel HUD has no runtime dependency on any other Sentinel plugin.

## Development

Requirements: .NET 10 and current Dalamud API 15 development files.

```powershell
git clone --recurse-submodules https://github.com/MarshalTitan/SentinelHUD.git
cd SentinelHUD
dotnet restore SentinelHUD.slnx -m:1
dotnet run --project tests/SentinelHUD.Core.Tests/SentinelHUD.Core.Tests.csproj -c Release
dotnet build src/SentinelHUD/SentinelHUD.csproj -c Release --no-restore
```

On Windows, place the current Dalamud development files under `%AppData%\XIVLauncher\addon\Hooks\dev`, or set `DALAMUD_HOME` to the directory containing `Dalamud.dll`.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Live test steps are in [LIVE_TEST_CHECKLIST.md](LIVE_TEST_CHECKLIST.md).

## Screenshots

Screenshots will be added after the first in-game layout and colour pass.

## Known limitations

- Targeting Me Counter sees only current hard targets for enemy player actors presently loaded and resolvable by the client. It cannot see soft-target, mouseover, intent, queued/future actions or distant/unloaded enemies; conservative fallback classification may intentionally undercount rather than label an ally as hostile.
- FFXIV's native silhouette API has a fixed seven-colour palette. Custom chooses and explicitly reports the nearest real palette colour; it is not arbitrary RGB. White is unavailable and disables the outline rather than rendering the wrong colour. Native opacity/intensity is also unavailable.
- Exact White/arbitrary-RGB model silhouettes would require a separate depth/stencil render path that Dalamud does not currently expose as a supported high-level API; this release deliberately does not inject one.
- Generic native danger detection covers only currently casting hostile actions with a supported standard telegraph shape. Many encounter mechanics require maintained encounter scripts and are intentionally not guessed.
- Splatoon geometry IPC v1 does not classify all drawings as danger versus safe/informational. Unclassified geometry is ignored by default; the opt-in trust mode can produce false danger states.
- Prevent AFK Disconnect touches current internal inactivity timer fields and may need maintenance after game updates. It is default Off, isolated behind one service and never synthesizes user input.
- Quest reward parsing and UI actions depend on the current `JournalResult` value layout and FFXIVClientStructs addon functions. They are isolated, default Manual, validate the complete window fingerprint before confirmation and may fail closed after a game UI change until updated.
- Dialogue skipping deliberately ignores response lists and Yes/No prompts. Cutscene skipping acts only when FFXIV exposes its normal skip callback; protected or unskippable cutscenes remain untouched.
- The position marker's downward terrain ray can be unavailable during loading or on unusual collision surfaces; the actor origin is used temporarily and Diagnostics reports the fallback.
- Both position-marker styles are Dalamud overlays without depth-buffer occlusion, so foreground terrain can occasionally cover incorrectly. Camera Facing intentionally keeps a stable screen-space radius for readability rather than shrinking with camera distance; Ground Projected retains natural perspective scaling and can flatten at shallow angles.
- Extended zoom changes an internal camera limit and can require maintenance after FFXIV patches. It defaults off, is isolated behind one service, validates camera state, and restores on disable/disposal.
- Sentinel HUD intentionally yields camera control when a known camera plugin is loaded; use one zoom controller at a time.
- Status display is intentionally a compact first-five summary; filtering and prioritization are future work.
- Some non-character objects do not expose meaningful HP, job, shield, status or cast data; unsupported fields are omitted safely.
- The native target supplement supports current combined and split stock target layouts and falls back to read-only root bounds if the expected gauge node is unavailable. A future FFXIV node-ID change may require maintenance; diagnostics exposes exactly which lookup failed.
- Focus Target's Target can only resolve actors currently exposed in the client object table. The row clears immediately when resolution fails, and click-to-target refuses actors that have vanished or are not currently targetable.
- Live native rendering, terrain collision, camera behavior, controller safety and full restart persistence require in-game testing because automated CI cannot launch FFXIV.
