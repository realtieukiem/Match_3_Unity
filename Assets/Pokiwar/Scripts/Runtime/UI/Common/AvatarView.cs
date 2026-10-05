using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Paper-doll player avatar: the base body plus one layer image per AvatarSlot, all on the same canvas.</summary>
    public sealed class AvatarView : MonoBehaviour
    {
        public Image Body;
        public Image[] Layers = new Image[4];

        public void Show(AvatarLook look, ContentDatabase db, SpriteLibrary sprites)
        {
            foreach (var l in Layers) if (l != null) l.gameObject.SetActive(false);
            if (look == null) return;
            if (Body != null) Body.sprite = sprites.Get("avatar.base");
            foreach (var id in look.ItemIds)
            {
                var item = db.TryAvatarItem(id);
                if (item == null) continue;
                var img = Layers[(int)item.Slot];
                if (img == null) continue;
                img.sprite = sprites.Get(item.SpriteKey);
                img.gameObject.SetActive(true);
            }
        }

        public static Sprite Icon(AvatarItemDef item, SpriteLibrary sprites) =>
            sprites.Has(item.SpriteKey + ".icon") ? sprites.Get(item.SpriteKey + ".icon") : sprites.Get(item.SpriteKey);
    }
}
