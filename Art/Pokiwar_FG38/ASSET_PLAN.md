# Asset mapping and production status

Functional mapping is based on the supplied brief; original Pokiwar screenshots/video were not attached. Existing Match_3_Unity art is unrelated to this pet-battle visual system.

| Pokiwar function | New asset/layer | Export | Pivot / layering | Priority / status |
|---|---|---|---|---|
| Shared 8x8 board | `battle_board_frame_v01`, `battle_board_tile_v01` | 1024 square frame + 256 tile, alpha | Frame above 64 repeated tiles | B1 ready |
| Six resource/action gems | `battle_gem_*_v01` | Six 512 square PNGs, alpha | Center over tile | B1 ready |
| Multiplier and states | `battle_gem_multiplier_x2/x3`, `battle_tile_selected/hint` | 256 square, alpha | Over individual gem/tile | B1 ready |
| Player and ordinary enemy | `pet_player_*`, `enemy_beetle_*` | 1402x1122 source, alpha | Bottom-center; sprite swap | B1 ready key poses |
| Boss two forms | `boss_crystaldrake_form01/02_*` | 1402x1122 source, alpha | Bottom-center; form swap | B1 ready key poses |
| Battle arena | `battle_bg_canyon_v01` | 1920x1080 opaque | Bottom-most | B1 ready |
| HP/Mana/Rage/Shield | `battle_hud_bar_frame_v01` | 1024x341, alpha aperture | Fill behind, TMP above | B1 ready frame; fills driven by Unity |
| Turn/timer/QTE | `ui_turn_arrow_right`, `ui_timer_frame`, `qte_arrow_*`, `ui_qte_slot_frame`, `ui_qte_timing_marker` | 64–256 px, alpha | Dynamic numbers and five repeated slots | B1 ready |
| Card/skill deck | `ui_card_slot_*` | 512x768, alpha | Art, mana and TMP on separate layers | B1 ready slots; card illustrations planned |
| Action buttons | `ui_button_confirm_*` | 768x256, alpha | TMP label above | B1 ready |
| Slash/heal/mana/rage/shield/transform | `vfx_*_v01` | PNG alpha | Independent overlay | B1 ready static VFX |
| Drain, win/lose effects | `vfx_drain`, `vfx_win`, `vfx_lose` | PNG alpha | Independent overlay | B1 planned |
| Hub | `hub_bg_canyon_v01` | 1920x1080 opaque | Under live menu controls | B2 ready background |
| PvE map | `map_bg_canyon_v01` | 1920x1080 opaque | Under dynamic nodes | B2 ready background |
| Preparation | Reuse pet, enemy preview, card slots, button | Mixed alpha | Five repeated slots | B2 reusable layers; distinct screen panel planned |
| Reward | `ui_reward_panel_v01`, `icon_gold_v01`, `icon_upgrade_stone_v01` | 1024x768 panel, 512 icons | TMP and numbers above | B2 ready basic layers |
| Map node locked/open/progress | `ui_map_node_*` | 256 square alpha | Over map path | B2 planned |
| Inventory / upgrade / stone merge | `ui_inventory_panel`, `ui_upgrade_panel`, `ui_stone_merge_panel` | Transparent UI | Live content on top | B2 planned |
| Reward EXP/card/material | `icon_exp`, `icon_card`, `icon_material` | 256 square alpha | Independent | B2 planned |
| Element icons | `icon_element_*` | 256 square alpha | Independent | B2 planned; gem symbols may be reused temporarily |
| New pet/monster roster, more boss forms | Per-character sprites | ≥1024 high alpha | Bottom-center | B3 planned |
| Card illustration library | Per-card illustration | ≥768x1152 alpha/opaque art | Inside card slot | B3 planned |
| Onboarding and extended VFX | Per-screen/FX sprite | As required | Independent | B3 planned |

The Pokiwar 2 star/upgrade view is an optional module after the main offline flow. Chat, server and multiplayer UI are outside this first version.
