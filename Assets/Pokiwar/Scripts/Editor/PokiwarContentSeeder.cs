using System.Collections.Generic;
using System.IO;
using Pokiwar.App;
using Pokiwar.Data;
using Pokiwar.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokiwar.EditorTools
{
    /// <summary>Writes DefaultContent into ScriptableObject assets. Existing assets are kept so Inspector tuning survives a re-run.</summary>
    public static class PokiwarContentSeeder
    {
        public const string DataFolder = "Assets/Pokiwar/Data";
        public const string CatalogPath = DataFolder + "/ContentCatalog.asset";
        public const string SpritesPath = DataFolder + "/SpriteLibrary.asset";

        public static ContentCatalog Seed(bool overwrite)
        {
            var db = DefaultContent.Create();
            var catalog = LoadOrCreate<ContentCatalog>(CatalogPath, out _);
            catalog.Pets.Clear();
            catalog.Monsters.Clear();
            catalog.Cards.Clear();
            catalog.Skills.Clear();
            catalog.GemProfiles.Clear();
            catalog.AiPolicies.Clear();
            catalog.Encounters.Clear();
            catalog.Rewards.Clear();
            catalog.AvatarItems.Clear();

            foreach (var c in db.Creatures)
            {
                if (c.Id.StartsWith("pet.")) catalog.Pets.Add(Asset<PetAsset, CreatureDef>("Pets", c.Id, c, overwrite, (a, d) => a.Def = d));
                else catalog.Monsters.Add(Asset<MonsterAsset, CreatureDef>("Monsters", c.Id, c, overwrite, (a, d) => a.Def = d));
            }
            foreach (var c in db.Cards) catalog.Cards.Add(Asset<CardAsset, CardDef>("Cards", c.Id, c, overwrite, (a, d) => a.Def = d));
            foreach (var s in db.Skills) catalog.Skills.Add(Asset<SkillAsset, SkillDef>("Skills", s.Id, s, overwrite, (a, d) => a.Def = d));
            foreach (var g in db.GemProfiles) catalog.GemProfiles.Add(Asset<GemEffectAsset, GemEffectProfile>("GemProfiles", g.Id, g, overwrite, (a, d) => a.Def = d));
            foreach (var p in db.AiPolicies) catalog.AiPolicies.Add(Asset<AIPolicyAsset, AIPolicy>("AI", p.Id, p, overwrite, (a, d) => a.Def = d));
            foreach (var e in db.Encounters) catalog.Encounters.Add(Asset<EncounterAsset, EncounterDef>("Encounters", e.Id, e, overwrite, (a, d) => a.Def = d));
            foreach (var r in db.RewardTables) catalog.Rewards.Add(Asset<RewardTableAsset, RewardTable>("Rewards", r.Id, r, overwrite, (a, d) => a.Def = d));
            catalog.Map = Asset<MapAsset, MapDef>("Rules", "map.main", db.Map, overwrite, (a, d) => a.Def = d);
            catalog.Board = Asset<BoardRuleAsset, BoardRuleProfile>("Rules", "board.default", db.Board, overwrite, (a, d) => a.Def = d);
            catalog.BattleRules = Asset<BattleRulesAsset, BattleRules>("Rules", "battle.rules", db.Rules, overwrite, (a, d) => a.Def = d);
            catalog.Upgrades = Asset<UpgradeConfigAsset, UpgradeConfig>("Rules", "upgrade.config", db.Upgrades, overwrite, (a, d) => a.Def = d);
            foreach (var i in db.AvatarItems) catalog.AvatarItems.Add(Asset<AvatarItemAsset, AvatarItemDef>("Avatar", i.Id, i, overwrite, (a, d) => a.Def = d));
            catalog.Progression = Asset<ProgressionConfigAsset, ProgressionConfig>("Rules", "progression.config", db.Progression, overwrite, (a, d) => a.Def = d);
            var prog = catalog.Progression.Def;
            if (prog.StarterAvatarIds == null || prog.StarterAvatarIds.Count == 0)
            {
                prog.StarterAvatarIds = new List<string>(db.Progression.StarterAvatarIds);
                if (string.IsNullOrEmpty(prog.DefaultPlayerName)) prog.DefaultPlayerName = db.Progression.DefaultPlayerName;
                EditorUtility.SetDirty(catalog.Progression);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[POKIWAR-BUILD] content: " + catalog.Pets.Count + " pets, " + catalog.Monsters.Count + " monsters, " + catalog.Cards.Count + " cards, " + catalog.Encounters.Count + " encounters");
            return catalog;
        }

        public static SpriteLibrary BuildSprites(Dictionary<string, Sprite> art)
        {
            var lib = LoadOrCreate<SpriteLibrary>(SpritesPath, out _);
            lib.Gems = new Sprite[Gems.Count];
            foreach (var g in Gems.All) lib.Gems[(int)g] = art["gem." + g];
            lib.Sprites.Clear();
            foreach (var kv in art)
            {
                if (kv.Key.StartsWith("gem.")) continue;
                lib.Sprites.Add(new KeyedSprite { Key = kv.Key, Sprite = kv.Value });
            }
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
                if (!art.ContainsKey("stone." + e)) lib.Sprites.Add(new KeyedSprite { Key = "stone." + e, Sprite = art["stone"] });
            if (!art.ContainsKey("stone.card")) lib.Sprites.Add(new KeyedSprite { Key = "stone.card", Sprite = art["stone"] });
            lib.Fallback = art["ui.circle"];
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return lib;
        }

        private static TAsset Asset<TAsset, TDef>(string folder, string id, TDef def, bool overwrite, System.Action<TAsset, TDef> assign) where TAsset : ScriptableObject
        {
            string path = DataFolder + "/" + folder + "/" + id + ".asset";
            var a = LoadOrCreate<TAsset>(path, out bool created);
            if (created || overwrite)
            {
                assign(a, def);
                EditorUtility.SetDirty(a);
            }
            return a;
        }

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            created = a == null;
            if (a != null) return a;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }
    }
}
