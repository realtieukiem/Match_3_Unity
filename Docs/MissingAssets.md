# Pokiwar - assets that differ from the original

The user supplies the art style (FG38, painted by Codex); the original Zing Me art is not copied. This list is
the original element, what stands in for it, and where it shows in the clips.

| Original element | Clip @ time | Stand-in now | Replace with |
|---|---|---|---|
| Octagonal faceted gems | A 02:12 | FG38 square gems (user's choice, 2026-10-05) | keep, user decision |
| Flaming turn arrow | A 02:12 | `Art/Placeholder/battle_arrow.png` (code-drawn) | done (Batch 6) |
| Level badge bottom corners | A 02:12 | element badges, Codex art `Art/FG38/element_*.png` | done |
| Shield effect on the pet | - | `Art/Placeholder/fx_bubble.png` (code-drawn) | done (Batch 6) |
| Top HUD panel, timer, name strips, bars | A 02:12 | `hud.bar` FG38 frame on a blue panel, flat bars | done (Batch 6) |
| "Äáº¾N LÆ¯á»¢T" stylised word art | A 02:32 | outlined text "YOUR TURN" | done (Batch 6) |
| "Táº¶NG PHáº¨M" gold popup with ray burst | A 04:10 | FG38 `ui_reward_panel_v01.png` + flat purple pill + grey X | done (Batch 7) |
| Room ("SÃ¢n") panel and stands | A 01:57 | blue rounded panel + white ellipse + "+" text slots | done (Batch 7) |
| Blue / red / grey buttons, dark panels | - | flat rounded rectangles | done (Batch 7); `ui_panel_dark_v01.png` has a smudge baked into its middle, so the game draws only its frame (`SkinPanel`) |

Every Batch 6/7 file is mapped in `Fg38Art.Items`; the builder keeps the flat stand-in until the file exists.
| Player avatar, team portraits, chat | A 02:12, B 01:20 | none (online features) | out of scope |
| Card faces with value overlay (+350) | B 01:22 | FG38 card art + cost strip | keep |
