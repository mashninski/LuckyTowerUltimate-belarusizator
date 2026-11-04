using TMPro;
using UnityEngine;

namespace BelarusianLocalization
{
    /// <summary>Сцэнавыя TMP па-за сыстэмай лякалізацыі: exact-match en ↔ be.</summary>
    internal static class SceneText
    {
        public static void Apply(TMP_Text t, bool toBe)
        {
            if (t == null) return;
            var text = t.text;
            if (string.IsNullOrEmpty(text)) return;
            if (toBe)
            {
                if (Plugin.Tr.Scene.TryGetValue(text, out var be)) { t.text = be; DebugLog.SceneHit(text, t); }
            }
            else if (Plugin.Tr.SceneReverse.TryGetValue(text, out var en))
            {
                t.text = en;
            }
        }

        public static void ApplyAll(bool toBe)
        {
            if (Plugin.Tr == null || Plugin.Tr.Scene.Count == 0) return;
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (t == null || !t.gameObject.scene.IsValid()) continue;
                Apply(t, toBe);
            }
        }
    }
}
