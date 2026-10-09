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
            uint width = portrait ? 1080u : 1920u, height = portrait ? 1920u : 1080u;
            var args = Environment.GetCommandLineArgs();
            int sizeArg = Array.IndexOf(args, "-smokeSize");
            if (sizeArg >= 0 && sizeArg + 1 < args.Length)
            {
                var parts = args[sizeArg + 1].Split('x');
                if (parts.Length != 2 || !uint.TryParse(parts[0], out width) || !uint.TryParse(parts[1], out height))
                    throw new ArgumentException("-smokeSize expects <width>x<height>, got " + args[sizeArg + 1]);
            }
            PlayModeWindow.SetCustomRenderingResolution(width, height, "PokiwarSmoke");
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
            Check(!ResponsiveCanvas.IsPortrait && Screen.width > Screen.height, "layout is locked to landscape (" + Screen.width + "x" + Screen.height + ")");
            yield return WaitFor(() => GameApp.Instance != null, 10);
            var app = GameApp.Instance;
            Check(app != null, "GameApp present");
            if (app == null) { Finish(); yield break; }

            FileSaveStore.OverridePath = Path.Combine(PokiwarSmoke.CaptureDir, "smoke_save.json");
            app.ResetSave();
            yield return null;
            Check(app.Home.gameObject.activeSelf && !app.Map.gameObject.activeSelf && !app.Hub.gameObject.activeSelf, "the town map is the first screen");
            Check(app.Home.PlayerLabel.text.Contains("EXP 0/40"), "a new trainer needs 40 EXP for level 2 (" + app.Home.PlayerLabel.text + ")");
            CheckOnScreen(app.Home.transform, "home");
            yield return Capture("00_home");
            app.Home.CardShopButton.onClick.Invoke();
            yield return null;
            Check(app.ShopScreen.gameObject.activeSelf && !app.Home.gameObject.activeSelf, "the card shop building opens the shop");
            var shopRows = app.ShopScreen.ItemTemplate.transform.parent.GetComponentsInChildren<RowView>(false);
            Check(shopRows.Length == app.Db.Cards.Count + 3 && shopRows.Length == app.Shop.Items.Count, "shop lists every single-use card and the three upgrade items (" + shopRows.Length + ")");
            Check(shopRows.Length > 0 && shopRows[0].Icon.sprite != null && shopRows[0].Icon.sprite.name.StartsWith("face_"), "a card on sale shows its card face");
            var shopItem = app.Shop.Items[0];
            app.Save.Gold += shopItem.Price;
            int shopGold = app.Save.Gold, shopOwned = app.Shop.Owned(app.Save, shopItem);
            if (shopRows.Length > 0) shopRows[0].ExtraA.onClick.Invoke();
            yield return null;
            Check(app.Save.Gold == shopGold - shopItem.Price && app.Shop.Owned(app.Save, shopItem) == shopOwned + 1, "buying spends gold and adds the item");
            CheckOnScreen(app.ShopScreen.transform, "shop");
            yield return Capture("00b_shop");
            app.ShopScreen.BackButton.onClick.Invoke();
            yield return null;
            Check(app.Home.gameObject.activeSelf && app.Home.RankLabel.text == "0", "shop returns to the town, a new trainer has no rank points");
            app.Home.HuntButton.onClick.Invoke();
            yield return null;
            Check(app.World.gameObject.activeSelf && !app.Home.gameObject.activeSelf && !app.Map.gameObject.activeSelf, "the hunt building opens the world map");
            CheckOnScreen(app.World.transform, "world");
            yield return Capture("00_world");
            app.World.IslandButton.onClick.Invoke();
            yield return null;
            Check(app.Map.gameObject.activeSelf && !app.World.gameObject.activeSelf, "the open island leads to the lobby");
            var regionTiles = app.Map.RegionTemplate.transform.parent.GetComponentsInChildren<RowView>(false);
            Check(regionTiles.Length == app.Map.RegionSlots && regionTiles[0].Button.interactable && !regionTiles[1].Button.interactable, "lobby lists " + regionTiles.Length + " regions, only the first open");
            CheckOnScreen(app.Map.transform, "lobby");
            yield return Capture("00_lobby");
            app.Map.BackButton.onClick.Invoke();
            yield return null;
            Check(app.Hub.gameObject.activeSelf && !app.Map.gameObject.activeSelf, "info screen opens from the lobby");
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
            Check(app.Map.gameObject.activeSelf && app.Map.PlayerLabel.text.StartsWith("Smokey"), "wardrobe returns to the lobby with the new name");
            app.Map.BackButton.onClick.Invoke();
            yield return null;
            Check(app.Hub.gameObject.activeSelf && WornLayers(app.Hub.Avatar) == 4 && app.Hub.PlayerLabel.text.StartsWith("Smokey"), "hub shows the new outfit and name");

            app.Hub.AdventureButton.onClick.Invoke();
            yield return null;
            Check(app.Map.gameObject.activeSelf, "lobby opens from the info screen");
            var nodes = app.Map.NodeArea.GetComponentsInChildren<RowView>(false);
            Check(nodes.Length == 6, "map shows the 6 East Sea nodes (" + nodes.Length + ")");
            Check(nodes.Length == 6 && nodes[0].Button.interactable && !nodes[1].Button.interactable, "only first node unlocked");
            CheckOnScreen(app.Map.transform, "map");
            yield return Capture("02_map");

            nodes[0].Button.onClick.Invoke();
            yield return null;
            Check(app.Prep.gameObject.activeSelf, "preparation opens");
            Check(app.Save.SelectedCardIds.Count == 5, "5 cards preselected");
            Check(app.Save.SelectedCardIds.FindAll(x => app.Save.SkillCard(x) != null).Count == 1, "exactly one skill card is preselected");
            CheckOnScreen(app.Prep.transform, "prep");
            yield return Capture("03_prep");
            app.Prep.CardSlotRemove[4].onClick.Invoke();
            yield return null;
            Check(app.Save.SelectedCardIds.Count == 4 && !app.Prep.CardSlotIcons[4].gameObject.activeSelf, "X empties a card slot");
            app.Prep.CardSlots[4].onClick.Invoke();
            yield return null;
            Check(app.Prep.CardPicker.activeSelf, "a card slot opens the card picker");
            RowView lastCardRow = null;
            int reusableRows = 0, facedRows = 0, shownRows = 0;
            foreach (Transform t in app.Prep.CardTemplate.transform.parent)
                if (t.gameObject.activeSelf)
                {
                    lastCardRow = t.GetComponent<RowView>();
                    shownRows++;
                    if (lastCardRow.Icon.sprite != null && lastCardRow.Icon.sprite.name.StartsWith("face_")) facedRows++;
                    if (lastCardRow.Subtitle.text.Contains("reusable")) reusableRows++;
                }
            Check(shownRows > 0 && facedRows == shownRows, "every row of the card picker shows the card's own face (" + facedRows + "/" + shownRows + ")");
            Check(reusableRows == app.Save.SkillCards.Count && reusableRows > 0, "the picker lists the player's reusable cards (" + reusableRows + ")");
            lastCardRow.Button.onClick.Invoke();
            yield return null;
            Check(app.Save.SelectedCardIds.Count == 5 && app.Prep.CardSlotIcons[4].gameObject.activeSelf, "picking a card fills the slot");
            yield return Capture("03b_card_picker");
            app.Prep.CardPickerClose.onClick.Invoke();
            yield return null;

            int energy = app.Save.Energy;
            int potionStock = app.Save.CardCount("card.mana_potion");
            app.Prep.FightButton.onClick.Invoke();
            yield return null;
            var bc = app.Battle;
            Check(app.BattleScreen.activeSelf && bc.Engine != null, "battle starts");
            yield return WaitFor(() => bc.IntroVs.gameObject.activeInHierarchy, 3);
            Check(bc.Intro.gameObject.activeSelf && bc.IntroPlayer.sprite != null && bc.IntroEnemy.sprite != null && bc.PlayerHud.Portrait.sprite.name.StartsWith(bc.IntroPlayer.sprite.name) && bc.EnemyHud.Portrait.sprite.name.StartsWith(bc.IntroEnemy.sprite.name), "VS intro shows both fighters");
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
            int valued = 0, shownCards = 0;
            foreach (var cb in bc.CardButtons)
            {
                if (!cb.gameObject.activeSelf) continue;
                shownCards++;
                if (cb.Value.text.Length > 0 && cb.Icon.sprite != null && cb.Icon.sprite.name.StartsWith("face_")) valued++;
            }
            Check(shownCards > 0 && valued == shownCards, "every single-use card wears its own face and shows the number it gives (" + valued + "/" + shownCards + ")");
            Check(bc.SkillButtons[0].Icon.sprite == app.Sprites.Get(CardFaces.SkillFace) && !bc.SkillButtons[1].gameObject.activeSelf && bc.SkillButtons[0].LevelBadge.activeSelf, "the one skill card wears the skill face and shows its level");
            float homeX = bc.PlayerHud.Portrait.rectTransform.position.x;
            bc.PlayerHud.StartCoroutine(bc.PlayerHud.Lunge(bc.EnemyHud.BodyCenter));
            yield return new WaitForSecondsRealtime(0.34f);
            float reached = bc.PlayerHud.Portrait.rectTransform.position.x - homeX;
            float span = bc.EnemyHud.Portrait.rectTransform.position.x - homeX;
            Check(reached > span * 0.5f, "an attacker dashes across to its target (" + reached.ToString("0") + " of " + span.ToString("0") + " px)");
            yield return Capture("04a0_dash");
            yield return new WaitForSecondsRealtime(0.7f);
            Check(Mathf.Abs(bc.PlayerHud.Portrait.rectTransform.position.x - homeX) < 1f, "and walks back to its place");
            Check(bc.PlayerHud.Shot == null, "a creature with no projectile fights up close");
            yield return bc.EnemyHud.Transform("bebeboom");
            Check(bc.EnemyHud.Shot != null && bc.EnemyHud.ShotIsLobbed, "Bebeboom throws its bomb instead of dashing");
            float enemyHomeX = bc.EnemyHud.Portrait.rectTransform.position.x;
            int shotsBefore = VfxLayer.Instance.SpawnedTotal;
            yield return bc.EnemyHud.Throw();
            float shotTime = VfxLayer.Instance.Shot(bc.EnemyHud.BodyCenter, bc.PlayerHud.BodyCenter, bc.EnemyHud.Shot, bc.EnemyHud.ShotSize, bc.EnemyHud.ShotSeconds, bc.EnemyHud.LobArc, bc.EnemyHud.LobSpin);
            yield return new WaitForSecondsRealtime(shotTime * 0.5f);
            Check(VfxLayer.Instance.SpawnedTotal == shotsBefore + 1 && Mathf.Abs(bc.EnemyHud.Portrait.rectTransform.position.x - enemyHomeX) < span * 0.2f, "the thrower stays on its rock while one projectile is in the air");
            yield return Capture("04a0b_lob");
            yield return new WaitForSecondsRealtime(0.7f);
            yield return bc.EnemyHud.Transform(bc.Engine.State.Get(Side.Enemy).SpriteKey);
            bc.Clock.Restart();
            var idleLib = ScriptableObject.CreateInstance<SpriteLibrary>();
            idleLib.Sprites.Add(new KeyedSprite { Key = "emberkit", Sprite = app.Sprites.Get("emberkit") });
            idleLib.Sprites.Add(new KeyedSprite { Key = "emberkit.idle2", Sprite = app.Sprites.Get("emberkit.hit") });
            idleLib.Sprites.Add(new KeyedSprite { Key = "emberkit.idle3", Sprite = app.Sprites.Get("emberkit.attack") });
            bc.PlayerHud.Setup(bc.Engine.State.Get(Side.Player), idleLib, true);
            var idleSeen = new System.Collections.Generic.HashSet<Sprite>();
            for (float waited = 0f; waited < 1.2f; waited += Time.unscaledDeltaTime)
            {
                idleSeen.Add(bc.PlayerHud.Portrait.sprite);
                yield return null;
            }
            Check(bc.PlayerHud.IdleFrameCount == 3 && idleSeen.Count == 3, "a creature with painted idle frames plays them in a loop (" + idleSeen.Count + " of " + bc.PlayerHud.IdleFrameCount + " shown)");
            bc.PlayerHud.Setup(bc.Engine.State.Get(Side.Player), app.Sprites, true);
            bc.PlayerHud.SetTurn(true);
            UnityEngine.Object.Destroy(idleLib);
            Check(bc.PlayerHud.IdleFrameCount == 3 && bc.EnemyHud.IdleFrameCount == 3, "both creatures carry their three painted idle frames (" + bc.PlayerHud.IdleFrameCount + ", " + bc.EnemyHud.IdleFrameCount + ")");
            int idleMissing = 0;
            foreach (var idleKey in new[] { "emberkit", "samgong", "bebeboom", "ngoclam", "doimora", "voirong", "ongnamhai", "ngoclam_evolved", "doimora_evolved", "voirong_evolved", "ongnamhai_evolved" })
                foreach (var idleFrame in new[] { ".idle2", ".idle3" })
                    if (!app.Sprites.Has(idleKey + idleFrame) || (idleKey != "emberkit" && !app.Sprites.Has("right." + idleKey + idleFrame))) idleMissing++;
            Check(idleMissing == 0, "every East Sea creature and Emberkit has idle frames 2 and 3 (" + idleMissing + " missing)");
            var firstForm = bc.EnemyHud.Portrait.sprite;
            var secondForm = app.Sprites.Get("ngoclam" + SpriteKeys.EvolvedSuffix);
            Check(Mathf.Approximately(secondForm.rect.width / secondForm.rect.height, firstForm.rect.width / firstForm.rect.height), "a second form is framed on the same canvas as its first form");
            bc.EnemyHud.SetSprite(secondForm);
            yield return null;
            yield return Capture("04a1_second_form");
            bc.EnemyHud.SetSprite(firstForm);
            bc.Qte.Root.SetActive(true);
            bc.Qte.Title.text = "Emberkit - Blaze Burst";
            bc.Qte.DamageLabel.text = "500";
            bc.Qte.Tokens[0].sprite = bc.Qte.TokenOk;
            bc.Qte.Tokens[1].sprite = bc.Qte.TokenBad;
            yield return null;
            var qteSlot = bc.Qte.Tokens[2];
            Check(qteSlot != null && qteSlot.sprite != null && qteSlot.sprite.name == "qte_token" && bc.Qte.TokenOk != null && bc.Qte.TokenBad != null && bc.Qte.Marker.GetComponent<Image>().sprite.name == "qte_knob", "the arrow mini game wears its painted bar, tokens and slider knob");
            CheckOnScreen(bc.Qte.Root.transform, "arrow mini game");
            yield return Capture("04a1_qte");
            bc.Qte.Root.SetActive(false);
            int liveBeforeBuff = vfx.ActiveCount;
            Check(app.Sprites.Has("buff.Heart") && app.Sprites.Has("buff.Lightning") && app.Sprites.Has("buff.Fire") && app.Sprites.Has("buff.Shield"), "buff icons are in the sprite library");
            vfx.Buff(bc.PlayerHud.BodyCenter, app.Sprites.Get("buff.Heart"), new Color(0.45f, 1f, 0.5f));
            yield return new WaitForSecondsRealtime(0.45f);
            Check(vfx.ActiveCount > liveBeforeBuff, "buff effect floats gem icons over the pet (" + vfx.ActiveCount + " live)");
            yield return Capture("04a_buff");
            yield return new WaitForSecondsRealtime(1.4f);
            int liveBeforeSwirl = vfx.ActiveCount;
            vfx.Swirl(bc.PlayerHud.Feet, app.Sprites.Get("fx.wisp"), new Color(1f, 0.5f, 0.15f), bc.PlayerHud.Portrait.rectTransform);
            yield return new WaitForSecondsRealtime(0.55f);
            Check(vfx.ActiveCount >= liveBeforeSwirl + 10, "rage gain spins a fire vortex at the pet's feet (" + vfx.ActiveCount + " live)");
            Check(vfx.BehindCount >= 4 && vfx.BehindCount < vfx.ActiveCount, "the far half of the vortex is drawn behind the pet (" + vfx.BehindCount + " behind)");
            Check(vfx.ActiveCount - liveBeforeSwirl == 16, "the rage vortex is one ring of flames and nothing else (" + (vfx.ActiveCount - liveBeforeSwirl) + " sprites)");
            yield return Capture("04a2_rage_swirl");
            yield return new WaitForSecondsRealtime(1.3f);
            int liveBeforeSiphon = vfx.ActiveCount;
            vfx.Siphon(bc.EnemyHud.BodyCenter, bc.PlayerHud.BodyCenter, new Color(1f, 0.25f, 0.2f), app.Sprites.Get("fx.mote"), true);
            yield return new WaitForSecondsRealtime(0.32f);
            Check(vfx.ActiveCount >= liveBeforeSiphon + 10, "a steal pulls motes from the victim into the thief (" + vfx.ActiveCount + " live)");
            yield return Capture("04a3_siphon");
            yield return new WaitForSecondsRealtime(1.4f);

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
            Check(app.Save.CardCount("card.mana_potion") == potionStock - 1, "the potion played in battle left the stock (" + potionStock + " -> " + app.Save.CardCount("card.mana_potion") + ")");
            if (won) Check(app.Progression.IsNodeUnlocked(app.Save, app.Db.Node("node.2")), "win unlocked node 2");
            if (won)
            {
                Check(app.Save.Pets.Exists(p => p.PetId == "mon.samgong" && p.Level == 1), "first win captured the Samgong just fought, at level 1");
                Check(app.Result.CapturedPet.gameObject.activeSelf && app.Result.CapturedPet.sprite == app.Sprites.Get("right.samgong"), "reward popup shows the captured pet");
            }
            else Check(!app.Result.CapturedPet.gameObject.activeSelf, "no captured pet on a defeat");

            var reloaded = new SaveService(new FileSaveStore(), new JsonSaveSerializer(), app.Progression).LoadOrCreate();
            Check(reloaded.Gold == app.Save.Gold && reloaded.CommittedBattleIds.Count == app.Save.CommittedBattleIds.Count, "save on disk matches memory");

            Check(app.BattleScreen.activeSelf, "reward popup shows over the finished battle");
            app.Result.CloseButton.onClick.Invoke();
            yield return null;
            Check(app.Prep.gameObject.activeSelf, "closing the reward returns to preparation");
            if (won) Check(app.Save.RankPoints == 20 && app.Prep.PetWins.text == "1", "the win paid 20 rank points and the room counts the pet's win (" + app.Save.RankPoints + ", " + app.Prep.PetWins.text + ")");
            yield return new WaitForSecondsRealtime(2.5f);
            Check(vfx.ActiveCount == 0, "particles all returned to the pool (" + vfx.ActiveCount + " live)");
            Check(bc.Shake.AtRest, "battle screen shake at rest");

            app.Save.Node("node.1", true).Wins = Math.Max(1, app.Save.Wins("node.1"));
            foreach (var open in new[] { "node.2", "node.3", "node.4", "node.5" }) app.Save.Node(open, true).Wins = app.Db.Node(open).WinsRequired;
            app.Save.Node("node.6", true).Wins = app.Db.Node("node.6").WinsRequired - 1;
            var pet = app.Save.Pet(app.Save.SelectedPetUid);
            pet.Level = 30;
            app.Save.Energy = 30;
            app.ShowMap();
            yield return null;
            nodes = app.Map.NodeArea.GetComponentsInChildren<RowView>(false);
            Check(nodes.Length == 6 && nodes[5].Button.interactable, "boss node unlocked");
            Check(nodes[0].Highlight.activeSelf && nodes[0].Icon.material != app.Map.NotOwnedMaterial && !nodes[5].Highlight.activeSelf && nodes[5].Icon.material == app.Map.NotOwnedMaterial, "an owned creature shows its star and colours, one not owned yet is grey without a star");
            yield return Capture("06b_map_owned");
            nodes[5].Button.onClick.Invoke();
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
            Check(bossSprite != null && bossSprite.name.StartsWith("char_ongnamhai_evolved"), "boss portrait switched to its second form (" + (bossSprite != null ? bossSprite.name : "null") + ")");
            Log("boss battle " + app.Result.Title.text + " turns " + bc.Engine.State.TurnNumber);
            int commits = app.Save.CommittedBattleIds.Count;
            yield return new WaitForSecondsRealtime(0.5f);
            Check(app.Save.CommittedBattleIds.Count == commits, "no second commit after result");
            if (app.Result.Title.text == "VICTORY")
                Check(app.Save.Pets.Exists(p => p.PetId == "boss.ongnamhai" && p.Level == 1), "the boss win that completes its wins captured the boss itself at level 1");
            yield return Capture("07a_boss_reward");

            var captured = app.Save.Pets.Find(p => p.PetId == "boss.ongnamhai") ?? app.Progression.AddPet(app.Save, "boss.ongnamhai");
            string keepPet = app.Save.SelectedPetUid;
            app.Save.SelectedPetUid = captured.Uid;
            app.ShowPrep(app.Db.Node("node.1"));
            yield return null;
            Check(app.Sprites.Has("right.ongnamhai") && app.Prep.PetImage.sprite == app.Sprites.Get("right.ongnamhai"), "captured boss stands in the room facing the opponent");
            yield return Capture("07b_captured_room");
            app.Prep.FightButton.onClick.Invoke();
            yield return null;
            Check(app.BattleScreen.activeSelf && bc.PlayerHud.Portrait.sprite == app.Sprites.Get("right.ongnamhai"), "captured boss fights on the player side facing the opponent");
            Check(bc.Engine.State.Get(Side.Player).Phases.Count == 0 && bc.Engine.State.Get(Side.Player).LockedSkills.Count == 0, "a captured boss has no second phase on the player side");
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
            app.Map.CardsButton.onClick.Invoke();
            yield return null;
            Check(app.CardForge.gameObject.activeSelf, "card forge opens from the lobby");
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
