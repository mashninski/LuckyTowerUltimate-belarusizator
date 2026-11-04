using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LuckyTower;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BelarusianLocalization
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("Lucky Tower Ultimate.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "by.tarask.luckytower.belarusian";
        public const string Name = "Belarusian Localization";
        public const string Version = "1.0.0";

        /// <summary>Новая мова: LocaDatabase.Locale {None..TR=9} → BE = 10.</summary>
        public const LocaDatabase.Locale BE = (LocaDatabase.Locale)10;
        public const string LanguageName = "Беларуская";

        internal static Plugin Instance;
        internal static ManualLogSource Log;
        internal static Translations Tr;
        internal static string PluginDir;
        internal static string DebugDir;

        internal static ConfigEntry<string> ForceLanguage;
        internal static ConfigEntry<bool> LogUntranslated;
        internal static ConfigEntry<bool> LogMissingGlyphs;
        internal static ConfigEntry<bool> LogUiDump;
        internal static ConfigEntry<float> ScreenshotInterval;
        internal static ConfigEntry<string> ScreenshotKey;
        internal static ConfigEntry<bool> AutotestCommands;

        internal static bool Forced => ForceLanguage != null && string.Equals(ForceLanguage.Value, "be", StringComparison.OrdinalIgnoreCase);

        /// <summary>Цяперашняя мова тэксту (з улікам ForceLanguage).</summary>
        internal static bool IsBE(LocaDatabase.Locale language)
        {
            if (language == LocaDatabase.Locale.None)
            {
                var dm = DataManager.Instance;
                if (dm == null || dm.GlobalSettings == null) return Forced;
                language = dm.GlobalSettings.TextLanguage;
            }
            return language == BE;
        }

        internal static bool CurrentIsBE()
        {
            try { return IsBE(LocaDatabase.Locale.None); }
            catch { return Forced; }
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            PluginDir = Path.GetDirectoryName(Info.Location);
            DebugDir = Path.Combine(Paths.BepInExRootPath, "BelarusianLocalization");

            ForceLanguage = Config.Bind("Debug", "ForceLanguage", "",
                "be — заўсёды ўключаць беларускую мову (незалежна ад наладаў гульні). Пуста — звычайны выбар у наладах.");
            LogUntranslated = Config.Bind("Debug", "LogUntranslated", false,
                "Пісаць у BepInEx/BelarusianLocalization/ непераведзеныя ключы і бачныя тэксты па-за лякалізацыяй (лацінка без кірыліцы).");
            LogMissingGlyphs = Config.Bind("Debug", "LogMissingGlyphs", false,
                "Пісаць у BepInEx/BelarusianLocalization/missing_glyphs.log сымбалі, якіх няма ні ў шрыфце, ні ў fallback.");
            LogUiDump = Config.Bind("Debug", "LogUiDump", false,
                "Пісаць у лог структуру сьпісу моў (для наладкі кнопкі «Беларуская»).");
            ScreenshotInterval = Config.Bind("Debug", "ScreenshotInterval", 0f,
                "Аўтаматычны скрыншот кожныя N сэкунд у BepInEx/screenshots/ (0 — выключана).");
            ScreenshotKey = Config.Bind("Debug", "ScreenshotKey", "F11",
                "Клявіша скрыншота (назва з UnityEngine.InputSystem.Key, напр. F11). Пуста — выключана.");
            AutotestCommands = Config.Bind("Debug", "AutotestCommands", false,
                "Выконваць каманды аўтатэсту з BepInEx/BelarusianLocalization/cmd.txt і запыты скрыншотаў з BepInEx/screenshots/request.txt (толькі для распрацоўкі).");

            if (AutotestCommands.Value) Application.runInBackground = true;

            try
            {
                Tr = Translations.Load(Path.Combine(PluginDir, "be.json"));
                Log.LogInfo($"be.json: dlg={Tr.Dialogue.Count} ui={Tr.Ui.Count} ui_en={Tr.UiByEn.Count} scene={Tr.Scene.Count} (v{Tr.Version}, game build {Tr.GameBuild})");
            }
            catch (Exception e)
            {
                Log.LogError("Не атрымалася загрузіць be.json: " + e);
                Tr = Translations.Empty();
            }

            DebugLog.Init(DebugDir);
            FontManager.Init(Path.Combine(PluginDir, "fonts"));

            var harmony = new Harmony(Guid);
            Patches.Apply(harmony);

            SceneManager.sceneLoaded += OnSceneLoaded;
            gameObject.AddComponent<DebugTools>();
            Log.LogInfo($"{Name} {Version} загружаны. ForceLanguage='{ForceLanguage.Value}'");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FontManager.AttachToAll();
            if (CurrentIsBE()) SceneText.ApplyAll(true);
            DebugLog.State($"scene={scene.name} be={CurrentIsBE()}");
        }
    }
}
