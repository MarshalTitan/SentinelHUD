# Sentinel HUD

Sentinel HUD is a modular Dalamud enhancement layer for the normal FFXIV HUD. It provides compact player, target, focus-target and target-of-target panels whose modules and individual fields can be enabled independently. It does not attempt to replace the full native HUD.

Current release: **0.2.0.0** · Dalamud API **15** · .NET **10**

## Features

- Player: name, job, role, level, independently selectable HP values, MP, shield and a compact status summary.
- Target: name, independently selectable HP values, optional distance, job/role, shield, cast name/bar/percentage and statuses.
- Focus target: name, HP, optional distance, shield and cast information.
- Target-of-target: an independent small module with name, HP and optional distance.
- Layout editor: unlock, drag, lock, per-module scale/opacity, one-module reset and complete layout reset.
- Resolution-safe normalized positions with screen-boundary protection and delayed persistent saves.
- Native, model-conforming self silhouette with Off, Always, Combat Only and Duty Only modes. It follows the animated model, equipment, weapons, mount and ornament without changing any target state.
- Independent Player Position Marker: a small terrain-projected world-space dot centred on the local actor's actual origin, with colour, custom colour, radius, opacity and border controls.
- Optional extended third-person zoom with a configurable 20–100-yalm maximum, automatic restoration, special-camera safeguards and known camera-plugin conflict handling.
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
| `/shud lock` | Lock all HUD modules and make them click-through |
| `/shud unlock` | Show movable editor windows |
| `/shud reset` | Reset all module positions |
| `/shud enable` | Enable overlays |
| `/shud disable` | Remove overlays while keeping configuration available |
| `/shud status` | Print a compact runtime state |
| `/shud help` | List commands |

## Configuration

Settings are separated into General, Player, Target, Focus Target, Target-of-Target, Awareness, Camera, Layout and Diagnostics tabs. Each displayed field has its own switch where practical. In particular, target and focus-target distance can be disabled independently.

HP presentation is controlled by independent current, maximum and percentage switches, allowing number-only, percentage-only, current/maximum or combined formats.

## Awareness implementation and safety

### Self Highlight

Version 0.2.0.0 removes the former projected circle/line/trapezoid prototype from user-facing rendering. `NativeSelfHighlightService` calls the current FFXIVClientStructs `GameObject.Highlight` render function for the local actor. FFXIV applies the outline to the actor's real draw object and propagates it to weapons, mounts and ornaments. The service never reads or writes hard target, soft target, mouseover target, nameplate mouseover, controller target, tab target, interaction target or action target.

The native renderer exposes a fixed palette. Yellow, Green and Blue are exact. White and Custom remain selectable and persistent, but currently resolve to the closest safe native palette colour; the configuration and diagnostics show the applied colour. Native opacity/intensity is not exposed. Sentinel HUD does not spoof target state or replace the silhouette with primitive geometry to work around these colour limits.

### Player Position Marker

The marker begins at the local actor's real world origin, casts a short downward collision ray using current FFXIVClientStructs terrain collision, and draws a small world-space disc on the returned surface plane. The projected disc follows camera movement and extended zoom and does not derive its location from the animated model, head, feet, or screen-space bounds. If collision data is temporarily unavailable, it fails safely to the actor origin and reports that fallback in Diagnostics.

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

- FFXIV's native silhouette API has a fixed colour palette. White/Custom use the nearest native colour, and native opacity/intensity is unavailable.
- Exact arbitrary-colour model silhouettes would require a separate depth/stencil render path that Dalamud does not currently expose as a supported high-level API; this release deliberately does not inject one.
- The position marker's downward terrain ray can be unavailable during loading or on unusual collision surfaces; the actor origin is used temporarily and Diagnostics reports the fallback.
- The position disc is world-projected but rendered as a Dalamud overlay without depth-buffer occlusion, so foreground terrain can occasionally cover incorrectly at extreme camera angles.
- Extended zoom changes an internal camera limit and can require maintenance after FFXIV patches. It defaults off, is isolated behind one service, validates camera state, and restores on disable/disposal.
- Sentinel HUD intentionally yields camera control when a known camera plugin is loaded; use one zoom controller at a time.
- Status display is intentionally a compact first-five summary; filtering and prioritization are future work.
- Some non-character objects do not expose meaningful HP, job, shield, status or cast data; unsupported fields are omitted safely.
- Live native rendering, terrain collision, camera behavior, controller safety and full restart persistence require in-game testing because automated CI cannot launch FFXIV.
