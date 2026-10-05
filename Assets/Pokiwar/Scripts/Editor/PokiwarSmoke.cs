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

            int energy = app.Save.Energy;
            app.Prep.FightButton.onClick.Invoke();
            yield return null;
            var bc = app.Battle;
            Check(app.BattleScreen.activeSelf && bc.Engine != null, "battle starts");
            Check(app.Save.Energy == energy - 1, "energy spent");
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
            bc.CardButtons[0].Button.onClick.Invoke();
            yield return WaitFor(() => bc.Engine.State.Log.Exists(e => e.Kind == CombatEventKind.CardUsed && e.Actor == Side.Player), 5);
            yield return WaitFor(() => bc.Board.InputEnabled, 5);
            Check(bc.Engine.IsTurnOf(Side.Player) && bc.Board.InputEnabled, "mana card did not end the turn");
            Check(bc.Engine.State.Get(Side.Player).Mana.Current > manaBefore || bc.Engine.State.Get(Side.Player).Mana.IsFull, "mana card added mana");
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

            var reloaded = new SaveService(new FileSaveStore(), new JsonSaveSerializer(), app.Progression).LoadOrCreate();
            Check(reloaded.Gold == app.Save.Gold && reloaded.CommittedBattleIds.Count == app.Save.CommittedBattleIds.Count, "save on disk matches memory");

            app.Result.RetryButton.onClick.Invoke();
            yield return null;
            Check(app.Prep.gameObject.activeSelf, "retry returns to preparation");
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
