# Pokiwar - video evidence

What the reference clips show, where, and what the game does with it. Frames were cut with
`Pokiwar.EditorTools.PokiwarVideoFrames.Run` (see "Re-checking a frame" below) and read one by one.

Clips (in `C:\Users\TS03128\Downloads`): **A** `Pokiwar trên Zing Me.mp4` (13:03, 1280x720),
**B** `[POKIWAR] Khi thánh chơi POKIWAR và cái kết.mp4` (11:04, 1276x720), **C** the "Pokiwar 2" troll clip,
**M** `Pokiwar..mp4` (Manaphy). Numbers for M come from the earlier report `Pokiwar-Video-Analysis-Update.md`.

Status: **CONFIRMED_BY_VIDEO** read off a frame here, **CONFIRMED_BY_USER** stated by the user,
**INFERRED** fitted to a few samples, **UNKNOWN** not visible in any clip.

## Battle layout and rhythm

| Clip @ time | Seen | Game | Status |
|---|---|---|---|
| A 02:12, B 01:22 | One top HUD panel: left name + HP/MP/rage bars with `cur/max`, right the same mirrored, big orange countdown digit between them, no ring | `TopHud` in `BattleScreen`: slanted code-drawn bars (`bar.shape`, `bar.gloss`), enemy side mirrored; the name is NOT repeated in the top panel (user, 2026-10-06: it cluttered the HUD) | bars CONFIRMED_BY_VIDEO, no top name CONFIRMED_BY_USER |
| A 02:12, 03:22 | The acting side's name strip turns gold with an arrow toward the opponent | not built: the top name strip was removed on the user's word (2026-10-06) | CONFIRMED_BY_VIDEO |
| A 02:12, B 01:22 | 8x8 board in the centre; pets stand below it, player left facing right, opponent right | board at the centre, pets bottom-left / bottom-right | CONFIRMED_BY_VIDEO |
| A 02:12, 03:22, B 04:35 | A flaming arrow at the acting side's feet/name points at the opponent | the pet's name sits under the pet; `AttackArrow` sits beside the acting side's name, nudging toward the foe | CONFIRMED_BY_VIDEO (art is FG38, not the original flame) |
| A 02:17, 02:20, 03:12, 03:22 | After a match the board disappears; the collected gems show as a centred row of icons with red counts under the HUD; icons drop out one by one as their effect plays | board fades out, `GemSummaryView` row under the HUD, `Consume(gem)` per effect in resolve order | CONFIRMED_BY_VIDEO |
| A 02:32 | "ĐẾN LƯỢT" in large stylised letters over the board when your turn starts | `TurnPop` "YOUR TURN" (English until Vietnamese is requested) | CONFIRMED_BY_VIDEO |
| A 02:20, 03:22 | Gains float as numbers at the pet (+83, +198) | `FloatingTextLayer` at the pet | CONFIRMED_BY_VIDEO |
| B 01:22, 04:37 | Loadout cards sit in a row under the board, art + cost | `CardBar` under the board: the pet's mana skills come first as reusable cards (B 01:22 `360`/`200` stay, the `+350` cards are gone by 04:37), then the single-use cards, which leave the row when used | layout CONFIRMED_BY_VIDEO, single use and "reusable = the pet's skill" CONFIRMED_BY_USER |
| A 02:32, B 02:22 | Round skill/item buttons with a cost in a column at the right | not built: those cost the trainer's own lightning counter (top-left), a resource the slice does not have; pet skills are the reusable cards in `CardBar` | CONFIRMED_BY_VIDEO |
| A/B bottom corners | Player level badge bottom-left, opponent bottom-right | element badge in those corners; level not shown (user, 2026-10-05) | layout CONFIRMED_BY_VIDEO, content CONFIRMED_BY_USER |
| B 02:22 | QTE: five arrow keys in a bar, a timing bar above with "good", damage +7032 beside | `QteView`; 2836 -> 7032 good / 7089 perfect | CONFIRMED_BY_VIDEO |
| B 04:33 -> 04:37 | BlueWings 34957/108000, transform card icon, large form at 54000/108000 | boss phase sets HP to 50%, sprite swap | HP after CONFIRMED_BY_VIDEO, trigger UNKNOWN |
| A 10:00 | Refill gems drop in from the top while the opponent plays on the same board | shared board, gravity refill | CONFIRMED_BY_VIDEO |

## Flow

| Clip @ time | Seen | Game | Status |
|---|---|---|---|
| A 01:57, 04:17, B 11:02 | "Sân" room: player and pet left, opponent right with its name, READY button, five "CHỌN CARD" slots with X, "Chọn PET" | `PrepScreen`: pet left, opponent right, READY, 5 slots (tap = picker, X = remove), CHOOSE PET picker | CONFIRMED_BY_VIDEO |
| A 04:10, B 10:50 | Gold "TẶNG PHẨM" popup over the battle listing the rewards (A: 1 pokemon Flygon + 20 points; B: chest of stones); close X | `ResultScreen` popup over the battle, X returns to the room; the first win hands over the creature just fought (`ProgressionService.CommitBattle`, `EncounterDef.CaptureOnFirstWin`) and the popup shows it | CONFIRMED_BY_VIDEO |
| A 04:17 | Back in the room the panel shows "+20 EXP" | reward lines in the popup | partial |
| A 01:57, B 11:02 | Player avatar is a chibi paper doll (bald body in underwear when nothing is worn, dressed in B) standing in the room beside the pet; bottom menu has "đổi avatar" and "shop avatar" | `AvatarView` in hub, room and wardrobe; `AvatarScreen` buys / wears pieces | CONFIRMED_BY_VIDEO, items and prices INFERRED |
| A 02:12, B 01:20 | Battle: avatar card (face + name) top-left; a wild monster opponent (Flygon, BlueWings) has none | `BattleController.PlayerCard`, `EnemyCard` hidden for monsters | CONFIRMED_BY_VIDEO |
| A 08:00 | Lucky wheel | not built (event meta, out of the offline slice) | out of scope |
| B 01:20 | Team portraits at the left, chat box at the bottom | not built (online room features) | out of scope |

## Re-checking a frame

Close the Editor, then:

```
Unity.exe -batchmode -projectPath <project> -executeMethod Pokiwar.EditorTools.PokiwarVideoFrames.Run
  -video <clip.mp4> -times "132,137.5" -out <dir> [-width 960] -logFile frames.log
```

Copy a clip to an ASCII path first; Vietnamese letters and `[` `]` in the file name break the video URL.
