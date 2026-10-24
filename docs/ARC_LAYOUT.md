# Optional arcs and separate casts (0.8.7.2)

In **HUD Elements > Player** or **Target**, choose **Bar style > Curved / Arc**. Classic exposes the same settings in Player/Target tabs. Standard stays the default, and changing style preserves saved module anchors. **Place arc near centre** remains an explicit optional positioning action.

## Side readouts

Player values sit on the outer-left side and percentages on the inner-right; Target mirrors them. **Total HP** sits on the upper outer side; **current HP** and **HP %** occupy the lower outer/inner sides. MP values/percentages follow in the lower section. **Value format: Current + Total** shows the split HP total and preserves current/total MP; **Current Only** omits totals. The maximum-HP information switch can independently hide the upper HP number. **Show resource percentages** and individual HP switches remain respected. Full/Compact number formatting applies, and literal HP/MP labels are omitted.

**Curved bar text scale** (0.5-2x, default 1x) independently resizes HP current/total, HP %, MP values and MP %. Tube height/thickness and the module's name/level text do not change. Shield text follows the resource scale; distance remains at module text size. Very large type in narrow modules wraps and grows the text bounds for readability.

HP values/percentage follow the resolved HP colour, including hostile red and health-state gradients. MP uses its configured colour, and shields use Shield colour. Names, level/job metadata and distance remain integrated beside the bars. Text wraps outside the full bar contours, and the module grows vertically when needed.

## Rendering and interaction

Cached bows retain flat horizontal terminations. Alpha edge fringes smooth the outer contours; cross-strip shading and restrained highlights create a liquid/tube presentation. Dimensions/orientation rebuild geometry only when they change. HP/MP/shield updates reuse the cached paths.

Locked Arc mode accepts clicks only through tight regions along visible HP/MP tracks. Names, numeric text, percentages and unused space stay click-through. Hidden MP has no region, and no large rectangular hover outline is drawn. Standard keeps its saved Whole module/Header-only choice. Unlocking uses the full module edit frame and gives editing priority over actor actions.

| Setting | Arc interpretation |
|---|---|
| Module width | Overall footprint, including the side text areas |
| Module/global scale | Arc height, thickness and type scale |
| Bar height | HP tube thickness, capped to fit narrow modules |
| Arc height | Vertical bow height before scale, 100–600 px |
| Curved bar text scale | Extra scale for resource readouts only; independent per Player/Target |
| Auto orientation | Player left, Target right; explicit Left/Right overrides remain available |
| Opacity/border | Track, fill, fringe, text and subtle shell outline |
| HP text alignment | Standard/cast-bar alignment; Arc uses mirrored side placement |

In edit mode, drag to move, horizontal edges for width, vertical edges for arc height, and corners for both. Adjust thickness numerically. Unresolved actors get a preview only in edit mode; non-character targets never retain a previous actor's track input regions.

## Separate cast bars

Enable **Separate Player cast bar** and/or **Separate Target cast bar** on their actor pages, then unlock to position them beneath the character. Arc pages show these controls immediately after Bar style and omit unused attached-cast switches. Each has independent enable, width, height, scale, opacity, visibility and cast name/progress/percentage/remaining-time controls. **Layout** lists both modules for reset/appearance tools. Gameplay hides idle casts; editing supplies a preview. A separate cast can run with its parent HP module disabled. Both modules must be enabled separately if you want both visible while targeting yourself; place them apart in the editor.

Arcs never attach casts. In Standard, an enabled separate cast suppresses the old attached presentation; disabling it restores the saved Standard cast options. The existing reader copies native [Character.GetCastInfo](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/Character/Character.cs) once into frame-local scalar data, with no retained pointer. Self-targeting shares the Player snapshot, allowing both independently positioned cast modules to render the same cast. Other players/enemies/NPC characters use their own non-null cast data, including characters whose managed wrapper does not implement IBattleChara. Invalid/missing cast data clears; target changes never substitute an unrelated Player cast. Existing name/progress/percentage/time formatting is shared.

Schema stays 14; the new text-scale initializer supplies 1x without resetting existing preferences, positions or geometry. Core stays at package 0.3.1, source `300703b360a58fb4b73bf7675d31fe8cab4614cd`. Initial arc support remains Player/Target; Focus/ToT stay Standard. Automated tests cover cast routing/validation, persistence, resource layout/scaling, colours and mouse capture; actual live actor casts, appearance and restart acceptance require the live checklist.
