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
        public HubScreen Hub;
        public MapScreen Map;
        public PrepScreen Prep;
        public GameObject BattleScreen;
        public BattleController Battle;
        public ResultScreen Result;
        public UpgradeScreen Upgrade;
        public ToastView ToastView;

        public ContentDatabase Db { get; private set; }
        public ProgressionService Progression { get; private set; }
        public UpgradeService Upgrades { get; private set; }
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
            saves = new SaveService(new FileSaveStore(), new JsonSaveSerializer(), Progression);
            Save = saves.LoadOrCreate();
            Rng = new SeededRng(Save.RngState);
        }

        private void Start()
        {
            ShowHub();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
            ShowHub();
            Toast("Save reset");
        }

        private void HideAll()
        {
            Hub.gameObject.SetActive(false);
            Map.gameObject.SetActive(false);
            Prep.gameObject.SetActive(false);
            BattleScreen.SetActive(false);
            Result.gameObject.SetActive(false);
            Upgrade.gameObject.SetActive(false);
        }

        public void ShowHub()
        {
            Progression.TickEnergy(Save, DateTime.UtcNow.Ticks);
            HideAll();
            Hub.gameObject.SetActive(true);
            Hub.Show(this);
        }

        public void ShowMap()
        {
            Progression.TickEnergy(Save, DateTime.UtcNow.Ticks);
            HideAll();
            Map.gameObject.SetActive(true);
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
            Upgrade.Show(this);
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
        }

        private void OnBattleFinished(BattleReport report)
        {
            var grant = Progression.CommitBattle(Save, report);
            if (grant != null)
            {
                LastGrant = grant;
                Persist();
            }
            HideAll();
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
