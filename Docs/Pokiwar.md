# Pokiwar Offline - vertical slice

Turn-based pet battle on one shared 8x8 match-3 board. One player pet vs one AI monster/boss, local save.
Unity 6 (6000.0.79f1 LTS), uGUI, mouse + touch, 16:9 landscape (1920x1080 reference, `Expand` scaler).

## Play it

1. Open the project in Unity 6000.0.79f1.
2. Open `Assets/Pokiwar/Scenes/Pokiwar.unity` (first scene in Build Settings) and press Play.
3. Hub -> ADVENTURE -> Dune Beach -> pick pet + up to 5 cards -> FIGHT.
   - Swap: drag a gem onto a neighbour, or tap one gem then a neighbour.
   - Cards / pet skill: buttons under the board. A skill opens the QTE: arrows (keys or on-screen),
     then STRIKE (or Space) while the marker is in the gold zone.
   - AUTO lets the AI play your side, LOG shows every CombatEvent with before/after values, GIVE UP forfeits.
4. Win -> reward -> next node unlocks. Lose -> RETRY. Boss: Azure Peak (transforms once).
5. Hub -> PETS & STONES: merge 3 stones, enhance a pet, socket stones. RESET SAVE starts over.
6. Hub top right: MUSIC / SOUND / SHAKE toggles, saved with the game.

Save file: `Application.persistentDataPath/pokiwar_save.json` (versioned, migrated on load).

## Layout

```
Assets/Pokiwar/
  Scenes/Pokiwar.unity          entry scene, every screen pre-placed (Hub, Map, Prep, Battle, Result, Upgrade, VfxLayer, Toast, PokiwarApp/Audio)
  Audio/                        generated SFX (sfx_*.wav) and bar-exact music loops (music_*.wav)
  Prefabs/GemView.prefab        pooled gem view (the only runtime-instantiated prefab)
  Data/                         ScriptableObject content (tune here)
    ContentCatalog.asset        the one asset GameApp loads; lists all others
    SpriteLibrary.asset         sprite per key (gems, pets, cards, skills, UI)
    Pets/ Monsters/ Cards/ Skills/ GemProfiles/ AI/ Encounters/ Rewards/
    Rules/  board.default, battle.rules, upgrade.config, progression.config, map.main
  Art/Placeholder/              generated placeholder PNGs (replace with final art)
  Scripts/Domain/               pure C# (asmdef noEngineReferences) - all game rules
    Core/      enums, SeededRng, Resource
    Board/     BoardState, BoardEngine (match, gravity, refill, cascade, moves, reshuffle, AI look-ahead)
    Battle/    Combatant, BattleEngine, CombatEvent, TurnClock, QteMath
    AI/        AIPlanner
    Data/      definitions, ContentDatabase, DefaultContent (seed data)
    Progression/ SaveData + migration, ProgressionService (energy, rewards, unlocks), UpgradeService
  Scripts/Runtime/              MonoBehaviours: GameApp, screens, BattleController, views, SO wrappers
  Scripts/Editor/               scene/art/content builder, balance report, play-mode smoke test
  Tests/EditMode/               NUnit tests on the domain
```

Rule of the code: the domain computes everything synchronously and returns `ActionResult` + `CombatEvent`s
(with snapshots). `BattleController` only replays them. Player and AI call the same `BattleEngine` methods.

## Editor menu `Pokiwar/`

| Item | Effect |
|---|---|
| Rebuild Scene + Art (overwrites scene layout) | writes missing placeholder PNGs and WAVs, refreshes SpriteLibrary and GemView prefab, rebuilds the whole scene |
| Regenerate Audio (overwrites) | re-renders every WAV from the recipes in `PokiwarAudioBuilder` |
| Seed Missing Content Assets | creates content SOs that do not exist yet; never overwrites tuning |
| Reset Content Assets To Defaults | overwrites every content SO with `DefaultContent` |
| Balance Report (AI vs AI) | 40 seeded AI-vs-AI fights per node and pet level, win rate + turns in Console |
| Run Smoke Test (Play mode) | clicks through Hub -> Map -> Prep -> Battle (real drag, timeout, card) -> Result -> boss -> Upgrade |

Headless:

```
Unity.exe -batchmode -nographics -projectPath <p> -runTests -testPlatform EditMode -assemblyNames Pokiwar.Tests.EditMode -testResults results.xml
Unity.exe -batchmode -projectPath <p> -executeMethod Pokiwar.EditorTools.PokiwarSmoke.Run -smokeOut <dir> -logFile smoke.log
```

Add `-smokePortrait` to run the smoke at 1080x1920. Both orientations check that every button is fully on
screen and that a board cell is at least 9.5% (portrait) / 6.5% (landscape) of the short screen side.

## Portrait and landscape

The game rotates freely (portrait + both landscape sides). `ResponsiveCanvas` on the Canvas picks the
orientation from the screen shape, switches the CanvasScaler reference (1920x1080 / 1080x1920) and applies
every `OrientationLayout` in the scene. Each laid-out element stores one rect per orientation.

To adjust a layout in the Editor:

1. On the Canvas, `ResponsiveCanvas` context menu -> `Preview Portrait` (or set `Force`).
2. Move/resize the element in the Scene view.
3. On that element, `OrientationLayout` context menu -> `Capture current as Portrait`.
4. Same for landscape with `Preview Landscape` / `Capture current as Landscape`. Set `Force` back to `Auto`.

`Pokiwar/Build All` regenerates the scene from `PokiwarSceneBuilder` and overwrites hand-made captures;
lasting layout changes go into its `Portrait(...)` calls.

## Where to tune (Inspector)

| Asset | Fields |
|---|---|
| `Rules/board.default` | size, spawn weights, x2/x3 odds, invalid-swap revert, reshuffle, extra turn on 4/5, turn seconds |
| `Rules/battle.rules` | element advantage/disadvantage/neutral, bonus per point, defense mode (Barrier / FlatDef / PercentReduction) |
| `GemProfiles/*` | per-actor Heart/Lightning % of max, Fire rage, Shield %, shield cap, YinYang amounts, Sword ATK factor, variation, rounding |
| `Pets/*`, `Monsters/*` | base stats + per-level growth, element + bonus, rage profile (threshold/cost/strong x/HP-damage rage/turn-start rage), skills, cards, boss phases |
| `Cards/*` | mana/rage cost, uses, can use before/after match, end turn after use, effect chain |
| `Skills/*` | costs, ATK multiplier, QTE profile, post-effect chain |
| `Encounters/*` | creature, level, start HP %, AI policy, reward table, energy, first turn |
| `Rewards/*` | EXP, pet EXP, gold, drops (stone/card/charm/pet, chance, first clear only) |
| `AI/*` | weights per gem, randomness, mistakes, opponent-opportunity penalty, card/skill use, QTE skill |
| `Rules/upgrade.config` | merge chance/cost per tier, lucky bonus, enhance chance/cost per level, fail accumulation, downgrade, sockets |
| `Rules/progression.config` | starter pets/cards/items, energy, EXP curves |
| `Rules/map.main` | regions and nodes (position, encounter, unlock requirement, wins required) |

## Sound and effects

- `PokiwarApp/Audio` (`AudioDirector`): one list of key -> clip -> volume. Buses in dB (SFX -2, music -9), 12 pooled voices,
  at most 3 of one key at once, 40 ms per-key cooldown, +-5% pitch jitter. Cascade steps raise the match pitch by
  2 semitones each. The boss transform ducks the music.
- Keys: ui.click, swap, invalid, match, shuffle, heal, mana, rage, shield, block, steal, swing, hit, hit.strong, card,
  skill, buff, summon, transform, death, fight, turn, tick, timeout, victory, defeat, qte.ok, qte.bad, qte.perfect,
  qte.good, qte.miss, upgrade.ok, upgrade.fail, music.menu, music.battle, music.boss.
- All sounds are synthesized (sfxr port, `Scripts/Editor/SfxSynth.cs`) - placeholders that are consistent and loop-clean;
  replace a WAV under the same name to swap in final audio. Mix the per-key volume on the Audio object.
- `Canvas/VfxLayer` (`VfxLayer`): pooled UI-sprite particles (bursts, rings, orbs that fly from cleared gems to the
  bar they fill, screen flash), because an overlay canvas cannot draw a ParticleSystem.
- `BattleScreen` (`ScreenShake`): trauma shake on hits, death and the boss transform; strong hits add a 50-110 ms hit-stop.

## Formulas in use

Labels: **VIDEO** seen in the clips, **USER_CONFIRMED** stated in the brief, **INFERRED** fitted to a few samples,
**PROVISIONAL** playtest baseline only. Everything PROVISIONAL is a data field, not engine code.

| Rule | Current implementation | Label |
|---|---|---|
| Board | 8x8, one board shared by both sides for the whole battle | USER_CONFIRMED |
| Turn timer | 10 s; timeout = skip turn only (no auto move, no penalty); accepted action finishes before handover | VIDEO |
| Gem meaning | Sword attack, Lightning mana, Fire rage, Heart HP, Shield barrier, YinYang steals mana or rage (never HP) | VIDEO |
| Resolve order | Heart -> Lightning -> Fire -> YinYang -> Shield -> Sword, whole turn's tally first | USER_CONFIRMED |
| Multiplier | cell carries x1/x2/x3 while falling; effective count = sum of multipliers | VIDEO |
| x2/x3 spawn odds | 5% / 1.5% per spawned cell | PROVISIONAL |
| Invalid swap | swaps back, turn and timer continue | PROVISIONAL |
| Dead board | reshuffle until no match and at least one move | PROVISIONAL |
| Match 4/5 | no special gem, no extra turn | PROVISIONAL |
| Heart / Lightning | `MaxHP x pct x eff` / `MaxMana x pct x eff`, per profile; `gem.legacy_low` = 1.45% / 6.25% | INFERRED (one low-level pet), others PROVISIONAL |
| Fire | `rage per gem x eff` (6 pets, 5 monsters, 8 boss) | PROVISIONAL |
| Rage strike | Sword with rage >= threshold (100) spends cost (100): 200 -> 100, not a reset | VIDEO |
| Strong multiplier | x1.70 (1346 / 788) | INFERRED |
| Rage from HP damage | `floor(100 x HPDamage / MaxHP)` on the target, from HP damage after shield | INFERRED (4 Manaphy samples) |
| Turn-start rage | +3 to the actor whose turn starts | PROVISIONAL |
| YinYang | 50% mana / 50% rage (seeded); mana `target MaxMana x 2-2.5% x eff`, rage `5-8 x eff`; taken = min(computed, target current), received capped by receiver max | transfer rule USER_CONFIRMED, amounts PROVISIONAL |
| Shield | barrier absorbs before HP; cap 35% MaxHP; `MaxHP x 1.2-1.5% x eff` | PROVISIONAL |
| Sword damage | `floor(ATK x SwordAtkPerGem x eff)` -> x strong -> x element -> defense policy | structure USER_CONFIRMED, factor 0.65-0.75 PROVISIONAL |
| Element | Metal>Wood>Earth>Water>Fire>Metal; 1.25 / 0.80 / 1.00; +N bonus = +1% per point | PROVISIONAL |
| QTE | `base + correct x floor(20% base)`, then x1.24 good / x1.25 perfect, rounded (2836 -> 5671 -> 7032 / 7089) | VIDEO (calibration sample) |
| Skill drain | pay 200 MP + 200 RG, then drain `floor(70% x target current mana)` (893 - 200 + 383 = 1076) | VIDEO |
| Card +100 mana | does not end the turn; matching afterwards is allowed | VIDEO |
| Boss phase | one-shot; sets HP to 50% MaxHP (VIDEO); trigger HP <= 32% and on a lethal hit (PROVISIONAL); ATK x1.15; unlocks Tidal Siphon; boss acts next (ContinueTurn) | mixed, per field |
| Reward commit | once per battle id, after the battle's event replay; second commit is a no-op | USER_CONFIRMED |
| Stone merge | 3 same tier+element -> 1 next tier; failure loses all 3; 90/75/60/45/32/20/12%; lucky +15%; gold per tier | rule USER_CONFIRMED, numbers PROVISIONAL |
| Pet enhance | chance per level, +/-5% per stone tier vs required, +4% per failure, -1 level on failure without protection, +4% HP/ATK/DEF per level | PROVISIONAL |
| Sockets | 3 sockets, +6 ATK / +60 HP per tier, +1 element bonus per tier of the pet's element | PROVISIONAL |
| Energy | 30 max, +1 per 5 min, node cost 1-3 | PROVISIONAL |

Balance (AI vs AI, 40 seeds, pet Lv 3/5/8/12): nodes 1-2 always won in ~15 turns; boss 12% / 67% / 87% / 100%.

## Art (FG38 set)

`Assets/Pokiwar/Art/FG38` holds the canyon-fantasy set made from `D:\Project\Pokiwar_Art_FG38` (Codex, 2026-10-05). `Fg38Art.Apply`
runs inside Rebuild Scene: it copies any file still missing from that folder, draws the six square gems itself (FG38 palette,
ink outline, bevel, dark emblem), sets importers and 9-slice borders, and overrides the sprite keys below. Drop a new PNG
under the same name in `Art/FG38` to replace one.

| Key | File |
|---|---|
| `emberkit` (+ `.attack` `.hit` `.defeat`) | `char_emberkit*` (FG38 player pet) |
| `dunewing` (+ poses) | `char_dunewing*` (FG38 beetle) |
| `azurewing` / `azurewing_ascended` (+ poses) | `char_azurewing*` (crystal drake form 1 / 2) |
| `gem.*` | `gem_*` (generated square gems) |
| `bg.battle` `bg.hub` `bg.map` | backgrounds; Prep reuses the hub one |
| `board.frame` `board.tile` `board.selected` `hud.bar` `card.slot` `ui.button.green` `ui.arrow` | frames, tiles, primary button, arrows |
| `card.fire_bolt` `card.herbal_salve` `card.mana_potion` `card.iron_skin` `card.war_cry` | card illustrations |
| `fx.slash` `fx.heal` `fx.mana` `fx.rage` `fx.shield` `fx.transform` | effect sprites shown by `VfxLayer.Decal` |

A key `<creature>.<pose>` is optional: `CombatantHud` shows it during the lunge (attack), the hit and death, and falls back to
the idle sprite. Creature art is drawn facing the opponent, so nothing is mirrored.
Still placeholder (no FG38 art yet): leafling, tidepup, psyling, cards summon_sprite / mana_leech / meteor, every skill icon.

## Placeholder assets to replace

Files in `Assets/Pokiwar/Art/Placeholder` (keep the name, or point `SpriteLibrary` at the new sprite):

- Gems: `gem_sword`, `gem_lightning`, `gem_fire`, `gem_heart`, `gem_shield`, `gem_yinyang`
- Pets / monsters: `pet_emberkit`, `pet_leafling`, `pet_tidepup`, `pet_dunewing`, `pet_psyling`, `pet_azurewing`, `pet_azurewing_ascended`
- Card icons: `card_mana_potion`, `card_herbal_salve`, `card_fire_bolt`, `card_summon_sprite`, `card_iron_skin`, `card_mana_leech`, `card_war_cry`, `card_meteor`
- Skill icons: `skill_blaze_burst`, `skill_thorn_bind`, `skill_tide_lance`, `skill_mind_spark`, `skill_tidal_siphon`
- UI: `ui_round` (9-slice 24px), `ui_frame`, `ui_circle`, `ui_ring`, `ui_arrow`, `ui_gradient`, `map`, `stone`

"Rebuild Scene + Art" only writes PNGs that are missing, so final art dropped in under the same name is kept. It does rebuild the scene layout.

## Not in this slice

Online, PvP, chat, server select, rooms, events, shop prices, Pokiwar 2 star/event pricing (kept out on purpose).
Final (recorded) audio, per-pet animations and particle art: the current sound and VFX are generated placeholders.
