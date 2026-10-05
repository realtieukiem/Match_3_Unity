using System.Collections.Generic;
using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>The one asset the game loads. Lists every content ScriptableObject; edit those assets to tune.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Content Catalog")]
    public sealed class ContentCatalog : ScriptableObject
    {
        public List<PetAsset> Pets = new List<PetAsset>();
        public List<MonsterAsset> Monsters = new List<MonsterAsset>();
        public List<CardAsset> Cards = new List<CardAsset>();
        public List<SkillAsset> Skills = new List<SkillAsset>();
        public List<GemEffectAsset> GemProfiles = new List<GemEffectAsset>();
        public List<AIPolicyAsset> AiPolicies = new List<AIPolicyAsset>();
        public List<EncounterAsset> Encounters = new List<EncounterAsset>();
        public List<RewardTableAsset> Rewards = new List<RewardTableAsset>();
        public List<AvatarItemAsset> AvatarItems = new List<AvatarItemAsset>();
        public MapAsset Map;
        public BoardRuleAsset Board;
        public BattleRulesAsset BattleRules;
        public UpgradeConfigAsset Upgrades;
        public ProgressionConfigAsset Progression;

        public ContentDatabase Build()
        {
            var db = new ContentDatabase();
            foreach (var a in Pets) if (a != null) db.Creatures.Add(a.Def);
            foreach (var a in Monsters) if (a != null) db.Creatures.Add(a.Def);
            foreach (var a in Cards) if (a != null) db.Cards.Add(a.Def);
            foreach (var a in Skills) if (a != null) db.Skills.Add(a.Def);
            foreach (var a in GemProfiles) if (a != null) db.GemProfiles.Add(a.Def);
            foreach (var a in AiPolicies) if (a != null) db.AiPolicies.Add(a.Def);
            foreach (var a in Encounters) if (a != null) db.Encounters.Add(a.Def);
            foreach (var a in Rewards) if (a != null) db.RewardTables.Add(a.Def);
            foreach (var a in AvatarItems) if (a != null) db.AvatarItems.Add(a.Def);
            if (Map != null) db.Map = Map.Def;
            if (Board != null) db.Board = Board.Def;
            if (BattleRules != null) db.Rules = BattleRules.Def;
            if (Upgrades != null) db.Upgrades = Upgrades.Def;
            if (Progression != null) db.Progression = Progression.Def;
            foreach (var e in db.Validate()) Debug.LogError("[Pokiwar] Content error: " + e);
            return db;
        }
    }
}
