# Pokiwar FG38 art - remaining batches for Codex

Paste ONE batch per NEW Codex thread. Each batch stays under 25 generated images, because a thread that
carries more inline images exceeds the 64 MiB request limit (`Encoded request body exceeds 67108864 bytes`)
and cannot be used again.

## Shared preamble (put at the top of every batch)

```
Continue the Pokiwar FG38 art set. Reply to me in Vietnamese.
Read first: D:\Project\Pokiwar_Art_FG38\ART_BIBLE.md, AssetManifest.json, and look at
D:\Project\Pokiwar_Art_FG38\Previews\Battle_form01_v01.png as the style target (canyon fantasy, chunky
silhouettes, near-black brown outline #21191F, two or three flat shade bands, cream highlight).
Thread budget rules - this thread dies above ~30 inline images:
- generate at most the images listed below, one at a time;
- never open a full-size output to check it; build a 512 px contact sheet and view only that;
- copy every accepted image out of ~/.codex/generated_images/<session>/ into the exact path below at
  once, with a transparent background, and update AssetManifest.json for it.
The game picks files up by these exact names (Fg38Art in D:\Project\GitHub\Match_3_Unity); do not rename.
Do not edit anything inside D:\Project\GitHub\Match_3_Unity.
```

## Batch 1 - square gems (6 images)

The board gems must be SQUARE tiles (rounded corners), not crystals. Same six functions and colours as
the bible: sword #F7C744, lightning #188EE9, fire #EA473B, heart #69CE30, shield #9F46D6, yin-yang #F2EEE5.
Each: 512x512, transparent, square bevelled gem filling ~90% of the canvas, one dark central emblem,
readable at 64 px. Save to `D:\Project\Pokiwar_Art_FG38\Assets\Battle\Gems\`:

- `battle_gem_sword_square_v01.png`
- `battle_gem_lightning_square_v01.png`
- `battle_gem_fire_square_v01.png`
- `battle_gem_heart_square_v01.png`
- `battle_gem_shield_square_v01.png`
- `battle_gem_yinyang_square_v01.png`

## Batch 2 - two player pets (8 images)

1402x1122, transparent, bottom-centre ground contact, facing RIGHT, same scale as `pet_player_idle_01.png`.
Four poses each: idle, attack, hit, defeat. Save to `D:\Project\Pokiwar_Art_FG38\Assets\Battle\Characters\`:

- Leafling (Wood element, small leaf sprite creature): `pet_leafling_idle_01.png`, `pet_leafling_attack_01.png`,
  `pet_leafling_hit_01.png`, `pet_leafling_defeat_01.png`
- Tidepup (Water element, pup with fins): `pet_tidepup_idle_01.png`, `pet_tidepup_attack_01.png`,
  `pet_tidepup_hit_01.png`, `pet_tidepup_defeat_01.png`

## Batch 3 - Echo Cave enemy (4 images)

Psyling (Metal element, small psychic cave creature), facing LEFT, same frame and scale as
`enemy_beetle_idle_01.png`. Save to `...\Assets\Battle\Characters\`: `enemy_psyling_idle_01.png`,
`enemy_psyling_attack_01.png`, `enemy_psyling_hit_01.png`, `enemy_psyling_defeat_01.png`.

## Batch 4 - three cards and five skill icons (8 images)

Cards: 768x1152 illustration only, no frame or text, like `card_fire_rage_v01.png`. Save to `...\Assets\Cards\`:
`card_summon_sprite_v01.png` (summons a small sprite ally), `card_mana_leech_v01.png` (steals enemy mana),
`card_meteor_v01.png` (meteor strike).

Skill icons: 512x512, square icon with the bible's frame language, no text. Save to `...\Assets\Battle\Skills\`:
`skill_blaze_burst_v01.png` (fire blast), `skill_thorn_bind_v01.png` (thorn vines binding),
`skill_tide_lance_v01.png` (water spear), `skill_mind_spark_v01.png` (psychic spark),
`skill_tidal_siphon_v01.png` (water vortex draining mana).

## Batch 5 - six element badges (6 images)

The battle HUD shows a pet's element as a small round badge in the top corner, next to its name, instead
of text. It is drawn at about 68 px, so it must read by shape and colour alone - NO letters, numbers or text.
Each: 512x512, transparent background, one circular badge filling ~92% of the canvas, thick near-black
outline #21191F, a coloured disc with two shade bands and a cream rim highlight, and ONE bold cream/white
emblem in the centre (about 55% of the disc). Same lighting and outline weight as the square gems.

| File | Element | Disc colour | Emblem |
|---|---|---|---|
| `element_fire_v01.png` | Fire | red-orange #E5492F | a single flame |
| `element_water_v01.png` | Water | blue #2B86E0 | a water drop with a small wave |
| `element_wood_v01.png` | Wood | green #4FA83D | a leaf with a short stem |
| `element_earth_v01.png` | Earth | ochre brown #B9803F | a chunky rock / mountain peak |
| `element_metal_v01.png` | Metal | steel grey-blue #9AA7B8 | a bolt head or a small ingot |
| `element_neutral_v01.png` | Neutral | warm grey #B8B2A6 | a simple four-point star |

Save to `D:\Project\Pokiwar_Art_FG38\Assets\UI\Elements\` (create the folder). Check all six together on
one contact sheet at 68 px: every badge must be told apart from the others at that size.

## Batch 6 - battle HUD kit (9 images)

The battle screen still draws its HUD from flat code shapes. Paint these so it matches the board frame
(`battle_board_frame_v01.png`: red-brown carved wood, gold diamond studs, near-black outline). No text in
any image except `text_your_turn_v01.png`. Transparent background. Save to
`D:\Project\Pokiwar_Art_FG38\Assets\UI\Battle\` (create the folder).

| File | Size | What it is | Rules |
|---|---|---|---|
| `battle_top_hud_panel_v01.png` | 1792x282 | Long top HUD plate holding both sides' name + 3 bars and a timer in the middle | Carved wood frame ~28 px, deep navy interior #142453, a round recess in the exact centre (~240 px) for the timer. Left and right halves plain - bars are drawn on top |
| `battle_timer_medallion_v01.png` | 256x256 | Round medallion the countdown digit sits on | Gold rim, dark navy face, empty centre (the digit is text) |
| `battle_name_ribbon_v01.png` | 512x64 | Name strip above each side's bars | PALE cream/white so code can tint it gold (active) or navy (waiting). Decorated ends inside 40 px from each edge, plain stretchable middle |
| `battle_bar_track_v01.png` | 512x48 | Empty groove for HP / MP / rage bars | Dark recessed groove, rounded ends inside 20 px, plain stretchable middle |
| `battle_bar_fill_v01.png` | 512x40 | The fill inside the groove | WHITE / very light grey with a soft top gloss, code tints it red/blue/yellow. Straight flat ends (it is cut, not sliced) |
| `battle_turn_arrow_flame_v01.png` | 512x512 | Arrow beside the acting pet, pointing at the foe | Points RIGHT, centred, orange-gold arrow wrapped in flame licks, chunky. Code mirrors it for the enemy |
| `battle_shield_bubble_v01.png` | 512x512 | Bubble covering a shielded pet | Glass sphere: interior only 15-25 % opaque so the pet stays visible, brighter cyan rim, one white highlight top-left, faint hex pattern optional |
| `battle_skill_frame_v01.png` | 256x256 | Ring around the round skill buttons | Light gold ring, transparent hole ~70 % of the diameter, 4 small studs |
| `text_your_turn_v01.png` | 1024x256 | Word art "YOUR TURN" shown over the board | English letters exactly `YOUR TURN`, chunky orange-gold with dark red-brown outline and a slight arc |

Contact sheet check: put the panel, ribbon, track and fill together at their game size (panel 1120x176,
ribbon 440x46, track 440x28) - the ribbon and fill must look fine stretched.

## Batch 7 - buttons, panels, popup, room (11 images)

Match `Battle\HUD\ui_button_confirm_normal.png` (the green button) for every button: same shape, bevel,
outline and stud corners, only the colour changes, NO text. Corners and border must sit inside 48 px of
each edge so the middle can stretch. Save to `D:\Project\Pokiwar_Art_FG38\Assets\UI\Common\`:

| File | Size | What it is | Rules |
|---|---|---|---|
| `ui_button_blue_v01.png` | 512x160 | Secondary button | blue #2F6FD0 |
| `ui_button_red_v01.png` | 512x160 | Give up / remove | red #C23B32 |
| `ui_button_gray_v01.png` | 512x160 | Back / settings / auto | slate grey #5A6070 |
| `ui_button_orange_v01.png` | 512x160 | Highlight button | orange-gold #E99A22 |
| `ui_panel_dark_v01.png` | 512x512 | Panel for pickers, hub pet card, upgrade columns | Wood frame inside 96 px of each edge, flat dark slate interior #1C2236 so white text reads |
| `ui_button_close_v01.png` | 256x256 | Round close button | Red disc, gold rim, a bold cream X |
| `ui_title_ribbon_v01.png` | 1024x240 | Ribbon behind popup titles (VICTORY / DEFEAT) | Purple #8E3FD0 banner with folded tails, decorated ends inside 120 px, plain middle, no text |
| `ui_reward_rays_v01.png` | 1024x1024 | Light burst behind the reward popup | Soft golden sunburst rays fading to transparent at the edge |

Save the room pieces to `D:\Project\Pokiwar_Art_FG38\Assets\UI\Room\`:

| File | Size | What it is | Rules |
|---|---|---|---|
| `room_panel_v01.png` | 1024x640 | Big room panel behind both pets, READY and the 5 card slots | Frame inside 120 px of each edge, calm light interior (pale sky-blue stone) so both pets read |
| `room_pet_pedestal_v01.png` | 512x192 | Round stone platform the player pet stands on | Seen from slightly above, sandstone with a gold rim |
| `room_card_slot_add_v01.png` | 384x480 | Empty card slot ("choose card") | Card-shaped dark recess with a carved frame inside 40 px, a large cream `+` in the centre |

## Batch 8 - player avatar paper doll (23 images)

The player is a chibi trainer who owns the pets, like the original Zing Me game: a bald base body in plain
underwear, with hair, top, bottom and hat drawn as separate layers stacked on top. The game stacks them in
the order base -> bottom -> top -> hair -> hat, every layer at the same position and size, so every file
MUST share one canvas and one pose.

Canvas for the base and all 11 layers: 512x512, transparent, front-facing chibi, arms down and slightly away
from the body, big head. Pin these pixel positions (y measured from the top):
head centre (256, 154), head radius about 88 px, chin about y 240, shoulders about y 262, waist about y 330,
feet bottom about y 474. Leave about 30 px of empty space at the top for tall hats.

How to keep layers aligned: paint the base first. For each layer, paint the piece ON a copy of the base, then
cut out only the piece's own pixels (everything else fully transparent) and save that. Check with a script
(PIL) that compositing base + layer gives the dressed character with no gap or shift.
Layer rules: hair stays close to the skull (no more than ~20 px above it) so any hat hides its top; hats
cover the top of the head; tops cover the torso and upper arms but leave the hands; bottoms cover hips to
knees or ankles; nothing covers the face.

Save to `D:\Project\Pokiwar_Art_FG38\Assets\Avatar\`:

| File | What |
|---|---|
| `avatar_base_v01.png` | Bald chibi body, warm skin, simple happy face, plain cream underwear shorts |
| `avatar_hair_spiky_v01.png` | Short dark-brown spiky hair (starter) |
| `avatar_hair_bob_v01.png` | Copper-red bob cut |
| `avatar_hair_ponytail_v01.png` | Blonde side ponytail |
| `avatar_top_tee_v01.png` | Blue T-shirt (starter) |
| `avatar_top_jacket_v01.png` | Orange explorer jacket with pockets, long sleeves |
| `avatar_top_robe_v01.png` | Purple mage robe reaching the knees, wide sleeves |
| `avatar_bottom_shorts_v01.png` | Brown shorts (starter) |
| `avatar_bottom_pants_v01.png` | Olive cargo pants with boots |
| `avatar_bottom_skirt_v01.png` | Pink pleated skirt |
| `avatar_hat_cap_v01.png` | Red trainer cap, brim to the right |
| `avatar_hat_wizard_v01.png` | Tall navy wizard hat with a star |

Shop icons: 256x256, transparent, the single item alone, centred, filling ~80 %, no body. Save to
`D:\Project\Pokiwar_Art_FG38\Assets\Avatar\Icons\` as `avatar_<slot>_<name>_icon_v01.png` for the same 11
items (for example `avatar_hat_cap_icon_v01.png`, `avatar_top_robe_icon_v01.png`).

Final check: one contact sheet with the base alone, the starter outfit (spiky + tee + shorts), and three mixed
outfits (bob + jacket + pants + cap, ponytail + robe + skirt + wizard hat, spiky + robe + pants), all
composited by script.

## After any batch

Tell Claude Code "lấy asset Codex mới vào game" (or run `Pokiwar/Rebuild Scene + Art` in Unity). Painted
square gems replace the code-drawn ones automatically; everything else fills the matching sprite key.
