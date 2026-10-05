using System;
using System.Collections.Generic;
using System.IO;
using Pokiwar.App;
using Pokiwar.Data;
using Pokiwar.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pokiwar.EditorTools
{
    /// <summary>Authors the whole game scene once: every screen is pre-placed so it can be laid out without Play mode.</summary>
    public static class PokiwarSceneBuilder
    {
        public const string ScenePath = "Assets/Pokiwar/Scenes/Pokiwar.unity";
        public const string GemPrefabPath = "Assets/Pokiwar/Prefabs/GemView.prefab";

        private static Font font;
        private static Dictionary<string, Sprite> art;

        private static readonly Color Gold = new Color(1f, 0.83f, 0.32f);
        private static readonly Color PanelCol = new Color(0.11f, 0.14f, 0.22f, 0.93f);
        private static readonly Color Blue = new Color(0.22f, 0.45f, 0.82f);
        private static readonly Color Green = new Color(0.22f, 0.62f, 0.3f);
        private static readonly Color Red = new Color(0.72f, 0.24f, 0.24f);
        private static readonly Color Gray = new Color(0.35f, 0.37f, 0.42f);

        [MenuItem("Pokiwar/Rebuild Scene + Art (overwrites scene layout)")]
        public static void RebuildMenu() => BuildAll(true);

        [MenuItem("Pokiwar/Seed Missing Content Assets")]
        public static void SeedMenu() => PokiwarContentSeeder.Seed(false);

        public static void BatchBuild()
        {
            bool ok = false;
            try
            {
                BuildAll(true);
                ok = true;
                Debug.Log("[POKIWAR-BUILD] OK " + ScenePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[POKIWAR-BUILD] FAIL " + e);
            }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void BuildAll(bool overwriteScene)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            art = PlaceholderArt.BuildAll();
            Fg38Art.Apply(art);
            var catalog = PokiwarContentSeeder.Seed(false);
            var lib = PokiwarContentSeeder.BuildSprites(art);
            var gem = BuildGemPrefab();
            var clips = PokiwarAudioBuilder.BuildAll();
            if (overwriteScene || !File.Exists(ScenePath)) BuildScene(catalog, lib, gem, clips);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
        }

        private static GemView BuildGemPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GemPrefabPath));
            var root = NewUI("GemView", null);
            root.sizeDelta = new Vector2(84, 84);
            var gv = root.gameObject.AddComponent<GemView>();
            gv.Icon = Img(root, "Icon", "gem.Sword", Color.white);
            Fill(gv.Icon.rectTransform);
            var badge = Img(root, "Badge", "ui.circle", new Color(0.08f, 0.08f, 0.12f, 0.92f));
            At(badge.rectTransform, 1, 1, -14, -14, 38, 38);
            gv.MultiplierBadge = badge.gameObject;
            gv.MultiplierLabel = Txt(badge.transform, "Label", "x2", 22, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(gv.MultiplierLabel.rectTransform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, GemPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<GemView>();
        }

        private static void BuildScene(ContentCatalog catalog, SpriteLibrary lib, GemView gemPrefab, List<KeyedClip> clips)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.1f);
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var root = canvasGo.transform;

            var hub = BuildHub(root);
            var map = BuildMap(root);
            var prep = BuildPrep(root);
            var battle = BuildBattle(root, lib, gemPrefab);
            var result = BuildResult(root);
            var upgrade = BuildUpgrade(root);
            BuildVfx(root);
            var toast = BuildToast(root);
            Portrait(toast, 0.5f, 0.5f, 0, -40, 900, 84);
            canvasGo.AddComponent<ResponsiveCanvas>();

            var appGo = new GameObject("PokiwarApp");
            var app = appGo.AddComponent<GameApp>();
            app.Catalog = catalog;
            app.Sprites = lib;
            app.Hub = hub;
            app.Map = map;
            app.Prep = prep;
            app.BattleScreen = battle.gameObject;
            app.Battle = battle;
            app.Result = result;
            app.Upgrade = upgrade;
            app.ToastView = toast;
            var audio = new GameObject("Audio").AddComponent<AudioDirector>();
            audio.transform.SetParent(appGo.transform, false);
            audio.Clips = clips;
            app.Audio = audio;

            hub.gameObject.SetActive(true);
            map.gameObject.SetActive(false);
            prep.gameObject.SetActive(false);
            battle.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            upgrade.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static HubScreen BuildHub(Transform root)
        {
            var s = Screen(root, "HubScreen");
            Fill(Img(s, "Bg", Key("bg.hub", "ui.map"), Color.white).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.18f)).rectTransform);
            var hub = s.gameObject.AddComponent<HubScreen>();
            var title = Txt(s, "Title", "POKIWAR", 120, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(title.rectTransform, 0.5f, 0.5f, 0, 410, 1200, 150);
            var subtitle = Txt(s, "Subtitle", "Offline Pet Battle Adventure", 36, Color.white, TextAnchor.MiddleCenter);
            At(subtitle.rectTransform, 0.5f, 0.5f, 0, 325, 1000, 50);
            hub.PlayerLabel = Txt(s, "PlayerLabel", "", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(hub.PlayerLabel.rectTransform, 0.5f, 0.5f, 0, 260, 1400, 48);
            hub.ResourcesLabel = Txt(s, "ResourcesLabel", "", 30, new Color(0.9f, 0.95f, 1f), TextAnchor.MiddleCenter);
            At(hub.ResourcesLabel.rectTransform, 0.5f, 0.5f, 0, 215, 1400, 44);

            var card = Img(s, "PetCard", "ui.round", PanelCol);
            At(card.rectTransform, 0.5f, 0.5f, -360, -110, 620, 560);
            hub.PetImage = Img(card.transform, "PetImage", "emberkit", Color.white);
            hub.PetImage.preserveAspect = true;
            At(hub.PetImage.rectTransform, 0.5f, 0.5f, 0, 60, 380, 380);
            hub.PetLabel = Txt(card.transform, "PetLabel", "", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(hub.PetLabel.rectTransform, 0.5f, 0.5f, 0, -200, 580, 110);

            hub.AdventureButton = Btn(s, "AdventureButton", "ADVENTURE", Green, 56, out _);
            At(Rt(hub.AdventureButton), 0.5f, 0.5f, 420, 40, 560, 160);
            hub.UpgradeButton = Btn(s, "UpgradeButton", "PETS & STONES", Blue, 42, out _);
            At(Rt(hub.UpgradeButton), 0.5f, 0.5f, 420, -140, 560, 120);
            hub.ResetButton = Btn(s, "ResetButton", "RESET SAVE", Red, 28, out _);
            At(Rt(hub.ResetButton), 0.5f, 0.5f, 420, -290, 300, 80);
            hub.MusicButton = Btn(s, "MusicButton", "MUSIC: ON", Gray, 24, out hub.MusicLabel);
            At(Rt(hub.MusicButton), 1, 1, -560, -50, 190, 64);
            hub.SfxButton = Btn(s, "SfxButton", "SOUND: ON", Gray, 24, out hub.SfxLabel);
            At(Rt(hub.SfxButton), 1, 1, -360, -50, 190, 64);
            hub.ShakeButton = Btn(s, "ShakeButton", "SHAKE: ON", Gray, 24, out hub.ShakeLabel);
            At(Rt(hub.ShakeButton), 1, 1, -160, -50, 190, 64);
            var hint = Txt(s, "Hint", "Drag or tap two neighbouring gems to swap. 10 seconds per turn.", 26, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter);
            At(hint.rectTransform, 0.5f, 0, 0, 50, 1600, 40);
            Portrait(title, 0.5f, 0.5f, 0, 700, 1000, 150);
            Portrait(subtitle, 0.5f, 0.5f, 0, 615, 1000, 50);
            Portrait(hub.PlayerLabel, 0.5f, 0.5f, 0, 545, 1000, 48);
            Portrait(hub.ResourcesLabel, 0.5f, 0.5f, 0, 495, 1000, 44);
            Portrait(card, 0.5f, 0.5f, 0, 120, 620, 560);
            Portrait(hub.AdventureButton, 0.5f, 0.5f, 0, -320, 560, 160);
            Portrait(hub.UpgradeButton, 0.5f, 0.5f, 0, -480, 560, 120);
            Portrait(hub.ResetButton, 0.5f, 0.5f, 0, -610, 300, 80);
            Portrait(hint, 0.5f, 0, 0, 60, 1000, 80);
            return hub;
        }

        private static MapScreen BuildMap(Transform root)
        {
            var s = Screen(root, "MapScreen");
            Fill(Img(s, "Bg", Key("bg.map", "ui.map"), Color.white).rectTransform);
            var map = s.gameObject.AddComponent<MapScreen>();
            var bar = Img(s, "TopBar", null, new Color(0, 0, 0, 0.45f));
            At(bar.rectTransform, 0.5f, 1, 0, -60, 4000, 120);
            map.Header = Txt(s, "Header", "Sunny Isle", 60, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(map.Header.rectTransform, 0.5f, 1, 0, -60, 900, 90);
            map.InfoLabel = Txt(s, "Info", "", 32, Color.white, TextAnchor.MiddleRight);
            At(map.InfoLabel.rectTransform, 1, 1, -340, -60, 600, 60);
            map.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(map.BackButton), 0, 1, 140, -60, 220, 86);
            var area = NewUI("NodeArea", s);
            Fill(area, 160, 160, 170, 110);
            map.NodeArea = area;
            map.PathTemplate = Img(area, "PathTemplate", "ui.round", new Color(1f, 0.9f, 0.5f));
            At(map.PathTemplate.rectTransform, 0.5f, 0.5f, 0, 0, 100, 12);
            map.NodeTemplate = MakeRow(area, "NodeTemplate", 330, 150, Blue, 110, 0, 28, 22);
            map.PathTemplate.gameObject.SetActive(false);
            map.NodeTemplate.gameObject.SetActive(false);
            PortraitStretch(area, 40, 40, 230, 120);
            Portrait(map.InfoLabel, 0.5f, 1, 0, -150, 900, 50);
            return map;
        }

        private static PrepScreen BuildPrep(Transform root)
        {
            var s = Screen(root, "PrepScreen");
            Fill(Img(s, "Bg", Key("bg.hub", null), art.ContainsKey("bg.hub") ? Color.white : new Color(0.07f, 0.09f, 0.15f)).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.3f)).rectTransform);
            var prep = s.gameObject.AddComponent<PrepScreen>();
            var room = Img(s, "Room", "ui.round", new Color(0.42f, 0.66f, 0.92f, 0.9f), true);
            At(room.rectTransform, 0.5f, 0.5f, 0, -30, 1720, 900);
            prep.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(prep.BackButton), 0, 1, 140, -60, 220, 86);

            var stand = Img(s, "PetStand", "ui.circle", new Color(1f, 1f, 1f, 0.55f));
            At(stand.rectTransform, 0.5f, 0.5f, -560, -45, 340, 80);
            prep.PetImage = Img(s, "PetImage", "emberkit", Color.white);
            prep.PetImage.preserveAspect = true;
            At(prep.PetImage.rectTransform, 0.5f, 0.5f, -560, 120, 440, 380);
            prep.PetName = Txt(s, "PetName", "Emberkit", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(prep.PetName.rectTransform, 0.5f, 0.5f, -560, -140, 500, 50);
            prep.PetName.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.15f, 0.4f);
            prep.ChoosePetButton = Btn(s, "ChoosePetButton", "CHOOSE PET", Blue, 32, out _);
            At(Rt(prep.ChoosePetButton), 0.5f, 0.5f, -630, -225, 280, 84);

            prep.EnemyImage = Img(s, "EnemyImage", "dunewing", Color.white);
            prep.EnemyImage.preserveAspect = true;
            At(prep.EnemyImage.rectTransform, 0.5f, 0.5f, 540, 110, 540, 440);
            prep.EnemyTitle = Txt(s, "EnemyTitle", "", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(prep.EnemyTitle.rectTransform, 0.5f, 0.5f, 540, -150, 640, 52);
            prep.EnemyTitle.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.15f, 0.4f);

            prep.FightButton = Btn(s, "FightButton", "READY", Green, 44, out prep.FightLabel);
            At(Rt(prep.FightButton), 0.5f, 0.5f, 0, 60, 380, 116);

            var row = NewUI("CardRow", s);
            At(row, 0.5f, 0.5f, 0, -320, 900, 200);
            for (int i = 0; i < PrepScreen.MaxCards; i++)
            {
                var slot = Img(row, "Slot" + i, "ui.round", new Color(0.16f, 0.32f, 0.62f), true);
                At(slot.rectTransform, 0.5f, 0.5f, -360 + i * 180, 0, 150, 190);
                var plus = Txt(slot.transform, "Plus", "+", 80, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
                Fill(plus.rectTransform);
                var icon = Img(slot.transform, "Icon", "card.mana_potion", Color.white);
                Fill(icon.rectTransform, 8, 8, 8, 8);
                if (art.ContainsKey("card.slot"))
                {
                    var fr = Img(slot.transform, "Frame", "card.slot", Color.white);
                    Fill(fr.rectTransform, -6, -6, -6, -6);
                    SliceTo(fr, 22);
                }
                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = slot;
                slot.gameObject.AddComponent<ClickSound>();
                var remove = Btn(slot.transform, "Remove", "X", Red, 26, out _);
                At(Rt(remove), 1, 1, -4, -4, 48, 48);
                prep.CardSlots[i] = btn;
                prep.CardSlotIcons[i] = icon;
                prep.CardSlotRemove[i] = remove;
            }

            prep.PetPicker = MakePicker(s, "PetPicker", "CHOOSE PET", out var petContent, out prep.PetPickerClose);
            prep.PetTemplate = MakeRow(petContent, "PetTemplate", 760, 120, Blue, 100, 0, 30, 24);
            prep.PetTemplate.gameObject.SetActive(false);
            prep.CardPicker = MakePicker(s, "CardPicker", "CHOOSE CARDS", out var cardContent, out prep.CardPickerClose);
            prep.CardTemplate = MakeRow(cardContent, "CardTemplate", 760, 110, new Color(0.24f, 0.3f, 0.45f), 90, 1, 28, 20);
            prep.CardTemplate.ExtraALabel.text = "REMOVE";
            prep.CardTemplate.ExtraA.GetComponent<Image>().color = Red;
            prep.CardTemplate.gameObject.SetActive(false);

            Portrait(room, 0.5f, 0.5f, 0, -20, 1040, 1660);
            Portrait(prep.EnemyImage, 0.5f, 0.5f, 0, 520, 540, 440);
            Portrait(prep.EnemyTitle, 0.5f, 0.5f, 0, 260, 640, 52);
            Portrait(stand, 0.5f, 0.5f, -220, -330, 340, 80);
            Portrait(prep.PetImage, 0.5f, 0.5f, -220, -140, 440, 380);
            Portrait(prep.PetName, 0.5f, 0.5f, -220, -400, 440, 50);
            Portrait(prep.ChoosePetButton, 0.5f, 0.5f, 260, -170, 320, 84);
            Portrait(prep.FightButton, 0.5f, 0.5f, 260, -320, 380, 116);
            Portrait(row, 0.5f, 0.5f, 0, -620, 900, 200, 1.05f);
            return prep;
        }

        private static GameObject MakePicker(Transform parent, string name, string title, out RectTransform content, out Button close)
        {
            var dim = Img(parent, name, null, new Color(0, 0, 0, 0.6f), true);
            Fill(dim.rectTransform);
            var panel = Img(dim.transform, "Panel", "ui.round", PanelCol, true);
            At(panel.rectTransform, 0.5f, 0.5f, 0, 0, 860, 860);
            At(Txt(panel.transform, "Title", title, 38, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -44, 700, 56);
            content = ScrollList(panel.transform, "List", out var view);
            At(view, 0.5f, 1, 0, -450, 800, 760);
            close = Btn(panel.transform, "Close", "X", Gray, 40, out _);
            At(Rt(close), 1, 1, -16, -16, 80, 80);
            Portrait(panel, 0.5f, 0.5f, 0, 0, 860, 860, 1.15f);
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        private static BattleController BuildBattle(Transform root, SpriteLibrary lib, GemView gemPrefab)
        {
            var s = Screen(root, "BattleScreen");
            Fill(Img(s, "Bg", Key("bg.battle", null), art.ContainsKey("bg.battle") ? Color.white : new Color(0.09f, 0.13f, 0.21f)).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.12f)).rectTransform);
            var bc = s.gameObject.AddComponent<BattleController>();
            bc.Shake = s.gameObject.AddComponent<ScreenShake>();
            bc.Shake.Target = s;

            var top = Img(s, "TopHud", "ui.round", new Color(0.07f, 0.15f, 0.38f, 0.93f), true);
            At(top.rectTransform, 0.5f, 1, 0, -98, 1120, 176);
            if (art.ContainsKey("hud.bar"))
            {
                var topFrame = Img(top.transform, "Frame", "hud.bar", Color.white);
                Fill(topFrame.rectTransform, -12, -12, -9, -9);
                SliceTo(topFrame, 16);
            }
            bc.TimerLabel = Txt(top.transform, "Timer", "10", 104, new Color(1f, 0.55f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
            At(bc.TimerLabel.rectTransform, 0.5f, 0.5f, 0, -6, 160, 130);
            var timerOutline = bc.TimerLabel.gameObject.AddComponent<Outline>();
            timerOutline.effectColor = new Color(0.25f, 0.05f, 0f, 1f);
            timerOutline.effectDistance = new Vector2(3, -3);

            bc.AutoButton = Btn(s, "AutoButton", "AUTO: OFF", Gray, 24, out bc.AutoLabel);
            At(Rt(bc.AutoButton), 0, 1, 100, -46, 170, 62);
            bc.GiveUpButton = Btn(s, "GiveUpButton", "GIVE UP", Red, 24, out _);
            At(Rt(bc.GiveUpButton), 0, 1, 280, -46, 170, 62);

            bc.PlayerHud = MakeHud(s, top.transform, "PlayerHud", true);
            bc.EnemyHud = MakeHud(s, top.transform, "EnemyHud", false);

            var boardRoot = NewUI("Board", s);
            At(boardRoot, 0.5f, 0.5f, 0, -8, 704, 704);
            bc.BoardGroup = boardRoot.gameObject.AddComponent<CanvasGroup>();
            var frame = Img(boardRoot, "BoardFrame", "ui.round", new Color(0.13f, 0.1f, 0.12f, 0.9f), true);
            At(frame.rectTransform, 0.5f, 0.5f, 0, 0, 700, 700);
            frame.rectTransform.localScale = Vector3.one * 0.92f;
            frame.gameObject.AddComponent<RectMask2D>();
            var cells = NewUI("Cells", frame.transform);
            At(cells, 0.5f, 0.5f, 0, 0, 672, 672);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    bool tile = art.ContainsKey("board.tile");
                    var c = Img(cells, "c" + x + y, Key("board.tile", "ui.round"), tile ? ((x + y) % 2 == 0 ? Color.white : new Color(0.88f, 0.84f, 0.76f)) : ((x + y) % 2 == 0 ? new Color(1, 1, 1, 0.07f) : new Color(1, 1, 1, 0.03f)));
                    At(c.rectTransform, 0.5f, 0.5f, (x - 3.5f) * 84, (y - 3.5f) * 84, 82, 82);
                }
            var gemRoot = NewUI("GemRoot", frame.transform);
            At(gemRoot, 0.5f, 0.5f, 0, 0, 672, 672);
            var sel = art.ContainsKey("board.selected") ? Img(gemRoot, "Selection", "board.selected", Color.white) : Img(gemRoot, "Selection", "ui.frame", Gold);
            At(sel.rectTransform, 0.5f, 0.5f, 0, 0, 84, 84);
            sel.gameObject.SetActive(false);
            var board = frame.gameObject.AddComponent<BoardView>();
            board.GemRoot = gemRoot;
            board.GemPrefab = gemPrefab;
            board.Selection = sel.rectTransform;
            board.CellSize = 84;
            board.Sprites = lib;
            bc.Board = board;
            if (art.ContainsKey("board.frame"))
            {
                var frameArt = Img(boardRoot, "BoardFrameArt", "board.frame", Color.white);
                At(frameArt.rectTransform, 0.5f, 0.5f, 0, 0, 704, 704);
                SliceTo(frameArt, 34);
            }

            var bar = NewUI("CardBar", s);
            At(bar, 0.5f, 0.5f, 0, -448, 700, 170);
            for (int i = 0; i < 5; i++)
            {
                bc.CardButtons[i] = MakeCard(bar, "Card" + i);
                At((RectTransform)bc.CardButtons[i].transform, 0.5f, 0.5f, -260 + i * 130, 0, 120, 150);
            }
            var skills = new RectTransform[2];
            for (int i = 0; i < 2; i++)
            {
                bc.SkillButtons[i] = MakeSkill(s, "Skill" + i);
                skills[i] = (RectTransform)bc.SkillButtons[i].transform;
                At(skills[i], 0.5f, 0.5f, 720, 250 - i * 160, 116, 116);
            }

            var sum = NewUI("GemSummary", s);
            At(sum, 0.5f, 1, 0, -290, 900, 190);
            var gs = sum.gameObject.AddComponent<GemSummaryView>();
            gs.Group = sum.gameObject.AddComponent<CanvasGroup>();
            gs.Group.blocksRaycasts = false;
            gs.Sprites = lib;
            gs.SlotSpacing = 132f;
            for (int i = 0; i < 6; i++)
            {
                gs.SlotIcons[i] = Img(sum, "Slot" + i, "gem.Sword", Color.white);
                gs.SlotIcons[i].preserveAspect = true;
                At(gs.SlotIcons[i].rectTransform, 0.5f, 0.5f, -330 + i * 132, 22, 112, 112);
                gs.SlotLabels[i] = Txt(gs.SlotIcons[i].transform, "Count", "3", 46, new Color(0.95f, 0.18f, 0.12f), TextAnchor.UpperCenter, FontStyle.Bold);
                At(gs.SlotLabels[i].rectTransform, 0.5f, 0, 0, -36, 160, 64);
                var o = gs.SlotLabels[i].gameObject.AddComponent<Outline>();
                o.effectColor = Color.white;
                o.effectDistance = new Vector2(2, -2);
            }
            bc.Summary = gs;

            var pop = Txt(s, "TurnPop", "YOUR TURN", 96, new Color(1f, 0.82f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
            At(pop.rectTransform, 0.5f, 0.5f, 0, -8, 900, 140);
            var popOutline = pop.gameObject.AddComponent<Outline>();
            popOutline.effectColor = new Color(0.55f, 0.08f, 0.02f, 1f);
            popOutline.effectDistance = new Vector2(4, -4);
            bc.TurnPop = pop.gameObject.AddComponent<CanvasGroup>();
            bc.TurnPop.blocksRaycasts = false;
            bc.TurnPop.alpha = 0f;

            var fl = NewUI("FloatingText", s);
            Fill(fl);
            var flv = fl.gameObject.AddComponent<FloatingTextLayer>();
            flv.Template = Txt(fl, "Template", "-123", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(flv.Template.rectTransform, 0.5f, 0.5f, 0, 0, 700, 80);
            flv.Template.gameObject.SetActive(false);
            bc.Floating = flv;

            var banner = Img(s, "Banner", null, new Color(0, 0, 0, 0.6f));
            At(banner.rectTransform, 0.5f, 0.5f, 0, 40, 4000, 150);
            bc.Banner = banner.gameObject.AddComponent<CanvasGroup>();
            bc.Banner.blocksRaycasts = false;
            bc.Banner.alpha = 0f;
            bc.BannerLabel = Txt(banner.transform, "Label", "FIGHT!", 76, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(bc.BannerLabel.rectTransform, 0.5f, 0.5f, 0, 0, 1600, 140);

            bc.Qte = BuildQte(s);
            bc.Log = BuildLog(s);
            Portrait(top, 0.5f, 1, 0, -190, 1120, 176, 0.94f);
            Portrait(bc.PlayerHud, 0.5f, 0.5f, -265, 470, 520, 500, 0.78f);
            Portrait(bc.EnemyHud, 0.5f, 0.5f, 265, 470, 520, 500, 0.78f);
            Portrait(bc.PlayerHud.ElementIcon, 0, 1, 64, -330, 96, 96);
            Portrait(bc.EnemyHud.ElementIcon, 1, 1, -64, -330, 96, 96);
            Portrait(boardRoot, 0.5f, 0.5f, 0, -255, 704, 704, 1.38f);
            Portrait(bar, 0.5f, 0, -60, 120, 700, 170);
            Portrait(skills[0], 0.5f, 0, 330, 120, 116, 116);
            Portrait(skills[1], 0.5f, 0, 460, 120, 116, 116);
            Portrait(sum, 0.5f, 0.5f, 0, -255, 900, 190, 1.2f);
            Portrait(pop, 0.5f, 0.5f, 0, -255, 900, 140);
            Portrait(banner, 0.5f, 0.5f, 0, -255, 4000, 150);
            return bc;
        }

        private static QteView BuildQte(Transform parent)
        {
            var holder = NewUI("Qte", parent);
            Fill(holder);
            var qv = holder.gameObject.AddComponent<QteView>();
            var dim = Img(holder, "Root", null, new Color(0, 0, 0, 0.65f), true);
            Fill(dim.rectTransform);
            qv.Root = dim.gameObject;
            var panel = Img(dim.transform, "Panel", "ui.round", new Color(0.12f, 0.14f, 0.24f, 0.97f), true);
            At(panel.rectTransform, 0.5f, 0.5f, 0, 0, 1200, 660);
            var p = panel.transform;
            qv.Title = Txt(p, "Title", "Skill", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(qv.Title.rectTransform, 0.5f, 0.5f, 0, 270, 1100, 60);
            qv.DamageLabel = Txt(p, "Damage", "DMG 0", 60, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(qv.DamageLabel.rectTransform, 0.5f, 0.5f, 0, 195, 800, 76);
            for (int i = 0; i < 5; i++)
            {
                qv.Arrows[i] = Img(p, "Arrow" + i, "ui.arrow", Color.white);
                At(qv.Arrows[i].rectTransform, 0.5f, 0.5f, -320 + i * 160, 75, 110, 110);
            }
            var tbg = Img(p, "TimerBg", "ui.round", new Color(1, 1, 1, 0.15f));
            At(tbg.rectTransform, 0.5f, 0.5f, 0, -10, 820, 18);
            qv.TimerFill = Img(tbg.transform, "Fill", null, Gold);
            qv.TimerFill.type = Image.Type.Filled;
            qv.TimerFill.fillMethod = Image.FillMethod.Horizontal;
            Fill(qv.TimerFill.rectTransform);
            var barArea = Img(p, "BarArea", "ui.round", new Color(0.25f, 0.25f, 0.32f));
            At(barArea.rectTransform, 0.5f, 0.5f, 0, -80, 820, 46);
            qv.BarArea = barArea.rectTransform;
            var good = Img(barArea.transform, "GoodZone", null, new Color(0.3f, 0.85f, 0.4f, 0.85f));
            At(good.rectTransform, 0.5f, 0.5f, 0, 0, 130, 46);
            qv.GoodZone = good.rectTransform;
            var perfect = Img(barArea.transform, "PerfectZone", null, Gold);
            At(perfect.rectTransform, 0.5f, 0.5f, 0, 0, 40, 46);
            qv.PerfectZone = perfect.rectTransform;
            var marker = Img(barArea.transform, "Marker", null, Color.white);
            At(marker.rectTransform, 0.5f, 0.5f, 0, 0, 10, 70);
            qv.Marker = marker.rectTransform;
            qv.Feedback = Txt(p, "Feedback", "", 56, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(qv.Feedback.rectTransform, 0.5f, 0.5f, 0, -170, 560, 70);
            qv.Hint = Txt(p, "Hint", "", 24, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleCenter);
            At(qv.Hint.rectTransform, 0.5f, 0.5f, 0, -280, 1100, 40);
            qv.Up = ArrowButton(p, "Up", -440, -140, 0);
            qv.Down = ArrowButton(p, "Down", -440, -250, 180);
            qv.Left = ArrowButton(p, "Left", -545, -195, 90);
            qv.Right = ArrowButton(p, "Right", -335, -195, -90);
            qv.Strike = Btn(p, "Strike", "STRIKE", Red, 40, out _);
            At(Rt(qv.Strike), 0.5f, 0.5f, 440, -195, 240, 120);
            dim.gameObject.SetActive(false);
            Portrait(panel, 0.5f, 0.5f, 0, 0, 1200, 660, 0.86f);
            return qv;
        }

        private static Button ArrowButton(Transform p, string name, float x, float y, float rot)
        {
            var b = Btn(p, name, "", Blue, 10, out _);
            At(Rt(b), 0.5f, 0.5f, x, y, 96, 96);
            var a = Img(b.transform, "Arrow", "ui.arrow", Color.white);
            At(a.rectTransform, 0.5f, 0.5f, 0, 0, 64, 64);
            a.rectTransform.localEulerAngles = new Vector3(0, 0, rot);
            return b;
        }

        private static EventLogView BuildLog(Transform parent)
        {
            var holder = NewUI("EventLog", parent);
            Fill(holder);
            var lv = holder.gameObject.AddComponent<EventLogView>();
            lv.Toggle = Btn(holder, "LogButton", "LOG", Gray, 26, out _);
            At(Rt(lv.Toggle), 1, 1, -110, -46, 180, 66);
            var panel = Img(holder, "Panel", "ui.round", new Color(0, 0, 0, 0.88f), true);
            At(panel.rectTransform, 1, 0.5f, -500, 20, 960, 820);
            panel.gameObject.AddComponent<RectMask2D>();
            var sr = panel.gameObject.AddComponent<ScrollRect>();
            var content = NewUI("Content", panel.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-24, 0);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var text = content.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 17;
            text.color = new Color(0.85f, 0.95f, 0.85f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            sr.content = content;
            sr.viewport = panel.rectTransform;
            sr.horizontal = false;
            sr.scrollSensitivity = 30;
            lv.Panel = panel.gameObject;
            lv.Content = text;
            lv.Scroll = sr;
            panel.gameObject.SetActive(false);
            Portrait(panel, 0.5f, 0.5f, 0, -100, 1000, 1300);
            return lv;
        }

        private static ResultScreen BuildResult(Transform root)
        {
            var s = Screen(root, "ResultScreen");
            Fill(Img(s, "Dim", null, new Color(0f, 0f, 0f, 0.45f), true).rectTransform);
            var rs = s.gameObject.AddComponent<ResultScreen>();
            var panel = Img(s, "Panel", "ui.round", new Color(1f, 0.82f, 0.25f), true);
            At(panel.rectTransform, 0.5f, 0.5f, 0, -20, 940, 500);
            rs.Panel = panel.rectTransform;
            var glow = Img(panel.transform, "Glow", "ui.gradient", new Color(1f, 1f, 0.85f, 0.75f));
            Fill(glow.rectTransform, 10, 10, 10, 10);
            var pill = Img(panel.transform, "TitlePill", "ui.round", new Color(0.6f, 0.25f, 0.85f), true);
            At(pill.rectTransform, 0.5f, 1, 0, 0, 340, 76);
            rs.Title = Txt(pill.transform, "Title", "VICTORY", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(rs.Title.rectTransform);
            var titleOutline = rs.Title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.3f, 0.05f, 0.4f, 1f);
            rs.Lines = Txt(panel.transform, "Lines", "", 34, new Color(0.55f, 0.2f, 0.75f), TextAnchor.UpperCenter, FontStyle.Bold);
            At(rs.Lines.rectTransform, 0.5f, 0.5f, 0, -30, 860, 360);
            rs.CloseButton = Btn(panel.transform, "CloseButton", "X", Gray, 40, out _);
            At(Rt(rs.CloseButton), 1, 1, -20, -20, 84, 84);
            Portrait(panel, 0.5f, 0.5f, 0, -255, 940, 500, 1.05f);
            return rs;
        }

        private static UpgradeScreen BuildUpgrade(Transform root)
        {
            var s = Screen(root, "UpgradeScreen");
            Fill(Img(s, "Bg", null, new Color(0.08f, 0.08f, 0.14f)).rectTransform);
            Fill(Img(s, "Glow", "ui.gradient", new Color(0.55f, 0.35f, 0.75f, 0.3f)).rectTransform);
            var up = s.gameObject.AddComponent<UpgradeScreen>();
            At(Txt(s, "Header", "PETS & STONES", 56, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -60, 900, 80);
            up.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(up.BackButton), 0, 1, 140, -60, 220, 86);

            var lp = Img(s, "PetPanel", "ui.round", PanelCol);
            At(lp.rectTransform, 0.5f, 0.5f, -680, -40, 500, 860);
            At(Txt(lp.transform, "Label", "PETS", 34, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -40, 460, 50);
            var petContent = ScrollList(lp.transform, "PetList", out var petView);
            At(petView, 0.5f, 1, 0, -440, 480, 780);
            up.PetTemplate = MakeRow(petContent, "PetTemplate", 450, 110, Blue, 90, 0, 28, 22);

            var cp = Img(s, "DetailPanel", "ui.round", PanelCol);
            At(cp.rectTransform, 0.5f, 0.5f, -110, -40, 600, 860);
            up.PetImage = Img(cp.transform, "PetImage", "emberkit", Color.white);
            up.PetImage.preserveAspect = true;
            At(up.PetImage.rectTransform, 0.5f, 1, 0, -170, 280, 280);
            up.PetDetails = Txt(cp.transform, "PetDetails", "", 24, Color.white, TextAnchor.UpperLeft);
            At(up.PetDetails.rectTransform, 0.5f, 1, 0, -440, 560, 200);
            up.LuckyToggle = MakeToggle(cp.transform, "LuckyToggle", "Use Lucky Charm (+chance)");
            At((RectTransform)up.LuckyToggle.transform, 0.5f, 0, 0, 290, 540, 56);
            up.ProtectToggle = MakeToggle(cp.transform, "ProtectToggle", "Use Protection (no downgrade)");
            At((RectTransform)up.ProtectToggle.transform, 0.5f, 0, 0, 225, 540, 56);
            up.WalletLabel = Txt(cp.transform, "Wallet", "", 26, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(up.WalletLabel.rectTransform, 0.5f, 0, 0, 160, 560, 44);
            up.MessageLabel = Txt(cp.transform, "Message", "", 24, new Color(0.8f, 0.95f, 1f), TextAnchor.MiddleCenter);
            At(up.MessageLabel.rectTransform, 0.5f, 0, 0, 75, 560, 110);

            var rp = Img(s, "StonePanel", "ui.round", PanelCol);
            At(rp.rectTransform, 0.5f, 0.5f, 560, -40, 720, 860);
            At(Txt(rp.transform, "Label", "STONES", 34, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -40, 680, 50);
            var stoneContent = ScrollList(rp.transform, "StoneList", out var stoneView);
            At(stoneView, 0.5f, 1, 0, -440, 700, 780);
            up.StoneTemplate = MakeRow(stoneContent, "StoneTemplate", 670, 104, new Color(0.24f, 0.26f, 0.36f), 64, 3, 24, 18);

            up.PetTemplate.gameObject.SetActive(false);
            up.StoneTemplate.gameObject.SetActive(false);
            Portrait(lp, 0.5f, 0.5f, -305, 449, 500, 860, 0.86f);
            Portrait(cp, 0.5f, 0.5f, 262, 449, 600, 860, 0.86f);
            Portrait(rp, 0.5f, 0.5f, 0, -470, 720, 860, 1.0f);
            return up;
        }

        private static VfxLayer BuildVfx(Transform root)
        {
            var rt = Screen(root, "VfxLayer");
            var v = rt.gameObject.AddComponent<VfxLayer>();
            v.Flash = Img(rt, "Flash", null, new Color(1, 1, 1, 0));
            Fill(v.Flash.rectTransform);
            v.Template = Img(rt, "ParticleTemplate", "fx.dot", Color.white);
            v.Template.preserveAspect = true;
            At(v.Template.rectTransform, 0.5f, 0.5f, 0, 0, 22, 22);
            v.Template.gameObject.SetActive(false);
            v.Dot = art["fx.dot"];
            v.RingSprite = art["ui.ring"];
            v.Star = art["fx.star"];
            return v;
        }

        private static ToastView BuildToast(Transform root)
        {
            var bg = Img(root, "Toast", "ui.round", new Color(0, 0, 0, 0.82f));
            At(bg.rectTransform, 0.5f, 0.5f, 0, -430, 900, 84);
            var tv = bg.gameObject.AddComponent<ToastView>();
            tv.Group = bg.gameObject.AddComponent<CanvasGroup>();
            tv.Group.blocksRaycasts = false;
            tv.Group.interactable = false;
            tv.Group.alpha = 0f;
            tv.Label = Txt(bg.transform, "Label", "", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(tv.Label.rectTransform, 10, 10, 4, 4);
            return tv;
        }

        private static CombatantHud MakeHud(Transform screen, Transform top, string name, bool player)
        {
            float side = player ? -1f : 1f;
            var strip = Img(top, name + "Strip", "ui.round", new Color(1f, 1f, 1f, 0f), true);
            At(strip.rectTransform, 0.5f, 0.5f, side * 300, 54, 440, 46);
            var root = NewUI(name, screen);
            At(root, 0.5f, 0.5f, side * 600, -90, 520, 500);
            var hud = root.gameObject.AddComponent<CombatantHud>();
            hud.NameStrip = strip;
            hud.NameLabel = Txt(strip.transform, "Name", "Name", 32, Color.white, player ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, FontStyle.Bold);
            Fill(hud.NameLabel.rectTransform, 16, 16, 0, 0);
            var nameOutline = hud.NameLabel.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = new Color(0.05f, 0.1f, 0.3f, 1f);
            hud.Hp = MakeThinBar(top, name + "Hp", new Color(0.86f, 0.18f, 0.22f), side * 300, 12, player);
            hud.Mana = MakeThinBar(top, name + "Mana", new Color(0.2f, 0.62f, 1f), side * 300, -22, player);
            hud.Rage = MakeThinBar(top, name + "Rage", new Color(0.95f, 0.78f, 0.15f), side * 300, -56, player);

            const float feet = -230f;
            var marker = Img(root, "TurnMarker", "ui.ring", new Color(1f, 0.85f, 0.3f, 0.9f));
            At(marker.rectTransform, 0.5f, 0.5f, 0, feet, 380, 84);
            hud.TurnMarker = marker.gameObject;
            var arrow = Img(root, "AttackArrow", "battle.arrow", player ? Color.white : new Color(1f, 0.55f, 0.5f));
            arrow.preserveAspect = true;
            At(arrow.rectTransform, 0.5f, 0.5f, -side * 200, feet + 20, 130, 130);
            if (!player) arrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            hud.AttackArrow = arrow.rectTransform;
            arrow.gameObject.SetActive(false);
            hud.Portrait = Img(root, "Portrait", "emberkit", Color.white);
            hud.Portrait.preserveAspect = true;
            At(hud.Portrait.rectTransform, 0.5f, 0.5f, 0, feet, 470, 350);
            hud.Portrait.rectTransform.pivot = new Vector2(0.5f, 0.05f);
            hud.Portrait.rectTransform.anchoredPosition = new Vector2(0, feet);
            hud.ShieldBubble = Img(hud.Portrait.transform, "ShieldBubble", "fx.bubble", Color.white);
            At(hud.ShieldBubble.rectTransform, 0.5f, 0.5f, 0, -8, 440, 440);
            hud.ShieldBubble.raycastTarget = false;
            hud.ShieldBubble.gameObject.SetActive(false);
            var badge = Img(hud.Portrait.transform, "ShieldBadge", "ui.round", new Color(0.08f, 0.1f, 0.22f, 0.8f));
            At(badge.rectTransform, 0.5f, 1, 0, 30, 170, 52);
            var badgeIcon = Img(badge.transform, "Icon", "gem.Shield", Color.white);
            badgeIcon.preserveAspect = true;
            At(badgeIcon.rectTransform, 0, 0.5f, 28, 0, 46, 46);
            hud.ShieldLabel = Txt(badge.transform, "Value", "0", 32, new Color(0.75f, 0.92f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            At(hud.ShieldLabel.rectTransform, 0.5f, 0.5f, 22, 0, 120, 48);
            hud.ShieldBadge = badge.gameObject;
            hud.ShieldBadge.SetActive(false);
            hud.FloatAnchor = NewUI("FloatAnchor", root);
            At(hud.FloatAnchor, 0.5f, 0.5f, 0, feet + 230, 10, 10);
            hud.StatusLabel = Txt(root, "Status", "", 22, new Color(0.7f, 1f, 0.8f), TextAnchor.UpperCenter);
            At(hud.StatusLabel.rectTransform, 0.5f, 0.5f, 0, feet - 50, 520, 40);
            hud.ElementIcon = Img(screen, name + "Element", "element.Fire", Color.white);
            hud.ElementIcon.preserveAspect = true;
            At(hud.ElementIcon.rectTransform, player ? 0 : 1, 0, player ? 72 : -72, 72, 104, 104);
            return hud;
        }

        private static BarView MakeThinBar(Transform parent, string name, Color c, float x, float y, bool leftToRight)
        {
            var bg = Img(parent, name, "ui.round", new Color(0.02f, 0.05f, 0.16f, 0.85f));
            At(bg.rectTransform, 0.5f, 0.5f, x, y, 440, 28);
            var bv = bg.gameObject.AddComponent<BarView>();
            bv.Fill = Img(bg.transform, "Fill", "ui.round", c);
            bv.Fill.type = Image.Type.Filled;
            bv.Fill.fillMethod = Image.FillMethod.Horizontal;
            bv.Fill.fillOrigin = leftToRight ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
            Fill(bv.Fill.rectTransform, 2, 2, 2, 2);
            bv.Label = Txt(bg.transform, "Label", "0/0", 20, Color.white, leftToRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, FontStyle.Bold);
            Fill(bv.Label.rectTransform, 12, 12, 0, 0);
            var o = bv.Label.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.8f);
            bv.Prefix = "";
            return bv;
        }

        private static ActionButtonView MakeCard(Transform parent, string name)
        {
            var bg = Img(parent, name, "ui.round", new Color(0.15f, 0.18f, 0.28f), true);
            var v = bg.gameObject.AddComponent<ActionButtonView>();
            v.Button = bg.gameObject.AddComponent<Button>();
            v.Button.targetGraphic = bg;
            bg.gameObject.AddComponent<ClickSound>();
            v.Group = bg.gameObject.AddComponent<CanvasGroup>();
            v.Icon = Img(bg.transform, "Icon", "card.mana_potion", Color.white);
            v.Icon.preserveAspect = false;
            Fill(v.Icon.rectTransform, 6, 6, 6, 6);
            bool slot = art.ContainsKey("card.slot");
            v.Frame = Img(bg.transform, "Frame", slot ? "card.slot" : "ui.frame", slot ? Color.white : new Color(0.35f, 0.55f, 0.85f));
            Fill(v.Frame.rectTransform, -6, -6, -6, -6);
            if (slot) SliceTo(v.Frame, 22);
            var costBg = Img(bg.transform, "CostBg", "ui.round", new Color(0f, 0f, 0f, 0.7f));
            At(costBg.rectTransform, 0.5f, 0, 0, 20, 104, 30);
            v.Cost = Txt(costBg.transform, "Cost", "0", 19, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(v.Cost.rectTransform);
            return v;
        }

        private static ActionButtonView MakeSkill(Transform parent, string name)
        {
            var bg = Img(parent, name, "ui.circle", new Color(0.1f, 0.18f, 0.4f, 0.95f));
            var v = bg.gameObject.AddComponent<ActionButtonView>();
            v.Button = bg.gameObject.AddComponent<Button>();
            v.Button.targetGraphic = bg;
            bg.gameObject.AddComponent<ClickSound>();
            v.Group = bg.gameObject.AddComponent<CanvasGroup>();
            v.Icon = Img(bg.transform, "Icon", "skill.blaze_burst", Color.white);
            v.Icon.preserveAspect = true;
            Fill(v.Icon.rectTransform, 14, 14, 14, 14);
            v.Frame = Img(bg.transform, "Ring", "ui.ring", new Color(1f, 0.8f, 0.3f));
            Fill(v.Frame.rectTransform, -4, -4, -4, -4);
            v.Cost = Txt(bg.transform, "Cost", "0", 22, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            At(v.Cost.rectTransform, 0.5f, 0, 0, -12, 160, 30);
            var o = v.Cost.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            return v;
        }

        private static RowView MakeRow(Transform parent, string name, float w, float h, Color bgColor, float icon, int extras, int titleSize, int subSize)
        {
            var bg = Img(parent, name, "ui.round", bgColor, true);
            At(bg.rectTransform, 0.5f, 0.5f, 0, 0, w, h);
            var row = bg.gameObject.AddComponent<RowView>();
            row.Background = bg;
            row.Button = bg.gameObject.AddComponent<Button>();
            row.Button.targetGraphic = bg;
            bg.gameObject.AddComponent<ClickSound>();
            var le = bg.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            var hl = Img(bg.transform, "Highlight", "ui.frame", Gold);
            Fill(hl.rectTransform, -5, -5, -5, -5);
            row.Highlight = hl.gameObject;
            row.Icon = Img(bg.transform, "Icon", null, Color.white);
            row.Icon.preserveAspect = true;
            At(row.Icon.rectTransform, 0, 0.5f, 14 + icon * 0.5f, 0, icon, icon);
            float right = extras * 112 + 10;
            row.Title = Txt(bg.transform, "Title", "Title", titleSize, Color.white, TextAnchor.LowerLeft, FontStyle.Bold);
            var tr = row.Title.rectTransform;
            tr.anchorMin = new Vector2(0, 0.5f);
            tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = new Vector2(icon + 28, 0);
            tr.offsetMax = new Vector2(-right, -6);
            row.Subtitle = Txt(bg.transform, "Subtitle", "", subSize, new Color(0.88f, 0.9f, 1f), TextAnchor.UpperLeft);
            var sr = row.Subtitle.rectTransform;
            sr.anchorMin = new Vector2(0, 0);
            sr.anchorMax = new Vector2(1, 0.5f);
            sr.offsetMin = new Vector2(icon + 28, 4);
            sr.offsetMax = new Vector2(-right, 0);
            var cols = new[] { new Color(0.3f, 0.55f, 0.9f), Green, new Color(0.65f, 0.45f, 0.2f) };
            for (int i = 0; i < extras; i++)
            {
                var b = Btn(bg.transform, "Extra" + i, "", cols[i], 18, out var label);
                At(Rt(b), 1, 0.5f, -(extras - i) * 112 + 48, 0, 104, h - 22);
                if (i == 0) { row.ExtraA = b; row.ExtraALabel = label; }
                if (i == 1) { row.ExtraB = b; row.ExtraBLabel = label; }
                if (i == 2) { row.ExtraC = b; row.ExtraCLabel = label; }
            }
            return row;
        }

        private static Toggle MakeToggle(Transform parent, string name, string label)
        {
            var root = NewUI(name, parent);
            var t = root.gameObject.AddComponent<Toggle>();
            var box = Img(root, "Box", "ui.round", new Color(0.9f, 0.9f, 0.95f), true);
            At(box.rectTransform, 0, 0.5f, 28, 0, 46, 46);
            var check = Img(box.transform, "Check", "ui.round", Green);
            At(check.rectTransform, 0.5f, 0.5f, 0, 0, 28, 28);
            var text = Txt(root, "Label", label, 26, Color.white, TextAnchor.MiddleLeft);
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(66, 0);
            rt.offsetMax = Vector2.zero;
            t.targetGraphic = box;
            root.gameObject.AddComponent<ClickSound>();
            t.graphic = check;
            t.isOn = false;
            return t;
        }

        private static RectTransform ScrollList(Transform parent, string name, out RectTransform view)
        {
            var v = Img(parent, name, "ui.round", new Color(0, 0, 0, 0.18f), true);
            v.gameObject.AddComponent<RectMask2D>();
            var sr = v.gameObject.AddComponent<ScrollRect>();
            var content = NewUI("Content", v.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 12, 12);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.viewport = v.rectTransform;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30;
            view = v.rectTransform;
            return content;
        }

        private static RectTransform Screen(Transform root, string name)
        {
            var rt = NewUI(name, root);
            Fill(rt);
            return rt;
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Rt(Component c) => (RectTransform)c.transform;

        private static string Key(string preferred, string fallback) => art.ContainsKey(preferred) ? preferred : fallback;

        private static OrientationLayout Dual(Component c)
        {
            var rt = (RectTransform)c.transform;
            var ol = rt.GetComponent<OrientationLayout>();
            if (ol == null) ol = rt.gameObject.AddComponent<OrientationLayout>();
            ol.Landscape = RectState.Capture(rt);
            ol.Portrait = ol.Landscape;
            return ol;
        }

        private static void Portrait(Component c, float ax, float ay, float x, float y, float w, float h, float scale = 1f, float px = 0.5f, float py = 0.5f)
        {
            Dual(c).Portrait = new RectState
            {
                AnchorMin = new Vector2(ax, ay),
                AnchorMax = new Vector2(ax, ay),
                Pivot = new Vector2(px, py),
                Position = new Vector2(x, y),
                Size = new Vector2(w, h),
                Scale = Vector3.one * scale
            };
        }

        private static void PortraitStretch(Component c, float l, float r, float t, float b)
        {
            Dual(c).Portrait = new RectState
            {
                AnchorMin = Vector2.zero,
                AnchorMax = Vector2.one,
                Pivot = new Vector2(0.5f, 0.5f),
                Position = new Vector2((l - r) * 0.5f, (b - t) * 0.5f),
                Size = new Vector2(-(l + r), -(t + b)),
                Scale = Vector3.one
            };
        }


        private static void SliceTo(Image img, float borderPx)
        {
            if (img.sprite == null) return;
            var b = img.sprite.border;
            float m = Mathf.Max(Mathf.Max(b.x, b.z), Mathf.Max(b.y, b.w));
            if (m <= 0f) return;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = m / borderPx;
        }

        private static RectTransform At(RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static RectTransform Fill(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        private static Image Img(Transform parent, string name, string spriteKey, Color c, bool raycast = false)
        {
            var rt = NewUI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            if (spriteKey != null && art.TryGetValue(spriteKey, out var sp)) img.sprite = sp;
            if (img.sprite != null && img.sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            img.color = c;
            img.raycastTarget = raycast;
            return img;
        }

        private static Text Txt(Transform parent, string name, string text, int size, Color c, TextAnchor align, FontStyle style = FontStyle.Normal)
        {
            var rt = NewUI(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.6f);
            sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        private static Button Btn(Transform parent, string name, string label, Color c, int fontSize, out Text text)
        {
            bool green = c == Green && art.ContainsKey("ui.button.green");
            var img = Img(parent, name, green ? "ui.button.green" : "ui.round", green ? Color.white : c, true);
            if (green) SliceTo(img, 34);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            img.gameObject.AddComponent<ClickSound>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            b.colors = colors;
            text = Txt(img.transform, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(text.rectTransform, 6, 6, 4, 4);
            return b;
        }
    }
}
