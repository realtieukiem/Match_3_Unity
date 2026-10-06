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

## Batch 9 - battle word art and the card stone (3 images)

The game currently draws these words with a system font. Paint them as lettering, in the same hand as
`text_your_turn_v01.png` (chunky rounded cartoon capitals, warm orange-to-yellow fill, thick dark brown
outline, thin cream rim, slight upward tilt, soft drop shadow). Transparent background, no extra objects,
no glow that reaches the canvas edge, the word centred with at least 6% clear margin.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\UI\Battle\`:

- `text_vs_v01.png` - 1024x640. The letters "VS", very bold, with two crossed lightning cracks behind them.
- `text_fight_v01.png` - 1536x512. The word "FIGHT!", red-orange fill, more aggressive slant than YOUR TURN.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\UI\`:

- `stone_card_v01.png` - 256x256. A faceted golden-yellow upgrade crystal for cards, clearly a different
  object from the round elemental pet stones: square-cut, card-suit sparkle on its face, same line weight.

Final check: one contact sheet of the three files on a mid-grey background and on the battle background.

## Batch 10 - lobby (8 images)

The lobby is the first screen (landscape only, 1920x1080): a grid of region tiles on the left, the region's
opponents standing on a ring around a planet on the right, a top bar and a bottom navigation bar. The game
draws all of it with flat shapes until these files exist. Same FG38 hand as the rest: clean cartoon, thick
dark outline, soft cel shading, no text baked into any image.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\Background\`:

- `lobby_bg_sky_v01.png` - 2048x1152, opaque. Bright blue sky sea with soft clouds and faint star sparkles,
  calm and low-contrast: UI panels and monsters sit on top of every part of it. No horizon objects, no
  characters.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\Lobby\` (transparent background, subject centred, at
least 6% clear margin):

- `lobby_planet_v01.png` - 512x512. A small glowing blue-white planet with one tilted ring around it, the
  centrepiece the opponents stand around. Soft outer glow that fades out before the canvas edge.
- `lobby_lock_v01.png` - 256x256. A chunky cartoon padlock, steel blue with a gold keyhole plate; it marks
  a region that is not open yet.
- `region_sunny_isle_v01.png` - 256x256. A tiny floating island emblem for the region "Sunny Isle": sandy
  rock, a few palm trees, a warm sun glint. Readable at 120 px.
- `nav_info_v01.png` - 256x256. Navigation icon "info": a rolled parchment scroll with a small star seal.
- `nav_avatar_v01.png` - 256x256. Navigation icon "change avatar": a cap resting on a folded jacket.
- `nav_cards_v01.png` - 256x256. Navigation icon "card training": two overlapping battle cards with an
  upward arrow.
- `nav_pets_v01.png` - 256x256. Navigation icon "pet training": a round paw-print medal with a small anvil
  hammer.

Final check: one contact sheet of the seven transparent files on the lobby background.

## Batch 11 - forge, reward and world map (not wired yet, paint after Batch 10)

Needed for the screens that are still plain lists. File names are fixed now so the code can pick them up.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\Forge\` (transparent background):

- `forge_pedestal_v01.png` - 768x512. A round stone pedestal with a glowing rune circle on top, seen
  slightly from above; the pet or the card stands on it.
- `forge_slot_v01.png` - 256x256. An empty square socket for one upgrade stone: dark inset, gold rim.
- `forge_tab_v01.png` - 512x160. A blank tab plate (no text), stretchable in the middle.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\Stones\` (256x256 each, transparent):

- `stone_metal_v01.png`, `stone_wood_v01.png`, `stone_water_v01.png`, `stone_fire_v01.png`,
  `stone_earth_v01.png` - one round elemental pet stone per element (the game's five: Metal, Wood, Water,
  Fire, Earth), same silhouette, element colour and a simple element mark inside.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\World\`:

- `world_bg_sea_v01.png` - 2048x1152, opaque. Open sea seen from above with drifting cloud banks.
- `world_island_sunny_v01.png` - 768x512, transparent. The Sunny Isle as a whole island seen from above.
- `world_island_locked_v01.png` - 512x384, transparent. A pale island hidden in cloud.
- `world_ship_v01.png` - 256x256, transparent. A small cartoon sailing ship, the player's marker.

## Batch 12 - second forms of the five creatures (20 images)

A pet the player has enhanced to +5 changes into its second form for good (the boss already has one:
`boss_crystaldrake_form02_*`). Until these files exist an evolved pet keeps its first-form art.

For each creature paint the SAME four poses as its first form - `idle`, `attack`, `hit`, `defeat` - 1024x1024,
transparent background, same canvas position, same facing as the first-form file (pets face right, enemies
face left; the game mirrors enemies itself), feet on the same ground line. The second form is the same animal
grown up: about 20% bigger inside the canvas, sharper silhouette, one new signature feature, same palette with
a brighter accent. It must read as "the same creature, evolved", not a new species.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Battle\Characters\`:

- `pet_player_form02_{idle,attack,hit,defeat}_01.png` - Emberkit (fire fox cub with crystal tufts): taller,
  a flame-tipped double tail, larger glowing crystal crest.
- `pet_leafling_form02_{idle,attack,hit,defeat}_01.png` - Leafling: a leafy mane, vine whips on the forelegs,
  a blooming flower on the back.
- `pet_tidepup_form02_{idle,attack,hit,defeat}_01.png` - Tidepup: fin crest along the spine, a wave-shaped
  tail, small water orbs circling.
- `enemy_beetle_form02_{idle,attack,hit,defeat}_01.png` - Dunewing (spiked desert beetle): heavier armour
  plates, longer horned antennae, sand-gold edges on the spikes.
- `enemy_psyling_form02_{idle,attack,hit,defeat}_01.png` - Psyling (dark psychic imp): open wings, a glowing
  third eye gem, floating rune shards.

Final check: for each creature, form 01 idle and form 02 idle side by side at the same scale.

## Batch 13 - town map, the first screen (10 images)

The game opens on a town map (landscape 1920x1080): floating islands in a bright sea and sky, each building a
button. The game places the buildings itself, so paint the background WITHOUT buildings and each building as
its own transparent file. Same FG38 hand, no text in any image (the game writes the labels).

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\Background\`:

- `home_bg_town_v01.png` - 2048x1152, opaque. Seen from above at a slight angle: a snowy mountain island top
  left, a volcano island top centre-left, a large grassy main island in the middle, a forest island on the
  right, open blue sea between them, soft clouds at the edges. Leave flat empty ground where buildings will
  stand: far left top and middle, lower left, centre, upper centre-right, right. Keep the bottom 12% and the top
  9% calm - UI bars cover them.

Save into `D:\Project\Pokiwar_Art_FG38\Assets\Meta\Home\` (512x512 each, transparent, building centred with a
small ground patch under it, at least 6% clear margin):

- `home_hunt_v01.png` - "boss hunt": a wild rocky gate with monster horns and a glowing portal.
- `home_arena_v01.png` - "arena": a round stadium with a domed glass roof and two crossed banners.
- `home_evolve_v01.png` - "evolve": a crystal shrine with a swirling light above an altar.
- `home_challenge_v01.png` - "challenge": a crossed-swords crest on a stone tower.
- `home_shopcard_v01.png` - "card shop": a small stall whose roof is a giant battle card.
- `home_shopavatar_v01.png` - "avatar shop": a boutique with a hat-and-shirt sign.
- `home_gift_v01.png` - "gift shop": a cottage stacked with wrapped presents.
- `home_wheel_v01.png` - "lucky wheel": a fairground prize wheel on a stand.
- `home_rank_v01.png` - "ranking": a golden trophy statue on a cloud pedestal.

Final check: the nine buildings placed on the background, one contact sheet.

## After any batch

Tell Claude Code "lấy asset Codex mới vào game" (or run `Pokiwar/Rebuild Scene + Art` in Unity). Painted
square gems replace the code-drawn ones automatically; everything else fills the matching sprite key.
