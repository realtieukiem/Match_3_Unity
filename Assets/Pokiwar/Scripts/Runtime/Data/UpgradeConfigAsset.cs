using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Stone merge and pet enhancement rates (PROVISIONAL tuning).</summary>
    [CreateAssetMenu(menuName = "Pokiwar/UpgradeConfig")]
    public sealed class UpgradeConfigAsset : ScriptableObject
    {
        public UpgradeConfig Def = new UpgradeConfig();
    }
}