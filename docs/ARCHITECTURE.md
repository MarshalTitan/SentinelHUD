# Architecture

## Boundaries

- `SentinelHUD.Core` is platform-neutral. It owns configuration data/defaults/migration, formatting, resolution-safe layout math and self-highlight mode/colour policy.
- `SentinelHUD` is the API 15 Dalamud plugin. It resolves transient game objects every frame, renders modules, owns the configuration window and uses the official game/data services.
- `external/SentinelCore` is pinned to an immutable commit. Selected Core projects are compiled and packaged with this plugin; there is no shared runtime plugin.

## Rendering

HUD modules are independent ImGui windows. When locked, they are titleless, immovable and click-through. When unlocked, missing target-dependent modules remain visible as movable placeholders. Position anchors are normalized against the main viewport work area and converted back to pixels using the current module size. Every position is clamped to a reachable screen boundary.

Game objects are never cached between frames. Player, target and focus references are resolved for the current draw only. Target-of-target uses the current target ID and a direct object-table lookup. Static job metadata and action/status names are cached.

## Self highlight

`SelfHighlightRenderer` is isolated from the HUD modules so a future safe native renderer can replace it. The current implementation reads local-player position, combat/duty conditions and UI visibility, projects a body-height segment with `IGameGui.WorldToScreen`, then draws a layered screen-space aura. It never sets any `ITargetManager` property.

## Configuration persistence

Sentinel Core's `ConfigurationCoordinator` loads and normalizes schema version 1, saves ordinary settings immediately, debounces drag saves, flushes due saves during draw and performs a final flush during disposal. Module layout anchors, scale, opacity, field visibility and self-highlight colour all persist.

## Future modules

The renderer and configuration model are organized by module kind so party/alliance, job gauges, status filtering, boss information, PvP data and optional versioned Sentinel IPC can be added without coupling the existing modules or requiring other Sentinel plugins.
