using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using LuckyTower;
using LuckyTower.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BelarusianLocalization
{
    /// <summary>Дадае кнопку «Беларуская» ў LanguageSelector (клон апошняй кнопкі мовы).</summary>
    internal static class LanguageButton
    {
        public static void Add(LanguageSelector selector)
        {
            var tr = Traverse.Create(selector);
            if (tr.Field<bool>("m_initialized").Value) return;
            var buttons = tr.Field<List<LanguageSelector.LanguageButton>>("m_buttons").Value;
            if (buttons == null || buttons.Count == 0) return;
            foreach (var b in buttons)
                if (b.Language == Plugin.BE) return;

            if (Plugin.LogUiDump.Value) Dump(selector, buttons);

            var last = buttons[buttons.Count - 1].Button;
            if (last == null) return;
            var srcGo = last.gameObject;
            // Шэраг (бацька з LayoutGroup) з найменшай колькасьцю кнопак: у гульні 4 + 5 → ставім у першы.
            var counts = new Dictionary<Transform, int>();
            foreach (var b in buttons)
                if (b.Button != null && b.Button.transform.parent != null)
                {
                    counts.TryGetValue(b.Button.transform.parent, out var n);
                    counts[b.Button.transform.parent] = n + 1;
                }
            var parent = srcGo.transform.parent;
            foreach (var kv in counts)
                if (kv.Value < counts[parent]) parent = kv.Key;

            // Клон ствараецца неактыўным, каб Awake ButtonHandler спрацаваў ужо пасьля нашых правак.
            bool wasActive = srcGo.activeSelf;
            srcGo.SetActive(false);
            var go = Object.Instantiate(srcGo, parent, false);
            srcGo.SetActive(wasActive);
            go.name = "LanguageButton_BE";

            foreach (var lt in go.GetComponentsInChildren<LocalizeText>(true)) Object.DestroyImmediate(lt);
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                var nav = button.navigation;
                nav.mode = Navigation.Mode.Automatic;
                button.navigation = nav;
            }
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                t.text = Plugin.LanguageName;
                // «Беларуская» даўжэйшая за іншыя назвы (і рэндэрыцца шырокім Open Sans) — максімум шрыфта ×0.62, палі па 14 px (R: пры ×0.70 налазіла на рамку), адзін радок.
                var max = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
                t.enableAutoSizing = true;
                t.fontSizeMax = max * 0.62f;
                t.margin = new Vector4(14f, t.margin.y, 14f, t.margin.w);
                t.fontSizeMin = Mathf.Min(t.fontSizeMin, t.fontSizeMax * 0.5f);
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }

            go.transform.SetAsLastSibling();
            go.SetActive(wasActive);
            var layout = parent != null ? parent.GetComponent<LayoutGroup>() : null;
            if (layout != null && layout.isActiveAndEnabled)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
            else
                Align(parent, go.transform as RectTransform, buttons);

            object boxed = new LanguageSelector.LanguageButton();
            var bt = Traverse.Create(boxed);
            bt.Field("m_button").SetValue(go.GetComponent<ButtonHandler>());
            bt.Field("m_language").SetValue(Plugin.BE);
            buttons.Add((LanguageSelector.LanguageButton)boxed);
            Plugin.Log.LogInfo($"Кнопка «{Plugin.LanguageName}» дададзеная ({buttons.Count} моў) у {Path(selector.transform)}");
        }

        /// <summary>
        /// LayoutGroup у гульні выключаны, пазыцыі ручныя. Калі ў іншым шэрагу столькі ж кнопак,
        /// колькі цяпер у нашым, — бярэм ягоныя X і памеры (5 + 5); інакш — працягваем крок.
        /// </summary>
        private static void Align(Transform parent, RectTransform clone, List<LanguageSelector.LanguageButton> buttons)
        {
            var rows = new Dictionary<Transform, List<RectTransform>>();
            foreach (var b in buttons)
            {
                var rt = b.Button != null ? b.Button.transform as RectTransform : null;
                if (rt == null || rt.parent == null) continue;
                if (!rows.TryGetValue(rt.parent, out var l)) rows[rt.parent] = l = new List<RectTransform>();
                l.Add(rt);
            }
            if (!rows.TryGetValue(parent, out var ours)) return;
            ours.Sort((x, y) => x.anchoredPosition.x.CompareTo(y.anchoredPosition.x));
            ours.Add(clone);
            foreach (var kv in rows)
            {
                if (kv.Key == parent || kv.Value.Count != ours.Count) continue;
                var refRow = new List<RectTransform>(kv.Value);
                refRow.Sort((x, y) => x.anchoredPosition.x.CompareTo(y.anchoredPosition.x));
                for (int i = 0; i < ours.Count; i++)
                {
                    ours[i].anchoredPosition = new Vector2(refRow[i].anchoredPosition.x, ours[i].anchoredPosition.y);
                    ours[i].sizeDelta = refRow[i].sizeDelta;
                }
                return;
            }
            if (ours.Count >= 3)
            {
                var a = ours[ours.Count - 3].anchoredPosition;
                var b = ours[ours.Count - 2].anchoredPosition;
                clone.anchoredPosition = b + (b - a);
            }
        }

        private static void Dump(LanguageSelector selector, List<LanguageSelector.LanguageButton> buttons)
        {
            var sb = new StringBuilder();
            sb.AppendLine("LanguageSelector " + Path(selector.transform));
            foreach (var b in buttons)
            {
                var rt = b.Button != null ? b.Button.transform as RectTransform : null;
                var sel = b.Button != null ? b.Button.GetComponent<Selectable>() : null;
                sb.AppendLine($"  {b.Language}: {(b.Button != null ? Path(b.Button.transform) : "null")} pos={(rt != null ? rt.anchoredPosition.ToString() : "-")} nav={(sel != null ? sel.navigation.mode.ToString() : "-")}");
            }
            var p = buttons[0].Button != null ? buttons[0].Button.transform.parent : null;
            if (p != null)
            {
                sb.Append("  parent components:");
                foreach (var c in p.GetComponents<Component>()) sb.Append(' ').Append(c.GetType().Name);
                sb.AppendLine();
            }
            Plugin.Log.LogInfo(sb.ToString());
        }

        internal static string Path(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }
    }
}
