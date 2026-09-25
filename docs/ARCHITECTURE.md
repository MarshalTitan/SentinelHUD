# Architecture

## Boundaries

- `SentinelHUD.Core` is platform-neutral. It owns configuration data/defaults/migration, formatting, resolution-safe layout math and self-highlight mode/colour policy.
- `SentinelHUD` is the API 15 Dalamud plugin. It resolves transient game objects every frame, renders modules, owns the configuration window and uses the official game/data services.
- `external/SentinelCore` is pinned to an immutable commit. Selected Core projects are compiled and packaged with this plugin; there is no shared runtime plugin.

## Rendering

HUD modules are independent ImGui windows. When locked, they are titleless, immovable and click-through. When unlocked, missing target-dependent modules remain visible as movable placeholders. Position anchors are normalized against the main viewport work area and converted back to pixels using the current module size. Every position is clamped to a reachable screen boundary.

Game objects are never cached between frames. Player, target and focus references are resolved for the current draw only. Target-of-target uses the current target ID and a direct object-table lookup. Static job metadata and action/status names are cached.

## Awareness services

`ISelfHighlightService` isolates low-level silhouette rendering from all HUD modules. `NativeSelfHighlightService` uses FFXIVClientStructs' current `GameObject.Highlight` virtual function on the validated local-player address. This changes draw-object outline state only and propagates through the game's own actor rendering. It never sets any `ITargetManager` property. Disable/disposal clears only the current local actor; stale actor pointers are never retained across zone changes.

`PlayerPositionMarkerRenderer` reads the current local actor once per draw, casts one short downward terrain ray, builds a 24-point disc in the returned surface plane and projects it through `IGameGui.WorldToScreen`. The point buffer is allocated once; there are no per-frame collection allocations or metadata lookups.

## Camera boundary

`IExtendedCameraZoomService` isolates the optional camera write from the HUD and awareness renderers. It uses the current API 15 world-camera layout confirmed against Cammy and modifies only maximum zoom (plus current/interpolated zoom when clamping during restore). It captures the pre-existing maximum, restores it on disable/disposal and abandons stale pointers on camera replacement. It pauses in first person, GPose, cutscenes, territory transitions and while known camera-controller plugins are loaded. An unexpected writer trips a fail-closed conflict state rather than starting a per-frame write fight.

## Configuration persistence

Sentinel Core's `ConfigurationCoordinator` loads and normalizes schema version 2, saves ordinary settings immediately, debounces drag saves, flushes due saves during draw and performs a final flush during disposal. Module layout anchors, scale, opacity, field visibility, awareness settings and camera settings all persist. Migration adds new settings without changing version-1 positions or field visibility.

## Future modules

The renderer and configuration model are organized by module kind so party/alliance, job gauges, status filtering, boss information, PvP data and optional versioned Sentinel IPC can be added without coupling the existing modules or requiring other Sentinel plugins.
