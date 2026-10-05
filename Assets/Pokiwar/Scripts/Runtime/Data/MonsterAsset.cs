using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Monster or boss: stats, cards, skills and BossPhase list.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Monster")]
    public sealed class MonsterAsset : ScriptableObject
    {
        public CreatureDef Def = new CreatureDef();
    }
}