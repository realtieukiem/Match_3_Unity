using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Starter kit, energy and EXP curves.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/ProgressionConfig")]
    public sealed class ProgressionConfigAsset : ScriptableObject
    {
        public ProgressionConfig Def = new ProgressionConfig();
    }
}