# Sentinel HUD

Sentinel HUD is a modular Dalamud enhancement layer for the normal FFXIV HUD. It provides compact player, target, focus-target and target-of-target panels whose modules and individual fields can be enabled independently. It does not attempt to replace the full native HUD.

Current release: **0.1.0.0** · Dalamud API **15** · .NET **10**

## Features

- Player: name, job, role, level, independently selectable HP values, MP, shield and a compact status summary.
- Target: name, independently selectable HP values, optional distance, job/role, shield, cast name/bar/percentage and statuses.
- Focus target: name, HP, optional distance, shield and cast information.
- Target-of-target: an independent small module with name, HP and optional distance.
- Layout editor: unlock, drag, lock, per-module scale/opacity, one-module reset and complete layout reset.
- Resolution-safe normalized positions with screen-boundary protection and delayed persistent saves.
- Safe self highlight with Off, Always, Combat Only and Duty Only modes; Yellow, Green, Blue, White and Custom colours; and adjustable intensity.
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

Settings are separated into General, Player, Target, Focus Target, Target-of-Target, Self Highlight, Layout and Diagnostics tabs. Each displayed field has its own switch where practical. In particular, target and focus-target distance can be disabled independently.

HP presentation is controlled by independent current, maximum and percentage switches, allowing number-only, percentage-only, current/maximum or combined formats.

## Self-highlight implementation and safety

Version 0.1.0.0 uses a **Sentinel-rendered screen-space body aura**. It projects the local player's position through Dalamud's supported `IGameGui.WorldToScreen` API and draws a compact glowing body outline with ImGui. It does not write to FFXIV's hard target, soft target, mouseover target, controller target, tab target or interaction target.

The safe renderer deliberately avoids native highlight hooks and target-state spoofing. Because the supported API does not expose the character model's exact silhouette, the first release uses a stylized body outline rather than a pixel-perfect outline around armour, weapons or mounts. The renderer hides when the player is off-screen, the FFXIV UI is hidden, or the local player is unavailable.

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

- The self highlight is a safe projected body aura, not the game's native mesh highlight.
- Projected outline height is approximate and may not perfectly match unusual character proportions, mounts or transformations.
- Status display is intentionally a compact first-five summary; filtering and prioritization are future work.
- Some non-character objects do not expose meaningful HP, job, shield, status or cast data; unsupported fields are omitted safely.
- Live rendering, controller behavior and full restart persistence require the first in-game test because automated CI cannot launch FFXIV.
