# East Sea - the first region

Owner's brief (2026-10-07): the first region is the East Sea. Its creatures come from animals that really live
there, each fused with something else so the picture and the name are new. The first one or two creatures may
have no second phase; every later one has.

Status (2026-10-07): the region is IN THE GAME. All art of Batches 14-16 is painted and wired; the map runs the
six creatures below as `node.1` .. `node.6`. Dunewing, Psyling and Azurewing stay in the content and keep their
art for a later, inland region, but no map node points at them.
## Roster, in hunt order

| # | Name | Real animal of the East Sea | Fused with | Element | Second phase | Role |
|---|---|---|---|---|---|---|
| 1 | Samgong | horseshoe crab (con sam), common on Vietnamese tidal flats | Dong Son bronze drum as its shell ("sam" + "gong") | Earth | none | first capture: slow, hard shell, hits when it drums |
| 2 | Bebeboom | peacock mantis shrimp (be be / tom tit), the fastest punch in the sea | firecracker clubs | Fire | none | glass cannon: low HP, bursts of rage damage |
| 3 | Ngoclam | giant clam (trai tai tuong), reef flats of the Spratly and Paracel groups | a pearl ("ngoc") as its heart, mother-of-pearl armour | Metal | Ngoclam Radiant: opens, the pearl shows | wall: big shields, punishes long fights |
| 4 | Doimora | hawksbill turtle (doi moi) | an islet with a sea-almond tree (bang vuong) on its back | Wood | Doimora Beacon: tree in bloom, lighthouse lit | healer: regenerates, wins by outlasting |
| 5 | Voirong | banded sea krait (ran bien) | a waterspout - "voi rong" is Vietnamese for waterspout, literally "dragon's trunk" | Water | Voirong Tempest: storm and dragon crest | drainer: steals mana and rage |
| 6 | Ongnamhai (boss) | Bryde's whale, the "Ca Ong" that fishermen along the coast honour as Nam Hai, the lord who brings boats home | ceremonial sash, bronze bells, lantern buoys | Water | Ongnamhai the Tide Lord | region boss: own large HP, 1.6x the pet's mana |

Why these six: one per element plus the boss, so every pet stone drops somewhere in the region and the starter
(Emberkit, Fire) meets both a favourable opponent (Ngoclam, Metal) and unfavourable ones (Voirong and the boss,
Water) before the region ends.

## More forms later

Owner (2026-10-07): creatures will get more than two phases later. The battle engine already takes any number
(see "Forms" in `Docs/Pokiwar.md`). Art for a further form follows the same file pattern with the next number -
`enemy_<name>_form03_{idle,attack,hit,defeat}_01.png` - and its sprite key is `<name>_form3`, `_form4`, ...
(the second form keeps the existing `_evolved` suffix). What is not decided: at which pet level a COLLECTED
creature takes its third form.

## Content numbers (tuned 2026-10-07 with `PokiwarBalance.BatchReseedAndReport`)

| Node | Encounter | Hunt level | Wins to open the next | Rank points | First-clear reward | Win rate at pet Lv 1 / 3 / 5 / 8 / 12 |
|---|---|---|---|---|---|---|
| Drum Shoal | Samgong | 2 | 1 | 20 | the Samgong itself | 97 / 100 / 100 / 100 / 100 |
| Firecracker Reef | Bebeboom | 4 | 2 | 30 | War Cry card | 22 / 55 / 75 / 82 / 95 |
| Pearl Garden | Ngoclam | 6 | 2 | 40 | Mind Spark skill card | 0 / 12 / 45 / 87 / 97 |
| Almond Cay | Doimora | 8 | 2 | 60 | Rage Ember card | 0 / 5 / 12 / 47 / 82 |
| Spout Strait | Voirong | 10 | 3 | 100 | Mana Leech card | 0 / 0 / 2 / 17 / 50 |
| Whale Lord's Deep | Ongnamhai | 12 | 3 | 200 | Tide Lance skill card, Meteor card | 0 / 0 / 0 / 10 / 40 |

AI against AI, 40 seeds a cell, the player carrying one skill card and four single-use cards. Owner's rule
(2026-10-07): the first creature is easy to win, every later one is hard to get. Samgong stays at hunt level 2
because the trainer EXP test follows clip A (two wins reach trainer level 2).
## Sources for the real animals and the whale lore

- Hawksbill and green turtles and the dugong recorded on East Sea reefs: https://www.sixthtone.com/news/1017516
- Whale worship (Ca Ong, "Nam Hai") among Vietnamese fishing villages: https://vnexpress.net/ruoc-ca-ong-2963366.html
  and https://baovanhoa.vn/van-hoa/le-cung-than-nam-hai-203661.html
- Horseshoe crab, mantis shrimp, giant clam, banded sea krait, sea-almond tree, Dong Son drum: general knowledge,
  not looked up for this document.
