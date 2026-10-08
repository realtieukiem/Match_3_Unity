# Pokiwar FG38 art - remaining batches for Codex

Paste ONE batch per NEW Codex thread. Each batch stays under 25 generated images, because a thread that
carries more inline images exceeds the 64 MiB request limit (`Encoded request body exceeds 67108864 bytes`)
and cannot be used again.

## Rule for this file

Prompts exist here ONLY for what is still to paint; the status board names everything else so nothing is painted
twice. The portable art workspace is `Art/Pokiwar_FG38` inside this repository; all paths below resolve
from the repository root, on any machine. Read `Art/Pokiwar_FG38/README.md` for the bundled inputs.
When a batch is painted its prompt is deleted from here, its board row turns to `HAVE` and the batch
moves to the "Already painted" table below - never leave a finished prompt in place, Codex would paint it
again. Before painting anything, check the file name under `Art/Pokiwar_FG38/Assets`: a file that
exists is finished, do not repaint or overwrite it unless the user asks for a new version by name.

## Status board - read this first (checked 2026-10-08 15:00 against `Art/Pokiwar_FG38/Assets`, 273 PNG; Batch 19 is in the game; Batch 20 is the only open order)

Every piece of art the game needs has exactly one status. Codex paints ONLY rows marked `PAINT NEW`.

| Status | Meaning for Codex |
|---|---|
| `PAINT NEW` | The file does not exist anywhere. Paint it, save under the exact name. |
| `HAVE` | Already painted and in the game. Do NOT paint, do NOT overwrite. |
| `CODE-DRAWN` | Drawn by a script inside the game repo, not by Codex. Do NOT paint. |
| `WAIT` | Undecided by the user. Do NOT paint until this row changes to `PAINT NEW`. |
| `NEVER` | Decided: will never be shown. Do NOT paint. |

| # | What | Images | Status | Where |
|---|---|---|---|---|
| 1 | East Sea creatures, first forms: Samgong, Bebeboom, Ngoclam, Doimora, Voirong, boss Ongnamhai | 24 | `HAVE` | painted 2026-10-07, in the game |
| 2 | East Sea second forms: Ngoclam, Doimora, Voirong, Ongnamhai | 16 | `HAVE` | painted 2026-10-07, in the game |
| 3 | East Sea region art: world island, region emblem, battle background | 3 | `HAVE` | painted 2026-10-07, in the game |
| 4 | Battle effects: rage fire vortex, flame wisp, siphon mote, hit burst, sparkle | 5 | `HAVE` | painted 2026-10-07, in the game |
| 5 | Card faces `Cards/Faces/face_*_v02.png`: herbal salve, iron skin, rage ember, meteor, summon sprite, war cry, mana leech, the skill face, the card back | 9 | `HAVE` | painted 2026-10-07, in the game (the card back has no use yet) |
| 16 | Card faces `face_mana_potion_v02.png`, `face_fire_bolt_v02.png` | 2 | `HAVE` | Batch 19, in the game since 2026-10-08 |
| 6 | Buff icons `buff_{heart,lightning,fire,shield}.png` | 4 | `CODE-DRAWN` | `Tools/DrawCardFaces.ps1`; heal and mana effects approved by the user 2026-10-07 |
| 7 | UI primitives (`ui.circle`, `ui.ring`, `ui.round`, `ui.frame`, `ui.gradient`, bar shapes) | - | `CODE-DRAWN` | plain shapes, tinted in game |
| 8 | Everything in Batches 1-13 (gems, starter pets, Dunewing, Psyling, Azurewing, cards art, elements, HUD, buttons, room, avatar, lobby, forge, stones, world map, town) | 179 files | `HAVE` | table "Already painted" below |
| 11 | Second forms of Dunewing and Psyling (`enemy_beetle_form02_*`, `enemy_psyling_form02_*`) | 8 | `HAVE` | painted small (68% of the frame); the game enlarges them with `Tools/ReframeSecondForms.ps1`. Do NOT repaint unless the user asks |
| 12 | Arrow mini game kit, first set (`Battle/QTE`: four arrows, slot frame, timing marker, timer frame) | 7 | `HAVE` | in the game since 2026-10-07 |
| 15 | Arrow mini game kit after the original (`Battle/QTE/qte_*_v01.png`): pill bar, three tokens, white arrow, slider track / good / perfect / knob, damage flame | 10 | `HAVE` | painted 2026-10-07, in the game |
| 17 | Arrow mini game touch buttons `qte_button_dir_v01.png`, `qte_button_strike_v01.png` | 2 | `HAVE` | Batch 19, in the game since 2026-10-08 |
| 13 | Idle animation frames 2 and 3 for the eleven creatures in play (Emberkit, six East Sea first forms, four East Sea second forms) | 22 | `HAVE` | painted 2026-10-07, in the game |
| 14 | East Sea battle background, version 2: the same reef with two flat rock outcrops where the two creatures stand | 1 | `HAVE` | Batch 19, in the game since 2026-10-08 (both creatures stand on the outcrops); v01 preserved |
| 18 | Charm icons `Meta/UI/charm_lucky_v01.png`, `Meta/UI/charm_protection_v01.png` | 2 | `PAINT NEW` | Batch 20 below; the game shows a flat white star and the purple shield gem tile in their place |
| 19 | Idle frames 2 and 3 for Dunewing, Psyling, Azurewing and their later forms | - | `WAIT` | none of them is on the map now (all six nodes are East Sea); paint only if they return |
| 20 | Projectiles for ranged creatures `Battle/Shots/shot_{bebeboom_bomb,ngoclam_pearl,voirong_bolt}_v01.png` | 3 | `PAINT NEW` | Batch 21 below; Bebeboom throws a code-drawn placeholder bomb until its file exists, the other two fight up close until theirs do |
| 9 | Second forms of Emberkit, Leafling, Tidepup; a card for Tidal Siphon | - | `NEVER` | decided 2026-10-06 |
| 10 | Third and later forms of any creature | - | `WAIT` | the rule for when a pet takes a third form is not decided |

Open orders: Batch 20 (two charm icons) and Batch 21 (three projectiles). Batch 19 is complete; rows marked `WAIT` remain undecided.

Framing rule for every future creature batch (learned from Batches 12, 14 and 15): the game shows a creature at
the size it fills its canvas, so paint the idle at about 80% of the canvas height, keep every pose (the hit pose
included) under 90%, and keep one ground line across the four poses. Creatures painted smaller are enlarged by
`Tools/ReframeSecondForms.ps1` on the game side, which costs sharpness.

## Already painted - do NOT repaint

| Batch | What | Folder | Files |
|---|---|---|---|
| 1 | square gems | `Battle/Gems` | `battle_gem_*_square_v01.png` x6 |
| 2 | Leafling, Tidepup | `Battle/Characters` | `pet_leafling_*`, `pet_tidepup_*` x8 |
| 3 | Psyling | `Battle/Characters` | `enemy_psyling_{idle,attack,hit,defeat}_01.png` |
| 4 | card art | `Cards`, `Battle/Skills` | `card_summon_sprite`, `card_mana_leech`, `card_meteor`, `skill_blaze_burst`, `skill_thorn_bind`, `skill_tide_lance`, `skill_mind_spark` (`_v01.png`) |
| 5 | element badges | `UI/Elements` | `element_*_v01.png` x6 |
| 6 | battle HUD kit | `UI/Battle` | `battle_*_v01.png`, `text_your_turn_v01.png` |
| 7 | buttons, panels, room | `UI/Common`, `UI/Room` | `ui_button_*`, `ui_panel_dark`, `ui_title_ribbon`, `ui_reward_rays`, `room_*` |
| 8 | avatar paper doll | `Avatar`, `Avatar/Icons` | `avatar_*_v01.png` and their `_icon_v01.png` |
| 9 | word art, card stone | `UI/Battle`, `Meta/UI` | `text_vs_v01.png`, `text_fight_v01.png`, `stone_card_v01.png` |
| 10 | lobby | `Meta/Background`, `Meta/Lobby` | `lobby_bg_sky`, `lobby_planet`, `lobby_lock`, `region_sunny_isle`, `nav_{info,avatar,cards,pets}` |
| 11 | forge, pet stones, world map | `Meta/Forge`, `Meta/Stones`, `Meta/World` | `forge_pedestal`, `forge_slot`, `forge_tab`, `stone_{metal,wood,water,fire,earth}`, `world_bg_sea`, `world_island_sunny`, `world_island_locked`, `world_ship` (`_v01.png`) |
| 12 | second forms | `Battle/Characters` | `enemy_beetle_form02_*`, `enemy_psyling_form02_*` x8 |
| 13 | town map | `Meta/Background`, `Meta/Home` | `home_bg_town_v01.png`, `home_{hunt,arena,evolve,challenge,shopcard,shopavatar,gift,wheel,rank}_v01.png` |
| 14 | East Sea creatures, first forms | `Battle/Characters` | `enemy_{samgong,bebeboom,ngoclam,doimora,voirong}_{idle,attack,hit,defeat}_01.png`, `boss_ongnamhai_form01_*` x24 |
| 15 | East Sea second forms and region art | `Battle/Characters`, `Meta/World`, `Meta/Lobby`, `Battle/Background` | `enemy_{ngoclam,doimora,voirong}_form02_*`, `boss_ongnamhai_form02_*` x16, `world_island_eastsea_v01.png`, `region_east_sea_v01.png`, `battle_bg_eastsea_v01.png` |
| 16 | battle effects | `Battle/FX` | `fx_{rage_vortex,flame_wisp,siphon_mote,hit_burst,sparkle}_v01.png` |
| 19 | remaining card faces, touch buttons and reef standing platforms | `Cards/Faces`, `Battle/QTE`, `Battle/Background` | `face_mana_potion_v02.png`, `face_fire_bolt_v02.png`, `qte_button_dir_v01.png`, `qte_button_strike_v01.png`, `battle_bg_eastsea_v02.png` |

Do NOT paint either (decided 2026-10-06, they would never be shown): second forms for Emberkit, Leafling or
Tidepup - the free starter never changes form and the other two are not obtainable; a card for Tidal Siphon -
only the boss uses it and boss cards are not drawn.

## Batch 20 - charm icons (2 images, `PAINT NEW`)

Both are item icons shown at 48-96 px in the wallet chips of the pet, forge and hub screens and in a shop row.
Save as transparent 256x256 PNG under `Art/Pokiwar_FG38/Assets/Meta/UI/`. Style reference: the five pet stones
`Meta/Stones/stone_*_v01.png` and `Meta/UI/stone_card_v01.png` - same outline weight, same shading, same
frame occupancy (subject about 86% of the canvas, clear transparent margin on every side).

| File | Subject |
|---|---|
| `charm_lucky_v01.png` | One lucky charm: a gold four-leaf clover medallion hanging from a short red knotted cord, one cream glint upper left. Reads as "luck" at 48 px. |
| `charm_protection_v01.png` | One protection charm: a small violet heraldic shield amulet with a silver rim and a pale star-shaped gem at its centre, hanging from the same short red knotted cord. Reads as "protect" at 48 px. |

```
Create one finished 2D game item icon: <SUBJECT FROM THE TABLE>. Square canvas, genuine transparent background,
one single object centred and front-facing, filling about 86% of the canvas. Canyon-fantasy casual cartoon:
chunky silhouette, thick closed near-black brown outline #21191F, two or three crisp flat shade bands, a
restrained cream highlight from the upper left, very little texture. The two charms are a pair: same cord, same
outline weight, same scale. No letters, numbers, text, UI frame, panel, coin, scenery, sparkles outside the
object, external cast shadow, glossy 3D rendering or airbrush gradients. Transparent alpha outside the object.
```
## Batch 21 - projectiles for ranged creatures (3 images, `PAINT NEW`)

A creature is either a melee fighter (it dashes across and hits) or a ranged one (it stays on its rock, throws
or shoots, and the projectile bursts on the target). The game decides by file: a creature with a projectile
here is ranged. Each projectile is one object alone on a transparent 256x256 canvas, filling about 80% of it,
drawn at 120-160 px in battle. Save under `Art/Pokiwar_FG38/Assets/Battle/Shots/`. Look at the creature's own
`Battle/Characters/<name>_attack_01.png` first so the projectile matches what it holds or spits.

| File | Motion in game | Subject |
|---|---|---|
| `shot_bebeboom_bomb_v01.png` | thrown in a high arc, tumbling | The round bomb or firecracker Bebeboom holds in its attack pose: same colours and fuse, a small lit spark on the fuse. Drawn upright; it reads from every angle because it spins. |
| `shot_ngoclam_pearl_v01.png` | flies straight, pointing along its path | One glowing pearl with a short tapering trail of pale light behind it. The pearl leads at the RIGHT edge, the trail points LEFT. Symmetric above and below the horizontal axis. |
| `shot_voirong_bolt_v01.png` | flies straight, pointing along its path | One spiralling water bolt with a thin core of lightning, blunt head at the RIGHT edge, tail thinning to the LEFT. Symmetric above and below the horizontal axis. |

```
Create one finished 2D game projectile sprite: <SUBJECT FROM THE TABLE>. Square canvas, genuine transparent
background, one single object centred, filling about 80% of the canvas. Canyon-fantasy casual cartoon: chunky
silhouette, thick closed near-black brown outline #21191F, two or three crisp flat shade bands, a cream
highlight from the upper left, very little texture. Readable at 120 px. No creature, hand, claw, impact burst,
smoke cloud, ground, cast shadow, text, UI or background. Transparent alpha outside the object.
```

Later creatures and second forms follow the same rule: a thrown projectile is `<creature>.lob`, a straight one
is `<creature>.shot` in `Fg38Art`; a second form without its own file uses its first form's.

## Shared preamble (put at the top of every batch)

```
Continue the Pokiwar FG38 art set. Reply to me in Vietnamese.
Read first: Art/Pokiwar_FG38/ART_BIBLE.md and Art/Pokiwar_FG38/AssetManifest.json, and look at
Art/Pokiwar_FG38/Previews/Battle_form01_v01.png as the style target (canyon fantasy, chunky
silhouettes, near-black brown outline #21191F, two or three flat shade bands, cream highlight).
Batch 19 definitions and prompts are an archive of completed work, not a new drawing order. Read only the batch pasted with this preamble (20 or 21).
The ordered definitions resolve their references and output filenames inside Art/Pokiwar_FG38.
Thread budget rules - this thread dies above ~30 inline images:
- generate at most the images listed below, one at a time;
- never open a full-size output to check it; build a 512 px contact sheet and view only that;
- copy every accepted image out of ~/.codex/generated_images/<session>/ into the exact path below at
  once into Art/Pokiwar_FG38/Assets/<requested filename>, and update the local AssetManifest.json.
- use an opaque magenta matte for raw card faces, then key only the outside corners; blank card
  interiors must remain opaque. Buttons use alpha; the battle background stays fully opaque.
- preserve new prompts, immutable raws and reports in the ordered batch directory under Art/Pokiwar_FG38/Sources.
The game picks files up by these exact names (Assets/Pokiwar/Scripts/Editor/Fg38Art.cs); do not rename.
Limit art-generation edits to Art/Pokiwar_FG38 and this order file. Keep existing scenes and gameplay.
```

## After any batch

Verify exports, update the manifest and mark completed order rows `HAVE` before handing off.
New exports are available to the Unity importer at `Art/Pokiwar_FG38/Assets`; preserve existing
scene layout when importing them. Do not rebuild the whole scene as part of an art-only request.
