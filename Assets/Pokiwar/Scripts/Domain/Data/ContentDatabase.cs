using System;
using System.Collections.Generic;

namespace Pokiwar.Domain
{
    /// <summary>Plain lookup over all game data. Built from ScriptableObjects at run time, or from DefaultContent in tests.</summary>
    public sealed class ContentDatabase
    {
        public readonly List<CreatureDef> Creatures = new List<CreatureDef>();
        public readonly List<CardDef> Cards = new List<CardDef>();
        public readonly List<SkillDef> Skills = new List<SkillDef>();
        public readonly List<GemEffectProfile> GemProfiles = new List<GemEffectProfile>();
        public readonly List<AIPolicy> AiPolicies = new List<AIPolicy>();
        public readonly List<EncounterDef> Encounters = new List<EncounterDef>();
        public readonly List<RewardTable> RewardTables = new List<RewardTable>();
        public readonly List<AvatarItemDef> AvatarItems = new List<AvatarItemDef>();
        public MapDef Map = new MapDef();
        public BoardRuleProfile Board = new BoardRuleProfile();
        public BattleRules Rules = new BattleRules();
        public UpgradeConfig Upgrades = new UpgradeConfig();
        public ProgressionConfig Progression = new ProgressionConfig();

        public CreatureDef Creature(string id) => Find(Creatures, c => c.Id == id, id);
        public CardDef Card(string id) => Find(Cards, c => c.Id == id, id);
        public SkillDef Skill(string id) => Find(Skills, c => c.Id == id, id);
        public EncounterDef Encounter(string id) => Find(Encounters, c => c.Id == id, id);
        public RewardTable Reward(string id) => Find(RewardTables, c => c.Id == id, id);
        public MapNodeDef Node(string id) => Find(Map.Nodes, c => c.Id == id, id);

        public GemEffectProfile GemProfile(string id) => GemProfiles.Find(g => g.Id == id) ?? new GemEffectProfile();
        public AIPolicy Ai(string id) => AiPolicies.Find(a => a.Id == id) ?? new AIPolicy();

        public CardDef TryCard(string id) => Cards.Find(c => c.Id == id);
        public SkillDef TrySkill(string id) => Skills.Find(c => c.Id == id);
        public CreatureDef TryCreature(string id) => Creatures.Find(c => c.Id == id);
        public AvatarItemDef TryAvatarItem(string id) => AvatarItems.Find(c => c.Id == id);

        private static T Find<T>(List<T> list, Predicate<T> p, string id) where T : class
        {
            var r = list.Find(p);
            if (r == null) throw new KeyNotFoundException(typeof(T).Name + " '" + id + "' is missing from content");
            return r;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            foreach (var e in Encounters)
            {
                if (TryCreature(e.CreatureId) == null) errors.Add("Encounter " + e.Id + " creature " + e.CreatureId);
                if (RewardTables.Find(r => r.Id == e.RewardTableId) == null) errors.Add("Encounter " + e.Id + " reward " + e.RewardTableId);
            }
            foreach (var n in Map.Nodes)
            {
                if (Encounters.Find(e => e.Id == n.EncounterId) == null) errors.Add("Node " + n.Id + " encounter " + n.EncounterId);
                if (!string.IsNullOrEmpty(n.RequiresNodeId) && Map.Nodes.Find(x => x.Id == n.RequiresNodeId) == null) errors.Add("Node " + n.Id + " requires " + n.RequiresNodeId);
            }
            foreach (var c in Creatures)
            {
                foreach (var s in c.SkillIds) if (Skills.Find(x => x.Id == s) == null) errors.Add("Creature " + c.Id + " skill " + s);
                foreach (var s in c.CardIds) if (TryCard(s) == null) errors.Add("Creature " + c.Id + " card " + s);
                foreach (var p in c.Phases)
                    foreach (var s in p.UnlockSkillIds)
                        if (Skills.Find(x => x.Id == s) == null) errors.Add("Phase " + p.Id + " skill " + s);
            }
            foreach (var id in Progression.StarterAvatarIds)
                if (TryAvatarItem(id) == null) errors.Add("Starter avatar item " + id);
            return errors;
        }
    }
}
