using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Board size, spawn odds and PROVISIONAL swap/dead-board rules.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/BoardRule")]
    public sealed class BoardRuleAsset : ScriptableObject
    {
        public BoardRuleProfile Def = new BoardRuleProfile();
    }
}