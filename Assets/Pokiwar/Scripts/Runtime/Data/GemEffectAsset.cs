using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Per-actor gem yields. Values are tuning data, not engine constants.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/GemEffect")]
    public sealed class GemEffectAsset : ScriptableObject
    {
        public GemEffectProfile Def = new GemEffectProfile();
    }
}