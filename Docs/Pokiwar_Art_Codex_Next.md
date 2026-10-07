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

## Status board - read this first (checked 2026-10-07 10:30 against `D:\Project\Pokiwar_Art_FG38\Assets`, 179 PNG)

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
| 1 | East Sea creatures, first forms: Samgong, Bebeboom, Ngoclam, Doimora, Voirong, boss Ongnamhai | 24 | `PAINT NEW` | Batch 14 below |
| 2 | East Sea second forms: Ngoclam, Doimora, Voirong, Ongnamhai | 16 | `PAINT NEW` | Batch 15 below |
| 3 | East Sea region art: world island, region emblem, battle background | 3 | `PAINT NEW` | Batch 15 below |
| 4 | Battle effects: rage fire vortex, flame wisp, siphon mote, hit burst, sparkle | 5 | `PAINT NEW` | Batch 16 below |
| 5 | Card faces `face_*.png` (9 single-use cards + the one skill face) | 10 | `WAIT` | code-drawn by `Tools/DrawCardFaces.ps1`, in the game now; the user has not judged them yet |
| 6 | Buff icons `buff_{heart,lightning,fire,shield}.png` | 4 | `CODE-DRAWN` | `Tools/DrawCardFaces.ps1`; heal and mana effects approved by the user 2026-10-07 |
| 7 | UI primitives (`ui.circle`, `ui.ring`, `ui.round`, `ui.frame`, `ui.gradient`, bar shapes) | - | `CODE-DRAWN` | plain shapes, tinted in game |
| 8 | Everything in Batches 1-13 (gems, starter pets, Dunewing, Psyling, Azurewing, cards art, elements, HUD, buttons, room, avatar, lobby, forge, stones, world map, town) | 179 files | `HAVE` | table "Already painted" below |
| 11 | Second forms of Dunewing and Psyling (`enemy_beetle_form02_*`, `enemy_psyling_form02_*`) | 8 | `HAVE` | painted small (68% of the frame); the game enlarges them with `Tools/ReframeSecondForms.ps1`. Do NOT repaint unless the user asks |
| 12 | Arrow mini game kit (`Battle/QTE`: four arrows, slot frame, timing marker, timer frame) | 7 | `HAVE` | in the game since 2026-10-07 |
| 9 | Second forms of Emberkit, Leafling, Tidepup; a card for Tidal Siphon | - | `NEVER` | decided 2026-10-06 |
| 10 | Third and later forms of any creature | - | `WAIT` | the rule for when a pet takes a third form is not decided |

Three batches, one Codex thread each: Batch 14 (24 images), Batch 15 (19 images), Batch 16 (5 images). The roster
behind 14 and 15 is in `Docs/EastSea.md`.
### Batch 14 - East Sea creatures, first forms (24 images)

```
<shared preamble>
Paint six sea creatures of the East Sea region. Each one is a real animal of that sea fused with one object from
Vietnamese coastal life, so the silhouette reads as ONE creature, not an animal carrying a prop.
Every creature: four poses (idle, attack, hit, defeat), 1024x1024, transparent background, the creature FACES
LEFT, same scale and ground line across its four poses, same style as enemy_beetle_idle_01.png.
Save to Battle/Characters/ under exactly these names:

1. enemy_samgong_{idle,attack,hit,defeat}_01.png
   SAMGONG - a horseshoe crab whose domed shell IS a Dong Son bronze drum: star in the centre of the shell,
   concentric bands, verdigris green in the grooves, warm bronze on the ridges. Long spike tail held up like a
   drumstick, small glowing eyes under the rim. Sturdy, low, earthy. Attack: slams its tail on its own shell,
   a shock ring bursts out. Element Earth.
2. enemy_bebeboom_{idle,attack,hit,defeat}_01.png
   BEBEBOOM - a peacock mantis shrimp boxer: rainbow shell (teal, orange, magenta), two club arms shaped like
   bundles of red firecrackers with short lit fuses, big round stalk eyes, cocky stance on its tail. Attack:
   a punch so fast the club leaves a white flash and sparks. Element Fire.
3. enemy_ngoclam_{idle,attack,hit,defeat}_01.png
   NGOCLAM - a giant clam with thick wavy lips, mantle in electric blue and violet with gold spots, shell plated
   like mother-of-pearl armour. It is almost shut: only two shy eyes and a faint glow show in the gap. Attack:
   snaps shut, a blade of water shoots out. Element Metal.
4. enemy_doimora_{idle,attack,hit,defeat}_01.png
   DOIMORA - a hawksbill turtle with a sharp hooked beak and amber tortoiseshell plates; on its back grows a
   tiny islet: sand, grass and one young sea-almond tree with square green fruit. Calm, old eyes. Attack: swings
   its body, the square fruit fly like sling stones. Element Wood.
5. enemy_voirong_{idle,attack,hit,defeat}_01.png
   VOIRONG - a banded sea krait (blue-grey and black rings, paddle tail) coiled upward inside a small waterspout,
   head above the spray, yellow snout, forked tongue. Attack: the spout bends forward like a whip. Element Water.
6. boss_ongnamhai_form01_{idle,attack,hit,defeat}_01.png
   ONGNAMHAI - the whale lord of the fishermen: a broad Bryde's whale with three ridges on its head, wearing a
   red and gold ceremonial sash across the brow, bronze bells on its flippers, barnacles like old medals, two
   round lantern-buoys trailing from its tail. Dignified, heavy, kind eyes but huge. Fills the frame more than
   the others. Attack: blows a tall spout that falls as a wave. Element Water.
```

### Batch 15 - East Sea second forms and region art (19 images)

```
<shared preamble>
Second forms of four East Sea creatures painted in Batch 14 - open the Batch 14 idle of each first and keep its
colours and proportions; the second form is the same creature after it powers up, about 10% bigger and brighter.
Four poses each (idle, attack, hit, defeat), 1024x1024, transparent, FACING LEFT. FRAMING: the second form must stand at least as tall in the frame as its first form and on the same ground line - no pose (the hit pose included) may be taller than 85% of the canvas, and the idle fills about 80% of its height. Batch 12 was painted at 68% and came out smaller than the first forms in the game. Save to Battle/Characters/:

1. enemy_ngoclam_form02_{idle,attack,hit,defeat}_01.png
   NGOCLAM RADIANT - the clam stands wide open: a great glowing pearl floats between the valves, light rays and
   small orbiting pearls, the mantle flares like a crown.
2. enemy_doimora_form02_{idle,attack,hit,defeat}_01.png
   DOIMORA BEACON - the sea-almond tree is fully grown and in white-pink bloom, a small stone lighthouse stands
   beside it with a warm beam, roots wrap the shell edges.
3. enemy_voirong_form02_{idle,attack,hit,defeat}_01.png
   VOIRONG TEMPEST - the waterspout is twice as tall and dark, lightning crawls inside it, the snake has grown a
   dragon crest and whiskers, storm cloud ring around its head.
4. boss_ongnamhai_form02_{idle,attack,hit,defeat}_01.png
   ONGNAMHAI THE TIDE LORD - the whale rises on a wave shaped like a temple roof, sash turned to a gold mantle,
   bells ringing with visible sound rings, a school of small glowing fish forms a halo.

Region art, no creature:
5. Meta/World/world_island_eastsea_v01.png - 1024, transparent: a cluster of coral reef and sand cays seen from
   above at the same angle as world_island_sunny_v01.png: turquoise lagoon, one lighthouse, a fishing boat.
6. Meta/Lobby/region_east_sea_v01.png - 256, transparent: emblem tile, a wave curling around a coral branch.
7. Battle/Background/battle_bg_eastsea_v01.png - 2048x1152, opaque: a shallow reef flat at low tide, coral
   heads and tide pools at the sides, open sea and far cays at the horizon; the middle and the two bottom
   corners stay calm and empty (the board and the pets stand there), same camera as battle_bg_canyon_v01.png.
```

### Batch 16 - battle effects (5 images)

```
<shared preamble>
Paint five battle effect sprites. No creature, no text. Transparent background. Save to Battle/FX/ (new folder):

1. fx_rage_vortex_v01.png - 1024x512: a ring of fire lying flat on the ground, seen from the same 3/4 camera as
   the creatures stand in: an ellipse about four times wider than tall, hollow in the middle (a creature stands
   inside it), flames licking upward along the rim and leaning one way as if the ring spins. Orange core, red
   edge, cream highlight, dark outline as in the set.
2. fx_flame_wisp_v01.png - 256x256: one single tongue of flame, teardrop shape, pointing up, same colours.
3. fx_siphon_mote_v01.png - 256x256: one comet-shaped wisp of energy, round head at the right and a tapering
   tail to the left. Paint it in WHITE and light grey only, with the dark outline - the game tints it red for
   stolen rage and blue for stolen mana.
4. fx_hit_burst_v01.png - 512x512: an impact burst, a jagged star of 8-10 points, cream centre to orange tips.
5. fx_sparkle_v01.png - 256x256: one four-point sparkle, WHITE and light grey only (tinted in game).
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
