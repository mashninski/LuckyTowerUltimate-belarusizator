using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BelarusianLocalization
{
    /// <summary>
    /// Fallback-шрыфты TMP (Open Sans, OFL) для літар, якіх няма ў атлясах гульні (і ў ґ …).
    /// Вага падбіраецца па назьве асноўнага шрыфта: Light / SemiBold / Bold / Regular.
    /// </summary>
    internal static class FontManager
    {
        private const string Prefix = "BE-Fallback-";
        private static string _dir;
        private static bool _created;
        private static readonly Dictionary<string, TMP_FontAsset> Fallbacks = new Dictionary<string, TMP_FontAsset>();
        private static readonly HashSet<int> Attached = new HashSet<int>();
        private static readonly Queue<KeyValuePair<TMP_FontAsset, TMP_Text>> Pending = new Queue<KeyValuePair<TMP_FontAsset, TMP_Text>>();
        private static readonly HashSet<string> LoggedGlyphs = new HashSet<string>();

        public static void Init(string dir)
        {
            _dir = dir;
            TMP_Text.OnMissingCharacter += OnMissingCharacter;
        }

        private static void EnsureCreated()
        {
            if (_created) return;
            _created = true;
            foreach (var weight in new[] { "Regular", "Light", "SemiBold", "Bold" })
            {
                var path = Path.Combine(_dir, "OpenSans-" + weight + ".ttf");
                if (!File.Exists(path)) continue;
                try
                {
                    var fa = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                    if (fa == null) { Plugin.Log.LogError("CreateFontAsset вярнуў null: " + path); continue; }
                    fa.name = Prefix + "OpenSans-" + weight;
                    fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    UnityEngine.Object.DontDestroyOnLoad(fa);
                    Fallbacks[weight] = fa;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Шрыфт не загрузіўся: " + path + ": " + e);
                }
            }
            Plugin.Log.LogInfo("Fallback-шрыфты: " + string.Join(", ", new List<string>(Fallbacks.Keys).ToArray()));
            if (Fallbacks.TryGetValue("Regular", out var regular))
            {
                try
                {
                    var global = TMP_Settings.fallbackFontAssets;
                    if (global != null && !global.Contains(regular)) global.Insert(0, regular);
                }
                catch (Exception e) { Plugin.Log.LogWarning("TMP_Settings.fallbackFontAssets: " + e.Message); }
            }
        }

        private static TMP_FontAsset Pick(TMP_FontAsset primary)
        {
            var n = primary.name ?? "";
            string w = "Regular";
            if (n.IndexOf("SemiBold", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Medium", StringComparison.OrdinalIgnoreCase) >= 0) w = "SemiBold";
            else if (n.IndexOf("ExtraBold", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Black", StringComparison.OrdinalIgnoreCase) >= 0) w = "Bold";
            else if (n.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Thin", StringComparison.OrdinalIgnoreCase) >= 0) w = "Light";
            if (Fallbacks.TryGetValue(w, out var fa)) return fa;
            Fallbacks.TryGetValue("Regular", out fa);
            return fa;
        }

        private static bool IsOurs(TMP_FontAsset fa) => fa != null && fa.name != null && fa.name.StartsWith(Prefix, StringComparison.Ordinal);

        /// <returns>true, калі fallback толькі што дададзены.</returns>
        public static bool Attach(TMP_FontAsset primary)
        {
            if (primary == null || IsOurs(primary)) return false;
            if (Attached.Contains(primary.GetInstanceID())) return false;
            EnsureCreated();
            var fb = Pick(primary);
            if (fb == null) return false;
            if (primary.fallbackFontAssetTable == null) primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!primary.fallbackFontAssetTable.Contains(fb))
                primary.fallbackFontAssetTable.Insert(0, fb); // першым — каб кірыліца не зьмешвалася з іншымі fallback
            Attached.Add(primary.GetInstanceID());
            Plugin.Log.LogInfo($"Fallback {fb.name} → {primary.name}");
            return true;
        }

        public static void AttachToAll()
        {
            try
            {
                foreach (var fa in Resources.FindObjectsOfTypeAll<TMP_FontAsset>()) Attach(fa);
            }
            catch (Exception e) { Plugin.Log.LogError("AttachToAll: " + e); }
        }

        private static void OnMissingCharacter(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent)
        {
            if (fontAsset != null && !IsOurs(fontAsset) && !Attached.Contains(fontAsset.GetInstanceID()))
            {
                Pending.Enqueue(new KeyValuePair<TMP_FontAsset, TMP_Text>(fontAsset, textComponent));
                return;
            }
            if (!Plugin.LogMissingGlyphs.Value) return;
            var key = (fontAsset != null ? fontAsset.name : "null") + ":" + unicode;
            if (!LoggedGlyphs.Add(key)) return;
            DebugLog.MissingGlyph(unicode, text, fontAsset, textComponent);
        }

        /// <summary>Выклікаецца з Update: дадаць fallback шрыфтам, якія сустрэліся ў OnMissingCharacter.</summary>
        public static void ProcessPending()
        {
            while (Pending.Count > 0)
            {
                var p = Pending.Dequeue();
                try
                {
                    Attach(p.Key);
                    if (p.Value != null) p.Value.SetAllDirty();
                }
                catch (Exception e) { Plugin.Log.LogError("ProcessPending: " + e); }
            }
        }
    }
}
