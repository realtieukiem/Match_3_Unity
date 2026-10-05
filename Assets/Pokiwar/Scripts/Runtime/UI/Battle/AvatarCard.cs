using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>A side's player card in battle: avatar bust and name. Hidden when that side is a wild monster.</summary>
    public sealed class AvatarCard : MonoBehaviour
    {
        public AvatarView Avatar;
        public Text NameLabel;

        public void Show(AvatarLook look, ContentDatabase db, SpriteLibrary sprites)
        {
            gameObject.SetActive(look != null);
            if (look == null) return;
            Avatar.Show(look, db, sprites);
            if (NameLabel != null) NameLabel.text = look.Name;
        }
    }
}
