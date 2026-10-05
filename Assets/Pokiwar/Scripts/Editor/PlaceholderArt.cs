using System;
using System.Collections.Generic;
using System.IO;
using Pokiwar.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokiwar.EditorTools
{
    /// <summary>Bakes simple, readable placeholder PNGs. Final art replaces these files (same names) or the SpriteLibrary entries.</summary>
    public static class PlaceholderArt
    {
        public const string Folder = "Assets/Pokiwar/Art/Placeholder";

        private delegate bool Shape(float x, float y);

        public static Dictionary<string, Sprite> BuildAll()
        {
            Directory.CreateDirectory(Folder);
            var made = new Dictionary<string, string>();

            made["ui.round"] = Write("ui_round", 64, (x, y) => RoundRect(x, y, 1f, 1f, 0.38f) ? Color.white : Color.clear);
            made["ui.frame"] = Write("ui_frame", 64, (x, y) => RoundRect(x, y, 1f, 1f, 0.38f) && !RoundRect(x, y, 0.78f, 0.78f, 0.26f) ? Color.white : Color.clear);
            made["ui.circle"] = Write("ui_circle", 128, (x, y) => x * x + y * y <= 1f ? Color.white : Color.clear);
            made["ui.ring"] = Write("ui_ring", 128, (x, y) => { float r = x * x + y * y; return r <= 1f && r >= 0.72f ? Color.white : Color.clear; });
            made["ui.arrow"] = Write("ui_arrow", 128, (x, y) => Arrow(x, y) ? Color.white : Color.clear);
            made["ui.gradient"] = Write("ui_gradient", 64, (x, y) => new Color(1, 1, 1, (y + 1f) * 0.5f));
            made["ui.map"] = WriteMap();
            made["fx.dot"] = Write("fx_dot", 64, (x, y) => { float d = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)); return new Color(1, 1, 1, d * d); });
            made["fx.star"] = Write("fx_star", 64, (x, y) => Star(x, y) ? Color.white : Color.clear);
            made["battle.arrow"] = Write("battle_arrow", 256, BattleArrow);
            made["fx.bubble"] = Write("fx_bubble", 512, ShieldBubble, 2);
            made["element.Neutral"] = WriteElement("element_neutral", Hex("#B8B2A6"), Star);
            made["element.Metal"] = WriteElement("element_metal", Hex("#9AA7B8"), Sword);
            made["element.Wood"] = WriteElement("element_wood", Hex("#4FA83D"), Leaf);
            made["element.Water"] = WriteElement("element_water", Hex("#2B86E0"), Drop);
            made["element.Fire"] = WriteElement("element_fire", Hex("#E5492F"), Flame);
            made["element.Earth"] = WriteElement("element_earth", Hex("#B9803F"), (x, y) => Mathf.Abs(x) * 0.9f + Mathf.Abs(y) * 0.75f <= 0.8f);
            made["stone"] = Write("stone", 128, (x, y) => Mathf.Abs(x) * 0.9f + Mathf.Abs(y) * 0.75f <= 0.9f ? Shade(Color.white, x, y) : Color.clear);

            foreach (var g in Gems.All) made["gem." + g] = WriteGem(g);

            made["emberkit"] = WriteCreature("emberkit", new Color(1f, 0.5f, 0.25f), ears: true, tail: true);
            made["leafling"] = WriteCreature("leafling", new Color(0.4f, 0.8f, 0.35f), leaf: true);
            made["tidepup"] = WriteCreature("tidepup", new Color(0.35f, 0.65f, 1f), fin: true);
            made["dunewing"] = WriteCreature("dunewing", new Color(0.82f, 0.62f, 0.32f), wings: true);
            made["psyling"] = WriteCreature("psyling", new Color(0.85f, 0.8f, 0.45f), antenna: true);
            made["azurewing"] = WriteCreature("azurewing", new Color(0.3f, 0.5f, 0.95f), wings: true, fin: true);
            made["azurewing_ascended"] = WriteCreature("azurewing_ascended", new Color(0.25f, 0.85f, 1f), wings: true, crown: true, glow: true);

            made["card.mana_potion"] = WriteIcon("card_mana_potion", new Color(0.25f, 0.45f, 0.9f), Lightning);
            made["card.herbal_salve"] = WriteIcon("card_herbal_salve", new Color(0.25f, 0.65f, 0.3f), Heart);
            made["card.fire_bolt"] = WriteIcon("card_fire_bolt", new Color(0.85f, 0.3f, 0.2f), Flame);
            made["card.summon_sprite"] = WriteIcon("card_summon_sprite", new Color(0.45f, 0.75f, 0.45f), Star);
            made["card.iron_skin"] = WriteIcon("card_iron_skin", new Color(0.55f, 0.35f, 0.8f), ShieldShape);
            made["card.mana_leech"] = WriteIcon("card_mana_leech", new Color(0.35f, 0.35f, 0.5f), YinYangWhite);
            made["card.war_cry"] = WriteIcon("card_war_cry", new Color(0.85f, 0.65f, 0.15f), Sword);
            made["card.meteor"] = WriteIcon("card_meteor", new Color(0.6f, 0.2f, 0.15f), (x, y) => x * x + (y + 0.15f) * (y + 0.15f) < 0.3f || Flame(x * 1.4f - 0.3f, y * 1.4f - 0.4f));
            made["skill.blaze_burst"] = WriteIcon("skill_blaze_burst", new Color(0.95f, 0.45f, 0.1f), Flame);
            made["skill.thorn_bind"] = WriteIcon("skill_thorn_bind", new Color(0.2f, 0.55f, 0.25f), Leaf);
            made["skill.tide_lance"] = WriteIcon("skill_tide_lance", new Color(0.2f, 0.45f, 0.85f), Drop);
            made["skill.mind_spark"] = WriteIcon("skill_mind_spark", new Color(0.7f, 0.6f, 0.2f), Star);
            made["skill.tidal_siphon"] = WriteIcon("skill_tidal_siphon", new Color(0.15f, 0.35f, 0.75f), YinYangWhite);

            AssetDatabase.Refresh();
            var result = new Dictionary<string, Sprite>();
            foreach (var kv in made)
            {
                ConfigureImporter(kv.Value, kv.Key == "ui.round" || kv.Key == "ui.frame");
                result[kv.Key] = AssetDatabase.LoadAssetAtPath<Sprite>(kv.Value);
            }
            return result;
        }

        private static void ConfigureImporter(string path, bool sliced)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = TextureWrapMode.Clamp;
            if (sliced) ti.spriteBorder = new Vector4(24, 24, 24, 24);
            ti.SaveAndReimport();
        }

        public static bool OverwriteExisting;

        private static string Write(string name, int size, Func<float, float, Color> shader, int ss = 3) => WriteTo(Folder + "/" + name + ".png", size, shader, ss);

        private static string WriteTo(string path, int size, Func<float, float, Color> shader, int ss = 3)
        {
            if (!OverwriteExisting && File.Exists(path)) return path;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int pxi = 0; pxi < size; pxi++)
                {
                    Color acc = Color.clear;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = ((pxi + (sx + 0.5f) / ss) / size) * 2f - 1f;
                            float v = ((py + (sy + 0.5f) / ss) / size) * 2f - 1f;
                            var c = shader(u, v);
                            acc += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                        }
                    }
                    acc /= ss * ss;
                    px[py * size + pxi] = acc.a > 0 ? new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a) : Color.clear;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return path;
        }

        private static Color Shade(Color c, float x, float y)
        {
            float k = 0.78f + 0.3f * Mathf.Clamp01((y - x) * 0.5f + 0.5f);
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        }

        private static string WriteGem(GemType g)
        {
            Color baseCol = Pokiwar.App.SpriteLibrary.GemColor(g);
            if (g == GemType.YinYang) baseCol = new Color(0.62f, 0.62f, 0.7f);
            Shape icon = g == GemType.Sword ? Sword : g == GemType.Lightning ? Lightning : g == GemType.Fire ? Flame : g == GemType.Heart ? Heart : g == GemType.Shield ? ShieldShape : (Shape)YinYangOuter;
            return Write("gem_" + g.ToString().ToLowerInvariant(), 128, (x, y) =>
            {
                if (!RoundRect(x, y, 0.94f, 0.94f, 0.3f)) return Color.clear;
                float ix = x / 0.72f, iy = y / 0.72f;
                if (g == GemType.YinYang && YinYangOuter(ix, iy)) return YinYangWhite(ix, iy) ? Color.white : new Color(0.1f, 0.1f, 0.12f);
                if (icon(ix, iy)) return new Color(1f, 1f, 1f);
                if (icon(ix - 0.07f, iy + 0.07f)) return new Color(0, 0, 0, 0.0f) + Shade(baseCol, x, y) * 0.55f + new Color(0, 0, 0, 0.45f);
                if (!RoundRect(x, y, 0.84f, 0.84f, 0.22f)) return Shade(baseCol, x, y) * 0.7f + new Color(0, 0, 0, 0.3f);
                return Shade(baseCol, x, y);
            });
        }

        private static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;

        private static Color Mul(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f);

        /// <summary>Square match-3 gem in the FG38 style: ink outline, bevelled rim, dark emblem with a cream rim light.</summary>
        internal static string WriteSquareGem(GemType g, string folder)
        {
            string hex = g == GemType.Sword ? "#F7C744" : g == GemType.Lightning ? "#188EE9" : g == GemType.Fire ? "#EA473B" : g == GemType.Heart ? "#69CE30" : g == GemType.Shield ? "#9F46D6" : "#F2EEE5";
            Color c = Hex(hex), ink = Hex("#21191F"), cream = Hex("#FFF0BE");
            Shape icon = g == GemType.Sword ? Sword : g == GemType.Lightning ? Lightning : g == GemType.Fire ? Flame : g == GemType.Heart ? Heart : g == GemType.Shield ? ShieldShape : (Shape)YinYangOuter;
            const float s = 0.57f, o = 0.08f;
            bool Emblem(float ex, float ey) => icon(ex / s, ey / s);
            bool Outline(float ex, float ey)
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    if (Emblem(ex + Mathf.Cos(a) * o, ey + Mathf.Sin(a) * o)) return true;
                }
                return false;
            }
            return WriteTo(folder + "/gem_" + g.ToString().ToLowerInvariant() + ".png", 256, (x, y) =>
            {
                if (!RoundRect(x, y, 0.97f, 0.97f, 0.26f)) return Color.clear;
                if (!RoundRect(x, y, 0.88f, 0.88f, 0.2f)) return ink;
                float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
                if (!RoundRect(x, y, 0.66f, 0.66f, 0.1f))
                {
                    if (Mathf.Abs(ax - ay) < 0.022f) return Mul(c, 0.5f);
                    if (y >= ax) return Color.Lerp(c, cream, 0.5f);
                    if (x <= -ay) return Color.Lerp(c, cream, 0.22f);
                    if (y <= -ax) return Mul(c, 0.6f);
                    return Mul(c, 0.78f);
                }
                if (!RoundRect(x, y, 0.62f, 0.62f, 0.08f)) return Mul(c, 0.42f);
                if (g == GemType.YinYang)
                {
                    float yx = x / s, yy = y / s;
                    if (YinYangOuter(yx, yy)) return YinYangWhite(yx, yy) ? cream : ink;
                    if (Outline(x, y)) return ink;
                }
                else
                {
                    if (Emblem(x, y))
                    {
                        Color fill = Color.Lerp(Mul(c, 0.36f), ink, 0.25f);
                        return Emblem(x - 0.045f, y + 0.045f) ? fill : Color.Lerp(fill, cream, 0.55f);
                    }
                    if (Outline(x, y)) return ink;
                }
                float dx = (x + 0.3f) / 0.2f, dy = (y - 0.4f) / 0.075f;
                if (dx * dx + dy * dy < 1f) return Color.Lerp(c, cream, 0.75f);
                float k = Mathf.Clamp01((0.62f - y) / 1.24f);
                return Color.Lerp(Color.Lerp(c, cream, 0.18f), Mul(c, 0.82f), k);
            });
        }

        private static string WriteElement(string name, Color bg, Shape icon)
        {
            var outline = Hex("#21191F");
            return Write(name, 128, (x, y) =>
            {
                float r = x * x + y * y;
                if (r > 0.96f) return Color.clear;
                if (r > 0.74f) return outline;
                if (icon(x / 0.62f, y / 0.62f)) return new Color(1f, 0.97f, 0.88f);
                return Shade(bg, x, y);
            });
        }

        private static string WriteIcon(string name, Color bg, Shape icon)
        {
            return Write(name, 128, (x, y) =>
            {
                if (!RoundRect(x, y, 0.96f, 0.96f, 0.25f)) return Color.clear;
                float ix = x / 0.7f, iy = y / 0.7f;
                if (icon(ix, iy)) return Color.white;
                return Shade(bg, x, y);
            });
        }

        private static string WriteCreature(string name, Color body, bool ears = false, bool tail = false, bool leaf = false, bool fin = false,
            bool wings = false, bool antenna = false, bool crown = false, bool glow = false)
        {
            Color dark = body * 0.55f;
            dark.a = 1f;
            Color belly = Color.Lerp(body, Color.white, 0.55f);
            return Write("pet_" + name, 256, (x, y) =>
            {
                float bx = x / 0.62f, by = (y + 0.25f) / 0.55f;
                bool bodyIn = bx * bx + by * by <= 1f;
                float hx = x / 0.5f, hy = (y - 0.32f) / 0.42f;
                bool headIn = hx * hx + hy * hy <= 1f;
                float ex = Mathf.Abs(x) - 0.2f, ey = y - 0.38f;
                bool eyeW = ex * ex / 0.012f + ey * ey / 0.02f <= 1f;
                bool eyeB = (ex - 0.02f) * (ex - 0.02f) + (ey + 0.01f) * (ey + 0.01f) <= 0.0035f;
                bool eyeShine = (ex - 0.045f) * (ex - 0.045f) + (ey - 0.05f) * (ey - 0.05f) <= 0.0007f;
                if (headIn || bodyIn)
                {
                    if (eyeShine) return Color.white;
                    if (eyeB) return new Color(0.08f, 0.08f, 0.12f);
                    if (eyeW) return Color.white;
                    float mx = x, my = y - 0.2f;
                    if (mx * mx / 0.01f + my * my / 0.002f <= 1f && my < 0) return dark;
                    float blx = x / 0.38f, bly = (y + 0.3f) / 0.32f;
                    if (bodyIn && !headIn && blx * blx + bly * bly <= 1f) return belly;
                    if (Edge(bx, by, bodyIn && !headIn) || Edge(hx, hy, headIn)) return dark;
                    return Shade(body, x, y);
                }
                if (ears && Tri(x, y, -0.42f, 0.6f, -0.18f, 0.72f, -0.38f, 0.98f)) return dark;
                if (ears && Tri(x, y, 0.42f, 0.6f, 0.18f, 0.72f, 0.38f, 0.98f)) return dark;
                if (tail && Tri(x, y, 0.5f, -0.45f, 0.62f, -0.15f, 0.95f, -0.05f)) return new Color(1f, 0.75f, 0.2f);
                if (leaf && Leaf((x - 0.05f) / 0.35f, (y - 0.85f) / 0.25f)) return new Color(0.2f, 0.6f, 0.2f);
                if (fin && Tri(x, y, -0.12f, 0.72f, 0.12f, 0.72f, 0f, 0.98f)) return dark;
                if (wings && (Tri(x, y, -0.55f, -0.1f, -0.55f, 0.3f, -0.98f, 0.55f) || Tri(x, y, 0.55f, -0.1f, 0.55f, 0.3f, 0.98f, 0.55f))) return Color.Lerp(body, Color.white, 0.25f);
                if (antenna && ((x + 0.25f) * (x + 0.25f) + (y - 0.88f) * (y - 0.88f) <= 0.006f || (x - 0.25f) * (x - 0.25f) + (y - 0.88f) * (y - 0.88f) <= 0.006f)) return new Color(0.9f, 0.4f, 0.9f);
                if (crown && y > 0.72f && y < 0.98f && Mathf.Abs(x) < 0.35f && (Mathf.Repeat(x * 6f, 1f) - 0.5f) * 2f * (0.98f - 0.72f) > -(0.98f - y) + 0.0f) return new Color(1f, 0.85f, 0.2f);
                if (glow)
                {
                    float r = Mathf.Sqrt(x * x + y * y);
                    if (r < 0.98f) return new Color(0.6f, 0.95f, 1f, 0.35f * (1f - r));
                }
                return Color.clear;
            });
        }

        private static bool Edge(float x, float y, bool inside) => inside && x * x + y * y >= 0.86f;

        private static string WriteMap()
        {
            return Write("map", 512, (x, y) =>
            {
                Color sea = Color.Lerp(new Color(0.15f, 0.45f, 0.75f), new Color(0.25f, 0.6f, 0.85f), (y + 1f) * 0.5f);
                float i1 = Blob(x + 0.55f, y + 0.35f, 0.42f, 0.3f);
                float i2 = Blob(x + 0.02f, y - 0.25f, 0.38f, 0.32f);
                float i3 = Blob(x - 0.6f, y + 0.15f, 0.36f, 0.4f);
                float m = Mathf.Max(i1, Mathf.Max(i2, i3));
                if (m > 1.0f) return Color.Lerp(new Color(0.35f, 0.7f, 0.3f), new Color(0.25f, 0.55f, 0.25f), Mathf.Clamp01((m - 1f) * 2f));
                if (m > 0.82f) return new Color(0.92f, 0.84f, 0.6f);
                if (m > 0.7f) return Color.Lerp(sea, Color.white, 0.35f);
                return sea;
            }, 1);
        }

        private static float Blob(float x, float y, float rx, float ry)
        {
            float d = (x * x) / (rx * rx) + (y * y) / (ry * ry);
            float wobble = 0.08f * Mathf.Sin(Mathf.Atan2(y, x) * 5f);
            return 1.4f - d + wobble;
        }

        private static bool RoundRect(float x, float y, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x) - (hw - r), qy = Mathf.Abs(y) - (hh - r);
            float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
            return ox * ox + oy * oy <= r * r && Mathf.Abs(x) <= hw && Mathf.Abs(y) <= hh;
        }

        private static bool Tri(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
            float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
            float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0;
            bool pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        private static bool Poly(float x, float y, float[] pts)
        {
            bool inside = false;
            int n = pts.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = pts[i * 2], yi = pts[i * 2 + 1], xj = pts[j * 2], yj = pts[j * 2 + 1];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        private static bool Sword(float x, float y) =>
            Poly(x, y, new[] { 0f, 0.95f, 0.13f, 0.75f, 0.11f, -0.35f, -0.11f, -0.35f, -0.13f, 0.75f }) ||
            (Mathf.Abs(x) < 0.42f && y > -0.47f && y < -0.35f) ||
            (Mathf.Abs(x) < 0.07f && y > -0.85f && y <= -0.47f) ||
            ((x * x) + (y + 0.9f) * (y + 0.9f) < 0.012f);

        private static bool Lightning(float x, float y) =>
            Poly(x, y, new[] { 0.15f, 0.95f, -0.45f, 0.0f, -0.05f, 0.0f, -0.25f, -0.95f, 0.45f, 0.15f, 0.05f, 0.15f, 0.35f, 0.95f });

        private static bool Flame(float x, float y)
        {
            bool bulb = x * x + (y + 0.3f) * (y + 0.3f) < 0.36f;
            bool tip = Tri(x, y, -0.58f, -0.2f, 0.58f, -0.2f, 0.1f, 0.95f);
            bool lick = Tri(x, y, -0.6f, -0.1f, -0.2f, 0.2f, -0.45f, 0.55f);
            return bulb || tip || lick;
        }

        private static bool Heart(float x, float y)
        {
            float hx = x * 1.15f, hy = y * 1.15f + 0.15f;
            float a = hx * hx + hy * hy - 0.5f;
            return a * a * a - hx * hx * hy * hy * hy * 0.9f < 0f;
        }

        private static bool ShieldShape(float x, float y)
        {
            if (y > 0.75f || Mathf.Abs(x) > 0.7f) return false;
            if (y >= -0.05f) return true;
            float k = (y + 0.95f) / 0.9f;
            return Mathf.Abs(x) < 0.7f * Mathf.Sqrt(Mathf.Clamp01(k));
        }

        private static bool YinYangOuter(float x, float y) => x * x + y * y < 0.81f;

        private static bool YinYangWhite(float x, float y)
        {
            if (x * x + y * y >= 0.81f) return false;
            float r = 0.45f;
            bool topSmall = x * x + (y - r) * (y - r) < 0.0225f;
            bool bottomSmall = x * x + (y + r) * (y + r) < 0.0225f;
            if (topSmall) return false;
            if (bottomSmall) return true;
            bool topHalf = x * x + (y - r) * (y - r) < r * r;
            bool bottomHalf = x * x + (y + r) * (y + r) < r * r;
            if (topHalf) return true;
            if (bottomHalf) return false;
            return x < 0;
        }

        private static bool Star(float x, float y)
        {
            float a = Mathf.Atan2(y, x);
            float r = Mathf.Sqrt(x * x + y * y);
            float k = Mathf.Cos(5f * (a - Mathf.PI / 2f));
            return r < 0.42f + 0.38f * Mathf.Pow(Mathf.Max(0, k), 2f);
        }

        private static bool Leaf(float x, float y) => Mathf.Abs(y) < 0.8f * (1f - x * x) && Mathf.Abs(x) < 1f;

        private static bool Drop(float x, float y) => x * x + (y + 0.3f) * (y + 0.3f) < 0.3f || Tri(x, y, -0.5f, -0.1f, 0.5f, -0.1f, 0f, 0.95f);

        private static bool AttackArrowShape(float x, float y) =>
            (x > -0.78f && x < 0.12f && Mathf.Abs(y) < 0.19f) ||
            (x <= -0.78f && (x + 0.78f) * (x + 0.78f) + y * y < 0.19f * 0.19f) ||
            Tri(x, y, 0.0f, 0.56f, 0.0f, -0.56f, 0.92f, 0f);

        private static Color BattleArrow(float x, float y)
        {
            if (AttackArrowShape(x, y))
            {
                float t = Mathf.Clamp01((x + 0.9f) / 1.8f);
                var c = Color.Lerp(Hex("#FFE36B"), Hex("#FF5A1F"), t);
                if (y > 0.04f) c = Color.Lerp(c, Color.white, 0.28f * Mathf.Clamp01(1f - Mathf.Abs(y - 0.12f) * 6f));
                if (y < -0.08f) c = Mul(c, 0.82f);
                return c;
            }
            const float d = 0.075f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                if (AttackArrowShape(x + Mathf.Cos(a) * d, y + Mathf.Sin(a) * d)) return Hex("#3A1606");
            }
            const float g = 0.16f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                if (AttackArrowShape(x + Mathf.Cos(a) * g, y + Mathf.Sin(a) * g)) return new Color(1f, 0.75f, 0.3f, 0.35f);
            }
            return Color.clear;
        }

        private static Color ShieldBubble(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 0.98f) return Color.clear;
            var c = new Color(0.55f, 0.85f, 1f, 0.08f + 0.5f * Mathf.Pow(r, 4f));
            float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.94f) / 0.04f);
            c = Color.Lerp(c, new Color(0.85f, 0.97f, 1f, 0.9f), rim);
            float ca = Mathf.Cos(-0.6f), sa = Mathf.Sin(-0.6f);
            float hx = (x + 0.38f) * ca - (y - 0.45f) * sa, hy = (x + 0.38f) * sa + (y - 0.45f) * ca;
            float spec = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx / 0.075f + hy * hy / 0.022f));
            float dot = Mathf.Clamp01(1f - Mathf.Sqrt((x + 0.12f) * (x + 0.12f) + (y - 0.66f) * (y - 0.66f)) / 0.06f);
            float ang = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            float arc = r > 0.76f && r < 0.84f && ang > -70f && ang < -15f ? 0.35f * Mathf.Clamp01(1f - Mathf.Abs(r - 0.8f) / 0.04f) : 0f;
            float w = Mathf.Clamp01(spec * 1.4f + dot + arc);
            return new Color(Mathf.Lerp(c.r, 1f, w), Mathf.Lerp(c.g, 1f, w), Mathf.Lerp(c.b, 1f, w), Mathf.Max(c.a, w * 0.85f));
        }

        private static bool Arrow(float x, float y) =>
            Tri(x, y, -0.75f, 0.05f, 0.75f, 0.05f, 0f, 0.9f) || (Mathf.Abs(x) < 0.28f && y > -0.85f && y <= 0.1f);
    }
}
