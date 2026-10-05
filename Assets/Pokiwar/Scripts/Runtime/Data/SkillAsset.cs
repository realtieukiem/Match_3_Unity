using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>Pet skill: costs, QTE profile, effect chain.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Skill")]
    public sealed class SkillAsset : ScriptableObject
    {
        public SkillDef Def = new SkillDef();
    }
}