# Architecture

## Boundaries

- `SentinelHUD.Core` is platform-neutral. It owns configuration data/defaults/migration, formatting, shield-segment math, conditional visibility, resolution-safe layout math and awareness policies.
- `SentinelHUD` is the API 15 Dalamud plugin. It resolves transient game objects every frame, renders modules, owns the configuration window and uses the official game/data services.
- `external/SentinelCore` is pinned to an immutable commit. Selected Core projects are compiled and packaged with this plugin; there is no shared runtime plugin.

## Rendering

HUD modules are independent, titleless, input-free ImGui windows in both locked and unlocked modes. They therefore have one invariant outer origin and cannot drift when editor state changes. `HudEditorInteractionRenderer` adds a separate transparent input layer only while unlocked: the module body moves the normalized anchor, left/right edges change explicit Width, top/bottom edges change Bar Height, and corners change both. Top/left resizing compensates the saved origin so the opposite edge remains stable. Foreground borders, labels and grips provide edit feedback without entering layout calculations. Moves and resizes are clamped to the viewport and debounced for persistence. Width and bar height are explicit dimensions, while Scale changes typography/style without distorting text or icons.

Health/MP/cast bars are Sentinel-rendered primitives. `ShieldBarPolicy` calculates an extension segment in unused HP space and an overlay segment for overflow/full-HP shields, keeping all colours inside one fixed bar. `HpColourPolicy` is platform-neutral and either preserves the resolved static role/disposition colour or continuously interpolates low-red → mid-yellow → high-green from current HP fraction. Target disposition uses Dalamud's supported character hostile/party/alliance/friend flags only in static mode. Player casting uses the same cast renderer as Target and Focus with independent field switches and no idle placeholder. The optional exact-HP supplement probes both combined `_TargetInfo` and split `_TargetInfoMainTarget`, anchors to the visible HP component's `GetBounds` result and draws through the foreground overlay; it never edits native nodes.

Game objects are never cached between frames. Player, target and focus references are resolved for the current draw only. Target-of-target and Focus Target's Target use the current parent actor's target ID and a direct object-table lookup. Static job metadata and action/status names are cached.

`ActorInteractionRenderer` is a reusable, explicit-input boundary for actor references. Locked module windows retain `NoInputs`; after content rendering, the helper creates transparent windows only over enabled whole-module, header/name or child-row regions. Child rows use a higher interaction priority than their parent module. `ActorTargetingService` re-resolves the game-object ID at left-click time, checks current validity/targetability and sets `ITargetManager.Target`. `ActorContextMenuService` independently re-resolves the ID for right-click and calls the current `AgentHUD.OpenContextMenuFromTarget` path, allowing FFXIV to populate valid actor/context actions rather than maintaining a static menu. No world-click simulation or native actor pointer is retained. Edit mode does not draw this interaction layer at all.

## Awareness services

`ISelfHighlightService` isolates low-level silhouette rendering from all HUD modules. `NativeSelfHighlightService` uses FFXIVClientStructs' current `GameObject.Highlight` virtual function on the validated local-player address. This changes draw-object outline state only and propagates through the game's own actor rendering. The official current structure exposes `ObjectHighlightColor` as a fixed byte palette (`None`, `Red`, `Green`, `Blue`, `Yellow`, `Orange`, `Magenta`, `Black`) and no arbitrary RGB field. Yellow/Green/Blue select exact entries. Custom RGB remains persisted and is resolved to the closest documented palette entry with the applied entry shown in UI/diagnostics. White fails closed because it has no documented exact entry; it is never substituted with Yellow. The service never sets any `ITargetManager` property. Disable/disposal clears only the current local actor; stale actor pointers are never retained across zone changes.

`PlayerPositionMarkerRenderer` reads the current local actor once per draw, uses the same login/combat/duty policy inputs as Self Highlight, casts one short downward terrain ray, builds a 24-point disc in the returned surface plane and projects it through `IGameGui.WorldToScreen`. Outer and inner point buffers are allocated once. The contrasting border is filled inward from the configured world radius, so border thickness never enlarges the marker. The danger colour changes only the fill and does not change how the true player point is obtained.

## Encounter-awareness boundary

`EncounterAwarenessService` owns independent providers and exposes only cached `DangerArea` geometry plus a point-in-shape result. `DangerArea` is platform-neutral and implements X/Z containment for circles, donuts, forward rectangles, cones, finite-width lines and crosses with a vertical floor tolerance. The marker never knows whether a shape came from the client, Splatoon or a future Sentinel encounter profile.

`NativeCastDangerProvider` samples hostile casting battle NPCs at 50 ms intervals, caches static action-shape metadata, and emits geometry only for current actions that have a native omen and a supported standard cast type. Unknown or encounter-specific mechanics are skipped. `SplatoonDangerProvider` polls optional `Splatoon.IsLoaded` and `Splatoon.GetActiveDrawGeometryV1` IPC at 150 ms while enabled; install status is checked only every two seconds while disabled. Splatoon v1 shapes without explicit danger semantics are withheld unless the user enables the clearly labelled trust-all-visible-geometry option. No Splatoon/Avarice source is copied and neither plugin is a dependency.

## Camera boundary

`IExtendedCameraZoomService` isolates the optional camera write from the HUD and awareness renderers. It uses the current API 15 world-camera layout confirmed against Cammy and modifies only maximum zoom (plus current/interpolated zoom when clamping during restore). It captures the pre-existing maximum, restores it on disable/disposal and abandons stale pointers on camera replacement. It pauses in first person, GPose, cutscenes, territory transitions and while known camera-controller plugins are loaded. An unexpected writer trips a fail-closed conflict state rather than starting a per-frame write fight.

## Configuration persistence

`ResilientConfigurationStore` is intentionally compiled into the plugin assembly. The former generic adapter called `IDalamudPluginInterface.GetPluginConfig()` from `SentinelCore.Dalamud.dll`; current Dalamud discovers its live-update-safe generic config type through `Assembly.GetCallingAssembly()`, so the adapter assembly could not expose `SentinelHUD.Configuration` and fell back to legacy `$type` metadata. That path can return null across an in-game assembly update, after which the coordinator's former save-after-load behavior overwrote the file with defaults. The replacement reads the stable `IDalamudPluginInterface.ConfigFile`, creates one schema-specific backup before migration, invokes typed loading from `SentinelHUD.dll`, retries compatible raw JSON when old assembly metadata is the only problem, and never permits an unreadable un-backed-up file to be overwritten. The coordinator then applies explicit v1→v2→…→v7 migrations, debounces editor saves, flushes due saves during draw and performs a final flush during disposal. Schema 7 adds only context-menu, encounter-awareness, danger-colour and convenience fields; existing anchors, dimensions, field visibility, colours, awareness, native-target and camera values remain untouched. Diagnostics exposes the load route and backup location.

## Convenience boundary

`AntiAfkService` is isolated from rendering and runs only on framework updates when explicitly enabled. At a ten-second cadence it obtains the current `InputTimerModule` and resets three positive inactivity counters after 30 seconds. It creates no thread and no simulated input. Disable/disposal stops writes; there is no persistent client setting to restore.

## Future modules

The renderer and configuration model are organized by module kind so party/alliance, job gauges, status filtering, boss information, PvP data and optional versioned Sentinel IPC can be added without coupling the existing modules or requiring other Sentinel plugins.
