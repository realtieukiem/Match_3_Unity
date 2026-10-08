# Pokiwar FG38 Art Bible — v1

## Evidence and direction
- Original external style references were inspected on the first workstation. They are historical inputs, not required setup. Use the bundled `Previews/Battle_form01_v01.png` as the current visual reference on any machine.
- Functional structure comes from the supplied Pokiwar brief. No Pokiwar gameplay frames or reports were attached, so any unobserved visual detail is a design variant, not a claim about the original game rules.
- Keep the one-player-versus-AI battle structure, shared 8x8 board, six gem functions, pet left/enemy right, turn/timer/cards/QTE, boss form change, and Hub → Map → Preparation → Battle → Reward → Upgrade flow. The new visual identity is canyon fantasy.

## Visual rules
| Axis | Rule |
|---|---|
| Silhouette | Chunky, compact, readable at 200–350 px. Player faces right; enemy faces left. Large head and eyes, sturdy short limbs. |
| Outline | Near-black brown `#21191F`, about 2–4 px at 64 px icons and 4–7 px at 256 px UI scale. Closed major contours. |
| Shading | Two or three planar shade bands; upper-left cream highlight. Avoid airbrush on solid sprites. VFX may have soft outer glow. |
| UI | Beveled wood/stone frames, dark corner mounts, small brass diamonds/rivets. Live TMP text and image fills sit on separate layers. |
| Background | Warm sandstone, russet cliffs, limited detail behind board/HUD. Full-bleed 16:9, no pre-painted UI or characters. |
| Depth | Flat front-facing board/UI; side-view three-quarter creatures; distant scene fades toward lighter warm haze. |
| Texture | Sparse chips and wood grain; avoid dense noise on board tiles or small icons. |
| Motion | Idle 1–2% bob, quick 120–180 ms anticipation, 80–120 ms impact, 180–260 ms settle. No continuous screen shake. |

## Palette
| Role | Hex |
|---|---|
| Ink | `#21191F` |
| Wood | `#A4512D` |
| Rock shadow | `#743B2F` |
| Canyon | `#C66A3D` |
| Sand | `#E9C992` |
| Cream highlight | `#FFF0BE` |
| Brass | `#F5C554` |
| Teal accent | `#43C4BD` |
| Sword / yellow | `#F7C744` |
| Lightning / blue | `#188EE9` |
| Fire / red | `#EA473B` |
| Heart / green | `#69CE30` |
| Shield / purple | `#9F46D6` |
| YinYang / ivory | `#F2EEE5` |

## Board and readability
- Gem sprites share a 512x512 transparent canvas. The 8x8 board displays them around 55–64 px per cell in a 1920x1080 layout.
- Every gem has both hue and a central dark emblem: sword, bolt, flame, heart, shield, yin-yang. Do not rely on hue alone.
- Tile art, gem art, selected/hint overlay, multiplier badges, and board frame are separate. Hide the whole board group during cinematic attack; do not bake an empty cinematic board into the background.
- HUD frames have alpha apertures. Put HP/Mana/Rage/Shield fills behind the frame and live numbers/labels above it. Use white text with dark shadow on wood, or ink text on parchment.
- Buttons use the same geometry for normal, pressed, disabled. Their labels remain TMP. `Arial Black` is the available bold fallback; an installed rounded heavy sans font with redistribution rights can replace it.
- Keep 5% left/right safe margin and 7% top/bottom safe margin for mobile crops. Anchor board center and player/enemy to side safe zones.

## Do / Don't
- Do: place a green heart emblem on a green crystal; a color-blind player still reads healing.
- Do: use one empty card frame and live cost/name/art layers for five selectable slots.
- Do: treat the small and large drake as the same boss through teal crown, gold eyes, cream armor and magenta body.
- Don't: paint 64 fixed gems into a board background, bake damage numbers into VFX, or mirror asymmetrical text.
- Don't: put dense rock detail behind a gem grid, use low-contrast green gem on green tile, or mix glossy 3D rendering into the flat outlines.

## Export and Unity handoff
- PNG, sRGB, alpha for all sprites except full background. Bilinear import for this non-pixel style; disable compression if it produces edge halos.
- Backgrounds are 1920x1080. Gem exports are 512x512; tile and QTE controls are 256x256; characters retain 1402x1122 sources. Preferred pivots: center for UI/gems/VFX, bottom-center for characters.
- Board frame and HUD frame have genuine transparent interiors. `Previews/` are composites only; use `Assets/` for production layers.
- Character files are key poses rather than frame sheets. `AnimationSpec.json` provides playable sprite-swap/transform timing. Each state currently has one authored pose; smoother frame-by-frame motion is planned.
