# Pokiwar - assets that differ from the original

The user supplies the art style (FG38, painted by Codex); the original Zing Me art is not copied. This list is
the original element, what stands in for it, and where it shows in the clips.

| Original element | Clip @ time | Stand-in now | Replace with |
|---|---|---|---|
| Octagonal faceted gems | A 02:12 | FG38 square gems (user's choice, 2026-10-05) | keep, user decision |
| Flaming turn arrow | A 02:12 | Codex `battle_turn_arrow_flame_v01.png` | done (Batch 6) |
| Level badge bottom corners | A 02:12 | element badges, Codex art `Art/FG38/element_*.png` | done |
| Shield effect on the pet | - | Codex `battle_shield_bubble_v01.png` | done (Batch 6) |
| Top HUD panel, timer, name strips, bars | A 02:12 | Codex HUD kit | done (Batch 6) |
| "ĐẾN LƯỢT" stylised word art | A 02:32 | Codex `text_your_turn_v01.png` ("YOUR TURN") | done (Batch 6) |
| "TẶNG PHẨM" gold popup with ray burst | A 04:10 | FG38 `ui_reward_panel_v01.png` + Codex ribbon, rays, round close | done (Batch 7) |
| Room ("Sân") panel and stands | A 01:57 | Codex room panel, pedestal, add-card slot | done (Batch 7) |
| Buttons and dark panels | - | Codex button set and `ui_panel_dark_v01.png` | done (Batch 7); the panel has a smudge baked into its middle, so the game draws only its frame over a flat fill (`SkinPanel`) |
| Player avatar (paper doll) | A 01:57, B 11:02 | Codex base body, 11 layers, 11 shop icons | done (Batch 8) |
| Team portraits, chat | A 02:12, B 01:20 | none (online features) | out of scope |
| Card faces with value overlay (+350) | B 01:22 | FG38 card art + cost strip | keep |

Every Codex file is mapped in `Fg38Art.Items`; the builder keeps the flat stand-in until the file exists.
