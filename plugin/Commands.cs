using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LuckyTower;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BelarusianLocalization
{
    /// <summary>
    /// Каманды аўтатэсту з файла BepInEx/BelarusianLocalization/cmd.txt (выконваюцца пры зьмене файла).
    /// Радок — каманда:
    ///   wait N           — чакаць N сэкунд;
    ///   shot TAG         — скрыншот у BepInEx/screenshots/;
    ///   click ТЭКСТ      — націснуць актыўную кнопку, у шляху або надпісе якой ёсьць ТЭКСТ (без уліку рэгістра);
    ///   dump             — сьпіс актыўных кнопак і тэкстаў у buttons.txt;
    ///   lang be|en|ru…   — зьмяніць мову тэксту (як кнопка ў наладах, без захаваньня);
    ///   say KEY          — прайграць рэпліку KEY праз DialogueManager гульца (як чыт «talk»: аўдыё + субтытры з тайм-маркерамі);
    ///   sub KEY [N]      — паказаць N-ы фрагмэнт (паміж #) тэксту KEY над гульцом і пакінуць на экране;
    ///   subtext ТЭКСТ    — паказаць адвольны тэкст над гульцом (\n — новы радок);
    ///   subhide          — схаваць субтытр;
    ///   subkeys ФАЙЛ     — для кожнага ключа з ФАЙЛА (у тэчцы BelarusianLocalization) паказаць кожны фрагмэнт і зрабіць скрыншот;
    ///   burst N T [TAG]  — N скрыншотаў праз кожныя T сэкунд (напр. экран загрузкі);
    ///   keys ТЭКСТ       — сьпіс ключоў дыялёгаў, што зьмяшчаюць ТЭКСТ, у keys.txt;
    ///   menu / menuhide  — мэню паўзы (UIManager.ShowMenu / HideMenu);
    ///   gameover / gameoverhide — экран «канец гульні» (толькі UI);
    ///   endscreen K+K…   — экран канцоўкі з тэкстам UI-ключоў (напр. tavern_start+tavern_hubertgood);
    ///   quit             — Application.Quit().
    /// Вынікі — у cmd_result.txt.
    /// </summary>
    internal static class Commands
    {
        private static string _file;
        private static DateTime _stamp;
        private static bool _running;

        public static void Init(string dir)
        {
            _file = Path.Combine(dir, "cmd.txt");
            if (File.Exists(_file)) _stamp = File.GetLastWriteTimeUtc(_file);
        }

        public static void Poll(MonoBehaviour host)
        {
            if (_running || _file == null || !File.Exists(_file)) return;
            var stamp = File.GetLastWriteTimeUtc(_file);
            if (stamp == _stamp) return;
            _stamp = stamp;
            string[] lines;
            try { lines = File.ReadAllLines(_file, Encoding.UTF8); } catch { return; }
            host.StartCoroutine(Run(lines));
        }

        private static void Result(string s)
        {
            Plugin.Log.LogInfo("cmd: " + s);
            try { File.AppendAllText(Path.Combine(Plugin.DebugDir, "cmd_result.txt"), DateTime.Now.ToString("HH:mm:ss") + " " + s + "\n", new UTF8Encoding(false)); } catch { }
        }

        private static IEnumerator Run(string[] lines)
        {
            _running = true;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var sp = line.IndexOf(' ');
                var cmd = (sp < 0 ? line : line.Substring(0, sp)).ToLowerInvariant();
                var arg = sp < 0 ? "" : line.Substring(sp + 1).Trim();
                if (cmd == "subkeys")
                {
                    yield return SubKeys(arg);
                    continue;
                }
                if (cmd == "burst")
                {
                    var a = arg.Split(' ');
                    int n = 5; float t = 1f;
                    if (a.Length > 0) int.TryParse(a[0], out n);
                    if (a.Length > 1) float.TryParse(a[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out t);
                    var tag = a.Length > 2 ? a[2] : "burst";
                    for (int i = 0; i < n; i++)
                    {
                        DebugTools.Instance.Shot($"{tag}{i:D2}");
                        yield return new WaitForSecondsRealtime(t);
                    }
                    Result($"burst {n}");
                    continue;
                }
                if (cmd == "wait")
                {
                    float.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s);
                    yield return new WaitForSecondsRealtime(s);
                    continue;
                }
                try { Exec(cmd, arg); }
                catch (Exception e) { Result($"ERR {line}: {e.Message}"); }
                yield return null;
            }
            Result("done");
            _running = false;
        }

        private static void Exec(string cmd, string arg)
        {
            switch (cmd)
            {
                case "shot":
                    DebugTools.Instance.Shot(string.IsNullOrEmpty(arg) ? "cmd" : arg);
                    Result("shot " + arg);
                    break;
                case "click":
                    Click(arg);
                    break;
                case "dump":
                    Dump();
                    break;
                case "lang":
                    SetLanguage(arg);
                    break;
                case "say":
                    Say(arg);
                    break;
                case "sub":
                    {
                        var a = arg.Split(' ');
                        int seg = 0;
                        if (a.Length > 1) int.TryParse(a[1], out seg);
                        var parts = Segments(a[0]);
                        if (parts == null) { Result("sub NOT FOUND: " + a[0]); break; }
                        ShowSub(parts[Mathf.Clamp(seg, 0, parts.Length - 1)]);
                        Result($"sub {a[0]} {seg}/{parts.Length}");
                    }
                    break;
                case "subtext":
                    ShowSub(arg.Replace("\\n", "\n"));
                    Result("subtext");
                    break;
                case "subhide":
                    {
                        var tb = PlayerTextBox();
                        if (tb != null) tb.gameObject.SetActive(false);
                        Result("subhide");
                    }
                    break;
                case "keys":
                    {
                        var sb = new StringBuilder();
                        foreach (var kv in Plugin.Tr.Dialogue)
                            if (kv.Key.IndexOf(arg, StringComparison.OrdinalIgnoreCase) >= 0) sb.AppendLine(kv.Key + "\t" + kv.Value.Replace("\n", "\\n"));
                        File.WriteAllText(Path.Combine(Plugin.DebugDir, "keys.txt"), sb.ToString(), new UTF8Encoding(false));
                        Result("keys ok");
                    }
                    break;
                case "menu":
                    UIManager.Instance.ShowMenu();
                    Result("menu");
                    break;
                case "menuhide":
                    UIManager.Instance.HideMenu(true);
                    Result("menuhide");
                    break;
                case "gameover":
                    UIManager.Instance.ShowGameOverScreen(true);
                    Result("gameover");
                    break;
                case "gameoverhide":
                    UIManager.Instance.HideGameOverScreen();
                    Result("gameoverhide");
                    break;
                case "endscreen":
                    // Толькі паказ экрана канцоўкі з тэкстам UI-ключоў (KEY+KEY…, як GetBarText); кнопкі без дзеяньняў.
                    {
                        var bar = new StringBuilder();
                        foreach (var k in arg.Split('+'))
                            bar.Append(Plugin.Tr.Ui.TryGetValue(k, out var t) ? t : k.Replace("\\n", "\n")).Append(Environment.NewLine);
                        UIManager.Instance.ShowEndScreen(() => { }, () => { }, bar.ToString());
                    }
                    Result("endscreen " + arg);
                    break;
                case "quit":
                    Result("quit");
                    Application.Quit();
                    break;
                default:
                    Result("unknown " + cmd);
                    break;
            }
        }

        private static IEnumerable<Selectable> ActiveSelectables()
        {
            foreach (var s in Selectable.allSelectablesArray)
                if (s != null && s.isActiveAndEnabled && s.interactable) yield return s;
        }

        private static string Label(Component c)
        {
            var t = c.GetComponentInChildren<TMP_Text>();
            return t != null ? t.text.Replace("\n", " ").Replace("\r", "") : "";
        }

        private static void Click(string what)
        {
            Selectable best = null;
            foreach (var s in ActiveSelectables())
            {
                var path = LanguageButton.Path(s.transform);
                if (Label(s).IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0) { best = s; break; }
            }
            if (best == null) { Result("click NOT FOUND: " + what); return; }
            if (best is Button b) b.onClick.Invoke();
            else if (best is Toggle t) t.isOn = !t.isOn;
            else { Result("click: не Button/Toggle " + best.GetType().Name); return; }
            Result($"click {LanguageButton.Path(best.transform)} «{Label(best)}»");
        }

        private static void Dump()
        {
            var sb = new StringBuilder();
            foreach (var s in ActiveSelectables())
                sb.AppendLine($"{s.GetType().Name}\t{LanguageButton.Path(s.transform)}\t{Label(s)}");
            sb.AppendLine("--- TMP ---");
            foreach (var t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                if (t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text))
                    sb.AppendLine($"{LanguageButton.Path(t.transform)}\t{t.font?.name}\t{t.text.Replace("\n", "\\n").Replace("\r", "\\r")}");
            File.WriteAllText(Path.Combine(Plugin.DebugDir, "buttons.txt"), sb.ToString(), new UTF8Encoding(false));
            Result("dump ok");
        }

        // ---- Субтытры ----

        private static TMP_Text PlayerTextBox()
        {
            var p = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (p == null) return null;
            var ac = p.Components.AudioComponents;
            return ac != null ? ac.TextBox : null;
        }

        private static void Say(string key)
        {
            var p = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (p == null) { Result("say: няма гульца"); return; }
            var dm = p.Components.DialogueManager;
            dm.OverrideKey = key;
            dm.PlayLine();
            dm.OverrideKey = string.Empty;
            Result("say " + key);
        }

        /// <summary>Фрагмэнты тэксту, як іх рэжа DialogueManager.TalkRoutine (па #, «#d.dd» — час).</summary>
        private static string[] Segments(string key)
        {
            if (GameManager.Instance == null) return null;
            var el = GameManager.Instance.LocaDatabase.GetTextElement(key);
            if (el.Key == null || el.Text == null) return null;
            var split = el.Text.Split('#');
            for (int j = 1; j < split.Length; j++)
                if (split[j].Length >= 4 && char.IsDigit(split[j][0])) split[j] = split[j].Substring(4);
            return split;
        }

        private static void ShowSub(string text)
        {
            var tb = PlayerTextBox();
            if (tb == null) { Result("sub: няма TextBox гульца"); return; }
            tb.text = text;
            var bg = tb.transform.parent != null ? tb.transform.parent.GetComponent<TextBackground>() : null;
            if (bg != null) bg.UpdateBackgroundTextManually(text);
            tb.gameObject.SetActive(true);
        }

        private static IEnumerator SubKeys(string file)
        {
            string[] keys;
            try { keys = File.ReadAllLines(Path.Combine(Plugin.DebugDir, file), Encoding.UTF8); }
            catch (Exception e) { Result("subkeys: " + e.Message); yield break; }
            int shots = 0;
            foreach (var raw in keys)
            {
                var key = raw.Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                string[] parts = null;
                try { parts = Segments(key); } catch (Exception e) { Result($"subkeys {key}: {e.Message}"); }
                if (parts == null) { Result("subkeys NOT FOUND: " + key); continue; }
                for (int i = 0; i < parts.Length; i++)
                {
                    try { ShowSub(parts[i]); } catch (Exception e) { Result($"subkeys {key}: {e.Message}"); break; }
                    yield return new WaitForSecondsRealtime(0.4f);
                    DebugTools.Instance.Shot(DebugTools.Sanitize($"sub_{key}_{i}"));
                    shots++;
                    yield return new WaitForSecondsRealtime(0.3f);
                }
            }
            Result($"subkeys {file}: {shots} скрыншотаў");
        }

        private static void SetLanguage(string arg)
        {
            LocaDatabase.Locale loc;
            if (string.Equals(arg, "be", StringComparison.OrdinalIgnoreCase)) loc = Plugin.BE;
            else loc = (LocaDatabase.Locale)Enum.Parse(typeof(LocaDatabase.Locale), arg, true);
            if (GameManager.Instance != null) GameManager.Instance.LocaDatabase.SetTextLanguage(loc);
            DataManager.Instance.GlobalSettings.TextLanguage = loc;
            DataManager.Instance.GlobalSettings.UpdateLanguage(loc);
            Result("lang " + loc);
        }
    }
}
