using System;
using Pokiwar.Data;
using Pokiwar.Domain;
using Pokiwar.UI;
using UnityEngine;

namespace Pokiwar.App
{
    /// <summary>Scene entry point. Owns content, save and the screen flow Hub -> Map -> Preparation -> Battle -> Result.</summary>
    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }

        public ContentCatalog Catalog;
        public SpriteLibrary Sprites;

        [Header("Screens")]
        public HomeScreen Home;
        public WorldScreen World;
        public HubScreen Hub;
        public MapScreen Map;
        public PrepScreen Prep;
        public GameObject BattleScreen;
        public BattleController Battle;
        public ResultScreen Result;
        public UpgradeScreen Upgrade;
        public AvatarScreen Wardrobe;
        public CardForgeScreen CardForge;
        public ShopScreen ShopScreen;
        public ToastView ToastView;
        public AudioDirector Audio;

        public ContentDatabase Db { get; private set; }
        public ProgressionService Progression { get; private set; }
        public UpgradeService Upgrades { get; private set; }
        public AvatarService Avatars { get; private set; }
        public ShopService Shop { get; private set; }
        public SaveData Save { get; private set; }
        public SeededRng Rng { get; private set; }
        public RewardGrant LastGrant { get; private set; }
        public MapNodeDef CurrentNode { get; private set; }

        private SaveService saves;

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Db = Catalog != null ? Catalog.Build() : DefaultContent.Create();
            Progression = new ProgressionService(Db);
            Upgrades = new UpgradeService(Db);
            Avatars = new AvatarService(Db);
            Shop = new ShopService(Progression);
            saves = new SaveService(new FileSaveStore(), new JsonSaveSerializer(), Progression);
            Save = saves.LoadOrCreate();
            Rng = new SeededRng(Save.RngState);
        }

        private void Start()
        {
            ApplySettings();
            ShowHome();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ApplySettings()
        {
            if (Audio != null) Audio.Apply(Save.MusicOn, Save.SfxOn);
            if (Battle != null && Battle.Shake != null) Battle.Shake.Enabled = Save.ShakeOn;
            Persist();
        }

        public void Persist()
        {
            Save.RngState = Rng.State;
            saves.Save(Save);
        }

        public void ResetSave()
        {
            Save = saves.Reset();
            Rng = new SeededRng(Save.RngState);
            ApplySettings();
            ShowHome();
            Toast("Save reset");
        }

        private void HideAll()
        {
            if (Home != null) Home.gameObject.SetActive(false);
            if (World != null) World.gameObject.SetActive(false);
            Hub.gameObject.SetActive(false);
            Map.gameObject.SetActive(false);
            Prep.gameObject.SetActive(false);
            BattleScreen.SetActive(false);
            Result.gameObject.SetActive(false);
            Upgrade.gameObject.SetActive(false);
            if (Wardrobe != null) Wardrobe.gameObject.SetActive(false);
            if (CardForge != null) CardForge.gameObject.SetActive(false);
            if (ShopScreen != null) ShopScreen.gameObject.SetActive(false);
        }

        public Sprite PetSprite(OwnedPet pet) => Sprites.Owned(Progression.PetSpriteKey(pet));

        public void ShowWardrobe()
        {
            HideAll();
            Wardrobe.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            Wardrobe.Show(this);
        }

        private bool cameFromLobby;

        public void Back()
        {
            if (cameFromLobby) ShowMap();
            else ShowHome();
        }

        public void ShowHome()
        {
            Progression.TickEnergy(Save, DateTime.UtcNow.Ticks);
            cameFromLobby = false;
            HideAll();
            Home.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            Home.Show(this);
        }

        public void ShowWorld()
        {
            if (World == null)
            {
                ShowMap();
                return;
            }
            cameFromLobby = false;
            HideAll();
            World.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            World.Show(this);
        }

        public void ShowHub()
        {
            Progression.TickEnergy(Save, DateTime.UtcNow.Ticks);
            HideAll();
            Hub.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            Hub.Show(this);
        }

        public void ShowMap()
        {
            Progression.TickEnergy(Save, DateTime.UtcNow.Ticks);
            cameFromLobby = true;
            HideAll();
            Map.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            Map.Show(this);
        }

        public void ShowPrep(MapNodeDef node)
        {
            CurrentNode = node;
            HideAll();
            Prep.gameObject.SetActive(true);
            Prep.Show(this, node);
        }

        public void ShowUpgrade()
        {
            HideAll();
            Upgrade.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            Upgrade.Show(this);
        }

        public void ShowCardForge()
        {
            HideAll();
            CardForge.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            CardForge.Show(this);
        }

        public void ShowShop()
        {
            HideAll();
            ShopScreen.gameObject.SetActive(true);
            if (Audio != null) Audio.PlayMusic("music.menu");
            ShopScreen.Show(this);
        }

        public void StartBattle(MapNodeDef node)
        {
            var check = Progression.CanEnter(Save, node);
            if (!check.Ok)
            {
                Toast(check.Reason);
                return;
            }
            CurrentNode = node;
            var setup = Progression.StartBattle(Save, node, Rng.NextUInt());
            Persist();
            HideAll();
            BattleScreen.SetActive(true);
            var enc = Db.Encounter(node.EncounterId);
            Battle.Begin(setup, enc, node.Id, Save.SelectedPetUid, Sprites, OnBattleFinished);
            if (Battle.PlayerCard != null) Battle.PlayerCard.Show(Avatars.Look(Save), Db, Sprites);
            if (Battle.EnemyCard != null) Battle.EnemyCard.Show(null, Db, Sprites);
        }

        private void OnBattleFinished(BattleReport report)
        {
            var grant = Progression.CommitBattle(Save, report);
            if (grant != null)
            {
                LastGrant = grant;
                Persist();
            }
            Result.gameObject.SetActive(true);
            Result.Show(this, report, LastGrant);
        }

        public void Toast(string msg)
        {
            if (ToastView != null) ToastView.Show(msg);
            else Debug.Log("[Pokiwar] " + msg);
        }

        public string ElementLabel(Element e, int bonus) => e + (bonus > 0 ? " +" + bonus : "");
    }
}
