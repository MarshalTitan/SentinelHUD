# Optional arcs and separate casts (0.8.6.0)

In **HUD Elements > Player** or **Target**, open **Bar style**, then choose **Curved / Arc**. In Classic use the Player or Target tab. Standard remains the default; changing style never moves the saved module. **Place arc near centre** is an explicit optional positioning action. Unlock the HUD to position the two arcs around your character.

**Auto / mirrored** bows the Player arc on the left and Target arc on the right. Left side and Right side override this independently. HP fills bottom to top; MP, when enabled, is the thinner parallel arc toward the centre. Existing Full/Compact numbers, HP value/percentage toggles, name/job/role/level, MP modes, shield modes, target distance and statuses remain available. Text wraps beside the arc; level remains in the header. Shields use the same integrated blue overlay/extension policy as Standard. HP colours, hostile red and health gradients use the existing resolver.

| Setting | Arc interpretation |
|---|---|
| Module width | Overall horizontal footprint including the text area; fonts do not stretch |
| Module scale / global scale | Scale arc height, stroke thickness and text |
| Bar height | HP stroke thickness, capped at 10% of available width so small modules remain legible |
| Arc height | Vertical bow height before scale, 100–600 px |
| Opacity / border | Apply to the arc tracks, fills, text and optional subtle outline |
| HP text alignment | Standard/cast-bar alignment; arc details wrap in the inner text area |
| Visibility / targeting | Existing conditions and whole-module/header targeting regions |

In edit mode, drag the body to move, horizontal edges to change width, vertical edges to change arc height, and corners to change both. Adjust thickness numerically. The editor uses the same origin in locked and unlocked modes. An unresolved actor gets an arc preview; this preview never appears in gameplay.

## Separate cast bars

Player and Target each have a **Separate ... cast bar** section. Enable it, choose cast name/bar/percentage/remaining time and visibility, then unlock and drag its independent frame below the character. Its width, height, scale, opacity and text alignment are independent. **Layout** lists **Player Cast** and **Target Cast** for position reset and appearance tools. Idle edit mode supplies a sample cast. Gameplay shows the module only while its actor is casting and its own visibility condition is satisfied. It can run with the parent HP module disabled.

Arc modules never contain an attached cast. In Standard, enabling a separate cast suppresses the old attached presentation to prevent duplicates; disabling it restores the parent's existing cast settings. The old name/bar/percentage/time controls on the parent still configure only its Standard attached cast. Separate cast fields are independent and default disabled. Both presentations reuse the existing live cast reader and draw/formatting path; they do not run another cast-tracking service.

## Persistence and scope

Schema **13** adds arc preferences and disabled cast configurations using defaults. It retains every existing setting and the protected pre-migration backup. Styles, orientation, dimensions, separate positions and cast preferences survive reload and update. Core is unchanged at package **0.3.1**, source `300703b360a58fb4b73bf7675d31fe8cab4614cd`.

Initial arc support is Player/Target. Focus Target and Target-of-Target remain Standard. Curve samples are cached in two fixed arrays per arc renderer and rebuilt only when dimensions/orientation change. HP/MP/shield changes reuse those paths. Arcs use the existing overlay layer and actor-menu interaction path.

Automated checks exercise migration, serialization, mirrored geometry, bounded resizing, cache reuse, cast visibility/ownership and native ImGui rendering at three widths/scales. Final visual placement/readability, real cast transitions, clicks/native menus and restart persistence need FFXIV live acceptance.
