using System;
using System.Collections.Generic;
using Pokiwar.Domain;
using UnityEngine;

namespace Pokiwar.App
{
    [Serializable]
    public sealed class KeyedSprite
    {
        public string Key;
        public Sprite Sprite;
    }

    /// <summary>Every placeholder sprite by key. Replace the sprites here (or the PNGs they point at) with final art.</summary>
    [CreateAssetMenu(menuName = "Pokiwar/Sprite Library")]
    public sealed class SpriteLibrary : ScriptableObject
    {
        public Sprite[] Gems = new Sprite[Pokiwar.Domain.Gems.Count];
        public List<KeyedSprite> Sprites = new List<KeyedSprite>();
        public Sprite Fallback;

        private Dictionary<string, Sprite> map;

        public Sprite Gem(GemType t) => t == GemType.None ? null : Gems[(int)t];

        public Sprite Get(string key)
        {
            key = Resolve(key);
            return key != null && Map.TryGetValue(key, out var s) && s != null ? s : Fallback;
        }

        public string Resolve(string key) =>
            key != null && !Has(key) && key.EndsWith(SpriteKeys.EvolvedSuffix) ? key.Substring(0, key.Length - SpriteKeys.EvolvedSuffix.Length) : key;

        public bool Has(string key) => key != null && Map.TryGetValue(key, out var s) && s != null;

        /// <summary>The key of the art that faces the given way: "right." + key when the library has one, else key.</summary>
        public string Facing(string key, bool right)
        {
            key = Resolve(key);
            return right && Has("right." + key) ? "right." + key : key;
        }

        public Sprite Owned(string key) => Get(Facing(key, true));

        private Dictionary<string, Sprite> Map
        {
            get
            {
                if (map == null)
                {
                    map = new Dictionary<string, Sprite>();
                    foreach (var k in Sprites) if (k != null && !string.IsNullOrEmpty(k.Key)) map[k.Key] = k.Sprite;
                }
                return map;
            }
        }

        public static Color GemColor(GemType t)
        {
            switch (t)
            {
                case GemType.Sword: return new Color(1f, 0.82f, 0.2f);
                case GemType.Lightning: return new Color(0.25f, 0.55f, 1f);
                case GemType.Fire: return new Color(0.95f, 0.25f, 0.2f);
                case GemType.Heart: return new Color(0.3f, 0.85f, 0.35f);
                case GemType.Shield: return new Color(0.65f, 0.35f, 0.95f);
                case GemType.YinYang: return new Color(0.95f, 0.95f, 0.95f);
                default: return Color.gray;
            }
        }

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Metal: return new Color(0.85f, 0.85f, 0.7f);
                case Element.Wood: return new Color(0.35f, 0.8f, 0.35f);
                case Element.Water: return new Color(0.3f, 0.6f, 1f);
                case Element.Fire: return new Color(1f, 0.45f, 0.25f);
                case Element.Earth: return new Color(0.8f, 0.6f, 0.3f);
                default: return Color.gray;
            }
        }
    }
}
