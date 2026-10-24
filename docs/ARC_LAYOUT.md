# Optional arcs and separate casts (0.8.7.0)

In **HUD Elements > Player** or **Target**, choose **Bar style > Curved / Arc**. Classic exposes the same settings in Player/Target tabs. Standard stays the default, and changing style preserves saved module anchors. **Place arc near centre** remains an explicit optional positioning action.

## Side readouts

Player values sit on the outer-left side and percentages on the inner-right; Target mirrors them. HP and MP have separate aligned rows. **Value format** chooses Current/Total or Current Only for both resources. **Show resource percentages** controls percentages; individual HP information switches remain respected. MP text modes retain their normal meaning. Full/Compact number formatting applies, and literal HP/MP labels are omitted.

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
| Auto orientation | Player left, Target right; explicit Left/Right overrides remain available |
| Opacity/border | Track, fill, fringe, text and subtle shell outline |
| HP text alignment | Standard/cast-bar alignment; Arc uses mirrored side placement |

In edit mode, drag to move, horizontal edges for width, vertical edges for arc height, and corners for both. Adjust thickness numerically. Unresolved actors get a preview only in edit mode; non-character targets never retain a previous actor's track input regions.

## Separate cast bars

Enable **Separate Player cast bar** or **Separate Target cast bar** on its actor page, then unlock to position it beneath the character. Each has independent width, height, scale, opacity, visibility and cast name/progress/percentage/remaining-time controls. **Layout** lists both modules for reset/appearance tools. Gameplay hides idle casts; editing supplies a preview. A separate cast can run with its parent HP module disabled.

Arcs never attach casts. In Standard, an enabled separate cast suppresses the old attached presentation; disabling it restores the saved Standard cast options. Both use the existing live cast reader/formatter rather than another cast-tracking service.

Schema 14 adds only text-option/currency defaults. Existing preferences, positions and geometry remain saved with the protected migration backup. Core stays at package 0.3.1, source `300703b360a58fb4b73bf7675d31fe8cab4614cd`. Initial arc support remains Player/Target; Focus/ToT stay Standard. Native ImGui tests cover rendering, colours, layout and mouse capture; final appearance, real actor clicks and restart acceptance require the live checklist.
