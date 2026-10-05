using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Battle card: costs, uses, timing flags, effect chain.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Card")]
    public sealed class CardAsset : ScriptableObject
    {
        public CardDef Def = new CardDef();
    }
}