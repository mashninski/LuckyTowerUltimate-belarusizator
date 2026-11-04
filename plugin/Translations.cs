using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace BelarusianLocalization
{
    /// <summary>Пераклады з be.json (зборка: tools/build_bejson.py).</summary>
    internal class Translations
    {
        public string Version = "?";
        public string GameBuild = "?";
        /// <summary>Ключ TextElement → be.</summary>
        public Dictionary<string, string> Dialogue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Ключ UiDictionary → be.</summary>
        public Dictionary<string, string> Ui = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>EN-тэкст UI → be (для GetText без ключа).</summary>
        public Dictionary<string, string> UiByEn = new Dictionary<string, string>(StringComparer.Ordinal);
        /// <summary>EN-тэкст сцэнавага TMP → be (exact-match).</summary>
        public Dictionary<string, string> Scene = new Dictionary<string, string>(StringComparer.Ordinal);
        /// <summary>be → EN для сцэнавых TMP (вяртаньне пры зьмене мовы).</summary>
        public Dictionary<string, string> SceneReverse = new Dictionary<string, string>(StringComparer.Ordinal);
        /// <summary>Усе be-тэксты (каб адрозьніць перакладзенае ад «мімо лякалізацыі»).</summary>
        public HashSet<string> AllBe = new HashSet<string>(StringComparer.Ordinal);

        public static Translations Empty() => new Translations();

        public static Translations Load(string path)
        {
            var t = new Translations();
            var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
            var meta = root["meta"] as JObject;
            if (meta != null)
            {
                t.Version = (string)meta["version"] ?? "?";
                t.GameBuild = (string)meta["game_build"] ?? "?";
            }
            Fill(root["dlg"] as JObject, t.Dialogue);
            Fill(root["ui"] as JObject, t.Ui);
            Fill(root["ui_en"] as JObject, t.UiByEn);
            Fill(root["scene"] as JObject, t.Scene);
            foreach (var kv in t.Scene)
                if (!t.SceneReverse.ContainsKey(kv.Value)) t.SceneReverse[kv.Value] = kv.Key;
            foreach (var d in new[] { t.Dialogue, t.Ui, t.Scene })
                foreach (var v in d.Values) t.AllBe.Add(v);
            return t;
        }

        private static void Fill(JObject obj, Dictionary<string, string> dict)
        {
            if (obj == null) return;
            foreach (var p in obj.Properties())
            {
                var v = (string)p.Value;
                if (!string.IsNullOrEmpty(v)) dict[p.Name] = v;
            }
        }
    }
}
