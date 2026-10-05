using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Player pet: base stats, per-level growth, element, skills.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Pet")]
    public sealed class PetAsset : ScriptableObject
    {
        public CreatureDef Def = new CreatureDef();
    }
}