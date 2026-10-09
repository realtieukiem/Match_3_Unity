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
        private static Font boldFont;
        private static Dictionary<string, Sprite> art;

        private static readonly Color Gold = new Color(1f, 0.83f, 0.32f);
        private static readonly Color PanelCol = new Color(0.11f, 0.14f, 0.22f, 0.93f);
        private static readonly Color Blue = new Color(0.22f, 0.45f, 0.82f);
        private static readonly Color Green = new Color(0.22f, 0.62f, 0.3f);
        private static readonly Color Red = new Color(0.72f, 0.24f, 0.24f);
        private static readonly Color Gray = new Color(0.35f, 0.37f, 0.42f);
        private static readonly Color RowCol = new Color(0.2f, 0.26f, 0.42f);

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
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Pokiwar/Fonts/Baloo2-Medium.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            boldFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Pokiwar/Fonts/Baloo2-ExtraBold.ttf") ?? font;
            art = PlaceholderArt.BuildAll();
            Fg38Art.Apply(art);
            var catalog = PokiwarContentSeeder.Seed(false);
            var lib = PokiwarContentSeeder.BuildSprites(art);
            var gem = BuildGemPrefab();
            var clips = PokiwarAudioBuilder.BuildAll();
            if (overwriteScene || !File.Exists(ScenePath)) BuildScene(catalog, lib, gem, clips);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath && File.Exists(s.path)) scenes.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
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
            var badge = Img(root, "Badge", "fx.burst", Color.white);
            At(badge.rectTransform, 1, 1, -13, -13, 46, 46);
            badge.raycastTarget = false;
            gv.MultiplierBadge = badge.gameObject;
            gv.MultiplierLabel = Txt(badge.transform, "Label", "x2", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            Fill(gv.MultiplierLabel.rectTransform, -8, -8, 0, 0);
            var badgeOutline = gv.MultiplierLabel.gameObject.AddComponent<Outline>();
            badgeOutline.effectColor = new Color(0.3f, 0.05f, 0.02f, 1f);
            badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);
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

            var home = BuildHome(root);
            var world = BuildWorld(root);
            var hub = BuildHub(root);
            var map = BuildMap(root);
            var prep = BuildPrep(root);
            var battle = BuildBattle(root, lib, gemPrefab);
            var result = BuildResult(root);
            var upgrade = BuildUpgrade(root);
            var wardrobe = BuildWardrobe(root);
            var forge = BuildCardForge(root);
            var shop = BuildShop(root);
            BuildVfx(root);
            var toast = BuildToast(root);
            Portrait(toast, 0.5f, 0, 0, 250, 900, 84);
            canvasGo.AddComponent<ResponsiveCanvas>().Force = ResponsiveCanvas.Mode.Landscape;

            var appGo = new GameObject("PokiwarApp");
            var app = appGo.AddComponent<GameApp>();
            app.Catalog = catalog;
            app.Sprites = lib;
            app.Home = home;
            app.World = world;
            app.Hub = hub;
            app.Map = map;
            app.Prep = prep;
            app.BattleScreen = battle.gameObject;
            app.Battle = battle;
            app.Result = result;
            app.Upgrade = upgrade;
            app.Wardrobe = wardrobe;
            app.CardForge = forge;
            app.ShopScreen = shop;
            app.ToastView = toast;
            var audio = new GameObject("Audio").AddComponent<AudioDirector>();
            audio.transform.SetParent(appGo.transform, false);
            audio.Clips = clips;
            app.Audio = audio;

            home.gameObject.SetActive(true);
            world.gameObject.SetActive(false);
            hub.gameObject.SetActive(false);
            map.gameObject.SetActive(false);
            prep.gameObject.SetActive(false);
            battle.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            upgrade.gameObject.SetActive(false);
            wardrobe.gameObject.SetActive(false);
            forge.gameObject.SetActive(false);
            shop.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static HomeScreen BuildHome(Transform root)
        {
            var s = Screen(root, "HomeScreen");
            Fill(Img(s, "Bg", null, new Color(0.03f, 0.08f, 0.2f)).rectTransform);
            var home = s.gameObject.AddComponent<HomeScreen>();
            var fitter = NewUI("Map", s).gameObject.AddComponent<MapFitter>();
            Fill((RectTransform)fitter.transform);
            var town = NewUI("Town", fitter.transform);
            At(town, 0.5f, 0.5f, 0, 0, 1920, 1080);
            Fill(Img(town, "Art", Key("bg.home", Key("bg.lobby", "ui.map")), Color.white).rectTransform);
            fitter.Content = town;
            var bar = Img(s, "TopBar", null, new Color(0.03f, 0.08f, 0.2f, 0.6f));
            At(bar.rectTransform, 0.5f, 1, 0, -50, 4000, 100);
            home.PlayerLabel = Txt(s, "PlayerLabel", "", 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(home.PlayerLabel.rectTransform, 0, 1, 440, -50, 800, 60);
            home.PlayerLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.1f, 0.25f);
            var info = NewUI("Resources", s);
            At(info, 1, 1, -365, -50, 690, 64);
            home.RankLabel = Chip(info, "Rank", Key("home.rank", "fx.star"), art.ContainsKey("home.rank") ? Color.white : Gold, -230, 210);
            home.GoldLabel = Chip(info, "Gold", Key("icon.gold", "ui.circle"), art.ContainsKey("icon.gold") ? Color.white : Gold, 0, 210);
            home.EnergyLabel = Chip(info, "Energy", "gem.Lightning", Color.white, 230, 210);

            var soon = new List<Button>();
            soon.Add(HomeSpot(town, "RankSpot", "RANKING", "home.rank", Gold, -785, 362, 130));
            soon.Add(HomeSpot(town, "WheelSpot", "LUCKY WHEEL", "home.wheel", Gold, -730, 178, 140));
            home.CardShopButton = HomeSpot(town, "CardShopSpot", "CARD SHOP", "home.shopcard", Blue, -790, -25, 160);
            home.AvatarShopButton = HomeSpot(town, "AvatarShopSpot", "AVATAR SHOP", "home.shopavatar", Blue, -480, -130, 200);
            soon.Add(HomeSpot(town, "GiftSpot", "GIFT SHOP", "home.gift", Green, -260, 305, 170));
            soon.Add(HomeSpot(town, "ArenaSpot", "ARENA", "home.arena", Red, -200, -15, 260));
            home.HuntButton = HomeSpot(town, "HuntSpot", "BOSS HUNT", "home.hunt", Green, 300, 225, 230);
            soon.Add(HomeSpot(town, "ChallengeSpot", "CHALLENGE", "home.challenge", Red, 480, 35, 200));
            home.EvolveButton = HomeSpot(town, "EvolveSpot", "EVOLVE", "home.evolve", Blue, 760, 280, 210);
            home.SoonButtons = soon.ToArray();

            var nav = Img(s, "NavBar", null, new Color(0.03f, 0.08f, 0.2f, 0.72f));
            At(nav.rectTransform, 0.5f, 0, 0, 65, 4000, 130);
            home.InfoButton = NavButton(s, "InfoButton", "INFO", "nav.info", Blue, 0);
            home.AvatarButton = NavButton(s, "AvatarButton", "AVATAR", "nav.avatar", Gold, 1);
            home.CardsButton = NavButton(s, "CardsButton", "CARDS", "nav.cards", Blue, 2);
            home.PetsButton = NavButton(s, "PetsButton", "PETS", "nav.pets", Green, 3);
            return home;
        }

        private static WorldScreen BuildWorld(Transform root)
        {
            var s = Screen(root, "WorldScreen");
            Fill(Img(s, "Bg", Key("bg.world", Key("bg.lobby", "ui.map")), Color.white).rectTransform);
            var world = s.gameObject.AddComponent<WorldScreen>();
            var locked = new List<Button>();
            locked.Add(WorldIsland(s, "LockedIslandA", "", "world.locked", Gray, -640, 250, 380, 285));
            locked.Add(WorldIsland(s, "LockedIslandB", "", "world.locked", Gray, 620, 270, 420, 315));
            locked.Add(WorldIsland(s, "LockedIslandC", "", "world.locked", Gray, 640, -270, 360, 270));
            world.LockedIslands = locked.ToArray();
            world.IslandButton = WorldIsland(s, "EastSea", "EAST SEA", Key("world.eastsea", "world.sunny"), Green, -40, -60, 690, 460);
            var ship = Img(s, "Ship", Key("world.ship", "ui.circle"), art.ContainsKey("world.ship") ? Color.white : Gold);
            ship.preserveAspect = true;
            At(ship.rectTransform, 0.5f, 0.5f, -520, -270, 170, 170);
            world.Ship = ship.rectTransform;
            var title = Txt(s, "Header", "WORLD MAP", 56, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(title.rectTransform, 0.5f, 1, 0, -60, 900, 80);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0.03f, 0.08f, 0.2f);
            world.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(world.BackButton), 0, 1, 140, -60, 220, 86);
            return world;
        }

        private static Button WorldIsland(Transform parent, string name, string label, string artKey, Color c, float x, float y, float w, float h)
        {
            bool painted = art.ContainsKey(artKey);
            var img = Img(parent, name, painted ? artKey : "ui.circle", painted ? Color.white : new Color(c.r, c.g, c.b, 0.85f), true);
            img.preserveAspect = true;
            At(img.rectTransform, 0.5f, 0.5f, x, y, w, h);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            img.gameObject.AddComponent<ClickSound>();
            if (label.Length > 0)
            {
                var text = Txt(img.transform, "Label", label, 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                At(text.rectTransform, 0.5f, 0f, 0, 6, 520, 56);
                text.gameObject.AddComponent<Outline>().effectColor = new Color(0.03f, 0.08f, 0.2f);
            }
            return b;
        }

        private static void ForgeHeader(Transform s)
        {
            if (!art.ContainsKey("forge.tab")) return;
            var plate = Img(s, "HeaderPlate", "forge.tab", Color.white);
            At(plate.rectTransform, 0.5f, 1, 0, -60, 760, 140);
        }

        private static Color ForgeHeaderInk => art.ContainsKey("forge.tab") ? new Color(0.27f, 0.11f, 0.03f) : Gold;

        private static Button HomeSpot(Transform parent, string name, string label, string artKey, Color c, float x, float y, float size)
        {
            bool painted = art.ContainsKey(artKey);
            var img = Img(parent, name, painted ? artKey : "ui.circle", painted ? Color.white : new Color(c.r, c.g, c.b, 0.85f), true);
            img.preserveAspect = true;
            At(img.rectTransform, 0.5f, 0.5f, x, y, size, size);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            img.gameObject.AddComponent<ClickSound>();
            var text = Txt(img.transform, "Label", label, 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(text.rectTransform, 0.5f, painted ? 0f : 0.5f, 0, painted ? 4 : 0, 320, 44);
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0.03f, 0.08f, 0.2f);
            return b;
        }

        private static HubScreen BuildHub(Transform root)
        {
            var s = Screen(root, "HubScreen");
            Fill(Img(s, "Bg", Key("bg.hub", "ui.map"), Color.white).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.18f)).rectTransform);
            var hub = s.gameObject.AddComponent<HubScreen>();
            var title = Txt(s, "Title", "POKIWAR", 120, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(title.rectTransform, 0.5f, 0.5f, 0, 410, 1200, 150);
            hub.PlayerLabel = Txt(s, "PlayerLabel", "", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(hub.PlayerLabel.rectTransform, 0.5f, 0.5f, 0, 300, 1400, 50);
            hub.PlayerLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.1f, 0.2f);
            var chips = NewUI("Resources", s);
            At(chips, 0.5f, 0.5f, 0, 228, 960, 64);
            hub.GoldLabel = Chip(chips, "Gold", Key("icon.gold", "ui.circle"), art.ContainsKey("icon.gold") ? Color.white : Gold, -360);
            hub.EnergyLabel = Chip(chips, "Energy", "gem.Lightning", Color.white, -120);
            hub.LuckyLabel = Chip(chips, "Lucky", Key("charm.lucky", "fx.star"), art.ContainsKey("charm.lucky") ? Color.white : Gold, 120);
            hub.ProtectLabel = Chip(chips, "Protect", Key("charm.protection", "gem.Shield"), Color.white, 360);

            var card = Img(s, "PetCard", "ui.round", PanelCol);
            At(card.rectTransform, 0.5f, 0.5f, -360, -110, 620, 560);
            SkinPanel(card, 48);
            hub.Avatar = MakeAvatar(card.transform, "Avatar");
            At((RectTransform)hub.Avatar.transform, 0.5f, 0.5f, -130, 70, 400, 400);
            hub.PetImage = Img(card.transform, "PetImage", "emberkit", Color.white);
            hub.PetImage.preserveAspect = true;
            At(hub.PetImage.rectTransform, 0.5f, 0.5f, 130, 20, 300, 300);
            hub.PetLabel = Txt(card.transform, "PetLabel", "", 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(hub.PetLabel.rectTransform, 0.5f, 0.5f, 170, -200, 280, 60);
            hub.PetElement = Img(card.transform, "PetElement", "element.Fire", Color.white);
            hub.PetElement.preserveAspect = true;
            At(hub.PetElement.rectTransform, 0.5f, 0.5f, -10, -200, 64, 64);

            hub.AdventureButton = Btn(s, "AdventureButton", "BACK", Green, 56, out _);
            At(Rt(hub.AdventureButton), 0.5f, 0.5f, 420, 40, 560, 160);
            hub.UpgradeButton = Btn(s, "UpgradeButton", "PETS", Blue, 42, out _);
            At(Rt(hub.UpgradeButton), 0.5f, 0.5f, 278, -140, 272, 120);
            hub.CardsButton = Btn(s, "CardsButton", "CARDS", Blue, 42, out _);
            At(Rt(hub.CardsButton), 0.5f, 0.5f, 562, -140, 272, 120);
            hub.AvatarButton = Btn(s, "AvatarButton", "AVATAR", Gold, 40, out _);
            At(Rt(hub.AvatarButton), 0.5f, 0.5f, 420, -270, 560, 110);
            hub.ResetButton = Btn(s, "ResetButton", "RESET SAVE", Red, 28, out _);
            At(Rt(hub.ResetButton), 0.5f, 0.5f, 420, -400, 300, 76);
            hub.MusicButton = Btn(s, "MusicButton", "MUSIC: ON", Gray, 24, out hub.MusicLabel);
            At(Rt(hub.MusicButton), 1, 1, -560, -50, 190, 64);
            hub.SfxButton = Btn(s, "SfxButton", "SOUND: ON", Gray, 24, out hub.SfxLabel);
            At(Rt(hub.SfxButton), 1, 1, -360, -50, 190, 64);
            hub.ShakeButton = Btn(s, "ShakeButton", "SHAKE: ON", Gray, 24, out hub.ShakeLabel);
            At(Rt(hub.ShakeButton), 1, 1, -160, -50, 190, 64);
            Portrait(title, 0.5f, 0.5f, 0, 700, 1000, 150);
            Portrait(hub.PlayerLabel, 0.5f, 0.5f, 0, 590, 1000, 50);
            Portrait(chips, 0.5f, 0.5f, 0, 510, 960, 64);
            Portrait(card, 0.5f, 0.5f, 0, 120, 620, 560);
            Portrait(hub.AdventureButton, 0.5f, 0.5f, 0, -320, 560, 160);
            Portrait(hub.UpgradeButton, 0.5f, 0.5f, -142, -480, 272, 120);
            Portrait(hub.CardsButton, 0.5f, 0.5f, 142, -480, 272, 120);
            Portrait(hub.AvatarButton, 0.5f, 0.5f, 0, -620, 560, 110);
            Portrait(hub.ResetButton, 0.5f, 0.5f, 0, -750, 300, 76);
            return hub;
        }

        private static MapScreen BuildMap(Transform root)
        {
            var s = Screen(root, "MapScreen");
            Fill(Img(s, "Bg", Key("bg.lobby", Key("bg.map", "ui.map")), Color.white).rectTransform);
            var map = s.gameObject.AddComponent<MapScreen>();
            var bar = Img(s, "TopBar", null, new Color(0.03f, 0.08f, 0.2f, 0.72f));
            At(bar.rectTransform, 0.5f, 1, 0, -50, 4000, 100);
            map.PlayerLabel = Txt(s, "PlayerLabel", "", 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(map.PlayerLabel.rectTransform, 0, 1, 420, -50, 560, 60);
            map.PlayerLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.1f, 0.25f);
            map.Header = Txt(s, "Header", "East Sea", 52, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(map.Header.rectTransform, 0.5f, 1, 310, -50, 700, 80);
            map.Header.gameObject.AddComponent<Outline>().effectColor = new Color(0.25f, 0.12f, 0.02f);
            var info = NewUI("Resources", s);
            At(info, 1, 1, -250, -50, 460, 64);
            map.GoldLabel = Chip(info, "Gold", Key("icon.gold", "ui.circle"), art.ContainsKey("icon.gold") ? Color.white : Gold, -115, 210);
            map.EnergyLabel = Chip(info, "Energy", "gem.Lightning", Color.white, 115, 210);

            var regions = Img(s, "Regions", "ui.round", new Color(0.04f, 0.1f, 0.26f, 0.78f));
            regions.type = Image.Type.Sliced;
            At(regions.rectTransform, 0, 0.5f, 340, 15, 600, 800);
            var grid = regions.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.cellSize = new Vector2(180, 182);
            grid.spacing = new Vector2(12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            map.RegionTemplate = MakeLobbyTile(regions.transform, "RegionTemplate");
            map.RegionLocked = art.TryGetValue("lobby.lock", out var lockSprite) ? lockSprite : null;

            var sky = Img(s, "Ring", "ui.round", new Color(0.1f, 0.3f, 0.66f, 0.55f));
            sky.type = Image.Type.Sliced;
            sky.rectTransform.anchorMin = Vector2.zero;
            sky.rectTransform.anchorMax = Vector2.one;
            sky.rectTransform.offsetMin = new Vector2(670, 155);
            sky.rectTransform.offsetMax = new Vector2(-40, -125);
            var planet = Img(sky.transform, "Planet", Key("lobby.planet", "ui.circle"), art.ContainsKey("lobby.planet") ? Color.white : new Color(0.55f, 0.8f, 1f, 0.35f));
            planet.preserveAspect = true;
            At(planet.rectTransform, 1, 1, -150, -120, 260, 260);
            var area = NewUI("NodeArea", sky.transform);
            Fill(area);
            map.NodeArea = area;
            map.PathTemplate = Img(area, "PathTemplate", "ui.round", new Color(1f, 0.9f, 0.5f));
            At(map.PathTemplate.rectTransform, 0.5f, 0.5f, 0, 0, 100, 12);
            map.NodeTemplate = MakeLobbyNode(area, "NodeTemplate");
            map.NotOwnedMaterial = GrayMaterial();
            map.PathTemplate.gameObject.SetActive(false);
            map.NodeTemplate.gameObject.SetActive(false);

            var nav = Img(s, "NavBar", null, new Color(0.03f, 0.08f, 0.2f, 0.72f));
            At(nav.rectTransform, 0.5f, 0, 0, 65, 4000, 130);
            map.CloseButton = Btn(s, "CloseButton", "X", Red, 40, out _);
            At(Rt(map.CloseButton), 0, 1, 70, -50, 96, 76);
            map.BackButton = NavButton(s, "InfoButton", "INFO", "nav.info", Blue, 0);
            map.AvatarButton = NavButton(s, "AvatarButton", "AVATAR", "nav.avatar", Gold, 1);
            map.CardsButton = NavButton(s, "CardsButton", "CARDS", "nav.cards", Blue, 2);
            map.PetsButton = NavButton(s, "PetsButton", "PETS", "nav.pets", Green, 3);
            return map;
        }

        private static Button NavButton(Transform parent, string name, string label, string iconKey, Color c, int index)
        {
            var b = Btn(parent, name, label, c, 34, out var text);
            At(Rt(b), 0, 0, 180 + index * 300, 65, 280, 104);
            if (art.ContainsKey(iconKey))
            {
                var icon = Img(b.transform, "Icon", iconKey, Color.white);
                icon.preserveAspect = true;
                At(icon.rectTransform, 0, 0.5f, 56, 6, 96, 96);
                Fill(text.rectTransform, 96, 8, 0, 0);
            }
            return b;
        }

        private static RowView MakeLobbyTile(Transform parent, string name)
        {
            var bg = Img(parent, name, "ui.round", Blue, true);
            bg.type = Image.Type.Sliced;
            var row = bg.gameObject.AddComponent<RowView>();
            row.Background = bg;
            row.Button = bg.gameObject.AddComponent<Button>();
            row.Button.targetGraphic = bg;
            bg.gameObject.AddComponent<ClickSound>();
            var hl = Img(bg.transform, "Highlight", "ui.frame", Gold);
            Fill(hl.rectTransform, -4, -4, -4, -4);
            row.Highlight = hl.gameObject;
            row.Icon = Img(bg.transform, "Icon", null, Color.white);
            row.Icon.preserveAspect = true;
            At(row.Icon.rectTransform, 0.5f, 1, 0, -70, 124, 124);
            row.Title = Txt(bg.transform, "Title", "Region", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(row.Title.rectTransform, 0.5f, 0, 0, 26, 172, 40);
            row.Title.resizeTextForBestFit = true;
            row.Title.resizeTextMinSize = 16;
            row.Title.resizeTextMaxSize = 26;
            row.Title.gameObject.AddComponent<Outline>().effectColor = new Color(0.03f, 0.08f, 0.2f);
            return row;
        }

        private static Material GrayMaterial()
        {
            const string path = "Assets/Pokiwar/Shaders/UIGrayscale.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Pokiwar/Shaders/UIGrayscale.shader");
            if (shader == null) return null;
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static RowView MakeLobbyNode(Transform parent, string name)
        {
            var bg = Img(parent, name, "ui.circle", Blue, true);
            At(bg.rectTransform, 0.5f, 0.5f, 0, 0, 170, 170);
            var row = bg.gameObject.AddComponent<RowView>();
            row.Background = bg;
            row.Button = bg.gameObject.AddComponent<Button>();
            row.Button.targetGraphic = bg;
            bg.gameObject.AddComponent<ClickSound>();
            row.Frame = Img(bg.transform, "Frame", "ui.ring", Color.white);
            At(row.Frame.rectTransform, 0.5f, 0.5f, 0, 0, 186, 186);
            row.Icon = Img(bg.transform, "Icon", null, Color.white);
            row.Icon.preserveAspect = true;
            At(row.Icon.rectTransform, 0.5f, 0.5f, 0, 14, 200, 200);
            row.Tag = Txt(bg.transform, "Number", "1", 58, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(row.Tag.rectTransform, 1, 0, 14, 30, 70, 70);
            var numberOutline = row.Tag.gameObject.AddComponent<Outline>();
            numberOutline.effectColor = new Color(0.03f, 0.08f, 0.2f);
            numberOutline.effectDistance = new Vector2(3, -3);
            var plate = Img(bg.transform, "Plate", "ui.round", new Color(0.04f, 0.09f, 0.24f, 0.9f), true);
            At(plate.rectTransform, 0.5f, 1, 0, 70, 290, 76);
            row.Badge = Img(plate.transform, "Element", "element.Fire", Color.white);
            row.Badge.preserveAspect = true;
            At(row.Badge.rectTransform, 0, 0.5f, 38, 0, 56, 56);
            row.Title = Txt(plate.transform, "Title", "Creature", 26, new Color(0.72f, 1f, 0.55f), TextAnchor.MiddleLeft, FontStyle.Bold);
            At(row.Title.rectTransform, 0.5f, 0.5f, 11, 16, 172, 36);
            row.Title.horizontalOverflow = HorizontalWrapMode.Overflow;
            row.Title.resizeTextForBestFit = true;
            row.Title.resizeTextMinSize = 16;
            row.Title.resizeTextMaxSize = 26;
            row.Subtitle = Txt(plate.transform, "Subtitle", "", 19, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleLeft);
            At(row.Subtitle.rectTransform, 0.5f, 0.5f, 11, -17, 172, 28);
            row.Subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            var star = Img(plate.transform, "Highlight", "fx.star", Gold);
            star.preserveAspect = true;
            At(star.rectTransform, 1, 0.5f, -30, 0, 44, 44);
            row.Highlight = star.gameObject;
            return row;
        }

        private static PrepScreen BuildPrep(Transform root)
        {
            var s = Screen(root, "PrepScreen");
            Fill(Img(s, "Bg", Key("bg.hub", null), art.ContainsKey("bg.hub") ? Color.white : new Color(0.07f, 0.09f, 0.15f)).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.3f)).rectTransform);
            var prep = s.gameObject.AddComponent<PrepScreen>();
            var room = Img(s, "Room", "ui.round", new Color(0.42f, 0.66f, 0.92f, 0.9f), true);
            At(room.rectTransform, 0.5f, 0.5f, 0, -30, 1720, 900);
            Skin(room, "room.panel", 60);
            prep.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(prep.BackButton), 0, 1, 140, -60, 220, 86);

            var stand = Img(s, "PetStand", "ui.circle", new Color(1f, 1f, 1f, 0.55f));
            At(stand.rectTransform, 0.5f, 0.5f, -560, -45, 340, 80);
            if (Skin(stand, "room.stand"))
            {
                stand.preserveAspect = true;
                At(stand.rectTransform, 0.5f, 0.5f, -560, -40, 420, 160);
            }
            prep.Avatar = MakeAvatar(s, "Avatar");
            At((RectTransform)prep.Avatar.transform, 0.5f, 0.5f, -680, 150, 440, 440);
            prep.PetImage = Img(s, "PetImage", "emberkit", Color.white);
            prep.PetImage.preserveAspect = true;
            At(prep.PetImage.rectTransform, 0.5f, 0.5f, -450, 70, 380, 330);
            prep.PetName = Txt(s, "PetName", "Emberkit", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(prep.PetName.rectTransform, 0.5f, 0.5f, -560, -140, 500, 50);
            prep.PetName.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.15f, 0.4f);
            var wins = NewUI("PetWins", s);
            At(wins, 0.5f, 0.5f, -300, -140, 150, 60);
            prep.PetWins = Chip(wins, "Wins", "gem.Sword", Color.white, 0, 150);
            prep.ChoosePetButton = Btn(s, "ChoosePetButton", "CHOOSE PET", Blue, 32, out _);
            At(Rt(prep.ChoosePetButton), 0.5f, 0.5f, -630, -225, 280, 84);

            prep.EnemyImage = Img(s, "EnemyImage", "dunewing", Color.white);
            prep.EnemyImage.preserveAspect = true;
            At(prep.EnemyImage.rectTransform, 0.5f, 0.5f, 540, 110, 540, 440);
            prep.EnemyTitle = Txt(s, "EnemyTitle", "", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(prep.EnemyTitle.rectTransform, 0.5f, 0.5f, 540, -150, 640, 52);
            prep.EnemyTitle.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.15f, 0.4f);
            prep.EnemyElement = Img(s, "EnemyElement", "element.Fire", Color.white);
            prep.EnemyElement.preserveAspect = true;
            At(prep.EnemyElement.rectTransform, 0.5f, 0.5f, 400, -150, 60, 60);

            prep.FightButton = Btn(s, "FightButton", "READY", Green, 44, out prep.FightLabel);
            At(Rt(prep.FightButton), 0.5f, 0.5f, 0, 60, 380, 116);

            var row = NewUI("CardRow", s);
            At(row, 0.5f, 0.5f, 0, -320, 900, 200);
            for (int i = 0; i < PrepScreen.MaxCards; i++)
            {
                var slot = Img(row, "Slot" + i, "ui.round", new Color(0.16f, 0.32f, 0.62f), true);
                At(slot.rectTransform, 0.5f, 0.5f, -360 + i * 180, 0, 150, 190);
                if (!Skin(slot, "card.add", 20))
                {
                    var plus = Txt(slot.transform, "Plus", "+", 80, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    Fill(plus.rectTransform);
                }
                var icon = Img(slot.transform, "Icon", "card.mana_potion", Color.white);
                Fill(icon.rectTransform, 8, 8, 8, 8);
                prep.CardSlotValues[i] = CardValue(icon.transform, 40);
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
            prep.PetTemplate = MakeRow(petContent, "PetTemplate", 760, 120, RowCol, 100, 0, 30, 24);
            prep.PetTemplate.gameObject.SetActive(false);
            prep.CardPicker = MakePicker(s, "CardPicker", "CHOOSE CARDS", out var cardContent, out prep.CardPickerClose);
            prep.CardTemplate = MakeRow(cardContent, "CardTemplate", 760, 110, RowCol, 90, 1, 28, 20, Red);
            prep.CardTemplate.ExtraALabel.text = "REMOVE";
            prep.CardTemplate.gameObject.SetActive(false);

            Portrait(room, 0.5f, 0.5f, 0, -20, 1040, 1660);
            Portrait(prep.EnemyImage, 0.5f, 0.5f, 0, 520, 540, 440);
            Portrait(prep.EnemyTitle, 0.5f, 0.5f, 0, 260, 640, 52);
            Portrait(stand, 0.5f, 0.5f, -220, -330, stand.rectTransform.sizeDelta.x, stand.rectTransform.sizeDelta.y);
            Portrait(prep.Avatar, 0.5f, 0.5f, -330, -110, 400, 400);
            Portrait(prep.PetImage, 0.5f, 0.5f, -140, -170, 340, 300);
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
            SkinPanel(panel, 48);
            At(Txt(panel.transform, "Title", title, 38, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -70, 700, 56);
            content = ScrollList(panel.transform, "List", out var view);
            At(view, 0.5f, 1, 0, -468, 790, 710);
            close = Btn(panel.transform, "Close", "X", Gray, 40, out var closeLabel);
            At(Rt(close), 1, 1, -16, -16, 80, 80);
            if (Skin((Image)close.targetGraphic, "ui.close"))
            {
                closeLabel.text = "";
                At(Rt(close), 1, 1, -6, -6, 100, 100);
            }
            Portrait(panel, 0.5f, 0.5f, 0, 0, 860, 860, 1.15f);
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        private static BattleController BuildBattle(Transform root, SpriteLibrary lib, GemView gemPrefab)
        {
            var s = Screen(root, "BattleScreen");
            Fill(Img(s, "Bg", Key("bg.battle", null), art.ContainsKey("bg.battle") ? Color.white : new Color(0.09f, 0.13f, 0.21f)).rectTransform);
            var shade = Img(s, "Shade", null, new Color(0, 0, 0, 0.12f));
            Fill(shade.rectTransform);
            var bc = s.gameObject.AddComponent<BattleController>();
            bc.Dim = shade;
            bc.Shake = s.gameObject.AddComponent<ScreenShake>();
            bc.Shake.Target = s;

            var top = Img(s, "TopHud", "ui.round", new Color(0.07f, 0.15f, 0.38f, 0.93f), true);
            At(top.rectTransform, 0.5f, 1, 0, -98, 1120, 176);
            if (!Skin(top, "hud.top") && art.ContainsKey("hud.bar"))
            {
                var topFrame = Img(top.transform, "Frame", "hud.bar", Color.white);
                Fill(topFrame.rectTransform, -12, -12, -9, -9);
                SliceTo(topFrame, 16);
            }
            if (art.ContainsKey("hud.timer"))
            {
                var medal = Img(top.transform, "TimerMedallion", "hud.timer", Color.white);
                medal.preserveAspect = true;
                At(medal.rectTransform, 0.5f, 0.5f, 0, 0, 180, 180);
            }
            bc.TimerLabel = Txt(top.transform, "Timer", "10", 92, new Color(1f, 0.55f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
            At(bc.TimerLabel.rectTransform, 0.5f, 0.5f, 0, 3, 160, 130);
            var timerOutline = bc.TimerLabel.gameObject.AddComponent<Outline>();
            timerOutline.effectColor = new Color(0.25f, 0.05f, 0f, 1f);
            timerOutline.effectDistance = new Vector2(3, -3);

            bc.AutoButton = Btn(s, "AutoButton", "AUTO: OFF", Gray, 24, out bc.AutoLabel);
            At(Rt(bc.AutoButton), 0, 1, 100, -46, 170, 62);
            bc.GiveUpButton = Btn(s, "GiveUpButton", "GIVE UP", Red, 24, out _);
            At(Rt(bc.GiveUpButton), 0, 1, 280, -46, 170, 62);

            bc.PlayerHud = MakeHud(s, top.transform, "PlayerHud", true);
            bc.EnemyHud = MakeHud(s, top.transform, "EnemyHud", false);
            bc.PlayerCard = MakeAvatarCard(s, "PlayerCard", true);
            At((RectTransform)bc.PlayerCard.transform, 0, 1, 104, -200, 180, 200);
            bc.EnemyCard = MakeAvatarCard(s, "EnemyCard", false);
            At((RectTransform)bc.EnemyCard.transform, 1, 1, -104, -200, 180, 200);
            bc.EnemyCard.gameObject.SetActive(false);

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
            At(bar, 0.5f, 0.5f, 0, -448, 920, 170);
            var barLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            barLayout.childAlignment = TextAnchor.MiddleCenter;
            barLayout.spacing = 10;
            barLayout.childControlWidth = barLayout.childControlHeight = false;
            barLayout.childForceExpandWidth = barLayout.childForceExpandHeight = false;
            for (int i = 0; i < 2; i++)
            {
                bc.SkillButtons[i] = MakeCard(bar, "Skill" + i);
                bc.SkillButtons[i].Icon.preserveAspect = true;
                At((RectTransform)bc.SkillButtons[i].transform, 0.5f, 0.5f, 0, 0, 120, 150);
            }
            for (int i = 0; i < 5; i++)
            {
                bc.CardButtons[i] = MakeCard(bar, "Card" + i);
                At((RectTransform)bc.CardButtons[i].transform, 0.5f, 0.5f, -260 + i * 130, 0, 120, 150);
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
            if (art.ContainsKey("text.yourturn"))
            {
                pop.text = "";
                popOutline.enabled = false;
                var word = Img(pop.transform, "Art", "text.yourturn", Color.white);
                word.preserveAspect = true;
                At(word.rectTransform, 0.5f, 0.5f, 0, 0, 760, 190);
            }
            bc.TurnPop = pop.gameObject.AddComponent<CanvasGroup>();
            bc.TurnPop.blocksRaycasts = false;
            bc.TurnPop.alpha = 0f;

            var combo = NewUI("Combo", s);
            At(combo, 0.5f, 0.5f, 0, -8, 900, 220);
            bc.ComboLabel = Txt(combo, "Label", "COMBO x2", 76, new Color(1f, 0.88f, 0.2f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            Fill(bc.ComboLabel.rectTransform);
            bc.ComboLabel.raycastTarget = false;
            bc.ComboLabel.rectTransform.localEulerAngles = new Vector3(0, 0, 5f);
            var comboInk = bc.ComboLabel.gameObject.AddComponent<Outline>();
            comboInk.effectColor = new Color(0.42f, 0.05f, 0.02f, 1f);
            comboInk.effectDistance = new Vector2(4, -4);
            comboInk.useGraphicAlpha = true;
            var comboRim = bc.ComboLabel.gameObject.AddComponent<Outline>();
            comboRim.effectColor = Color.white;
            comboRim.effectDistance = new Vector2(2, -2);
            comboRim.useGraphicAlpha = true;
            bc.ComboLabel.gameObject.SetActive(false);

            var fl = NewUI("FloatingText", s);
            Fill(fl);
            var flv = fl.gameObject.AddComponent<FloatingTextLayer>();
            flv.Template = Txt(fl, "Template", "-123", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(flv.Template.rectTransform, 0.5f, 0.5f, 0, 0, 700, 80);
            var floatOutline = flv.Template.gameObject.AddComponent<Outline>();
            floatOutline.effectColor = new Color(0.08f, 0.04f, 0.02f, 0.95f);
            floatOutline.effectDistance = new Vector2(2, -2);
            flv.Template.gameObject.SetActive(false);
            bc.Floating = flv;

            var banner = Img(s, "Banner", null, new Color(0, 0, 0, 0.6f));
            At(banner.rectTransform, 0.5f, 0.5f, 0, 40, 4000, 150);
            bc.Banner = banner.gameObject.AddComponent<CanvasGroup>();
            bc.Banner.blocksRaycasts = false;
            bc.Banner.alpha = 0f;
            bc.BannerLabel = Txt(banner.transform, "Label", "FIGHT!", 76, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(bc.BannerLabel.rectTransform, 0.5f, 0.5f, 0, 0, 1600, 140);

            var intro = Img(s, "Intro", null, new Color(0.02f, 0.03f, 0.1f, 0.78f), true);
            Fill(intro.rectTransform);
            bc.Intro = intro.gameObject.AddComponent<CanvasGroup>();
            var introBand = Img(intro.transform, "BandLow", "ui.gradient", new Color(1f, 0.6f, 0.15f, 0.4f));
            At(introBand.rectTransform, 0.5f, 0.5f, 0, -150, 4000, 300);
            var introBandTop = Img(intro.transform, "BandHigh", "ui.gradient", new Color(1f, 0.6f, 0.15f, 0.4f));
            At(introBandTop.rectTransform, 0.5f, 0.5f, 0, 150, 4000, 300);
            introBandTop.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            bc.IntroPlayer = Img(intro.transform, "Player", "emberkit", Color.white);
            bc.IntroPlayer.preserveAspect = true;
            bc.IntroPlayer.rectTransform.anchorMin = bc.IntroPlayer.rectTransform.anchorMax = new Vector2(0.24f, 0.5f);
            bc.IntroPlayer.rectTransform.sizeDelta = new Vector2(470, 470);
            bc.IntroEnemy = Img(intro.transform, "Enemy", "dunewing", Color.white);
            bc.IntroEnemy.preserveAspect = true;
            bc.IntroEnemy.rectTransform.anchorMin = bc.IntroEnemy.rectTransform.anchorMax = new Vector2(0.76f, 0.5f);
            bc.IntroEnemy.rectTransform.sizeDelta = new Vector2(470, 470);
            bc.IntroVs = StickerText(intro.transform, "Vs", "VS", 260, new Color(1f, 0.62f, 0.1f), new Color(0.35f, 0.08f, 0f));
            At(bc.IntroVs.rectTransform, 0.5f, 0.5f, 0, 0, 700, 320);
            bc.IntroFight = StickerText(intro.transform, "Fight", "FIGHT!", 200, new Color(1f, 0.3f, 0.22f), new Color(0.35f, 0.03f, 0.03f));
            At(bc.IntroFight.rectTransform, 0.5f, 0.5f, 0, 0, 1000, 300);
            if (art.ContainsKey("text.vs")) WordArt(bc.IntroVs, "text.vs", 520, 320);
            if (art.ContainsKey("text.fight")) WordArt(bc.IntroFight, "text.fight", 900, 300);
            intro.gameObject.SetActive(false);

            bc.Qte = BuildQte(s);
            bc.Log = BuildLog(s);
            Portrait(top, 0.5f, 1, 0, -190, 1120, 176, 0.94f);
            Portrait(bc.PlayerHud, 0.5f, 0.5f, -265, 500, 520, 500, 0.78f);
            Portrait(bc.EnemyHud, 0.5f, 0.5f, 265, 500, 520, 500, 0.78f);
            Portrait(bc.PlayerHud.ElementIcon, 0, 1, 64, -330, 96, 96);
            Portrait(bc.EnemyHud.ElementIcon, 1, 1, -64, -330, 96, 96);
            Portrait(bc.PlayerCard, 0, 1, 58, -455, 180, 200, 0.6f);
            Portrait(bc.EnemyCard, 1, 1, -58, -455, 180, 200, 0.6f);
            Portrait(boardRoot, 0.5f, 0.5f, 0, -255, 704, 704, 1.38f);
            Portrait(bar, 0.5f, 0, 0, 120, 920, 170);
            Portrait(sum, 0.5f, 0.5f, 0, -255, 900, 190, 1.2f);
            Portrait(pop, 0.5f, 0.5f, 0, -255, 900, 140);
            Portrait(combo, 0.5f, 0.5f, 0, -255, 900, 220);
            Portrait(banner, 0.5f, 0.5f, 0, -255, 4000, 150);
            return bc;
        }

        private static Text StickerText(Transform parent, string name, string text, int size, Color fill, Color ink)
        {
            var t = Txt(parent, name, text, size, fill, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.rectTransform.localEulerAngles = new Vector3(0, 0, 4f);
            var inner = t.gameObject.AddComponent<Outline>();
            inner.effectColor = ink;
            inner.effectDistance = new Vector2(6, -6);
            var rim = t.gameObject.AddComponent<Outline>();
            rim.effectColor = Color.white;
            rim.effectDistance = new Vector2(3, -3);
            return t;
        }

        private static void WordArt(Text label, string key, float w, float h)
        {
            label.text = "";
            foreach (var o in label.GetComponents<Outline>()) o.enabled = false;
            var word = Img(label.transform, "Art", key, Color.white);
            word.preserveAspect = true;
            word.raycastTarget = false;
            At(word.rectTransform, 0.5f, 0.5f, 0, 0, w, h);
        }

        private static QteView BuildQte(Transform parent)
        {
            var holder = NewUI("Qte", parent);
            Fill(holder);
            var qv = holder.gameObject.AddComponent<QteView>();
            var dim = Img(holder, "Root", null, new Color(0, 0, 0, 0.6f), true);
            Fill(dim.rectTransform);
            qv.Root = dim.gameObject;
            var panel = NewUI("Panel", dim.transform);
            At(panel, 0.5f, 0.5f, 0, 10, 1440, 680);
            Transform p = panel;
            var ribbon = Img(p, "Ribbon", "ui.round", new Color(0.2f, 0.24f, 0.4f));
            At(ribbon.rectTransform, 0.5f, 0.5f, 0, 270, 760, 84);
            if (Skin(ribbon, "ui.ribbon", 40)) At(ribbon.rectTransform, 0.5f, 0.5f, 0, 270, 820, 124);
            qv.Title = Txt(ribbon.transform, "Title", "Skill", 42, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(qv.Title.rectTransform, 90, 90, 14, 22);

            var track = Img(p, "Slider", "ui.round", new Color(0.25f, 0.25f, 0.32f));
            At(track.rectTransform, 0.5f, 0.5f, -190, 150, 760, 58);
            bool slider = Skin(track, "qte.track");
            if (slider) At(track.rectTransform, 0.5f, 0.5f, -190, 150, 880, 82);
            var barArea = NewUI("BarArea", track.transform);
            At(barArea, 0.5f, 0.5f, 0, 0, slider ? 740 : 760, slider ? 40 : 58);
            qv.BarArea = barArea;
            var good = Img(barArea, "GoodZone", null, new Color(0.3f, 0.85f, 0.4f, 0.9f));
            At(good.rectTransform, 0.5f, 0.5f, 0, 0, 130, Skin(good, "qte.good") ? 84 : 58);
            qv.GoodZone = good.rectTransform;
            var perfect = Img(barArea, "PerfectZone", null, Gold);
            At(perfect.rectTransform, 0.5f, 0.5f, 0, 0, 40, Skin(perfect, "qte.perfect") ? 100 : 58);
            qv.PerfectZone = perfect.rectTransform;
            var marker = Img(barArea, "Marker", null, Color.white);
            At(marker.rectTransform, 0.5f, 0.5f, 0, 0, 10, 78);
            if (Skin(marker, Key("qte.knob", "qte.marker")))
            {
                marker.preserveAspect = true;
                At(marker.rectTransform, 0.5f, 0.5f, 0, 0, 72, 72);
            }
            qv.Marker = marker.rectTransform;

            var bar = Img(p, "Bar", "ui.round", new Color(0.05f, 0.2f, 0.26f, 0.95f));
            At(bar.rectTransform, 0.5f, 0.5f, -190, 0, 900, 170);
            if (Skin(bar, "qte.bar")) At(bar.rectTransform, 0.5f, 0.5f, -190, 0, 1000, 225);
            bool tokens = art.ContainsKey("qte.token");
            for (int i = 0; i < 5; i++)
            {
                var slot = Img(bar.transform, "Token" + i, "ui.round", new Color(0.08f, 0.09f, 0.16f));
                At(slot.rectTransform, 0.5f, 0.5f, -330 + i * 165, 0, tokens ? 156 : 130, tokens ? 156 : 130);
                if (!Skin(slot, "qte.token")) Skin(slot, "qte.slot");
                qv.Tokens[i] = slot;
                qv.Arrows[i] = Img(slot.transform, "Arrow", Key("qte.arrow", "ui.arrow"), Color.white);
                qv.Arrows[i].preserveAspect = true;
                At(qv.Arrows[i].rectTransform, 0.5f, 0.5f, 0, 2, 84, 84);
            }
            if (tokens)
            {
                qv.TokenIdle = art["qte.token"];
                qv.TokenOk = art[Key("qte.token.ok", "qte.token")];
                qv.TokenBad = art[Key("qte.token.bad", "qte.token")];
            }

            var flame = Img(p, "Flame", null, new Color(1f, 0.45f, 0.1f, 0f));
            At(flame.rectTransform, 0.5f, 0.5f, 478, 0, 430, 161);
            Skin(flame, "qte.flame");
            qv.DamageLabel = Txt(flame.transform, "Damage", "0", 76, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(qv.DamageLabel.rectTransform, 0.5f, 0.5f, -30, 0, 300, 110);
            var edge = qv.DamageLabel.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.25f, 0.05f, 0f, 1f);
            edge.effectDistance = new Vector2(3, -3);

            var tbg = Img(p, "TimerBg", "ui.round", new Color(1, 1, 1, 0.15f));
            At(tbg.rectTransform, 0.5f, 0.5f, -190, -128, 520, 24);
            bool tracked = Skin(tbg, "hud.track", 12);
            qv.TimerFill = Img(tbg.transform, "Fill", null, Gold);
            qv.TimerFill.type = Image.Type.Filled;
            qv.TimerFill.fillMethod = Image.FillMethod.Horizontal;
            if (tracked) Fill(qv.TimerFill.rectTransform, 6, 6, 6, 6);
            else Fill(qv.TimerFill.rectTransform);

            qv.Feedback = Txt(p, "Feedback", "", 60, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(qv.Feedback.rectTransform, 0.5f, 0.5f, 0, -205, 520, 76);
            qv.Hint = Txt(p, "Hint", "", 24, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
            At(qv.Hint.rectTransform, 0.5f, 0.5f, 0, -318, 1100, 40);
            qv.Up = ArrowButton(p, "Up", -500, -200, 0);
            qv.Down = ArrowButton(p, "Down", -500, -310, 180);
            qv.Left = ArrowButton(p, "Left", -605, -255, 90);
            qv.Right = ArrowButton(p, "Right", -395, -255, -90);
            qv.Strike = Btn(p, "Strike", "STRIKE", Red, 40, out var strikeLabel);
            At(Rt(qv.Strike), 0.5f, 0.5f, 500, -255, 240, 120);
            if (Skin((Image)qv.Strike.targetGraphic, "qte.button.strike"))
            {
                At(Rt(qv.Strike), 0.5f, 0.5f, 500, -255, 280, 140);
                strikeLabel.text = "";
            }
            dim.gameObject.SetActive(false);
            Portrait(panel, 0.5f, 0.5f, 0, 10, 1440, 680, 0.74f);
            return qv;
        }

        private static Button ArrowButton(Transform p, string name, float x, float y, float rot)
        {
            var b = Btn(p, name, "", Blue, 10, out _);
            At(Rt(b), 0.5f, 0.5f, x, y, 96, 96);
            bool round = Skin((Image)b.targetGraphic, "qte.button.dir");
            if (round) At(Rt(b), 0.5f, 0.5f, x, y, 108, 108);
            var a = Img(b.transform, "Arrow", round ? Key("qte.arrow", "ui.arrow") : "ui.arrow", Color.white);
            a.preserveAspect = true;
            At(a.rectTransform, 0.5f, 0.5f, 0, 0, round ? 58 : 64, round ? 58 : 64);
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
            if (art.ContainsKey("ui.rays"))
            {
                var rays = Img(s, "Rays", "ui.rays", new Color(1f, 1f, 1f, 0.85f));
                rays.preserveAspect = true;
                At(rays.rectTransform, 0.5f, 0.5f, 0, -20, 1300, 1300);
                Portrait(rays, 0.5f, 0.5f, 0, -255, 1300, 1300);
            }
            var panel = Img(s, "Panel", "ui.round", new Color(1f, 0.82f, 0.25f), true);
            At(panel.rectTransform, 0.5f, 0.5f, 0, -20, 940, 500);
            rs.Panel = panel.rectTransform;
            if (!Skin(panel, "popup.reward", 70))
            {
                var glow = Img(panel.transform, "Glow", "ui.gradient", new Color(1f, 1f, 0.85f, 0.75f));
                Fill(glow.rectTransform, 10, 10, 10, 10);
            }
            var pill = Img(panel.transform, "TitlePill", "ui.round", new Color(0.6f, 0.25f, 0.85f), true);
            At(pill.rectTransform, 0.5f, 1, 0, 0, 340, 76);
            if (Skin(pill, "ui.ribbon", 40)) At(pill.rectTransform, 0.5f, 1, 0, 6, 520, 120);
            rs.Title = Txt(pill.transform, "Title", "VICTORY", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(rs.Title.rectTransform);
            var titleOutline = rs.Title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.3f, 0.05f, 0.4f, 1f);
            rs.Lines = Txt(panel.transform, "Lines", "", 34, new Color(0.55f, 0.2f, 0.75f), TextAnchor.MiddleCenter, FontStyle.Bold);
            At(rs.Lines.rectTransform, 0.5f, 0.5f, 0, -24, 760, 340);
            rs.Lines.resizeTextForBestFit = true;
            rs.Lines.resizeTextMinSize = 20;
            rs.Lines.resizeTextMaxSize = 34;
            rs.CapturedPet = Img(panel.transform, "CapturedPet", "dunewing", Color.white);
            rs.CapturedPet.preserveAspect = true;
            rs.CapturedPet.raycastTarget = false;
            At(rs.CapturedPet.rectTransform, 0.5f, 0.5f, -295, -24, 230, 230);
            rs.CloseButton = Btn(panel.transform, "CloseButton", "X", Gray, 40, out var closeLabel);
            At(Rt(rs.CloseButton), 1, 1, -56, -56, 84, 84);
            if (Skin((Image)rs.CloseButton.targetGraphic, "ui.close"))
            {
                closeLabel.text = "";
                At(Rt(rs.CloseButton), 1, 1, -50, -50, 104, 104);
            }
            Portrait(panel, 0.5f, 0.5f, 0, -255, 940, 500, 0.98f);
            return rs;
        }

        private static UpgradeScreen BuildUpgrade(Transform root)
        {
            var s = Screen(root, "UpgradeScreen");
            Fill(Img(s, "Bg", Key("bg.hub", null), art.ContainsKey("bg.hub") ? Color.white : new Color(0.08f, 0.08f, 0.14f)).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.4f)).rectTransform);
            var up = s.gameObject.AddComponent<UpgradeScreen>();
            up.PaintedStones = art.ContainsKey("stone.Fire");
            ForgeHeader(s);
            At(Txt(s, "Header", "PETS & STONES", 56, ForgeHeaderInk, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -60, 900, 80);
            up.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(up.BackButton), 0, 1, 140, -60, 220, 86);

            var lp = Img(s, "PetPanel", "ui.round", PanelCol);
            At(lp.rectTransform, 0.5f, 0.5f, -705, -50, 450, 860);
            SkinPanel(lp, 48);
            At(Txt(lp.transform, "Label", "PETS", 34, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -70, 380, 50);
            var petContent = ScrollList(lp.transform, "PetList", out var petView);
            At(petView, 0.5f, 1, 0, -464, 390, 710);
            up.PetTemplate = MakeRow(petContent, "PetTemplate", 366, 110, RowCol, 84, 0, 26, 20);

            var cp = Img(s, "DetailPanel", "ui.round", PanelCol);
            At(cp.rectTransform, 0.5f, 0.5f, -185, -50, 570, 860);
            SkinPanel(cp, 48);
            if (art.ContainsKey("forge.pedestal"))
            {
                var petPedestal = Img(cp.transform, "Pedestal", "forge.pedestal", Color.white);
                petPedestal.preserveAspect = true;
                At(petPedestal.rectTransform, 0.5f, 1, 0, -262, 270, 180);
            }
            up.PetImage = Img(cp.transform, "PetImage", "emberkit", Color.white);
            up.PetImage.preserveAspect = true;
            At(up.PetImage.rectTransform, 0.5f, 1, 0, -160, 240, 240);
            if (art.ContainsKey("forge.slot"))
            {
                var slot = Img(cp.transform, "StoneSlot", "forge.slot", Color.white);
                At(slot.rectTransform, 0.5f, 1, 205, -110, 110, 110);
                up.StoneSlotIcon = Img(slot.transform, "Stone", "stone.Fire", Color.white);
                up.StoneSlotIcon.preserveAspect = true;
                At(up.StoneSlotIcon.rectTransform, 0.5f, 0.5f, 0, 0, 72, 72);
            }
            up.PetDetails = Txt(cp.transform, "PetDetails", "", 22, Color.white, TextAnchor.UpperLeft);
            At(up.PetDetails.rectTransform, 0.5f, 1, 0, -445, 490, 190);
            up.LuckyToggle = MakeToggle(cp.transform, "LuckyToggle", "Use Lucky Charm (+chance)");
            At((RectTransform)up.LuckyToggle.transform, 0.5f, 0, 0, 300, 490, 56);
            up.ProtectToggle = MakeToggle(cp.transform, "ProtectToggle", "Use Protection (no downgrade)");
            At((RectTransform)up.ProtectToggle.transform, 0.5f, 0, 0, 238, 490, 56);
            var wallet = NewUI("Wallet", cp.transform);
            At(wallet, 0.5f, 0, 0, 170, 500, 60);
            up.GoldLabel = Chip(wallet, "Gold", Key("icon.gold", "ui.circle"), art.ContainsKey("icon.gold") ? Color.white : Gold, -152, 176);
            up.LuckyLabel = Chip(wallet, "Lucky", Key("charm.lucky", "fx.star"), art.ContainsKey("charm.lucky") ? Color.white : Gold, 14, 136);
            up.ProtectLabel = Chip(wallet, "Protect", Key("charm.protection", "gem.Shield"), Color.white, 160, 136);
            up.MessageLabel = Txt(cp.transform, "Message", "", 22, new Color(0.8f, 0.95f, 1f), TextAnchor.MiddleCenter);
            At(up.MessageLabel.rectTransform, 0.5f, 0, 0, 90, 490, 90);

            var rp = Img(s, "StonePanel", "ui.round", PanelCol);
            At(rp.rectTransform, 0.5f, 0.5f, 520, -50, 810, 860);
            SkinPanel(rp, 48);
            At(Txt(rp.transform, "Label", "STONES", 34, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -70, 680, 50);
            var stoneContent = ScrollList(rp.transform, "StoneList", out var stoneView);
            At(stoneView, 0.5f, 1, 0, -464, 750, 710);
            up.StoneTemplate = MakeRow(stoneContent, "StoneTemplate", 726, 104, RowCol, 64, 3, 24, 18);

            up.PetTemplate.gameObject.SetActive(false);
            up.StoneTemplate.gameObject.SetActive(false);
            Portrait(lp, 0.5f, 0.5f, -300, 449, 450, 860, 0.86f);
            Portrait(cp, 0.5f, 0.5f, 245, 449, 570, 860, 0.86f);
            Portrait(rp, 0.5f, 0.5f, 0, -470, 810, 860, 1.0f);
            return up;
        }

        private static CardForgeScreen BuildCardForge(Transform root)
        {
            var s = Screen(root, "CardForgeScreen");
            Fill(Img(s, "Bg", Key("bg.hub", null), art.ContainsKey("bg.hub") ? Color.white : new Color(0.08f, 0.08f, 0.14f)).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0.1f, 0f, 0.2f, 0.5f)).rectTransform);
            var f = s.gameObject.AddComponent<CardForgeScreen>();
            f.PaintedStone = art.ContainsKey("stone.card");
            ForgeHeader(s);
            At(Txt(s, "Header", "CARD FORGE", 56, ForgeHeaderInk, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -60, 900, 80);
            f.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(f.BackButton), 0, 1, 140, -60, 220, 86);

            var lp = Img(s, "CardPanel", "ui.round", PanelCol);
            At(lp.rectTransform, 0.5f, 0.5f, -705, -50, 450, 860);
            SkinPanel(lp, 48);
            At(Txt(lp.transform, "Label", "CARDS", 34, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -70, 380, 50);
            var cardContent = ScrollList(lp.transform, "CardList", out var cardView);
            At(cardView, 0.5f, 1, 0, -464, 390, 710);
            f.CardTemplate = MakeRow(cardContent, "CardTemplate", 366, 110, RowCol, 84, 0, 26, 20);

            var cp = Img(s, "DetailPanel", "ui.round", PanelCol);
            At(cp.rectTransform, 0.5f, 0.5f, -185, -50, 570, 860);
            SkinPanel(cp, 48);
            bool paintedPedestal = art.ContainsKey("forge.pedestal");
            var pedestal = Img(cp.transform, "Pedestal", paintedPedestal ? "forge.pedestal" : "ui.circle", paintedPedestal ? Color.white : new Color(0.6f, 0.3f, 0.95f, 0.55f));
            pedestal.preserveAspect = paintedPedestal;
            if (paintedPedestal) At(pedestal.rectTransform, 0.5f, 1, 0, -345, 270, 180);
            else At(pedestal.rectTransform, 0.5f, 1, 0, -400, 380, 90);
            f.CardFace = MakeCard(cp.transform, "CardFace");
            At((RectTransform)f.CardFace.transform, 0.5f, 1, 0, paintedPedestal ? -215 : -235, 120, 150);
            f.CardFace.transform.localScale = Vector3.one * (paintedPedestal ? 1.8f : 2f);
            f.CardFace.Button.interactable = false;
            f.CardFace.Icon.preserveAspect = true;
            f.CardDetails = Txt(cp.transform, "CardDetails", "", 20, Color.white, TextAnchor.UpperCenter);
            At(f.CardDetails.rectTransform, 0.5f, 1, 0, -492, 490, 110);
            f.LuckyToggle = MakeToggle(cp.transform, "LuckyToggle", "Use Lucky Charm (+chance)");
            At((RectTransform)f.LuckyToggle.transform, 0.5f, 0, 0, 300, 490, 56);
            f.ProtectToggle = MakeToggle(cp.transform, "ProtectToggle", "Use Protection (no downgrade)");
            At((RectTransform)f.ProtectToggle.transform, 0.5f, 0, 0, 238, 490, 56);
            var wallet = NewUI("Wallet", cp.transform);
            At(wallet, 0.5f, 0, 0, 170, 500, 60);
            f.GoldLabel = Chip(wallet, "Gold", Key("icon.gold", "ui.circle"), art.ContainsKey("icon.gold") ? Color.white : Gold, -152, 176);
            f.LuckyLabel = Chip(wallet, "Lucky", Key("charm.lucky", "fx.star"), art.ContainsKey("charm.lucky") ? Color.white : Gold, 14, 136);
            f.ProtectLabel = Chip(wallet, "Protect", Key("charm.protection", "gem.Shield"), Color.white, 160, 136);
            f.MessageLabel = Txt(cp.transform, "Message", "", 22, new Color(0.8f, 0.95f, 1f), TextAnchor.MiddleCenter);
            At(f.MessageLabel.rectTransform, 0.5f, 0, 0, 90, 490, 90);

            var rp = Img(s, "StonePanel", "ui.round", PanelCol);
            At(rp.rectTransform, 0.5f, 0.5f, 520, -50, 810, 860);
            SkinPanel(rp, 48);
            At(Txt(rp.transform, "Label", "CARD STONES", 34, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform, 0.5f, 1, 0, -70, 680, 50);
            var stoneContent = ScrollList(rp.transform, "StoneList", out var stoneView);
            At(stoneView, 0.5f, 1, 0, -464, 750, 710);
            f.StoneTemplate = MakeRow(stoneContent, "StoneTemplate", 726, 104, RowCol, 64, 2, 24, 18);

            f.CardTemplate.gameObject.SetActive(false);
            f.StoneTemplate.gameObject.SetActive(false);
            Portrait(lp, 0.5f, 0.5f, -300, 449, 450, 860, 0.86f);
            Portrait(cp, 0.5f, 0.5f, 245, 449, 570, 860, 0.86f);
            Portrait(rp, 0.5f, 0.5f, 0, -470, 810, 860, 1.0f);
            return f;
        }

        private static AvatarScreen BuildWardrobe(Transform root)
        {
            var s = Screen(root, "AvatarScreen");
            Fill(Img(s, "Bg", Key("bg.hub", null), art.ContainsKey("bg.hub") ? Color.white : new Color(0.07f, 0.09f, 0.15f)).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.35f)).rectTransform);
            var w = s.gameObject.AddComponent<AvatarScreen>();
            var header = Txt(s, "Header", "AVATAR", 56, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(header.rectTransform, 0.5f, 1, 0, -60, 900, 80);
            w.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(w.BackButton), 0, 1, 140, -60, 220, 86);
            var wallet = NewUI("Wallet", s);
            At(wallet, 1, 1, -150, -60, 230, 64);
            w.WalletLabel = Chip(wallet, "Gold", Key("icon.gold", "ui.circle"), art.ContainsKey("icon.gold") ? Color.white : Gold, 0, 220);

            var left = Img(s, "PreviewPanel", "ui.round", PanelCol);
            At(left.rectTransform, 0.5f, 0.5f, -500, -50, 680, 860);
            SkinPanel(left, 48);
            var stand = Img(left.transform, "Stand", "ui.circle", new Color(1f, 1f, 1f, 0.35f));
            At(stand.rectTransform, 0.5f, 0.5f, 0, -170, 360, 90);
            if (Skin(stand, "room.stand"))
            {
                stand.preserveAspect = true;
                At(stand.rectTransform, 0.5f, 0.5f, 0, -170, 420, 160);
            }
            w.Preview = MakeAvatar(left.transform, "Preview");
            At((RectTransform)w.Preview.transform, 0.5f, 0.5f, 0, 70, 600, 600);
            w.NameField = MakeInput(left.transform, "NameField", Pokiwar.Domain.AvatarService.MaxNameLength);
            At((RectTransform)w.NameField.transform, 0.5f, 0, -80, 90, 400, 76);
            w.SaveNameButton = Btn(left.transform, "SaveName", "OK", Green, 34, out _);
            At(Rt(w.SaveNameButton), 0.5f, 0, 210, 90, 140, 76);

            var right = Img(s, "ItemPanel", "ui.round", PanelCol);
            At(right.rectTransform, 0.5f, 0.5f, 360, -50, 960, 860);
            SkinPanel(right, 48);
            string[] tabs = { "PANTS", "TOP", "HAIR", "HAT" };
            for (int i = 0; i < 4; i++)
            {
                w.Tabs[i] = Btn(right.transform, "Tab" + tabs[i], tabs[i], Gray, 30, out _);
                At(Rt(w.Tabs[i]), 0.5f, 1, -324 + i * 216, -88, 200, 76);
                w.TabBackgrounds[i] = (Image)w.Tabs[i].targetGraphic;
            }
            var content = ScrollList(right.transform, "Items", out var view);
            At(view, 0.5f, 1, 0, -484, 880, 670);
            w.ItemTemplate = MakeRow(content, "ItemTemplate", 840, 130, RowCol, 110, 1, 32, 24, Green);
            w.ItemTemplate.gameObject.SetActive(false);

            Portrait(left, 0.5f, 0.5f, 0, 420, 680, 860, 0.9f);
            Portrait(right, 0.5f, 0.5f, 0, -470, 960, 860, 1.05f);
            Portrait(wallet, 1, 1, -150, -60, 260, 70);
            return w;
        }

        private static ShopScreen BuildShop(Transform root)
        {
            var s = Screen(root, "ShopScreen");
            Fill(Img(s, "Bg", Key("bg.hub", null), art.ContainsKey("bg.hub") ? Color.white : new Color(0.07f, 0.09f, 0.15f)).rectTransform);
            Fill(Img(s, "Shade", null, new Color(0, 0, 0, 0.35f)).rectTransform);
            var shop = s.gameObject.AddComponent<ShopScreen>();
            var header = Txt(s, "Header", "CARD SHOP", 56, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(header.rectTransform, 0.5f, 1, 0, -60, 900, 80);
            shop.BackButton = Btn(s, "BackButton", "BACK", Gray, 36, out _);
            At(Rt(shop.BackButton), 0, 1, 140, -60, 220, 86);
            var wallet = NewUI("Wallet", s);
            At(wallet, 1, 1, -150, -60, 230, 64);
            shop.WalletLabel = Chip(wallet, "Gold", Key("icon.gold", "ui.circle"), art.ContainsKey("icon.gold") ? Color.white : Gold, 0, 220);

            var panel = Img(s, "ItemPanel", "ui.round", PanelCol);
            At(panel.rectTransform, 0.5f, 0.5f, 0, -50, 1100, 860);
            SkinPanel(panel, 48);
            var content = ScrollList(panel.transform, "Items", out var view);
            At(view, 0.5f, 1, 0, -430, 1020, 760);
            shop.ItemTemplate = MakeRow(content, "ItemTemplate", 980, 150, RowCol, 120, 1, 34, 24, Green);
            shop.ItemTemplate.gameObject.SetActive(false);
            return shop;
        }

        private static AvatarView MakeAvatar(Transform parent, string name)
        {
            var root = NewUI(name, parent);
            var v = root.gameObject.AddComponent<AvatarView>();
            v.Body = Img(root, "Body", "avatar.base", Color.white);
            v.Body.preserveAspect = true;
            Fill(v.Body.rectTransform);
            foreach (Pokiwar.Domain.AvatarSlot slot in System.Enum.GetValues(typeof(Pokiwar.Domain.AvatarSlot)))
            {
                var layer = Img(root, slot.ToString(), "avatar.base", Color.white);
                layer.preserveAspect = true;
                Fill(layer.rectTransform);
                layer.gameObject.SetActive(false);
                v.Layers[(int)slot] = layer;
            }
            return v;
        }

        private static AvatarCard MakeAvatarCard(Transform parent, string name, bool player)
        {
            var bg = Img(parent, name, "ui.round", new Color(0.07f, 0.15f, 0.38f, 0.93f), true);
            SkinPanel(bg, 24);
            var card = bg.gameObject.AddComponent<AvatarCard>();
            var mask = Img(bg.transform, "Window", "ui.round", new Color(0.55f, 0.8f, 1f, 1f));
            At(mask.rectTransform, 0.5f, 1, 0, -84, 144, 136);
            mask.gameObject.AddComponent<RectMask2D>();
            card.Avatar = MakeAvatar(mask.transform, "Avatar");
            At((RectTransform)card.Avatar.transform, 0.5f, 0.5f, 0, -100, 330, 330);
            card.NameLabel = Txt(bg.transform, "Name", "Trainer", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            At(card.NameLabel.rectTransform, 0.5f, 0, 0, 24, 168, 34);
            card.NameLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.1f, 0.3f);
            card.NameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            card.NameLabel.resizeTextMaxSize = 24;
            card.NameLabel.resizeTextMinSize = 14;
            card.NameLabel.resizeTextForBestFit = true;
            return card;
        }

        private static InputField MakeInput(Transform parent, string name, int maxChars)
        {
            var bg = Img(parent, name, "ui.round", new Color(0.95f, 0.93f, 0.86f), true);
            var field = bg.gameObject.AddComponent<InputField>();
            field.targetGraphic = bg;
            field.characterLimit = maxChars;
            field.lineType = InputField.LineType.SingleLine;
            var text = Txt(bg.transform, "Text", "", 34, new Color(0.15f, 0.1f, 0.12f), TextAnchor.MiddleLeft, FontStyle.Bold);
            Fill(text.rectTransform, 20, 20, 4, 4);
            text.supportRichText = false;
            text.GetComponent<Shadow>().enabled = false;
            var hint = Txt(bg.transform, "Placeholder", "Your name", 34, new Color(0.15f, 0.1f, 0.12f, 0.45f), TextAnchor.MiddleLeft, FontStyle.Italic);
            Fill(hint.rectTransform, 20, 20, 4, 4);
            hint.GetComponent<Shadow>().enabled = false;
            field.textComponent = text;
            field.placeholder = hint;
            return field;
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
            v.Star = art[Key("fx.sparkle", "fx.star")];
            return v;
        }

        private static ToastView BuildToast(Transform root)
        {
            var bg = Img(root, "Toast", "ui.round", new Color(0, 0, 0, 0.82f));
            At(bg.rectTransform, 0.5f, 0.5f, 0, -340, 900, 84);
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
            var root = NewUI(name, screen);
            At(root, 0.5f, 0.5f, side * 640, -90, 520, 500);
            var hud = root.gameObject.AddComponent<CombatantHud>();
            hud.Hp = MakeThinBar(top, name + "Hp", new Color(0.92f, 0.2f, 0.22f), side * 295, 40, player);
            hud.Mana = MakeThinBar(top, name + "Mana", new Color(0.2f, 0.62f, 1f), side * 295, 0, player);
            hud.Rage = MakeThinBar(top, name + "Rage", new Color(1f, 0.8f, 0.15f), side * 295, -40, player);

            const float feet = -230f;
            hud.NameLabel = Txt(root, "Name", "Name", 34, player ? Color.white : new Color(1f, 0.5f, 0.42f), TextAnchor.MiddleCenter, FontStyle.Bold);
            At(hud.NameLabel.rectTransform, 0.5f, 0.5f, 0, feet - 64, 270, 40);
            hud.NameLabel.resizeTextForBestFit = true;
            hud.NameLabel.resizeTextMinSize = 22;
            hud.NameLabel.verticalOverflow = VerticalWrapMode.Truncate;
            hud.NameLabel.resizeTextMaxSize = 34;
            var nameOutline = hud.NameLabel.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = new Color(0.08f, 0.04f, 0.02f, 1f);
            nameOutline.effectDistance = new Vector2(2, -2);
            var marker = Img(root, "TurnMarker", "ui.ring", new Color(1f, 0.85f, 0.3f, 0.9f));
            At(marker.rectTransform, 0.5f, 0.5f, 0, feet, 380, 84);
            hud.TurnMarker = marker.gameObject;
            var arrow = Img(root, "AttackArrow", "battle.arrow", Color.white);
            arrow.preserveAspect = true;
            At(arrow.rectTransform, 0.5f, 0.5f, -side * 190, feet - 64, 100, 100);
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
            At(hud.StatusLabel.rectTransform, 0.5f, 0.5f, 0, feet - 104, 520, 40);
            hud.ElementIcon = Img(screen, name + "Element", "element.Fire", Color.white);
            hud.ElementIcon.preserveAspect = true;
            At(hud.ElementIcon.rectTransform, player ? 0 : 1, 0, player ? 72 : -72, 72, 104, 104);
            return hud;
        }

        private static BarView MakeThinBar(Transform parent, string name, Color c, float x, float y, bool leftToRight)
        {
            var bg = NewUI(name, parent);
            At(bg, 0.5f, 0.5f, x, y, 420, 32);
            var bv = bg.gameObject.AddComponent<BarView>();
            var body = NewUI("Body", bg);
            Fill(body);
            if (!leftToRight) body.localScale = new Vector3(-1f, 1f, 1f);
            var rim = Img(body, "Rim", "bar.shape", new Color(0.01f, 0.02f, 0.07f, 1f));
            Fill(rim.rectTransform);
            SliceTo(rim, 9);
            var back = Img(body, "Back", "bar.shape", new Color(0.1f, 0.16f, 0.34f, 1f));
            Fill(back.rectTransform, 3, 3, 3, 3);
            SliceTo(back, 8);
            var area = NewUI("FillArea", body);
            Fill(area, 3, 3, 3, 3);
            bv.Under = Img(area, "Under", "bar.shape", c);
            Fill(bv.Under.rectTransform);
            SliceTo(bv.Under, 8);
            bv.Under.enabled = false;
            bv.Fill = Img(area, "Fill", "bar.shape", c);
            Fill(bv.Fill.rectTransform);
            SliceTo(bv.Fill, 8);
            var gloss = Img(bv.Fill.transform, "Gloss", "bar.gloss", Color.white);
            Fill(gloss.rectTransform);
            SliceTo(gloss, 8);
            bv.Label = Txt(bg, "Label", "0/0", 21, Color.white, leftToRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, FontStyle.Bold);
            Fill(bv.Label.rectTransform, 20, 20, 0, 0);
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
            v.Value = CardValue(v.Icon.transform, 34);
            bool slot = art.ContainsKey("card.slot");
            v.Frame = Img(bg.transform, "Frame", slot ? "card.slot" : "ui.frame", slot ? Color.white : new Color(0.35f, 0.55f, 0.85f));
            Fill(v.Frame.rectTransform, -6, -6, -6, -6);
            if (slot) SliceTo(v.Frame, 22);
            var costBg = Img(bg.transform, "CostBar", "ui.round", new Color(0.08f, 0.3f, 0.75f, 0.96f));
            At(costBg.rectTransform, 0, 1, 42, -15, 80, 30);
            costBg.raycastTarget = false;
            var bolt = Img(costBg.transform, "Bolt", "gem.Lightning", Color.white);
            bolt.preserveAspect = true;
            At(bolt.rectTransform, 0, 0.5f, 15, 0, 24, 24);
            v.Cost = Txt(costBg.transform, "Cost", "0", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(v.Cost.rectTransform, 26, 4, 0, 0);
            v.CostBar = costBg.gameObject;
            var badge = Img(bg.transform, "LevelBadge", "ui.circle", new Color(1f, 0.72f, 0.12f));
            At(badge.rectTransform, 1, 1, -13, -13, 38, 38);
            badge.raycastTarget = false;
            v.Level = Txt(badge.transform, "Level", "1", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(v.Level.rectTransform);
            v.Level.gameObject.AddComponent<Outline>().effectColor = new Color(0.4f, 0.18f, 0f, 1f);
            v.LevelBadge = badge.gameObject;
            var powerBg = Img(bg.transform, "PowerBar", "ui.round", new Color(0.05f, 0.08f, 0.2f, 0.92f));
            At(powerBg.rectTransform, 0.5f, 0, 0, 19, 104, 30);
            powerBg.raycastTarget = false;
            var sword = Img(powerBg.transform, "Sword", "gem.Sword", Color.white);
            sword.preserveAspect = true;
            At(sword.rectTransform, 0, 0.5f, 17, 0, 24, 24);
            v.PowerIcon = sword;
            v.Power = Txt(powerBg.transform, "Power", "0", 20, new Color(1f, 0.75f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(v.Power.rectTransform, 28, 4, 0, 0);
            v.PowerBar = powerBg.gameObject;
            return v;
        }

        private static Text CardValue(Transform face, int size)
        {
            var t = Txt(face, "Value", "", size, new Color(1f, 0.92f, 0.25f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.2f);
            rt.anchorMax = new Vector2(1f, 0.52f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, 8f);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 14;
            t.resizeTextMaxSize = size;
            t.raycastTarget = false;
            var ink = t.gameObject.AddComponent<Outline>();
            ink.effectColor = new Color(0.25f, 0.05f, 0.02f, 1f);
            ink.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        private static RowView MakeRow(Transform parent, string name, float w, float h, Color bgColor, float icon, int extras, int titleSize, int subSize, Color? extraA = null)
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
            var cols = new[] { extraA ?? Blue, Green, Gold };
            for (int i = 0; i < extras; i++)
            {
                var b = Btn(bg.transform, "Extra" + i, "", cols[i], 18, out var label);
                At(Rt(b), 1, 0.5f, -(extras - i) * 112 + 48, 0, 104, h - 22);
                SliceTo((Image)b.targetGraphic, 18);
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
            bool painted = art.ContainsKey("ui.checkbox.off") && art.ContainsKey("ui.checkbox.on");
            var box = painted ? Img(root, "Box", "ui.checkbox.off", Color.white) : Img(root, "Box", "ui.round", new Color(0.9f, 0.9f, 0.95f), true);
            At(box.rectTransform, 0, 0.5f, 28, 0, painted ? 54 : 46, painted ? 54 : 46);
            var check = Img(box.transform, "Check", painted ? "ui.checkbox.on" : "ui.round", painted ? Color.white : Green);
            if (painted) Fill(check.rectTransform);
            else At(check.rectTransform, 0.5f, 0.5f, 0, 0, 28, 28);
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

        private static Text Chip(Transform parent, string name, string iconKey, Color iconTint, float x, float w = 220f)
        {
            var bg = Img(parent, name, "ui.round", new Color(0.05f, 0.08f, 0.18f, 0.8f));
            At(bg.rectTransform, 0.5f, 0.5f, x, 0, w, 60);
            var icon = Img(bg.transform, "Icon", iconKey, iconTint);
            icon.preserveAspect = true;
            At(icon.rectTransform, 0, 0.5f, 36, 0, 50, 50);
            var value = Txt(bg.transform, "Value", "0", 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Fill(value.rectTransform, 72, 8, 0, 0);
            return value;
        }

        private static void SkinPanel(Image img, float edgePx)
        {
            if (!Skin(img, "ui.panel", edgePx)) return;
            img.fillCenter = false;
            var inner = Img(img.transform, "Inner", null, new Color(0.1f, 0.12f, 0.19f, 0.97f));
            float inset = edgePx * 0.6f;
            Fill(inner.rectTransform, inset, inset, inset, inset);
            inner.transform.SetAsFirstSibling();
        }

        private static bool Skin(Image img, string key, float edgePx = 0f)
        {
            if (!art.TryGetValue(key, out var sp)) return false;
            img.sprite = sp;
            img.color = Color.white;
            img.type = sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            if (edgePx > 0f) SliceTo(img, edgePx);
            return true;
        }

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
            bool heavy = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            t.font = heavy ? boldFont : font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.fontStyle = !heavy || boldFont == font ? style : style == FontStyle.Bold ? FontStyle.Normal : FontStyle.Italic;
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
            string key = c == Green ? "ui.button.green" : c == Blue ? "ui.button.blue" : c == Red ? "ui.button.red" : c == Gray ? "ui.button.gray" : c == Gold ? "ui.button.orange" : null;
            bool skin = key != null && art.ContainsKey(key);
            var img = Img(parent, name, skin ? key : "ui.round", skin ? Color.white : c, true);
            if (skin) SliceTo(img, 24);
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
