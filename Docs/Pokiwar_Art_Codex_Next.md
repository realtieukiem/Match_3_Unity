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

## Status board - read this first (checked 2026-10-08 against `Art/Pokiwar_FG38/Assets`, 273 PNG; all five Batch 19 exports present)

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
| 16 | Card faces `face_mana_potion_v02.png`, `face_fire_bolt_v02.png` | 2 | `HAVE` | Batch 19, exported 2026-10-08; available to the importer |
| 6 | Buff icons `buff_{heart,lightning,fire,shield}.png` | 4 | `CODE-DRAWN` | `Tools/DrawCardFaces.ps1`; heal and mana effects approved by the user 2026-10-07 |
| 7 | UI primitives (`ui.circle`, `ui.ring`, `ui.round`, `ui.frame`, `ui.gradient`, bar shapes) | - | `CODE-DRAWN` | plain shapes, tinted in game |
| 8 | Everything in Batches 1-13 (gems, starter pets, Dunewing, Psyling, Azurewing, cards art, elements, HUD, buttons, room, avatar, lobby, forge, stones, world map, town) | 179 files | `HAVE` | table "Already painted" below |
| 11 | Second forms of Dunewing and Psyling (`enemy_beetle_form02_*`, `enemy_psyling_form02_*`) | 8 | `HAVE` | painted small (68% of the frame); the game enlarges them with `Tools/ReframeSecondForms.ps1`. Do NOT repaint unless the user asks |
| 12 | Arrow mini game kit, first set (`Battle/QTE`: four arrows, slot frame, timing marker, timer frame) | 7 | `HAVE` | in the game since 2026-10-07 |
| 15 | Arrow mini game kit after the original (`Battle/QTE/qte_*_v01.png`): pill bar, three tokens, white arrow, slider track / good / perfect / knob, damage flame | 10 | `HAVE` | painted 2026-10-07, in the game |
| 17 | Arrow mini game touch buttons `qte_button_dir_v01.png`, `qte_button_strike_v01.png` | 2 | `HAVE` | Batch 19, exported 2026-10-08; available to the importer |
| 13 | Idle animation frames 2 and 3 for the eleven creatures in play (Emberkit, six East Sea first forms, four East Sea second forms) | 22 | `HAVE` | painted 2026-10-07, in the game |
| 14 | East Sea battle background, version 2: the same reef with two flat rock outcrops where the two creatures stand | 1 | `HAVE` | Batch 19, exported 2026-10-08; v01 preserved |
| 9 | Second forms of Emberkit, Leafling, Tidepup; a card for Tidal Siphon | - | `NEVER` | decided 2026-10-06 |
| 10 | Third and later forms of any creature | - | `WAIT` | the rule for when a pet takes a third form is not decided |

No images are currently ordered for painting. Batch 19 is complete; rows marked `WAIT` remain undecided.

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

## Shared preamble (put at the top of every batch)

```
Continue the Pokiwar FG38 art set. Reply to me in Vietnamese.
Read first: Art/Pokiwar_FG38/ART_BIBLE.md and Art/Pokiwar_FG38/AssetManifest.json, and look at
Art/Pokiwar_FG38/Previews/Battle_form01_v01.png as the style target (canyon fantasy, chunky
silhouettes, near-black brown outline #21191F, two or three flat shade bands, cream highlight).
Batch 19 definitions and prompts are an archive of completed work, not a new drawing order. Read only the next explicitly ordered batch.
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
