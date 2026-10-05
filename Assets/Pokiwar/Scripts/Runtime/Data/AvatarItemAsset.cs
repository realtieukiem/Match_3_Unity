using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.Data
{
    /// <summary>One wearable avatar piece: slot, price and the sprite key of its paper-doll layer.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Avatar Item")]
    public sealed class AvatarItemAsset : ScriptableObject
    {
        public AvatarItemDef Def = new AvatarItemDef();
    }
}
