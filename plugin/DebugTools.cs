using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace BelarusianLocalization
{
    /// <summary>
    /// Штокадравыя задачы: адкладзенае падключэньне fallback-шрыфтоў і скрыншоты
    /// (па таймеры, па клявішы і па запыце-файле BepInEx/screenshots/request.txt для аўтатэсту).
    /// </summary>
    internal class DebugTools : MonoBehaviour
    {
        internal static DebugTools Instance;
        private string _shotDir;
        private float _nextShot;
        private float _nextPoll;
        private DateTime _requestStamp;
        private UnityEngine.InputSystem.Key _key = UnityEngine.InputSystem.Key.None;
        private bool _inputFailed;

        private void Start()
        {
            Instance = this;
            Commands.Init(Plugin.DebugDir);
            _shotDir = Path.Combine(Paths.BepInExRootPath, "screenshots");
            try { Directory.CreateDirectory(_shotDir); } catch { }
            var req = Path.Combine(_shotDir, "request.txt");
            if (File.Exists(req)) _requestStamp = File.GetLastWriteTimeUtc(req);
            var k = Plugin.ScreenshotKey.Value;
            if (!string.IsNullOrEmpty(k))
            {
                try { _key = (UnityEngine.InputSystem.Key)Enum.Parse(typeof(UnityEngine.InputSystem.Key), k, true); }
                catch { Plugin.Log.LogWarning("Невядомая ScreenshotKey: " + k); }
            }
            _nextShot = Time.realtimeSinceStartup + Mathf.Max(5f, Plugin.ScreenshotInterval.Value);
        }

        private void Update()
        {
            FontManager.ProcessPending();
            // Аўтатэст у фоне: гульня не павінна ставіцца на паўзу без фокусу (толькі пры AutotestCommands).
            if (Plugin.AutotestCommands.Value && !Application.runInBackground) Application.runInBackground = true;

            var interval = Plugin.ScreenshotInterval.Value;
            if (interval > 0f && Time.realtimeSinceStartup >= _nextShot)
            {
                _nextShot = Time.realtimeSinceStartup + interval;
                Shot("timer");
            }

            if (_key != UnityEngine.InputSystem.Key.None && !_inputFailed)
            {
                try
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb != null && kb[_key].wasPressedThisFrame) Shot("key");
                }
                catch (Exception e)
                {
                    _inputFailed = true;
                    Plugin.Log.LogWarning("InputSystem недаступны для клявішы скрыншота: " + e.Message);
                }
            }

            if (Plugin.AutotestCommands.Value && Time.realtimeSinceStartup >= _nextPoll)
            {
                _nextPoll = Time.realtimeSinceStartup + 0.5f;
                PollRequest();
                Commands.Poll(this);
            }
        }

        private void PollRequest()
        {
            try
            {
                var req = Path.Combine(_shotDir, "request.txt");
                if (!File.Exists(req)) return;
                var stamp = File.GetLastWriteTimeUtc(req);
                if (stamp == _requestStamp) return;
                _requestStamp = stamp;
                var tag = File.ReadAllText(req).Trim();
                Shot(string.IsNullOrEmpty(tag) ? "request" : Sanitize(tag));
            }
            catch (Exception e) { Plugin.Log.LogWarning("request.txt: " + e.Message); }
        }

        internal void Shot(string tag)
        {
            var name = $"BE_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{tag}.png";
            var path = Path.Combine(_shotDir, name);
            ScreenCapture.CaptureScreenshot(path);
            Plugin.Log.LogInfo("Скрыншот: " + path);
        }

        internal static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Length > 70 ? s.Substring(0, 70) : s;
        }
    }
}
