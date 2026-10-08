# Pokiwar - video evidence

What the reference clips show, where, and what the game does with it. Frames were cut with
`Pokiwar.EditorTools.PokiwarVideoFrames.Run` (see "Re-checking a frame" below) and read one by one.

Clips (in `C:\Users\TS03128\Downloads`): **A** `Pokiwar trên Zing Me.mp4` (13:03, 1280x720),
**B** `[POKIWAR] Khi thánh chơi POKIWAR và cái kết.mp4` (11:04, 1276x720), **C** the "Pokiwar 2" troll clip, **D** `danh boss pokiwar 2015 phan 2.mp4`,
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
| B 01:05 | Room "SÂN 8709": five CHỌN CARD slots holding one reusable card and three "+350" consumables, pet carousel, SẴN SÀNG | `PrepScreen`: five slots hold reusable and single-use cards together (one reusable card at most; user, 2026-10-07) | CONFIRMED_BY_VIDEO |
| B 01:15 | VS splash: player team left, boss right, big "VS", "BlueWings (độ khó sàn cấp 108)" | `BattleController.PlayIntro`: both fighters slide in, VS slams, FIGHT! pops | CONFIRMED_BY_VIDEO (FIGHT! is ours) |
| B 01:05, D 03:00 (re-read 2026-10-07) | The room holds THREE copies of the same `+350` card beside the skill card (cost 380, damage 3162, pet mana 2345); clip D holds two `+50` fire cards against a rage bar of 200 | single-use cards are small top-ups next to the skill: about 15% of the mana bar, 25% of the rage bar, a tenth of the skill's damage. Ours follow that scale, and `card.rage_ember` (+25 on a rage bar of 100) is the `+50` fire card; taking several copies of one card is not built | CONFIRMED_BY_VIDEO |
| B 02:22 (re-read 2026-10-07) | Arrow mini game: a pill bar with five round arrow tokens, a small timing slider above it with `good`, the damage `+7032` in a flame at the right; no dark panel, the board is hidden | `QteView` in a painted panel with a ribbon, five framed arrow slots, a painted track and marker, and touch buttons the original (keyboard) does not need | CONFIRMED_BY_VIDEO |
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

## First screen, EXP and rank points (read 2026-10-06)

| Clip @ time | Seen | Game | Status |
|---|---|---|---|
| A 01:15, user screenshot | The game opens on a town map: islands with buildings (xếp hạng, vòng quay may mắn, shop card, shop avatar, shop quà tặng, đấu trường chuyên nghiệp / tự do, chinh phục pokémon, đại chiến thách đấu, Tiến Hóa, a ship), top strip (Đổi quà, x2 EXP timer, events), bottom nav and energy | `HomeScreen` is the launch screen: nine buildings; BOSS HUNT opens the lobby, EVOLVE the pet screen, AVATAR SHOP the wardrobe, the other six say "Coming soon"; same bottom nav. Flat discs until Codex Batch 13 | layout CONFIRMED_BY_VIDEO + CONFIRMED_BY_USER |
| A 04:14 | Room after beating Flygon (hunt 2) with the trainer at level 1: "phuongkid1991 +20 EXP" | win EXP = 19 + 3 x hunt level - 5 x trainer level | CONFIRMED_BY_VIDEO |
| A 05:22 | Room after LOSING to Abra: "+1 EXP"; trainer still level 1 (21 EXP so far) | loss EXP = 1; level 1 needs 40 | CONFIRMED_BY_VIDEO |
| A 11:06 | After the next win the trainer badge is 2 | level N needs 40 x N | bound only, INFERRED |
| B 10:55 | Room after BlueWings (hunt 108): ailan1999 (trainer 35) +167 EXP, the two trainers at 36 +162 EXP each - five less per trainer level | same formula gives 168 and 163 | CONFIRMED_BY_VIDEO, formula INFERRED |
| A 04:14, 05:22, 11:06; B 01:15 -> 10:55; M 00:02 | Counter under the pet in the room: 0/1 before the first win, 1/1 after it, 2/1 after the second; a different pet that lost shows 0/1; 1/3 -> 2/3 and 4/3 -> 5/3 over one boss win; 33/3 | `OwnedPet.Wins`, +1 per win of the pet that fought; the room shows the first number as a sword chip beside the pet name (the second number is not shown) | +1 per win CONFIRMED_BY_VIDEO, second number UNKNOWN |
| A 04:10, 07:48, 11:02; B 10:51 | Rank points ("chiến tích") in the reward popup: Flygon 20, Abra 40, BlueWings 200; the top bar's red badge adds them up (0 -> 20 -> 80; 313800 -> 314000) | `EncounterDef.RankPoints` 20 / 40 / 200, summed in `SaveData.RankPoints`, shown in the reward popup and as the trophy chip on the town map; the ranking board itself is not built | CONFIRMED_BY_VIDEO |
| A 13:00, M 02:56, B 10:51 | Reward popups list stones by element and tier with counts ("2 đá CƯỜNG HÓA cấp 2", "3 đá MỘC...", a row of stone icons with numbers) | text lines | CONFIRMED_BY_VIDEO |

## Trainer level and clothes (read 2026-10-06)

| Clip @ time | Seen | Game | Status |
|---|---|---|---|
| A 02:12, 05:40 vs A 10:00 | Same LV7 pet: 2405/3207 HP and 770 mana with trainer badge 1; 2409/3212 and 775 with trainer badge 2. The boss's mana follows (770 -> 775) | +5 HP and +5 mana per trainer level in `ProgressionService.BattleStats` | CONFIRMED_BY_VIDEO (one step) |
| A 04:41 vs A 11:21 | Two different LV1 pets: 510 HP / 300 mana at trainer 1, 505 / 305 at trainer 2 - the mana fits the same +5 | - | consistent |
| C 02:05 vs M 00:20, B 01:22 | Pet mana at Cấp 14 is 1680 for every pet; in battle LV14 pets show 2410 (trainer 39) and 2326 / 2346 / 2350 (trainers 35-36). Trainer level explains 170-190; 480-540 is left over | bought avatar pieces add HP, mana or ATK | gear adding stats INFERRED, amounts UNKNOWN |
| A 01:35 | CƯỜNG HÓA: enhance an avatar piece or a badge (HUY HIỆU) with three materials, 50% success, Bùa Thăng Hoa 20000 | not built | CONFIRMED_BY_VIDEO |
| A 09:08 | "Đổi avatar" tabs: AVATAR BỘ, AVATAR RỜI, NỀN, THÚ CƯNG, HIỆU ỨNG, CÁNH, VŨ KHÍ | wardrobe has hair, top, bottom, hat | CONFIRMED_BY_VIDEO |

## Clip D, team boss fight and the card row (read 2026-10-06)

Clip **D** is `danh boss pokiwar 2015 phan 2.mp4` (about 11:00): three players against Kaiorga.

| Clip @ time | Seen | Game | Status |
|---|---|---|---|
| D 00:20 | Kaiorga 1254365/2500000 HP, 4600 mana, 200 rage; player 15506/39559 HP, 2727 mana; many gems carry x2, x3, x4 | bosses have their own HP; x2 / x3 gems only | CONFIRMED_BY_VIDEO |
| D 00:20, 03:00 | Card row under the board holds four cards: one ornate gold-framed card with a round badge "8" and no cost, two identical white-and-red "+50" cards with a fire icon, one skill card with the pet's portrait and cost 200 | five slots: reusable cards with cost / level / damage, then single-use cards | CONFIRMED_BY_VIDEO |
| D 09:20 | Tooltip of the gold-framed card: "hồi lại 50% máu trên tổng số máu của người chơi và đồng đội nếu có bất kì ai máu hiện tại thấp hơn 50%. Ràng buộc: chỉ xài 1 lần trong trận" | not built (team heal, once per battle) | CONFIRMED_BY_VIDEO |
| D 11:00, 07:00, 10:20 | Tooltip of the skill card: "Hao tốn 200 mana, 200 nộ. Kết Ấn - Mega Icarus bắn một mũi tên Lửa Thiêng phong ấn một vùng trên bàn chơi. Khi bộc hỏa ... sát thương gây ra sẽ được x2 và tổng số viên ăn được có thể lên đến 50-80 viên." A glowing square is sealed on the board, later it bursts | not built (seal a board area, then burst it for double damage and a large gem haul) | CONFIRMED_BY_VIDEO |
| D 00:40, 04:40, 11:00 | A card the player cannot use right now is drawn in grey | the button is disabled, art not greyed | CONFIRMED_BY_VIDEO |
| D bottom-left | Under the pet: a lightning icon with "1562 (+205)", "1457 (+220)", "1562 (+205)" for the three players - a stat and its bonus | pet ATK plus the clothes bonus is not shown in battle | reading CONFIRMED_BY_VIDEO, meaning INFERRED |

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

## Gems on the board (counted 2026-10-08)

Five whole 8x8 boards counted gem by gem (320 gems): A 02:12 (first frame of a battle), A 05:41, B 01:22,
D 00:20, D 03:00.

| Gem | A 02:12 | A 05:41 | B 01:22 | D 00:20 | D 03:00 | Share |
|---|---|---|---|---|---|---|
| Sword | 0 | 2 | 4 | 8 | 7 | 6.6% |
| Lightning | 14 | 13 | 13 | 11 | 14 | 20.3% |
| Fire | 11 | 9 | 12 | 18 | 12 | 19.4% |
| Heart | 10 | 15 | 12 | 8 | 11 | 17.5% |
| Shield | 13 | 16 | 16 | 11 | 14 | 21.9% |
| Yin-yang | 16 | 9 | 7 | 8 | 6 | 14.4% |

- Sword is scarce on the OPENING board and common in the REFILL. Opening boards: A 02:12 has none, B 01:22
  (boss at its starting 81000/108000, nobody has mana yet) has 4. Refill: in the turn A 02:13 -> 02:16.5 the
  player cleared 21 gems and 6 swords fell in (3 were matched in the cascade, 3 are left on the board) against
  3 hearts, 4 fire, 1 lightning, 5 shield, 2 yin-yang - sword 29% of the refill. Boards read mid-battle sit low
  (3-12%) because both sides take swords first. CONFIRMED_BY_VIDEO; one refill sample, so the rate is rough.
- The reading of 2026-10-08 morning ("sword is the rare gem, weight 0.5 everywhere") was WRONG: it treated
  board snapshots as the spawn rate. The user had said sword comes out more, and the refill count agrees.
- Heart is at the even share; shield and lightning sit a little above it. CONFIRMED_BY_VIDEO.
- The game follows this with two tables on `BoardRuleProfile`: `InitialSpawnWeights` = sword 0.2, the rest 1;
  `SpawnWeights` (refill) = sword 1.5, lightning 1, fire 1, heart 1, shield 1.1, yin-yang 0.9.
- Clip D (hunt level far above 100) carries x2 / x3 / x4 on about half its gems; clips A and B show none. The
  game keeps 5% x2 and 1.5% x3; a multiplier rate that grows with hunt level is NOT built.
- The HP bars in all five frames are a single red bar for both sides. The three-layer boss bar (green, yellow,
  red) is CONFIRMED_BY_USER (2026-10-08), not read from these clips.

## One whole turn, frame by frame (read 2026-10-08, clip A 02:12.5 -> 02:30.2 at 4 frames per second)

| Time | What is on screen | Ours |
|---|---|---|
| 02:13.0 -> 02:16.5 | The swap and three cascade steps play on the board; refill drops in from the top | same |
| 02:16.7 | The board is gone within one frame step; the tally row stands where its top row was: lightning 3, fire 3, yin-yang 6, shield 6, sword 3 - red counts under the icons | board fades in 0.15 s, same row, same order (`Gems.ResolveOrder`) |
| 02:17.2 -> 02:18.5 | Lightning: a blue ring spins at the pet's feet, the pet holds a charging pose, `+144` in blue floats over it at 02:17.7, the mana text in the HUD changes by 02:18.0; the lightning icon fades only at 02:18.5 | bar and number change at the start of the beat (user rule), ring and icons at the pet, icon fades when the next gem starts |
| 02:18.7 -> 02:20.0 | Fire: red flame swirl on the ground under the pet, `+83` in red; icon fades at 02:20.0 | `VfxLayer.Swirl` |
| 02:20.0 -> 02:21.5 | Yin-yang: the whole scene darkens, a white orb and a cone of light on the ENEMY, `+0` on the player and `-0` on the enemy (a steal that found nothing) | `Steal` event with motes, no darkening |
| 02:21.7 -> 02:23.2 | Shield: a column of gold light and sparkles around the pet, `+318` in gold | bubble and ring |
| 02:23.5 -> 02:24.7 | Sword: the pet stays where it is and THROWS its crescent; it crosses in about 0.4 s and bursts on the enemy; the damage `788` sits inside a red spiky badge on the target for about 0.7 s | ranged creatures throw (`<key>.lob` / `.shot`), hit burst for 0.6 s, number floats above |
| 02:25.0 -> 02:25.5 | Empty stage for half a second | summary hides, board fades in |
| 02:25.7 | Board back, timer 10, the turn arrow moves to the enemy's name | same |
| 02:28.2 | The enemy swaps after about 2.5 s of thinking | `BattleRules.AiThinkSeconds` 2 |
| 02:29.2 -> 02:30.2 | Enemy tally: sword 3. Flygon DASHES about half way across and strikes; the badge on the player reads `0` (the 318 shield took it) | melee creatures dash (`CombatantHud.Lunge`) |

What that settles:

- One gem's effect holds the stage for 1.2 to 1.5 s and its icon stays lit until that effect is over. The game
  uses `GainBeatSeconds` 0.8 - shorter on purpose, the user asked for the smoothest read.
- Nothing flies from the board to the bars: the board is hidden, every effect happens AT the pet. The orbs the
  game used to send from the cleared cells are removed.
- The original has both attack kinds in one fight: the player's pet throws, the wild creature dashes.
  CONFIRMED_BY_VIDEO, and it is what the user asked for the same day.
- NOT built: the charging pose during a gain, the scene darkening on yin-yang, the damage number inside the
  badge, multipliers that grow with hunt level.
## Re-checking a frame

Close the Editor, then:

```
Unity.exe -batchmode -projectPath <project> -executeMethod Pokiwar.EditorTools.PokiwarVideoFrames.Run
  -video <clip.mp4> -times "132,137.5" -out <dir> [-width 960] -logFile frames.log
```

Copy a clip to an ASCII path first; Vietnamese letters and `[` `]` in the file name break the video URL.
