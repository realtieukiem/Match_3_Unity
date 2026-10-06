using System.Text;
using Pokiwar.Data;
using Pokiwar.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokiwar.EditorTools
{
    /// <summary>Headless AI-vs-AI runs over the real content assets: win rate and turn count per encounter and pet level.</summary>
    public static class PokiwarBalance
    {
        [MenuItem("Pokiwar/Balance Report (AI vs AI)")]
        public static void Report()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ContentCatalog>(PokiwarContentSeeder.CatalogPath);
            var db = catalog != null ? catalog.Build() : DefaultContent.Create();
            var sb = new StringBuilder("[BALANCE]\n");
            foreach (var node in db.Map.Nodes)
            {
                foreach (int level in new[] { 1, 3, 5, 8, 12 })
                {
                    int wins = 0, turns = 0, runs = 40;
                    for (int i = 0; i < runs; i++)
                    {
                        var r = Simulate(db, node, level, (uint)(1000 + i * 7919));
                        if (r.won) wins++;
                        turns += r.turns;
                    }
                    sb.AppendLine(node.Id + " pet Lv " + level + ": win " + (wins * 100 / runs) + "%  avg turns " + (turns / runs));
                }
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Pokiwar/Reset Content Assets To Defaults")]
        public static void ResetContent() => PokiwarContentSeeder.Seed(true);

        public static void BatchReseedAndReport()
        {
            PokiwarContentSeeder.Seed(true);
            BatchReport();
        }

        public static void BatchReport()
        {
            Report();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static (bool won, int turns) Simulate(ContentDatabase db, MapNodeDef node, int petLevel, uint seed)
        {
            var prog = new ProgressionService(db);
            var save = prog.CreateNewSave(0);
            save.Pets[0].Level = petLevel;
            var e = new BattleEngine(prog.StartBattle(save, node, seed));
            var rng = new SeededRng(seed ^ 0x5151u);
            int guard = 0;
            while (!e.State.Ended && guard++ < 400)
            {
                e.BeginTurn();
                if (e.State.Ended) break;
                var side = e.State.Current;
                var me = e.State.Get(side);
                var d = AIPlanner.Decide(e, side, me.Ai, rng);
                foreach (var c in d.CardsBeforeMatch) e.UseCard(side, c);
                bool acted = false;
                if (!e.State.Ended && d.SkillIndex >= 0 && e.PrepareSkill(side, d.SkillIndex).Ok)
                    acted = e.UseSkill(side, d.SkillIndex, AIPlanner.RollQte(me.Ai, me.Skills[d.SkillIndex].Qte, rng)).Accepted;
                if (!acted && !e.State.Ended)
                {
                    var d2 = AIPlanner.Decide(e, side, new AIPolicy { UseCards = false, UseSkills = false }, rng);
                    if (d2.HasSwap) e.Swap(side, d2.Swap.A, d2.Swap.B);
                    else e.Timeout(side);
                }
                e.EndTurn();
            }
            return (e.State.Ended && e.State.Winner == Side.Player, e.State.TurnNumber);
        }
    }
}
