# Encounter Colors and Sentinel Encounter Overlay — 0.8.8.0

Open **Encounter Awareness** in Modern or Classic. Existing hazard detection keeps its existing enable switch. **Encounter Colors**, **Shared Encounter Palette**, and **Sentinel Encounter Overlay** are separate opt-in sections. The Sentinel HUD master switch gates all of them. Splatoon is optional; native recoloring works independently.

## Native omen colors

1. Enable **Enable Native AoE Recoloring**. If Chroma is loaded, disable that plugin before testing Sentinel's native writer; Sentinel pauses while Chroma is loaded to avoid competing writers.
2. Pick Hostile AoE Color through the RGBA picker, HEX input, or Red/Orange/Yellow/Green/Blue/Purple/Pink preset. Start with intensity 1x and alpha 1.
3. Observe a **new** hostile ground telegraph. Status changes from Ready to Active as supported native omens are observed. Existing omens created before enabling Sentinel are left alone.
4. Friendly/non-hostile omens retain their native appearance until **Enable Friendly AoE Recoloring** is enabled. Pick a distinct friendly color and test a native friendly telegraph or ground-target preview. Persistent ability effects and bespoke textures may use other VFX paths and retain their own artwork.
5. Enable **Cast Warning Transition** and choose an ending color. A uniquely matched visible hostile cast blends from the hostile tint through orange into the warning tint. When the warning binding opts into the shared palette, the palette Warning slot supplies the midpoint. Unknown/ambiguous links use the static hostile tint.
6. Adjust intensity/alpha while an owned omen is visible. Disable the feature, disable HUD, log out, change territory, or unload to release owned tint state. Reset Native Colors restores only these settings and switches this feature Off.

This is native resource tinting: it preserves omen geometry and does not add safe zones, infer mechanics, change targeting, or replace telegraph textures. Some custom materials blend tint differently. Missing signatures/layouts report Not Available and stop only this optional feature. A toggle off/on retries initialization.

### Native ownership and compatibility

The adapter hooks the native omen factory and VFX-data destruction entrypoint. Native callbacks use copied scalar settings; visible cast metadata is gathered on Framework at most every 100 ms when transitions are enabled. Static tints apply on creation/settings changes; matched warning tints animate on Framework. Only known hostile/non-hostile classifications are accepted. The registry is bounded to 512 objects and observes destruction before native memory is released. Concurrent destruction while a factory is returning invalidates that result. Address reuse has a fresh generation; restoration checks the current resource pointer and that Sentinel still owns the current color. A different writer's color is retained.

API-15 ABI facts isolated in the native adapter/memory view:

| Descriptor | Reviewed value |
|---|---|
| Omen factory parameters | uint, pointer, pointer, float, int, int, float, int, byte hostility, byte; pointer return |
| Known hostility flags | 1 hostile, 0 non-hostile; other values skipped |
| VFX-data destruction | void(pointer) |
| VFX-data size | 0x1E0, cross-checked against current FFXIVClientStructs |
| Resource pointer within VFX data | 0x1B8 |
| RGBA vector within render resource | 0xA0; writable range and finite values validated |

Windows memory protection is checked before reading the resource mapping or writing its tint. These checks and SDK layout/signature validation are safeguards, not a substitute for live acceptance after game patches. Hook ABI validity and actual material appearance still require the game. Native instances and debug drawings are never persisted in configuration.

## Shared palette

Enable the shared palette to edit Danger, Warning, Safe, Friendly, Target and Objective slots. Each native/overlay binding can choose **use encounter palette** plus a slot, or use its saved custom color. Editing a custom color/preset selects the custom source. If the palette is Off, bindings fall back to their saved custom color.

**Position marker uses encounter palette (Safe / Danger)** is an independent opt-in. It preserves the marker's own opacity, geometry, visibility and danger-detection controls. Existing outline/HP/MP/cast colors remain independent; the new target/objective integrations are the overlay ring/line. Native warning color has its own binding. Palette reset resets only the palette; native and overlay resets are separate.

## Splatoon connection and tests

Splatoon remains the renderer. Sentinel checks `Splatoon.IsLoaded` using Core's optional IPC wrapper, then binds only the plugin's public dynamic-element API. No ECommons, Splatoon or Pictomancy assembly is linked or bundled; the integration uses no private Dalamud plugin-manager APIs and needs no HTTP server.

Expected status: **Splatoon Integration: Ready**. Missing/unloaded/incompatible Splatoon shows **Not Available** with a concise detail. Failed probes are bounded to once per two seconds and do not repeatedly log errors. Plugin changes invalidate the public API binding and recreate owned elements when available again.

1. Enable **Sentinel Encounter Overlay**.
2. Choose radius in yalms, screen-pixel thickness, opacity, labels and custom/palette colors.
3. Click **Draw test circle around player**. It follows the player.
4. Select a valid actor and click **Draw test target ring** and **Draw test line to target**. Ring follows the current target; the line updates its world endpoints at most every 50 ms. Clearing/changing target updates the test presentation.
5. Click **Clear test elements**, or wait 15 seconds. Disable the overlay/HUD, log out, change territory, or unload to clear owned elements immediately when Splatoon is available.
6. Disable/reload Splatoon and verify HUD/native coloring remain usable, status changes cleanly, and reconnect works. Tests never reappear after a HUD restart unless requested again.

Every published element belongs to an exact `SentinelHUD.EncounterOverlay.<id>` namespace, has an absolute expiry plus territory-change destruction, and is removed only through its own namespace. Other plugins' drawings/layouts are never cleared. Owned/debug overlay geometry is excluded from the existing danger-dot geometry input, including trust-all mode, to avoid feedback from test circles.

### Infrastructure surface

`EncounterVisualService.Submit(request, durationMilliseconds)` accepts validated Circle, Ring, Line or Label intents, with world positions, optional actor anchoring for circle/ring/label, radius, thickness, RGBA, label and fill. Duration is 100–30000 ms and at most 32 elements are owned. `Remove(id)` removes an intent; `ClearTests()` removes only test intents. Requests are marshalled to Framework; public Splatoon element fields/methods are cached and updated when the intent changes. Circles/rings are actor-relative, lines use explicit world endpoints, and coordinate conversion accounts for Splatoon's X/Z/Y field order.

Ring currently means an outlined circle with configurable screen thickness. Filled circles support future safe/danger area intents. No boss scripts, mechanic library, automatic objective selection or replacement world renderer are included.

## Persistence and checks

Schema 15 adds only new opt-in defaults; upgrade from schema 14 keeps every existing module, camera, awareness, convenience, currency, cast and window setting. The protected store makes its normal schema-v14 backup. Core remains pinned to package 0.3.1 / commit `300703b360a58fb4b73bf7675d31fe8cab4614cd`.

Automated coverage includes old-field JSON preservation, custom/palette fallback, hostile/friendly gating, transition stops/opacity, lease ownership, destruction/reuse/capacity, unambiguous cast matching, validation, a Windows synthetic ABI memory check, and the actual public-field/method adapter against a fake renderer. This does **not** launch FFXIV or demonstrate a live hook/material. Use the release's LIVE_TEST_CHECKLIST.md for actual encounter visuals, cleanup, renderer reload and restart acceptance.

## Research and source boundary

The implementation is original Sentinel code. Chroma was consulted for native ABI/hostility facts; Pictomancy/SourOmen for the corresponding native data/resource mapping and destruction ABI. No GPL/AGPL implementation, package, renderer, UI or encounter script was copied into or linked by Sentinel. Splatoon's public API names/field contracts and ECommons' public API documentation informed the isolated adapter; no foreign source is vendored. Native byte patterns/offsets are version-sensitive interoperability descriptors, not a Chroma renderer dependency.

Primary references:

- [Chroma](https://github.com/Akechi-kun/Chroma), technical reference only.
- [FFXIVClientStructs VfxData](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Graphics/Vfx/VfxData.cs) and [resource](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Graphics/Vfx/VfxResourceInstance.cs).
- [Pictomancy VFX interop declarations](https://github.com/sourpuh/ffxiv_pictomancy/blob/master/Pictomancy/VfxDraw/VfxFunctions.cs), ABI facts only.
- [Splatoon public plugin](https://github.com/PunishXIV/Splatoon/blob/main/Splatoon/Splatoon.cs) and [element contract](https://github.com/PunishXIV/Splatoon/blob/main/Splatoon/Serializables/Element.cs).
- [ECommons Splatoon API](https://github.com/NightmareXIV/ECommons/blob/master/ECommons/SplatoonAPI/Splatoon.cs), public contract documentation.
