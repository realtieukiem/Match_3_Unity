using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Element relation/bonus chart and defense policy.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/BattleRules")]
    public sealed class BattleRulesAsset : ScriptableObject
    {
        public BattleRules Def = new BattleRules();
    }
}