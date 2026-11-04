using System;
using System.Collections.Generic;
using HarmonyLib;
using LuckyTower;
using TMPro;

namespace BelarusianLocalization
{
    /// <summary>Runtime-патчы Harmony. Файлы гульні на дыску не мяняюцца.</summary>
    internal static class Patches
    {
        private static readonly AccessTools.FieldRef<LocaDatabase, Dictionary<string, LocaDatabase.TextElement>> KeyValuePairsRef =
            AccessTools.FieldRefAccess<LocaDatabase, Dictionary<string, LocaDatabase.TextElement>>("m_keyValuePairs");
        private static readonly AccessTools.FieldRef<LocaDatabase, Dictionary<string, string>> KeyRedirectorRef =
            AccessTools.FieldRefAccess<LocaDatabase, Dictionary<string, string>>("m_keyRedirector");

        private static readonly HashSet<string> LoggedUiMisses = new HashSet<string>(StringComparer.Ordinal);

        public static void Apply(Harmony h)
        {
            Patch(h, AccessTools.Method(typeof(LocaDatabase), "CreateDictionaries"), prefix: nameof(CreateDictionaries_Prefix));
            Patch(h, AccessTools.Method(typeof(LocaDatabase), nameof(LocaDatabase.SetTextLanguage)), prefix: nameof(ForceLocale_Prefix));
            Patch(h, AccessTools.PropertyGetter(typeof(GlobalSettings), nameof(GlobalSettings.TextLanguage)), postfix: nameof(TextLanguageGetter_Postfix));
            Patch(h, AccessTools.Method(typeof(DataManager), "GetSystemLanguage"), postfix: nameof(GetSystemLanguage_Postfix));
            Patch(h, AccessTools.Method(typeof(GlobalSettings.LocalizedTexts), nameof(GlobalSettings.LocalizedTexts.GetText)), postfix: nameof(UiGetText_Postfix));
            Patch(h, AccessTools.Method(typeof(LocalizedTexts), nameof(LocalizedTexts.GetText)), postfix: nameof(UiGetText2_Postfix));
            Patch(h, AccessTools.Method(typeof(GlobalSettings), nameof(GlobalSettings.UpdateLanguage)), postfix: nameof(UpdateLanguage_Postfix));
            Patch(h, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TMP_Text.text)), prefix: nameof(TmpSetText_Prefix), postfix: nameof(TmpSetText_Postfix));
            Patch(h, AccessTools.Method(typeof(TextMeshProUGUI), "Awake"), postfix: nameof(TmpAwake_Postfix));
            Patch(h, AccessTools.Method(typeof(TextMeshPro), "Awake"), postfix: nameof(TmpAwake_Postfix));
            Patch(h, AccessTools.Method(typeof(LuckyTower.UI.LanguageSelector), nameof(LuckyTower.UI.LanguageSelector.Init)), prefix: nameof(LanguageSelectorInit_Prefix));
        }

        private static void Patch(Harmony h, System.Reflection.MethodBase target, string prefix = null, string postfix = null)
        {
            if (target == null)
            {
                Plugin.Log.LogError($"Патч не прымяніўся: мэтад ня знойдзены (prefix={prefix}, postfix={postfix})");
                return;
            }
            try
            {
                h.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(typeof(Patches), prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(typeof(Patches), postfix));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Патч {target.DeclaringType?.Name}.{target.Name} не прымяніўся: {e}");
            }
        }

        // ---- Дыялёгі: слоўнік TextElement ----

        private static bool CreateDictionaries_Prefix(LocaDatabase __instance, ref LocaDatabase.Locale locale)
        {
            if (Plugin.Forced) locale = Plugin.BE;
            if (locale != Plugin.BE) return true;
            var data = DataManager.Instance.LocaData;
            KeyValuePairsRef(__instance) = BuildDialogue(data.EnValuePairs);
            KeyRedirectorRef(__instance) = data.RedirectPairs;
            return false;
        }

        /// <summary>Копія EN-слоўніка з be-тэкстамі (EN — fallback для адсутных ключоў).</summary>
        internal static Dictionary<string, LocaDatabase.TextElement> BuildDialogue(Dictionary<string, LocaDatabase.TextElement> en)
        {
            var result = new Dictionary<string, LocaDatabase.TextElement>(en.Count, en.Comparer);
            int translated = 0;
            var missing = new List<string>();
            foreach (var kv in en)
            {
                var te = kv.Value;
                if (Plugin.Tr.Dialogue.TryGetValue(kv.Key, out var be))
                {
                    te.Text = be;
                    translated++;
                }
                else if (!string.IsNullOrEmpty(te.Text))
                {
                    missing.Add(kv.Key);
                }
                result[kv.Key] = te;
            }
            Plugin.Log.LogInfo($"BE-слоўнік дыялёгаў: {translated}/{en.Count} перакладзена, {missing.Count} з EN-тэкстам без перакладу");
            DebugLog.UntranslatedKeys("dlg", missing);
            if (Plugin.LogUntranslated.Value)
            {
                var stale = new List<string>();
                foreach (var k in Plugin.Tr.Dialogue.Keys) if (!en.ContainsKey(k)) stale.Add(k);
                DebugLog.UntranslatedKeys("dlg_stale", stale);
            }
            return result;
        }

        private static void ForceLocale_Prefix(ref LocaDatabase.Locale locale)
        {
            if (Plugin.Forced) locale = Plugin.BE;
        }

        private static void TextLanguageGetter_Postfix(ref LocaDatabase.Locale __result)
        {
            if (Plugin.Forced) __result = Plugin.BE;
        }

        private static void GetSystemLanguage_Postfix(ref LocaDatabase.Locale __result)
        {
            if (UnityEngine.Application.systemLanguage == UnityEngine.SystemLanguage.Belarusian) __result = Plugin.BE;
        }

        // ---- UI: GlobalSettings.LocalizedTexts.GetText (default-галіна = EnText) ----

        private static void UiGetText_Postfix(ref string __result, LocaDatabase.Locale language)
        {
            if (!Plugin.IsBE(language)) return;
            __result = UiLookup(__result);
        }

        private static void UiGetText2_Postfix(ref string __result)
        {
            if (!Plugin.CurrentIsBE()) return;
            __result = UiLookup(__result);
        }

        private static string UiLookup(string en)
        {
            if (string.IsNullOrEmpty(en)) return en;
            if (Plugin.Tr.UiByEn.TryGetValue(en, out var be)) return be;
            if (Plugin.LogUntranslated.Value && LoggedUiMisses.Add(en)) DebugLog.Untranslated("ui", en, null);
            return en;
        }

        private static void UpdateLanguage_Postfix(LocaDatabase.Locale language)
        {
            SceneText.ApplyAll(Plugin.IsBE(language));
            DebugLog.State($"language={(int)language} be={Plugin.IsBE(language)}");
        }

        // ---- Сцэнавыя TMP па-за лякалізацыяй ----

        private static void TmpSetText_Prefix(TMP_Text __instance, ref string value)
        {
            if (value == null || value.Length == 0) return;
            if (!Plugin.CurrentIsBE()) return;
            if (Plugin.Tr.Scene.TryGetValue(value, out var be)) { DebugLog.SceneHit(value, __instance); value = be; }
        }

        private static void TmpSetText_Postfix(TMP_Text __instance, string value)
        {
            if (!Plugin.LogUntranslated.Value || value == null) return;
            if (!Plugin.CurrentIsBE()) return;
            DebugLog.CheckVisibleText(__instance, value, "set");
        }

        private static void TmpAwake_Postfix(TMP_Text __instance)
        {
            if (!Plugin.CurrentIsBE()) return;
            SceneText.Apply(__instance, true);
            // LocalizeText перапіша тэкст у Start — такія аб'екты не «па-за лякалізацыяй».
            if (Plugin.LogUntranslated.Value && __instance.GetComponent<LocalizeText>() == null)
                DebugLog.CheckVisibleText(__instance, __instance.text, "awake");
        }

        // ---- Кнопка «Беларуская» ----

        private static void LanguageSelectorInit_Prefix(LuckyTower.UI.LanguageSelector __instance)
        {
            try { LanguageButton.Add(__instance); }
            catch (Exception e) { Plugin.Log.LogError("Кнопка «Беларуская» не дададзеная: " + e); }
        }
    }
}
