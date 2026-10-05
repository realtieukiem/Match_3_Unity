using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>EXP, gold and drops granted once per won battle.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/RewardTable")]
    public sealed class RewardTableAsset : ScriptableObject
    {
        public RewardTable Def = new RewardTable();
    }
}