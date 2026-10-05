using System.Collections.Generic;
using System.IO;
using Pokiwar.Domain;
using UnityEditor;
using UnityEngine;

namespace Pokiwar.EditorTools
{
    /// <summary>Brings the FG38 art set into the project and maps each file onto the sprite keys the game reads.</summary>
    public static class Fg38Art
    {
        public const string Source = "D:/Project/Pokiwar_Art_FG38/Assets";
        public const string Folder = "Assets/Pokiwar/Art/FG38";

        private sealed class Item
        {
            public string Src;
            public string Name;
            public int Max;
            public bool Slice;
            public int Edge;
            public string[] Keys;
        }

        private static Item I(string src, string name, int max, bool slice, params string[] keys) =>
            new Item { Src = src, Name = name, Max = max, Slice = slice, Keys = keys };

        private static Item S(string src, string name, int max, int edge, params string[] keys) =>
            new Item { Src = src, Name = name, Max = max, Slice = true, Edge = edge, Keys = keys };

        private static readonly Item[] Items =
        {
            I("Battle/Background/battle_bg_canyon_v01.png", "bg_battle", 2048, false, "bg.battle"),
            I("Meta/Background/hub_bg_canyon_v01.png", "bg_hub", 2048, false, "bg.hub"),
            I("Meta/Background/map_bg_canyon_v01.png", "bg_map", 2048, false, "bg.map"),
            I("Battle/Board/battle_board_frame_v01.png", "board_frame", 1024, true, "board.frame"),
            I("Battle/Board/battle_board_tile_v01.png", "board_tile", 256, false, "board.tile"),
            I("Battle/Board/battle_tile_selected.png", "board_selected", 256, false, "board.selected"),
            I("Battle/Characters/pet_player_idle_01.png", "char_emberkit", 1024, false, "emberkit"),
            I("Battle/Characters/pet_player_attack_01.png", "char_emberkit_attack", 1024, false, "emberkit.attack"),
            I("Battle/Characters/pet_player_hit_01.png", "char_emberkit_hit", 1024, false, "emberkit.hit"),
            I("Battle/Characters/pet_player_defeat_01.png", "char_emberkit_defeat", 1024, false, "emberkit.defeat"),
            I("Battle/Characters/enemy_beetle_idle_01.png", "char_dunewing", 1024, false, "dunewing"),
            I("Battle/Characters/enemy_beetle_attack_01.png", "char_dunewing_attack", 1024, false, "dunewing.attack"),
            I("Battle/Characters/enemy_beetle_hit_01.png", "char_dunewing_hit", 1024, false, "dunewing.hit"),
            I("Battle/Characters/enemy_beetle_defeat_01.png", "char_dunewing_defeat", 1024, false, "dunewing.defeat"),
            I("Battle/Characters/boss_crystaldrake_form01_idle_01.png", "char_azurewing", 1024, false, "azurewing"),
            I("Battle/Characters/boss_crystaldrake_form02_idle_01.png", "char_azurewing_ascended", 1024, false, "azurewing_ascended"),
            I("Battle/Characters/boss_crystaldrake_form02_attack_01.png", "char_azurewing_ascended_attack", 1024, false, "azurewing_ascended.attack"),
            I("Battle/Characters/boss_crystaldrake_form02_hit_01.png", "char_azurewing_ascended_hit", 1024, false, "azurewing_ascended.hit"),
            I("Battle/Characters/boss_crystaldrake_form02_defeat_01.png", "char_azurewing_ascended_defeat", 1024, false, "azurewing_ascended.defeat"),
            I("Battle/HUD/battle_hud_bar_frame_v01.png", "hud_bar_frame", 1024, true, "hud.bar"),
            I("Battle/HUD/ui_button_confirm_normal.png", "button_green", 1024, true, "ui.button.green"),
            I("Battle/HUD/ui_card_slot_empty.png", "card_slot", 1024, true, "card.slot"),
            I("Battle/QTE/qte_arrow_up.png", "arrow_up", 256, false, "ui.arrow"),
            I("Battle/VFX/vfx_slash_v01.png", "fx_slash", 512, false, "fx.slash"),
            I("Battle/VFX/vfx_heal_v01.png", "fx_heal", 512, false, "fx.heal"),
            I("Battle/VFX/vfx_mana_v01.png", "fx_mana", 512, false, "fx.mana"),
            I("Battle/VFX/vfx_fire_rage_v01.png", "fx_rage", 512, false, "fx.rage"),
            I("Battle/VFX/vfx_shield_v01.png", "fx_shield", 512, false, "fx.shield"),
            I("Battle/VFX/vfx_boss_transform_v01.png", "fx_transform", 1024, false, "fx.transform"),
            I("Cards/card_fire_rage_v01.png", "card_fire_bolt", 512, false, "card.fire_bolt"),
            I("Cards/card_healing_bloom_v01.png", "card_herbal_salve", 512, false, "card.herbal_salve"),
            I("Cards/card_lightning_arc_v01.png", "card_mana_potion", 512, false, "card.mana_potion"),
            I("Cards/card_shield_ward_v01.png", "card_iron_skin", 512, false, "card.iron_skin"),
            I("Cards/card_stone_strike_v01.png", "card_war_cry", 512, false, "card.war_cry"),
            I("Cards/card_summon_sprite_v01.png", "card_summon_sprite", 512, false, "card.summon_sprite"),
            I("Cards/card_mana_leech_v01.png", "card_mana_leech", 512, false, "card.mana_leech"),
            I("Cards/card_meteor_v01.png", "card_meteor", 512, false, "card.meteor"),
            I("Battle/Skills/skill_blaze_burst_v01.png", "skill_blaze_burst", 512, false, "skill.blaze_burst"),
            I("Battle/Skills/skill_thorn_bind_v01.png", "skill_thorn_bind", 512, false, "skill.thorn_bind"),
            I("Battle/Skills/skill_tide_lance_v01.png", "skill_tide_lance", 512, false, "skill.tide_lance"),
            I("Battle/Skills/skill_mind_spark_v01.png", "skill_mind_spark", 512, false, "skill.mind_spark"),
            I("Battle/Skills/skill_tidal_siphon_v01.png", "skill_tidal_siphon", 512, false, "skill.tidal_siphon"),
            I("UI/Elements/element_neutral_v01.png", "element_neutral", 256, false, "element.Neutral"),
            I("UI/Elements/element_metal_v01.png", "element_metal", 256, false, "element.Metal"),
            I("UI/Elements/element_wood_v01.png", "element_wood", 256, false, "element.Wood"),
            I("UI/Elements/element_water_v01.png", "element_water", 256, false, "element.Water"),
            I("UI/Elements/element_fire_v01.png", "element_fire", 256, false, "element.Fire"),
            I("UI/Elements/element_earth_v01.png", "element_earth", 256, false, "element.Earth"),
            I("Battle/Characters/pet_leafling_idle_01.png", "char_leafling", 1024, false, "leafling"),
            I("Battle/Characters/pet_leafling_attack_01.png", "char_leafling_attack", 1024, false, "leafling.attack"),
            I("Battle/Characters/pet_leafling_hit_01.png", "char_leafling_hit", 1024, false, "leafling.hit"),
            I("Battle/Characters/pet_leafling_defeat_01.png", "char_leafling_defeat", 1024, false, "leafling.defeat"),
            I("Battle/Characters/pet_tidepup_idle_01.png", "char_tidepup", 1024, false, "tidepup"),
            I("Battle/Characters/pet_tidepup_attack_01.png", "char_tidepup_attack", 1024, false, "tidepup.attack"),
            I("Battle/Characters/pet_tidepup_hit_01.png", "char_tidepup_hit", 1024, false, "tidepup.hit"),
            I("Battle/Characters/pet_tidepup_defeat_01.png", "char_tidepup_defeat", 1024, false, "tidepup.defeat"),
            I("Battle/Characters/enemy_psyling_idle_01.png", "char_psyling", 1024, false, "psyling"),
            I("Battle/Characters/enemy_psyling_attack_01.png", "char_psyling_attack", 1024, false, "psyling.attack"),
            I("Battle/Characters/enemy_psyling_hit_01.png", "char_psyling_hit", 1024, false, "psyling.hit"),
            I("Battle/Characters/enemy_psyling_defeat_01.png", "char_psyling_defeat", 1024, false, "psyling.defeat"),
            I("Battle/Gems/battle_gem_sword_square_v01.png", "gem_sword_painted", 512, false, "gem.Sword"),
            I("Battle/Gems/battle_gem_lightning_square_v01.png", "gem_lightning_painted", 512, false, "gem.Lightning"),
            I("Battle/Gems/battle_gem_fire_square_v01.png", "gem_fire_painted", 512, false, "gem.Fire"),
            I("Battle/Gems/battle_gem_heart_square_v01.png", "gem_heart_painted", 512, false, "gem.Heart"),
            I("Battle/Gems/battle_gem_shield_square_v01.png", "gem_shield_painted", 512, false, "gem.Shield"),
            I("Battle/Gems/battle_gem_yinyang_square_v01.png", "gem_yinyang_painted", 512, false, "gem.YinYang"),
            S("Meta/UI/ui_reward_panel_v01.png", "popup_reward", 1024, 150, "popup.reward"),
            I("UI/Battle/battle_top_hud_panel_v01.png", "hud_top", 2048, false, "hud.top"),
            I("UI/Battle/battle_timer_medallion_v01.png", "hud_timer", 256, false, "hud.timer"),
            S("UI/Battle/battle_name_ribbon_v01.png", "hud_name", 512, 40, "hud.name"),
            S("UI/Battle/battle_bar_track_v01.png", "hud_track", 512, 20, "hud.track"),
            S("UI/Battle/battle_bar_fill_v01.png", "hud_fill", 512, 16, "hud.fill"),
            I("UI/Battle/battle_turn_arrow_flame_v01.png", "battle_arrow_flame", 512, false, "battle.arrow"),
            I("UI/Battle/battle_shield_bubble_v01.png", "fx_bubble_painted", 512, false, "fx.bubble"),
            I("UI/Battle/battle_skill_frame_v01.png", "skill_frame", 256, false, "skill.frame"),
            I("UI/Battle/text_your_turn_v01.png", "text_your_turn", 1024, false, "text.yourturn"),
            S("UI/Common/ui_button_blue_v01.png", "button_blue", 512, 48, "ui.button.blue"),
            S("UI/Common/ui_button_red_v01.png", "button_red", 512, 48, "ui.button.red"),
            S("UI/Common/ui_button_gray_v01.png", "button_gray", 512, 48, "ui.button.gray"),
            S("UI/Common/ui_button_orange_v01.png", "button_orange", 512, 48, "ui.button.orange"),
            S("UI/Common/ui_panel_dark_v01.png", "panel_dark", 512, 96, "ui.panel"),
            I("UI/Common/ui_button_close_v01.png", "button_close", 256, false, "ui.close"),
            S("UI/Common/ui_title_ribbon_v01.png", "title_ribbon", 1024, 120, "ui.ribbon"),
            I("UI/Common/ui_reward_rays_v01.png", "reward_rays", 1024, false, "ui.rays"),
            S("UI/Room/room_panel_v01.png", "room_panel", 1024, 120, "room.panel"),
            I("UI/Room/room_pet_pedestal_v01.png", "room_stand", 512, false, "room.stand"),
            S("UI/Room/room_card_slot_add_v01.png", "card_add", 512, 40, "card.add"),
        };

        /// <summary>Copies missing files from Source, draws the square gems, and overrides the matching keys in art.</summary>
        public static void Apply(Dictionary<string, Sprite> art)
        {
            Directory.CreateDirectory(Folder);
            var made = new List<Item>();
            foreach (var it in Items)
            {
                string dest = Folder + "/" + it.Name + ".png";
                string src = Path.Combine(Source, it.Src);
                if (!File.Exists(dest) && File.Exists(src)) File.Copy(src, dest);
                if (File.Exists(dest)) made.Add(new Item { Src = dest, Max = it.Max, Slice = it.Slice, Edge = it.Edge, Keys = it.Keys });
            }
            foreach (var g in Gems.All)
            {
                if (made.Exists(m => System.Array.IndexOf(m.Keys, "gem." + g) >= 0)) continue;
                made.Add(new Item { Src = PlaceholderArt.WriteSquareGem(g, Folder), Max = 256, Keys = new[] { "gem." + g } });
            }

            AssetDatabase.Refresh();
            foreach (var it in made)
            {
                Configure(it.Src, it.Max, it.Slice, it.Edge);
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(it.Src);
                if (sp == null) continue;
                foreach (var k in it.Keys) art[k] = sp;
            }
        }

        private static void Configure(string path, int max, bool slice, int edge = 0)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = max;
            ti.textureCompression = max <= 256 ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            ti.spriteBorder = !slice ? Vector4.zero : edge > 0 ? Fixed(path, max, edge) : Border(path, max);
            ti.SaveAndReimport();
        }

        private static Vector4 Fixed(string path, int max, int edge)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            int w = tex.width, h = tex.height;
            Object.DestroyImmediate(tex);
            float scale = Mathf.Min(1f, (float)max / Mathf.Max(w, h));
            float e = Mathf.Min(edge, Mathf.Min(w, h) / 2 - 1) * scale;
            return new Vector4(e, e, e, e);
        }

        private static Vector4 Border(string path, int max)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            int Scan(int x0, int y0, int dx, int dy, int len)
            {
                bool solid = false;
                for (int i = 0; i < len / 2; i++)
                {
                    byte a = px[(y0 + dy * i) * w + (x0 + dx * i)].a;
                    if (a > 128) solid = true;
                    else if (solid && a < 40) return i;
                }
                return -1;
            }
            int l = Scan(0, h / 2, 1, 0, w), r = Scan(w - 1, h / 2, -1, 0, w);
            int b = Scan(w / 2, 0, 0, 1, h), t = Scan(w / 2, h - 1, 0, -1, h);
            if (l < 0 || r < 0) { l = r = (int)(h * 0.45f); }
            if (b < 0 || t < 0) { b = t = (int)(h * 0.35f); }
            float scale = Mathf.Min(1f, (float)max / Mathf.Max(w, h));
            const int pad = 6;
            return new Vector4(Mathf.Min(l + pad, w / 2 - 1), Mathf.Min(b + pad, h / 2 - 1), Mathf.Min(r + pad, w / 2 - 1), Mathf.Min(t + pad, h / 2 - 1)) * scale;
        }
    }
}
