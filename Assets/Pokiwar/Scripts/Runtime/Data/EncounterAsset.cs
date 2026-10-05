using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>One fight: enemy, level, start HP, AI, reward table, energy cost.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Encounter")]
    public sealed class EncounterAsset : ScriptableObject
    {
        public EncounterDef Def = new EncounterDef();
    }
}