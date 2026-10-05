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
| Pet drop chance | first clear only (Tidepup from the boss) | `Rewards/*` | several boss clears |
