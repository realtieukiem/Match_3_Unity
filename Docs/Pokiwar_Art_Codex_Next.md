# Pokiwar FG38 art - remaining batches for Codex

Paste ONE batch per NEW Codex thread. Each batch stays under 25 generated images, because a thread that
carries more inline images exceeds the 64 MiB request limit (`Encoded request body exceeds 67108864 bytes`)
and cannot be used again.

## Rule for this file

Prompts exist here ONLY for what is still to paint; the status board names everything else so nothing is painted
twice. When a batch is painted its prompt is deleted from here, its board row turns to `HAVE` and the batch
moves to the "Already painted" table below - never leave a finished prompt in place, Codex would paint it
again. Before painting anything, check the file name under `D:\Project\Pokiwar_Art_FG38\Assets`: a file that
exists is finished, do not repaint or overwrite it unless the user asks for a new version by name.

## Status board - read this first (checked 2026-10-07 12:34 against `D:\Project\Pokiwar_Art_FG38\Assets`, 227 PNG)

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
| 5 | Card faces `face_*.png` (9 single-use cards + the one skill face) | 11 | `PAINT NEW` | Batch 18 below, part A (the owner found the code-drawn faces ugly, 2026-10-07); the code-drawn ones stay in the game until then |
| 6 | Buff icons `buff_{heart,lightning,fire,shield}.png` | 4 | `CODE-DRAWN` | `Tools/DrawCardFaces.ps1`; heal and mana effects approved by the user 2026-10-07 |
| 7 | UI primitives (`ui.circle`, `ui.ring`, `ui.round`, `ui.frame`, `ui.gradient`, bar shapes) | - | `CODE-DRAWN` | plain shapes, tinted in game |
| 8 | Everything in Batches 1-13 (gems, starter pets, Dunewing, Psyling, Azurewing, cards art, elements, HUD, buttons, room, avatar, lobby, forge, stones, world map, town) | 179 files | `HAVE` | table "Already painted" below |
| 11 | Second forms of Dunewing and Psyling (`enemy_beetle_form02_*`, `enemy_psyling_form02_*`) | 8 | `HAVE` | painted small (68% of the frame); the game enlarges them with `Tools/ReframeSecondForms.ps1`. Do NOT repaint unless the user asks |
| 12 | Arrow mini game kit, first set (`Battle/QTE`: four arrows, slot frame, timing marker, timer frame) | 7 | `HAVE` | in the game since 2026-10-07 |
| 15 | Arrow mini game kit after the original: pill bar, round tokens, slider, damage flame, touch buttons | 12 | `PAINT NEW` | Batch 18 below, part B |
| 13 | Idle animation frames 2 and 3 for the eleven creatures in play (Emberkit, six East Sea first forms, four East Sea second forms) | 22 | `PAINT NEW` | Batch 17 below |
| 14 | East Sea battle background, version 2: the same reef with two flat rock outcrops where the two creatures stand | 1 | `PAINT NEW` | Batch 17 below, item 12 |
| 9 | Second forms of Emberkit, Leafling, Tidepup; a card for Tidal Siphon | - | `NEVER` | decided 2026-10-06 |
| 10 | Third and later forms of any creature | - | `WAIT` | the rule for when a pet takes a third form is not decided |

Two batches are ordered, one Codex thread each: Batch 17 (22 idle animation frames and one battle background) and Batch 18 (11 card faces and 12 arrow mini game pieces). Nothing else.

Framing rule for every future creature batch (learned from Batches 12, 14 and 15): the game shows a creature at
the size it fills its canvas, so paint the idle at about 80% of the canvas height, keep every pose (the hit pose
included) under 90%, and keep one ground line across the four poses. Creatures painted smaller are enlarged by
`Tools/ReframeSecondForms.ps1` on the game side, which costs sharpness.
### Batch 17 - idle animation frames and the battle background with standing rocks (23 images)

```
<shared preamble>
Items 1-11: paint TWO more idle frames for each of eleven creatures, so the game can play a short breathing loop
(frame 1 -> 2 -> 3 -> 2 -> 1). Frame 1 already exists - it is the creature's idle picture. For EACH creature:
- open its existing idle picture first and use it as the image to edit, not as loose inspiration;
- keep the SAME canvas size, the same position, the same scale, the same ground line and the same colours -
  if the two new frames are laid over frame 1, the feet and the body outline must line up;
- change only small living parts, a little in frame 2 and a little more in frame 3. The body itself may rise
  or sink by 1-2% (breathing). No new props, no new pose, no effects that are not in frame 1.
Transparent background. Save to Battle/Characters/ under exactly these names (_idle_02 and _idle_03):

 1. pet_player_idle_02.png, pet_player_idle_03.png          (edit pet_player_idle_01.png, 1402x1122)
    Emberkit: tail tip sways, ears twitch, the crystal on the forehead glints, chest breathes.
 2. enemy_samgong_idle_02.png, _03.png                      (edit enemy_samgong_idle_01.png)
    Samgong: the spike tail tilts like a drumstick, legs shuffle slightly, eyes glow brighter then dimmer.
 3. enemy_bebeboom_idle_02.png, _03.png                     (edit enemy_bebeboom_idle_01.png)
    Bebeboom: the fuse sparks flicker and shorten, the club arms bob like a boxer's guard, stalk eyes swivel.
 4. enemy_ngoclam_idle_02.png, _03.png                      (edit enemy_ngoclam_idle_01.png)
    Ngoclam: the shell opens a crack wider then closes, the mantle ripples, the eyes blink (shut in frame 3).
 5. enemy_doimora_idle_02.png, _03.png                      (edit enemy_doimora_idle_01.png)
    Doimora: the head nods slowly, flippers paddle a little, the sea-almond leaves sway.
 6. enemy_voirong_idle_02.png, _03.png                      (edit enemy_voirong_idle_01.png)
    Voirong: the waterspout swirls (splashes move round), the head weaves side to side, tongue in then out.
 7. boss_ongnamhai_form01_idle_02.png, _03.png              (edit boss_ongnamhai_form01_idle_01.png)
    Ongnamhai: the tail fluke lifts and drops, the lantern buoys and bells swing, a small puff at the blowhole.
 8. enemy_ngoclam_form02_idle_02.png, _03.png               (edit enemy_ngoclam_form02_idle_01.png)
    Ngoclam Radiant: the pearl pulses brighter, the small pearls move along their orbit, sparkles change place.
 9. enemy_doimora_form02_idle_02.png, _03.png               (edit enemy_doimora_form02_idle_01.png)
    Doimora Beacon: the lighthouse beam turns, blossoms and leaves sway, a petal falls, the head nods.
10. enemy_voirong_form02_idle_02.png, _03.png               (edit enemy_voirong_form02_idle_01.png)
    Voirong Tempest: lightning jumps to a new place in the spout, the cloud ring turns, whiskers wave.
11. boss_ongnamhai_form02_idle_02.png, _03.png              (edit boss_ongnamhai_form02_idle_01.png)
    Ongnamhai the Tide Lord: the wave under him rolls, the ring of water flows round, bells swing, mantle flutters.

Check before saving each pair: flip between frame 1 and the new frame at 100% - the creature must not jump,
shrink or change proportions. If it does, redo that frame rather than keeping it.

12. Battle/Background/battle_bg_eastsea_v02.png - 2048x1152, opaque. A NEW VERSION of battle_bg_eastsea_v01.png
    (open it first; keep its sea, sky, far cays, coral and colours) with ONE change: the two creatures need a
    place to stand. Add two flat-topped rock outcrops rising out of the shallow water, one on each side:
    - left rock: flat top centred at x = 350, y = 885; right rock: flat top centred at x = 1700, y = 885
      (pixels in the 2048x1152 image, y counted from the top);
    - each top is a level, clearly readable standing surface about 560 px wide and 110 px deep, seen from the
      same slightly raised camera as the rest of the picture, lit from above, a little wet at the edge;
    - the rock face below each top runs down to the bottom edge of the image (weathered reef stone, barnacles,
      a few small corals), so a name label can sit on it;
    - nothing tall on the rocks: no plants, posts or props that would stand in front of or behind a creature.
    The whole middle of the image between x = 640 and x = 1410 stays calm open water (the game board covers it),
    and the top 190 px stay simple sky (the health bars cover it).
    Do not overwrite v01.
```
### Batch 18 - card faces and the arrow mini game kit (23 images)

```
<shared preamble>
PART A - eleven card faces. The owner finds the current code-drawn faces ugly; paint them properly in the set's
style (chunky shapes, near-black brown outline, two or three flat shade bands, cream highlight).
Every face: 864x1104, rounded corners (radius about 80 px, transparent outside), NO text, NO numbers, NO frame -
the game draws the frame, a big number and the cost on top. Layout of every face:
- one flat background colour block for the whole card, with one lighter diagonal band from the top-left corner;
- ONE big pictogram, centred at about x 432, y 400, filling roughly 520x520 px - readable at 100 px card width;
- the lower 40% of the card (below y 660) stays plain background: the game prints the number there.
A player must tell the resource from the colour alone. Save to Cards/Faces/ (new folder):

 1. face_mana_potion_v02.png   - BLUE card. A round flask of glowing blue liquid with a small lightning bolt on it.
 2. face_rage_ember_v02.png    - RED card with a YELLOW diagonal band. One fat flame, orange core.
 3. face_herbal_salve_v02.png  - GREEN card. A white heart with a green cross on it, two small leaves.
 4. face_iron_skin_v02.png     - PURPLE card. A heater shield, steel rim, purple field.
 5. face_fire_bolt_v02.png     - DARK SLATE card with a yellow band. A fireball flying down-right with a tail.
 6. face_meteor_v02.png        - DARK SLATE card with a red band. A large burning rock with a long flame tail.
 7. face_summon_sprite_v02.png - DARK SLATE card with a yellow band. A small winged sprite, white-gold, arms open.
 8. face_war_cry_v02.png       - DARK SLATE card with an orange band. A war horn with three sound arcs.
 9. face_mana_leech_v02.png    - TEAL card. A blue lightning bolt being pulled into a dark swirl.
10. face_skill_v02.png         - ORANGE-GOLD card. A black silhouette fighter mid-punch with a white impact star
                                 at the fist (this one face is shared by every reusable skill card).
11. face_back_v02.png          - card back: deep blue with a gold paw-print emblem in a ring.

PART B - arrow mini game kit, after the original game's bar: a pill-shaped bar holding five round arrow tokens,
a small timing slider above it, the damage number in a flame at the right. Transparent background, no text.
Save to Battle/QTE/ :

12. qte_bar_v01.png            - 1280x288: a horizontal pill (fully rounded ends), dark teal glass inside, glowing
                                 cyan rim, thin dark outline. Empty - the tokens are drawn on top by the game.
13. qte_token_v01.png          - 256x256: a round glossy button, pink-magenta, white rim, soft top highlight. Empty.
14. qte_token_ok_v01.png       - 256x256: the same button in bright green with a faint glow.
15. qte_token_bad_v01.png      - 256x256: the same button in dull red-grey, slightly cracked.
16. qte_arrow_white_v01.png    - 256x256: a bold WHITE arrow pointing UP, rounded, thin dark outline (the game
                                 rotates it for the other three directions and puts it on a token).
17. qte_slider_track_v01.png   - 1024x96: a thin capsule slider track, dark inside, silver rim.
18. qte_slider_good_v01.png    - 256x96: a green glowing segment that sits inside that track (the game stretches it).
19. qte_slider_perfect_v01.png - 128x96: a gold glowing segment for the centre of the track.
20. qte_slider_knob_v01.png    - 128x128: a round red knob with a white ring - the moving marker.
21. qte_damage_flame_v01.png   - 768x288: a horizontal burst of flame pointing right, orange to yellow, darker
                                 in the middle so a white number reads on it.
22. qte_button_dir_v01.png     - 256x256: a round blue touch button, raised, empty (the white arrow goes on it).
23. qte_button_strike_v01.png  - 512x256: a wide red touch button with a crossed-swords emblem, raised, no text.
```
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

Do NOT paint either (decided 2026-10-06, they would never be shown): second forms for Emberkit, Leafling or
Tidepup - the free starter never changes form and the other two are not obtainable; a card for Tidal Siphon -
only the boss uses it and boss cards are not drawn.

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

## After any batch

Tell Claude Code "lấy asset Codex mới vào game" (or run `Pokiwar/Rebuild Scene + Art` in Unity). Painted
square gems replace the code-drawn ones automatically; everything else fills the matching sprite key.
