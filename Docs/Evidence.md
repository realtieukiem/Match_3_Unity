# Pokiwar - video evidence

What the reference clips show, where, and what the game does with it. Frames were cut with
`Pokiwar.EditorTools.PokiwarVideoFrames.Run` (see "Re-checking a frame" below) and read one by one.

Clips (in `C:\Users\TS03128\Downloads`): **A** `Pokiwar trên Zing Me.mp4` (13:03, 1280x720),
**B** `[POKIWAR] Khi thánh chơi POKIWAR và cái kết.mp4` (11:04, 1276x720), **C** the "Pokiwar 2" troll clip,
**M** `Pokiwar..mp4` (Manaphy). Numbers for M come from the earlier report `Pokiwar-Video-Analysis-Update.md`.

Status: **CONFIRMED_BY_VIDEO** read off a frame here, **CONFIRMED_BY_USER** stated by the user,
**INFERRED** fitted to a few samples, **UNKNOWN** not visible in any clip.

## Clip B, lobby to battle (read 2026-10-06)

The game is locked to landscape (user, 2026-10-06: portrait is not needed for now): `ResponsiveCanvas.Force`
is `Landscape` and autorotate to portrait is off. The portrait rects stay in the scene builder, unused.

| Clip @ time | Seen | Game | Status |
|---|---|---|---|
| B 00:06 | World map "Đại hải trình": sea with islands, tap an island | not built - `MapScreen` is a 3-node path | CONFIRMED_BY_VIDEO |
| B 00:20 | Hub: grid of 12 regions on the left, ring of 12 numbered bosses on the right, top bar (rank points, gold, server, ĐỘI CHIẾN), bottom nav (thông tin, đổi avatar, luyện thẻ, luyện poke, shop thẻ, shop avatar), energy 36/80 | `MapScreen` is the lobby and the first screen: 12 region tiles on the left (one real, the rest locked), the region's opponents numbered on a ring around a planet (`RingLayout`, `RingRadius`), top bar with name, gold and energy, bottom nav INFO / AVATAR / CARDS / PETS. `HubScreen` is the INFO page. Flat shapes until Codex Batch 10; no rank points, server, team button, shops or chat | layout CONFIRMED_BY_VIDEO |
| B 00:35, 00:50 | "Luyện poke": pet on a pedestal, three ĐÁ slots, stone grid with counts, NÂNG CẤP, carousel of six pets, lucky and protection charms with a price and MUA | `UpgradeScreen` has the same actions as lists, not the pedestal layout; no charm shop | CONFIRMED_BY_VIDEO |
| B 01:00 | "Luyện thẻ": tabs Luyện thẻ / Luyện đá / Tặng phẩm; card on a pedestal with three stone slots; YELLOW card stones in their own grid; lucky points stepper; tray of owned cards with mana cost top-left, level badge top-right, damage at the bottom (160 L5 372, 200 L6 675, 140 L6 396, 80 L5 193, 360 L12 3162) | `CardForgeScreen`: card face with cost, level badge and damage; `SaveData.CardStones` are separate from pet `Stones`; `UpgradeService.UpgradeCard`. No lucky points, one stone per attempt | card face and separate stones CONFIRMED_BY_VIDEO, odds INFERRED |
| B 01:05 | Room "SÂN 8709": five CHỌN CARD slots holding one reusable card and three "+350" consumables, pet carousel, SẴN SÀNG | `PrepScreen`: five slots hold reusable and single-use cards together (max 2 reusable) | CONFIRMED_BY_VIDEO |
| B 01:15 | VS splash: player team left, boss right, big "VS", "BlueWings (độ khó sàn cấp 108)" | `BattleController.PlayIntro`: both fighters slide in, VS slams, FIGHT! pops | CONFIRMED_BY_VIDEO (FIGHT! is ours) |
| B 01:22 -> 04:37 | The `360` and `200` cards stay all battle; the `+350` cards are gone after use | reusable cards = `SkillDef` owned by the player (`SaveData.SkillCards`, level per card), any pet carries them, bosses use theirs at `EncounterDef.SkillLevel` | CONFIRMED_BY_VIDEO + CONFIRMED_BY_USER |
| B 10:52 | Reward popup lists stones by element and tier, each with an icon and count | text lines only | CONFIRMED_BY_VIDEO |

## Boss stats against the player (read 2026-10-06)

Every number below is the `cur/max` text in the top HUD at the first frame of the battle. "Hunt level" is the
"độ khó săn cấp N" under the boss in the room; the same number is printed on the boss's tag in the lobby
(A 04:20: Flygon 2, Abra 3, Whiscash 14, Cubone 25, Purifly 27, Dramangon 40 ... Qilong 92; B 00:20: 97 to
about 125), so it belongs to the boss, not to the player.

| Clip @ time | Boss (hunt level) | Boss HP | Boss mana | Player pet HP | Player mana |
|---|---|---|---|---|---|
| A 02:12 | Flygon (2) | 1650/2200 | 770 | 2405/3207 | 770 |
| A 04:41 | Abra (3) | 1650/2200 | 300 | 382/510 | 300 |
| A 05:40 | Abra (3) | 1650/2200 | 770 | 2405/3207 | 770 |
| A 11:21 | Abra (3) | 1650/2200 | 305 | 378/505 | 305 |
| M 00:20 | Manaphy (84) | 28400 max | 3856 | 12815 max | 2410 |
| B 01:22 | BlueWings (108), three players | 81000/108000 | 4650 | 13895 max | 2326 |

What that settles:

- Start HP is 75% of max on both sides in every clean first frame (1650/2200, 2405/3207, 382/510, 378/505,
  81000/108000). CONFIRMED_BY_VIDEO.
- Boss max HP does NOT follow the pet that is fielded: Abra has 2200 against 3207, 510 and 505. HP looks like a
  value of the boss (hunt level 2 and 3 -> 2200, 84 -> 28400, 108 -> 108000 = 1000 per level). Three points, no
  single formula fits. CONFIRMED_BY_VIDEO for "not scaled in clip A", formula UNKNOWN.
- Boss max MANA does follow the player: equal at hunt level 2-3 (three different pets, three matching values),
  x1.6 at level 84 (2410 -> 3856, exact), x2.0 at level 108. CONFIRMED_BY_VIDEO.
- The game follows this (user, 2026-10-06: follow the video, we pick the numbers): a boss's HP is its own, its
  max mana is the pet's times `EncounterDef.ManaVsPlayer`, and the lobby tag shows `Hunt Lv` and `Wins x/y`.
  Numbers and win rates: `Docs/UnverifiedRules.md`.

Lobby tags also show `THẮNG x/y` (wins / wins needed: 0/1, 1/1, 0/3, 4/3), `YÊU CẦU` (a rank-point
requirement) and `CHIẾN TÍCH +10` (rank points for the win). Not built.

## Pet upgrade and the boss's second form (read 2026-10-06)

| Clip @ time | Seen | Game | Status |
|---|---|---|---|
| C 02:05 | "Luyện poke": Sclerosis (hệ hỏa) Cấp14, "Pet đã đạt cấp cao nhất", HP 10080, ATK 772, mana 1680; three ĐÁ slots; the stone grid holds only fire stones (168, 252, 478, 330, 17, 277, 142); "Tích lũy khi thất bại: 0%"; Bùa May Mắn and Bùa An at 10000 each | one pet level raised by stones of the pet's element, max 14, fail bonus, lucky and protection charms | CONFIRMED_BY_VIDEO |
| C 02:30, 03:45, 06:00 | Other pets at Cấp14: 9660 / 768 / 1680, 13963 / 798 / 1680, Shenlong (kim) 11130 / 789 / 1680 - mana is 1680 for every pet at Cấp14 | per-creature stat tables | CONFIRMED_BY_VIDEO |
| C 06:00 | Skill tooltip: "Hao tốn 200 mana, 200 nộ. Tấn công gây 140% sát thương đồng thời biến số sát thương thành giáp trong 1 lượt" | not built as such | CONFIRMED_BY_VIDEO |
| M 00:20, A 02:12 | Pet level badge bottom-left in battle (LV14, LV7, LV1) - the same number as the upgrade screen's Cấp | level not shown in battle (user, 2026-10-05) | CONFIRMED_BY_VIDEO |
| B 04:36 -> 04:40 | BlueWings 34857/108000 changes form and stands at 54311/108000: HP refilled to half of max, max unchanged | boss phase sets HP to 50% of max, attack x1.15 | HP CONFIRMED_BY_VIDEO, attack CONFIRMED_BY_USER (no number in the clip) |

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
