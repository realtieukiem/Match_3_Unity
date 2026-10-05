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

## After any batch

Tell Claude Code "lấy asset Codex mới vào game" (or run `Pokiwar/Rebuild Scene + Art` in Unity). Painted
square gems replace the code-drawn ones automatically; everything else fills the matching sprite key.
