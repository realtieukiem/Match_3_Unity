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
| Boss HP vs player | every opponent's max HP is `max(its own table HP, player pet max HP x HpVsPlayer)`, ratio 1.2 / 1.6 / 2.2 for the three nodes (`EncounterDef.HpVsPlayer`). The RULE is the user's (2026-10-06: a boss always has more HP than the pet, however far the pet is upgraded). The clips do not show it: Abra keeps 2200 HP against pets of 3207, 510 and 505 HP (see `Docs/Evidence.md`, "Boss stats against the player"); only 2.2 comes from a clip (M: Manaphy 28400 vs 12815). AI-vs-AI: node 1 100%, node 2 92-100%, node 3 pet Lv 8 42%, Lv 12 62% | `Encounters/enc.*` | whether the original scales HP at all; ratios 1.2 and 1.6 are ours |
| Boss mana vs player | not built. In the clips the boss's max mana is the player pet's max mana times 1.0 (hunt level 2-3), 1.6 (level 84), 2.0 (level 108) | - | the multiplier between those three points |
| Pet capture | first win gives the creature just fought, at the encounter's level, every encounter (`CaptureOnFirstWin`) | `Encounters/*` | whether every monster or only some give themselves; the level it arrives at |
