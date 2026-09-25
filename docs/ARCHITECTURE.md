# Architecture

## Boundaries

- `SentinelHUD.Core` is platform-neutral. It owns configuration data/defaults/migration, formatting, shield-segment math, conditional visibility, resolution-safe layout math and awareness policies.
- `SentinelHUD` is the API 15 Dalamud plugin. It resolves transient game objects every frame, renders modules, owns the configuration window and uses the official game/data services.
- `external/SentinelCore` is pinned to an immutable commit. Selected Core projects are compiled and packaged with this plugin; there is no shared runtime plugin.

## Rendering

HUD modules are independent ImGui windows. When locked, they are titleless, immovable and click-through. When unlocked, enabled conditional or target-dependent modules remain visible as movable placeholders. A saved position always represents the visible module-body/content origin: edit title-bar height is measured after `Begin`, removed from persistence math and reapplied only to the outer edit window. Auto-position frames are not written back as user drags, so repeated unlock/lock transitions cannot accumulate vertical drift. Position anchors are normalized against the main viewport work area and converted back to pixels using the current content size. Every content body is clamped to a reachable screen boundary. Width and bar height are explicit layout dimensions; scale changes typography/style without horizontally distorting text or icons.

Health/MP/cast bars are Sentinel-rendered primitives. `ShieldBarPolicy` calculates an extension segment in unused HP space and an overlay segment for overflow/full-HP shields, keeping all colours inside one fixed bar. `HpColourPolicy` is platform-neutral and either preserves the resolved static role/disposition colour or continuously interpolates low-red → mid-yellow → high-green from current HP fraction. Target disposition uses Dalamud's supported character hostile/party/alliance/friend flags only in static mode. The optional exact-HP supplement reads `_TargetInfo` root position, scale and bounds and draws through the foreground overlay; it never edits native nodes.

Game objects are never cached between frames. Player, target and focus references are resolved for the current draw only. Target-of-target uses the current target ID and a direct object-table lookup. Static job metadata and action/status names are cached.

## Awareness services

`ISelfHighlightService` isolates low-level silhouette rendering from all HUD modules. `NativeSelfHighlightService` uses FFXIVClientStructs' current `GameObject.Highlight` virtual function on the validated local-player address. This changes draw-object outline state only and propagates through the game's own actor rendering. It never sets any `ITargetManager` property. Disable/disposal clears only the current local actor; stale actor pointers are never retained across zone changes.

`PlayerPositionMarkerRenderer` reads the current local actor once per draw, uses the same login/combat/duty policy inputs as Self Highlight, casts one short downward terrain ray, builds a 24-point disc in the returned surface plane and projects it through `IGameGui.WorldToScreen`. Outer and inner point buffers are allocated once. The contrasting border is filled inward from the configured world radius, so border thickness never enlarges the marker.

## Camera boundary

`IExtendedCameraZoomService` isolates the optional camera write from the HUD and awareness renderers. It uses the current API 15 world-camera layout confirmed against Cammy and modifies only maximum zoom (plus current/interpolated zoom when clamping during restore). It captures the pre-existing maximum, restores it on disable/disposal and abandons stale pointers on camera replacement. It pauses in first person, GPose, cutscenes, territory transitions and while known camera-controller plugins are loaded. An unexpected writer trips a fail-closed conflict state rather than starting a per-frame write fight.

## Configuration persistence

Sentinel Core's `ConfigurationCoordinator` loads and normalizes schema version 4, saves ordinary settings immediately, debounces drag saves, flushes due saves during draw and performs a final flush during disposal. Module anchors, scale, width, bar height, MP modes, HP colour modes, background/border appearance, field visibility, awareness settings, native-target offsets and camera settings all persist. Schema-1/2 migration preserves existing positions, scale, colours, enabled states, awareness and camera settings while deriving marker/shield presentation from legacy switches; schema-3 migration maps the former Player `ShowMp` switch to the new MP presentation mode without resetting any layout or appearance data.

## Future modules

The renderer and configuration model are organized by module kind so party/alliance, job gauges, status filtering, boss information, PvP data and optional versioned Sentinel IPC can be added without coupling the existing modules or requiring other Sentinel plugins.
