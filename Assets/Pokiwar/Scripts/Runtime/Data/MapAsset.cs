using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>PvE regions and nodes.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Map")]
    public sealed class MapAsset : ScriptableObject
    {
        public MapDef Def = new MapDef();
    }
}