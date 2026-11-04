using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;

namespace BelarusianLocalization
{
    /// <summary>Файлы дэбаг-рэжыму ў BepInEx/BelarusianLocalization/.</summary>
    internal static class DebugLog
    {
        private static string _dir;
        private static readonly HashSet<string> Seen = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Regex Tags = new Regex("<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex Latin = new Regex("[A-Za-z]{3,}", RegexOptions.Compiled);
        private static readonly Regex Digits = new Regex("[0-9]+([.,][0-9]+)?", RegexOptions.Compiled);
        private static readonly Regex Cyrillic = new Regex("[\u0400-\u04FF]", RegexOptions.Compiled);
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static void Init(string dir)
        {
            _dir = dir;
            try { Directory.CreateDirectory(dir); } catch { }
        }

        private static void Append(string file, string line)
        {
            try { File.AppendAllText(Path.Combine(_dir, file), line + "\n", Utf8); } catch { }
        }

        public static void State(string s)
        {
            try { File.WriteAllText(Path.Combine(_dir, "state.txt"), DateTime.Now.ToString("s") + " " + s + "\n", Utf8); } catch { }
            Plugin.Log.LogInfo("state: " + s);
        }

        public static void UntranslatedKeys(string kind, List<string> keys)
        {
            if (!Plugin.LogUntranslated.Value) return;
            try
            {
                File.WriteAllText(Path.Combine(_dir, "untranslated_keys_" + kind + ".log"),
                    $"# {keys.Count} ключоў\n" + string.Join("\n", keys.ToArray()) + "\n", Utf8);
            }
            catch { }
        }

        private static readonly HashSet<string> SceneHitsSeen = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Сцэнавы TMP перакладзены (для праверкі scene_unverified): scene_hits.log, кожны en адзін раз.</summary>
        public static void SceneHit(string en, TMPro.TMP_Text t)
        {
            if (!Plugin.LogUntranslated.Value || !SceneHitsSeen.Add(en)) return;
            Append("scene_hits.log", $"{DateTime.Now:HH:mm:ss}\t{Escape(en)}\t{(t != null ? LanguageButton.Path(t.transform) + " [" + t.gameObject.scene.name + "]" : "")}");
        }

        public static void Untranslated(string kind, string text, string where)
        {
            Append("untranslated_text.log", $"{DateTime.Now:HH:mm:ss}\t{kind}\t{Escape(text)}\t{where ?? ""}");
        }

        /// <summary>Бачны тэкст бяз кірыліцы, але з лацінскімі словамі — кандыдат «па-за лякалізацыяй».</summary>
        public static void CheckVisibleText(TMP_Text t, string value, string src)
        {
            if (string.IsNullOrEmpty(value)) return;
            var plain = Tags.Replace(value, "");
            if (!Latin.IsMatch(plain) || Cyrillic.IsMatch(plain)) return;
            // Аднолькавыя з дакладнасьцю да лічбаў (FPS, лічыльнікі) — адзін раз.
            if (!Seen.Add(Digits.Replace(value, "#"))) return;
            string where = "";
            try { if (t != null) where = LanguageButton.Path(t.transform) + " [" + t.gameObject.scene.name + "]"; } catch { }
            // Дэбаг-панэль і тытры (імёны) — свядома не перакладаюцца (DECISIONS сэсіі 02 п. 10).
            if (where.IndexOf("Canvas - Debug", StringComparison.Ordinal) >= 0 || where.IndexOf("/Credits/", StringComparison.Ordinal) >= 0) return;
            Untranslated("tmp:" + src, value, where);
        }

        public static void MissingGlyph(int unicode, string text, TMP_FontAsset font, TMP_Text comp)
        {
            string where = "";
            try { if (comp != null) where = LanguageButton.Path(comp.transform); } catch { }
            Append("missing_glyphs.log", $"U+{unicode:X4}\t'{char.ConvertFromUtf32(unicode)}'\t{(font != null ? font.name : "null")}\t{where}\t{Escape(text)}");
        }

        private static string Escape(string s) =>
            s == null ? "" : s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }
}
