# Pokiwar - rules not yet verified

Each of these runs on a tunable default so the game plays; none is claimed as the original rule. The full
formula table with labels is in `Pokiwar.md` ("Formulas in use"). What would settle each one:

| Rule | Default now | Where to change it | Measurement that would settle it |
|---|---|---|---|
| Match 4/5 | no special gem, no extra turn | `Rules/board.default` | one match-4 and one match-5 with HUD before/after |
| Invalid swap | swaps back, turn continues | `Rules/board.default` | an invalid swap on video |
| Dead board | reshuffle | `Rules/board.default` | a board with no moves |
| x2/x3 spawn odds | 5% / 1.5% | `Rules/board.default` | many refills counted |
| Heart / Lightning amounts | % of max per gem, per pet profile | `GemProfiles/*` | same pet, several turns with known counts |
| Shield model | barrier before HP, 35% cap | `Rules/battle.rules`, `GemProfiles/*` | a hit on a shielded pet with HP before/after and a tooltip |
| Element bonus `+N` | +1% per point, relation 1.25 / 0.80 | `Rules/battle.rules` | same attack into two elements |
| Strong multiplier | x1.70 | `Pets/*`, `Monsters/*` rage profile | more rage strikes with HP before/after |
| Boss phase trigger | HP <= 32% or a lethal hit | `Monsters/*` phases | several transforms at different HP |
| Stone merge / enhance chances | 90..12% per tier | `Rules/upgrade.config` | upgrade success and failure on video |
| Start HP | both sides start at 75% of max HP (`BattleRules.StartHpPct`; clip A 02:12: 2405/3207 and 1650/2200, clip B: 81000/108000) | `Rules/battle.rules` | whether 75% holds for every battle type |
| Boss HP | a boss's max HP is its own (creature stats at the encounter's hunt level) and does not follow the player's pet, as in clip A (Abra 2200 against pets of 3207, 510 and 505 HP). Against a pet of the boss's own level the three opponents have 0.75x / 1.6x / 2.2x the pet's HP; 0.75 is clip A's first boss (2200 vs 3207 = 0.69), 2.2 is clip M (Manaphy 28400 vs 12815). The user left the numbers to us (2026-10-06: follow the video, make hunting hard enough to come back). AI-vs-AI win rate by pet level 3 / 5 / 8 / 12: node 1 100 / 100 / 100 / 100, node 2 55 / 70 / 92 / 100, node 3 0 / 10 / 35 / 75 | `Monsters/*`, `Encounters/enc.*` | the original's HP per hunt level (2 and 3 -> 2200, 84 -> 28400, 108 -> 108000); the middle ratio 1.6 is ours |
| Boss mana vs player | a boss's max mana is the player pet's max mana times `EncounterDef.ManaVsPlayer`: 1.0, 1.0, 1.6 for the three nodes. Clips: x1.0 at hunt level 2-3 (three pets, three matching values), x1.6 at level 84 (2410 -> 3856, exact), about x2.0 at level 108 | `Encounters/enc.*` | the multiplier between those three points |
| Wins to open the next boss | a node opens after `MapNodeDef.WinsRequired` wins on the node before it: 1, 2, 3. Lobby tags in the clips show `THANG x/y` with y = 1 on early bosses and 3 on later ones | `Rules/map.main` | the real count per boss |
| Pet capture | first win gives the creature just fought, at the encounter's level, every encounter (`CaptureOnFirstWin`) | `Encounters/*` | whether every monster or only some give themselves; the level it arrives at |
