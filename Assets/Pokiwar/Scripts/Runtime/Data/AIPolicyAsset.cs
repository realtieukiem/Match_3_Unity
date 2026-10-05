using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>AI weights, randomness and QTE skill.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/AIPolicy")]
    public sealed class AIPolicyAsset : ScriptableObject
    {
        public AIPolicy Def = new AIPolicy();
    }
}