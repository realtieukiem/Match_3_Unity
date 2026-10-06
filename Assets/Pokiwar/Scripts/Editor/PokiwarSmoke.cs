using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Pokiwar.App;
using Pokiwar.Domain;
using Pokiwar.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokiwar.EditorTools
{
    /// <summary>Play-mode smoke run driven from the Editor assembly, so nothing of it ships. Entry: PokiwarSmoke.Run.</summary>
    public static class PokiwarSmoke
    {
        private const string RunningKey = "PokiwarSmoke.Running";
        private const string DeadlineKey = "PokiwarSmoke.Deadline";
        private const string StageKey = "PokiwarSmoke.Stage";
        private const string FailKey = "PokiwarSmoke.Failures";

        public static string CaptureDir
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < args.Length; i++)
                    if (args[i] == "-smokeOut") return args[i + 1];
                return Path.GetFullPath("Library/PokiwarSmoke");
            }
        }

        [MenuItem("Pokiwar/Run Smoke Test (Play mode)")]
        public static void Run()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetInt(FailKey, 1);
            SessionState.SetString(StageKey, "start");
            SessionState.SetString(DeadlineKey, (EditorApplication.timeSinceStartup + 900).ToString(CultureInfo.InvariantCulture));
            EditorSceneManager.OpenScene(PokiwarSceneBuilder.ScenePath);
            bool portrait = Array.IndexOf(Environment.GetCommandLineArgs(), "-smokePortrait") >= 0;
            PlayModeWindow.SetCustomRenderingResolution(portrait ? 1080u : 1920u, portrait ? 1920u : 1080u, "PokiwarSmoke");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnChanged;
            EditorApplication.playModeStateChanged += OnChanged;
            EditorApplication.update -= Watchdog;
            EditorApplication.update += Watchdog;
        }

        private static void OnChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
                new GameObject("SmokeDriver").AddComponent<SmokeDriver>();
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(RunningKey, false);
                int fails = SessionState.GetInt(FailKey, 1);
                if (Application.isBatchMode) EditorApplication.Exit(fails == 0 ? 0 : 1);
            }
        }

        private static void Watchdog()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            double.TryParse(SessionState.GetString(DeadlineKey, "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var deadline);
            if (EditorApplication.timeSinceStartup < deadline) return;
            SessionState.SetBool(RunningKey, false);
            Debug.Log("[SMOKE] RESULT FAIL - no verdict in time, stuck at: " + SessionState.GetString(StageKey, "?"));
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else EditorApplication.isPlaying = false;
        }

        internal static void Stage(string s) => SessionState.SetString(StageKey, s);
        internal static void SetFailures(int n) => SessionState.SetInt(FailKey, n);
    }

    public sealed class SmokeDriver : MonoBehaviour
    {
        private int failures;
        private int checks;
        private int errors;
        private string abortReason;
        private bool finished;
        private float deadline;

        private void Awake()
        {
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            deadline = Time.realtimeSinceStartup + 840f;
            Directory.CreateDirectory(PokiwarSmoke.CaptureDir);
        }

        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (condition.StartsWith("[SMOKE]")) return;
            errors++;
            if (type == LogType.Exception && abortReason == null) abortReason = condition + "\n" + stack;
        }

        private void Update()
        {
            if (finished) return;
            if (abortReason != null)
            {
                failures++;
                Log("FAIL exception aborted the run: " + abortReason);
                Finish();
            }
            else if (Time.realtimeSinceStartup > deadline)
            {
                failures++;
                Log("FAIL driver deadline");
                Finish();
            }
        }

        private static void Log(string s) => Debug.Log("[SMOKE] " + s);

        private static int WornLayers(AvatarView v)
        {
            int n = 0;
            foreach (var l in v.Layers) if (l != null && l.gameObject.activeSelf) n++;
            return n;
        }

        private void Check(bool ok, string what)
        {
            checks++;
            PokiwarSmoke.Stage(what);
            if (ok) Log("ok   " + what);
            else
            {
                failures++;
                Log("FAIL " + what);
            }
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            Log("RESULT " + (failures == 0 && errors == 0 ? "PASS" : "FAIL") + " checks=" + checks + " failures=" + failures + " errors=" + errors);
            PokiwarSmoke.SetFailures(failures + errors);
            EditorApplication.isPlaying = false;
        }

        private static IEnumerator WaitFor(Func<bool> cond, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!cond() && Time.realtimeSinceStartup < end) yield return null;
        }

        private IEnumerator Start()
        {
            Log("screen " + Screen.width + "x" + Screen.height);
            yield return null;
            Check(ResponsiveCanvas.IsPortrait == (Screen.height > Screen.width), "layout matches screen shape (" + (ResponsiveCanvas.IsPortrait ? "portrait" : "landscape") + ")");
            yield return WaitFor(() => GameApp.Instance != null, 10);
            var app = GameApp.Instance;
            Check(app != null, "GameApp present");
            if (app == null) { Finish(); yield break; }

            FileSaveStore.OverridePath = Path.Combine(PokiwarSmoke.CaptureDir, "smoke_save.json");
            app.ResetSave();
            yield return null;
            Check(app.Hub.gameObject.activeSelf && !app.Map.gameObject.activeSelf, "hub screen visible");
            CheckOnScreen(app.Hub.transform, "hub");
            yield return Capture("01_hub");
            var audio = app.Audio;
            var vfx = VfxLayer.Instance;
            Check(audio != null && audio.Clips.Count >= 36 && audio.Clips.TrueForAll(c => c.Clip != null), "audio director has every clip (" + (audio != null ? audio.Clips.Count : 0) + ")");
            Check(vfx != null && vfx.Template != null && vfx.Star != null, "vfx layer wired");
            bool sfxBefore = app.Save.SfxOn;
            app.Hub.SfxButton.onClick.Invoke();
            var reread = new SaveService(new FileSaveStore(), new JsonSaveSerializer(), app.Progression).LoadOrCreate();
            Check(app.Save.SfxOn == !sfxBefore && audio.SfxOn == app.Save.SfxOn && reread.SfxOn == app.Save.SfxOn && app.Hub.SfxLabel.text.EndsWith(app.Save.SfxOn ? "ON" : "OFF"), "sound toggle applies and persists");
            app.Hub.SfxButton.onClick.Invoke();
            app.Hub.ShakeButton.onClick.Invoke();
            Check(!app.Battle.Shake.Enabled && !app.Save.ShakeOn, "shake toggle reaches the battle shake");
            app.Hub.ShakeButton.onClick.Invoke();
            Check(app.Battle.Shake.Enabled && audio.SfxOn, "settings restored");

            Check(WornLayers(app.Hub.Avatar) == 3, "hub shows the player avatar in the starter outfit (" + WornLayers(app.Hub.Avatar) + " layers)");
            app.Hub.AvatarButton.onClick.Invoke();
            yield return null;
            Check(app.Wardrobe.gameObject.activeSelf, "wardrobe opens from hub");
            app.Wardrobe.Tabs[(int)AvatarSlot.Hat].onClick.Invoke();
            yield return null;
            var hatRows = app.Wardrobe.ItemTemplate.transform.parent.GetComponentsInChildren<RowView>(false);
            Check(hatRows.Length == 2, "hat tab lists 2 hats (" + hatRows.Length + ")");
            int goldBefore = app.Save.Gold;
            if (hatRows.Length > 0) hatRows[0].ExtraA.onClick.Invoke();
            yield return null;
            string hat = app.Avatars.WornIn(app.Save, AvatarSlot.Hat);
            Check(hat != null && app.Save.Gold == goldBefore - app.Db.TryAvatarItem(hat).Price, "buying a hat spends gold and wears it");
            Check(WornLayers(app.Wardrobe.Preview) == 4, "preview shows the new hat");
            app.Wardrobe.NameField.text = "Smokey";
            app.Wardrobe.SaveNameButton.onClick.Invoke();
            var rereadAvatar = new SaveService(new FileSaveStore(), new JsonSaveSerializer(), app.Progression).LoadOrCreate();
            Check(rereadAvatar.PlayerName == "Smokey" && rereadAvatar.AvatarWorn.Contains(hat), "name and outfit persist");
            CheckOnScreen(app.Wardrobe.transform, "wardrobe");
            yield return Capture("01b_wardrobe");
            app.Wardrobe.BackButton.onClick.Invoke();
            yield return null;
            Check(app.Hub.gameObject.activeSelf && WornLayers(app.Hub.Avatar) == 4 && app.Hub.PlayerLabel.text.StartsWith("Smokey"), "hub shows the new outfit and name");

            app.Hub.AdventureButton.onClick.Invoke();
            yield return null;
            Check(app.Map.gameObject.activeSelf, "map opens from hub");
            var nodes = app.Map.NodeArea.GetComponentsInChildren<RowView>(false);
            Check(nodes.Length == 3, "map shows 3 nodes (" + nodes.Length + ")");
            Check(nodes.Length == 3 && nodes[0].Button.interactable && !nodes[1].Button.interactable, "only first node unlocked");
            CheckOnScreen(app.Map.transform, "map");
            yield return Capture("02_map");

            nodes[0].Button.onClick.Invoke();
            yield return null;
            Check(app.Prep.gameObject.activeSelf, "preparation opens");
            Check(app.Save.SelectedCardIds.Count == 5, "5 cards preselected");
            CheckOnScreen(app.Prep.transform, "prep");
            yield return Capture("03_prep");
            app.Prep.CardSlotRemove[4].onClick.Invoke();
            yield return null;
            Check(app.Save.SelectedCardIds.Count == 4 && !app.Prep.CardSlotIcons[4].gameObject.activeSelf, "X empties a card slot");
            app.Prep.CardSlots[4].onClick.Invoke();
            yield return null;
            Check(app.Prep.CardPicker.activeSelf, "a card slot opens the card picker");
            RowView lastCardRow = null;
            int reusableRows = 0;
            foreach (Transform t in app.Prep.CardTemplate.transform.parent)
                if (t.gameObject.activeSelf)
                {
                    lastCardRow = t.GetComponent<RowView>();
                    if (lastCardRow.Subtitle.text.Contains("reusable")) reusableRows++;
                }
            Check(reusableRows == app.Save.SkillCards.Count && reusableRows > 0, "the picker lists the player's reusable cards (" + reusableRows + ")");
            lastCardRow.Button.onClick.Invoke();
            yield return null;
            Check(app.Save.SelectedCardIds.Count == 5 && app.Prep.CardSlotIcons[4].gameObject.activeSelf, "picking a card fills the slot");
            yield return Capture("03b_card_picker");
            app.Prep.CardPickerClose.onClick.Invoke();
            yield return null;

            int energy = app.Save.Energy;
            app.Prep.FightButton.onClick.Invoke();
            yield return null;
            var bc = app.Battle;
            Check(app.BattleScreen.activeSelf && bc.Engine != null, "battle starts");
            yield return WaitFor(() => bc.IntroVs.gameObject.activeInHierarchy, 3);
            Check(bc.Intro.gameObject.activeSelf && bc.IntroPlayer.sprite == bc.PlayerHud.Portrait.sprite && bc.IntroEnemy.sprite == bc.EnemyHud.Portrait.sprite, "VS intro shows both fighters");
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Capture("03c_intro_vs");
            yield return WaitFor(() => bc.IntroFight.gameObject.activeInHierarchy, 3);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture("03d_intro_fight");
            yield return WaitFor(() => !bc.Intro.gameObject.activeSelf, 3);
            Check(!bc.Intro.gameObject.activeSelf, "intro clears before the first turn");
            Check(app.Save.Energy == energy - 1, "energy spent");
            Check(bc.PlayerCard.gameObject.activeSelf && bc.PlayerCard.NameLabel.text == "Smokey" && !bc.EnemyCard.gameObject.activeSelf, "player avatar card shows; a wild monster has none");
            string firstBattle = bc.Engine.State.BattleId;

            yield return WaitFor(() => bc.Engine.IsTurnOf(Side.Player) && bc.Board.InputEnabled, 20);
            Check(bc.Board.InputEnabled, "player turn waits for input");
            Check(bc.Board.Matches(bc.Engine.State.Board), "board view mirrors domain board");
            CheckOnScreen(app.BattleScreen.transform, "battle");
            float cell = Vector2.Distance(CellScreen(bc.Board, new Pos(0, 0)), CellScreen(bc.Board, new Pos(1, 0)));
            float shortSide = Mathf.Min(Screen.width, Screen.height);
            Log("board cell " + cell.ToString("0") + " px on a " + Screen.width + "x" + Screen.height + " screen");
            Check(cell >= shortSide * (ResponsiveCanvas.IsPortrait ? 0.095f : 0.065f), "board cells are big enough to tap (" + cell.ToString("0") + " px)");
            yield return Capture("04_battle_start");

            var moves = BoardEngine.FindMoves(bc.Engine.State.Board, 3);
            Check(moves.Count > 0, "board has moves");
            int turn = bc.Engine.State.TurnNumber;
            int spawnedBefore = vfx.SpawnedTotal;
            Drag(bc.Board, moves[0].A, moves[0].B);
            yield return WaitFor(() => vfx.ActiveCount > 0, 5);
            Check(vfx.ActiveCount > 0, "match spawned particles");
            int playing = 0;
            foreach (var src in audio.GetComponents<AudioSource>()) if (src.isPlaying) playing++;
            Log("audio output " + AudioSettings.outputSampleRate + " Hz, voices playing now " + playing);
            yield return new WaitForSecondsRealtime(0.08f);
            yield return Capture("04b_match_fx");
            bc.ShowCombo(4);
            yield return new WaitForSecondsRealtime(0.3f);
            Check(bc.ComboLabel.gameObject.activeSelf && bc.ComboLabel.text.Contains("COMBO"), "combo call-out shows over the board");
            yield return Capture("04b2_combo");
            yield return WaitFor(() => bc.Summary.Group.alpha > 0.95f, 8);
            float sumX = 0f;
            int icons = 0;
            foreach (var ic in bc.Summary.SlotIcons)
                if (ic.gameObject.activeSelf) { sumX += ic.rectTransform.anchoredPosition.x; icons++; }
            Check(icons > 0 && Mathf.Abs(sumX / icons) < 1f, "gem summary row is centred (" + icons + " icons, mean x " + (icons > 0 ? sumX / icons : 0f).ToString("0.0") + ")");
            yield return Capture("04c_summary");
            yield return WaitFor(() => bc.Engine.State.Current == Side.Enemy || bc.Engine.State.Ended, 20);
            Check(bc.Engine.State.Current == Side.Enemy || bc.Engine.State.Ended, "drag swap resolved and passed the turn");
            Check(bc.Engine.State.Log.Exists(e => e.Kind == CombatEventKind.GemSummary && e.Actor == Side.Player), "player match produced a gem summary");
            Check(Played(audio, "swap") && Played(audio, "match"), "swap and match sounds played");
            Check(vfx.SpawnedTotal > spawnedBefore, "vfx spawned on the match (" + (vfx.SpawnedTotal - spawnedBefore) + ")");

            yield return WaitFor(() => bc.Engine.IsTurnOf(Side.Player) && bc.Board.InputEnabled, 30);
            Check(bc.Engine.IsTurnOf(Side.Player), "turn came back after AI acted on the shared board");
            int logBefore = bc.Engine.State.Log.Count;
            var boardBefore = bc.Engine.State.Board.Clone();
            yield return WaitFor(() => bc.Engine.State.Current == Side.Enemy, 14);
            bool skipped = bc.Engine.State.Log.FindIndex(logBefore, e => e.Kind == CombatEventKind.TurnSkipped && e.Actor == Side.Player) >= 0;
            Check(skipped, "10 s without input skips the player's turn");
            Check(bc.Engine.State.Board.SameLayout(boardBefore), "timeout made no move");

            yield return WaitFor(() => bc.Engine.IsTurnOf(Side.Player) && bc.Board.InputEnabled, 30);
            int manaBefore = bc.Engine.State.Get(Side.Player).Mana.Current;
            Check(bc.PlayerHud.AttackArrow.gameObject.activeSelf && !bc.EnemyHud.AttackArrow.gameObject.activeSelf, "attack arrow shows beside the pet whose turn it is");
            int cardsBefore = ActiveCount(bc.CardButtons);
            bc.CardButtons[0].Button.onClick.Invoke();
            yield return WaitFor(() => bc.Engine.State.Log.Exists(e => e.Kind == CombatEventKind.CardUsed && e.Actor == Side.Player), 5);
            yield return WaitFor(() => bc.Board.InputEnabled, 5);
            Check(bc.Engine.IsTurnOf(Side.Player) && bc.Board.InputEnabled, "mana card did not end the turn");
            Check(bc.Engine.State.Get(Side.Player).Mana.Current > manaBefore || bc.Engine.State.Get(Side.Player).Mana.IsFull, "mana card added mana");
            Check(ActiveCount(bc.CardButtons) == cardsBefore - 1, "a used card leaves the bar (" + cardsBefore + " -> " + ActiveCount(bc.CardButtons) + ")");
            bc.Engine.State.Get(Side.Player).Shield.Set(240);
            bc.PlayerHud.Set(bc.Engine.State.Get(Side.Player).Snapshot());
            Check(bc.PlayerHud.ShieldBubble.gameObject.activeSelf && bc.PlayerHud.ShieldLabel.text == "240", "shield bubble and value show over the pet");
            Check(!bc.EnemyHud.ShieldBubble.gameObject.activeSelf || bc.Engine.State.Get(Side.Enemy).Shield.Current > 0, "no bubble without a shield");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture("05_battle_mid");

            bc.AnimationSpeed = 8f;
            bc.AutoPlay = true;
            yield return WaitFor(() => app.Result.gameObject.activeSelf, 300);
            Check(app.Result.gameObject.activeSelf, "battle reached the result screen");
            Check(Played(audio, "turn") && (Played(audio, "hit") || Played(audio, "hit.strong")) && Played(audio, "fight"), "turn, hit and fight sounds played");
            Check(Played(audio, "victory") || Played(audio, "defeat"), "end sound played");
            yield return Capture("06_result");
            bool won = app.Result.Title.text == "VICTORY";
            Log("first battle " + (won ? "won" : "lost") + " in " + bc.Engine.State.TurnNumber + " turns");
            Check(app.Save.CommittedBattleIds.Contains(firstBattle), "result committed for this battle id");
            if (won) Check(app.Progression.IsNodeUnlocked(app.Save, app.Db.Node("node.2")), "win unlocked node 2");
            if (won)
            {
                Check(app.Save.Pets.Exists(p => p.PetId == "mon.dunewing" && p.Level == 3), "first win captured the Dunewing just fought");
                Check(app.Result.CapturedPet.gameObject.activeSelf && app.Result.CapturedPet.sprite == app.Sprites.Get("right.dunewing"), "reward popup shows the captured pet");
            }
            else Check(!app.Result.CapturedPet.gameObject.activeSelf, "no captured pet on a defeat");

            var reloaded = new SaveService(new FileSaveStore(), new JsonSaveSerializer(), app.Progression).LoadOrCreate();
            Check(reloaded.Gold == app.Save.Gold && reloaded.CommittedBattleIds.Count == app.Save.CommittedBattleIds.Count, "save on disk matches memory");

            Check(app.BattleScreen.activeSelf, "reward popup shows over the finished battle");
            app.Result.CloseButton.onClick.Invoke();
            yield return null;
            Check(app.Prep.gameObject.activeSelf, "closing the reward returns to preparation");
            yield return new WaitForSecondsRealtime(2.5f);
            Check(vfx.ActiveCount == 0, "particles all returned to the pool (" + vfx.ActiveCount + " live)");
            Check(bc.Shake.AtRest, "battle screen shake at rest");

            app.Save.Node("node.1", true).Wins = Math.Max(1, app.Save.Wins("node.1"));
            app.Save.Node("node.2", true).Wins = 1;
            var pet = app.Save.Pet(app.Save.SelectedPetUid);
            pet.Level = 30;
            app.Save.Energy = 30;
            app.ShowMap();
            yield return null;
            nodes = app.Map.NodeArea.GetComponentsInChildren<RowView>(false);
            Check(nodes.Length == 3 && nodes[2].Button.interactable, "boss node unlocked");
            nodes[2].Button.onClick.Invoke();
            yield return null;
            app.Prep.FightButton.onClick.Invoke();
            yield return null;
            Check(app.BattleScreen.activeSelf && app.Battle.Engine.State.Get(Side.Enemy).Phases.Count == 1, "boss battle with a phase");
            bc.AnimationSpeed = 8f;
            bc.AutoPlay = true;
            yield return WaitFor(() => bc.Engine.State.Log.Exists(e => e.Kind == CombatEventKind.PhaseTriggered) || app.Result.gameObject.activeSelf, 300);
            yield return new WaitForSecondsRealtime(0.4f);
            if (app.BattleScreen.activeSelf) yield return Capture("07_boss_phase");
            var phaseEv = bc.Engine.State.Log.Find(e => e.Kind == CombatEventKind.PhaseTriggered);
            Check(phaseEv != null, "boss transformed");
            if (phaseEv != null) Check(phaseEv.After == bc.Engine.State.Get(Side.Enemy).Hp.Max / 2, "transform set HP to 50% (" + phaseEv.Before + " -> " + phaseEv.After + ")");
            Check(bc.Engine.State.Log.FindAll(e => e.Kind == CombatEventKind.PhaseTriggered).Count == 1, "phase fired once");
            Check(phaseEv == null || Played(audio, "transform"), "transform sound played");
            yield return WaitFor(() => app.Result.gameObject.activeSelf, 300);
            Check(app.Result.gameObject.activeSelf, "boss battle finished");
            var bossSprite = bc.EnemyHud.Portrait.sprite;
            Check(bossSprite == app.Sprites.Get("azurewing_ascended") || bossSprite == app.Sprites.Get("azurewing_ascended.defeat") || bossSprite == app.Sprites.Get("azurewing_ascended.hit") || bossSprite == app.Sprites.Get("azurewing_ascended.attack"), "boss portrait switched to the ascended form (" + (bossSprite != null ? bossSprite.name : "null") + ")");
            Log("boss battle " + app.Result.Title.text + " turns " + bc.Engine.State.TurnNumber);
            int commits = app.Save.CommittedBattleIds.Count;
            yield return new WaitForSecondsRealtime(0.5f);
            Check(app.Save.CommittedBattleIds.Count == commits, "no second commit after result");
            if (app.Result.Title.text == "VICTORY")
                Check(app.Save.Pets.Exists(p => p.PetId == "boss.azurewing" && p.Level == 8), "first boss win captured the boss itself");
            yield return Capture("07a_boss_reward");

            var captured = app.Save.Pets.Find(p => p.PetId == "boss.azurewing") ?? app.Progression.AddPet(app.Save, "boss.azurewing");
            string keepPet = app.Save.SelectedPetUid;
            app.Save.SelectedPetUid = captured.Uid;
            app.ShowPrep(app.Db.Node("node.1"));
            yield return null;
            Check(app.Sprites.Has("right.azurewing") && app.Prep.PetImage.sprite == app.Sprites.Get("right.azurewing"), "captured boss stands in the room facing the opponent");
            yield return Capture("07b_captured_room");
            app.Prep.FightButton.onClick.Invoke();
            yield return null;
            Check(app.BattleScreen.activeSelf && bc.PlayerHud.Portrait.sprite == app.Sprites.Get("right.azurewing"), "captured boss fights on the player side facing the opponent");
            Check(bc.Engine.State.Get(Side.Player).Phases.Count == 1 && bc.Engine.State.Get(Side.Player).LockedSkills.Count == 1, "captured boss keeps its ascended form");
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture("07c_captured_battle");
            bc.AnimationSpeed = 8f;
            bc.AutoPlay = true;
            yield return WaitFor(() => app.Result.gameObject.activeSelf, 300);
            Check(app.Result.gameObject.activeSelf, "battle with the captured boss finished");
            app.Save.SelectedPetUid = keepPet;

            app.ShowUpgrade();
            yield return null;
            Check(app.Upgrade.gameObject.activeSelf, "upgrade screen opens");
            CheckOnScreen(app.Upgrade.transform, "upgrade");
            yield return Capture("08_upgrade");
            var stoneRows = app.Upgrade.GetComponentsInChildren<RowView>(false);
            RowView mergeRow = null;
            foreach (var r in stoneRows) if (r.ExtraA != null && r.ExtraA.interactable) { mergeRow = r; break; }
            Check(mergeRow != null, "a stone stack can be merged");
            if (mergeRow != null)
            {
                int stonesBefore = 0;
                foreach (var st in app.Save.Stones) stonesBefore += st.Count;
                mergeRow.ExtraA.onClick.Invoke();
                yield return null;
                int stonesAfter = 0;
                foreach (var st in app.Save.Stones) stonesAfter += st.Count;
                Check(stonesAfter == stonesBefore - 3 || stonesAfter == stonesBefore - 2, "merge consumed 3 stones (" + stonesBefore + " -> " + stonesAfter + ")");
            }
            app.Save.AddCardStones(1, 6);
            app.Save.Gold += 5000;
            app.Hub.CardsButton.onClick.Invoke();
            yield return null;
            Check(app.CardForge.gameObject.activeSelf, "card forge opens from the hub");
            CheckOnScreen(app.CardForge.transform, "card forge");
            Check(app.CardForge.CardFace.LevelBadge.activeSelf && app.CardForge.CardFace.Level.text == "1" && app.CardForge.CardFace.Power.text.Length > 0, "card face shows cost, level and damage");
            RowView forgeRow = null;
            foreach (var r in app.CardForge.GetComponentsInChildren<RowView>(false)) if (r.ExtraB != null && r.ExtraB.gameObject.activeInHierarchy && r.ExtraB.interactable) { forgeRow = r; break; }
            Check(forgeRow != null, "a card stone can be fed to the card");
            if (forgeRow != null)
            {
                int stones = app.Save.CardStoneCount(1), petStoneTotal = 0, tries = 0;
                foreach (var st in app.Save.Stones) petStoneTotal += st.Count;
                while (app.Save.SkillCards[0].Level == 1 && tries < 6 && app.Save.CardStoneCount(1) > 0)
                {
                    foreach (var r in app.CardForge.GetComponentsInChildren<RowView>(false)) if (r.ExtraB != null && r.ExtraB.gameObject.activeInHierarchy) { r.ExtraB.onClick.Invoke(); break; }
                    tries++;
                    yield return null;
                }
                int petStoneAfter = 0;
                foreach (var st in app.Save.Stones) petStoneAfter += st.Count;
                Check(app.Save.CardStoneCount(1) == stones - tries && petStoneAfter == petStoneTotal, "upgrade spends card stones only (" + tries + " used)");
                Check(app.Save.SkillCards[0].Level == 2 && app.CardForge.CardFace.Level.text == "2", "card reached level 2 and the face shows it");
            }
            yield return Capture("08b_card_forge");
            app.ShowHub();
            yield return null;
            yield return Capture("09_hub_after");
            Check(audio.MissingKeys.Count == 0, "no missing audio keys (" + string.Join(",", audio.MissingKeys) + ")");
            Log("audio plays " + audio.TotalPlays + " distinct " + audio.PlayCounts.Count + ": " + string.Join(" ", System.Linq.Enumerable.Select(audio.PlayCounts, kv => kv.Key + "=" + kv.Value)));
            Log("vfx spawned " + vfx.SpawnedTotal);
            Finish();
        }

        private void CheckOnScreen(Transform screen, string name)
        {
            Canvas.ForceUpdateCanvases();
            var off = new System.Collections.Generic.List<string>();
            var corners = new Vector3[4];
            foreach (var b in screen.GetComponentsInChildren<Selectable>(false))
            {
                if (b.GetComponentInParent<ScrollRect>() != null) continue;
                ((RectTransform)b.transform).GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    var p = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
                    if (p.x < -1 || p.y < -1 || p.x > Screen.width + 1 || p.y > Screen.height + 1) { off.Add(b.name); break; }
                }
            }
            Check(off.Count == 0, name + ": every control fully on screen" + (off.Count > 0 ? " (off: " + string.Join(",", off) + ")" : ""));
        }

        private static int ActiveCount(ActionButtonView[] buttons)
        {
            int n = 0;
            foreach (var b in buttons) if (b.gameObject.activeSelf) n++;
            return n;
        }

        private static bool Played(AudioDirector a, string key) => a.PlayCounts.TryGetValue(key, out var n) && n > 0;

        private static Vector2 CellScreen(BoardView board, Pos p)
        {
            var world = board.GemRoot.TransformPoint(board.CellPos(p.X, p.Y));
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }

        private static void Drag(BoardView board, Pos a, Pos b)
        {
            var es = EventSystem.current;
            var pd = new PointerEventData(es) { button = PointerEventData.InputButton.Left };
            pd.position = pd.pressPosition = CellScreen(board, a);
            ExecuteEvents.Execute(board.gameObject, pd, ExecuteEvents.pointerDownHandler);
            pd.position = Vector2.Lerp(CellScreen(board, a), CellScreen(board, b), 0.5f);
            ExecuteEvents.Execute(board.gameObject, pd, ExecuteEvents.dragHandler);
            pd.position = CellScreen(board, b);
            ExecuteEvents.Execute(board.gameObject, pd, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(board.gameObject, pd, ExecuteEvents.pointerUpHandler);
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var canvas = FindFirstObjectByType<Canvas>();
            int w = Screen.width, h = Screen.height;
            var rt = new RenderTexture(w, h, 24);
            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.targetTexture = rt;
            var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(PokiwarSmoke.CaptureDir, name + ".jpg"), tex.EncodeToJPG(80));
            canvas.renderMode = mode;
            canvas.worldCamera = null;
            cam.targetTexture = null;
            UnityEngine.Object.Destroy(tex);
            UnityEngine.Object.Destroy(camGo);
            rt.Release();
            UnityEngine.Object.Destroy(rt);
        }
    }
}
