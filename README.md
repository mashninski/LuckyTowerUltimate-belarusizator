# Lucky Tower Ultimate па-беларуску

Поўны аматарскі пераклад гульні **[Lucky Tower Ultimate](https://store.steampowered.com/app/1700270/)** на беларускую мову (клясычны правапіс, тарашкевіца).
Перакладзена ўсё: дыялёгі, мэню, налады, падказкі, канцоўкі і тытры (≈9900 радкоў). Жарты, рыфмы і загадкі не перакладаліся даслоўна, а адаптаваліся.

**Вэрсія перакладу:** 1.0 · **Вэрсія гульні:** Steam build 25727595

## ⬇️ [Спампаваць пераклад](https://github.com/mashninski/LuckyTowerUltimate-belarusizator/releases/latest/download/LuckyTowerUltimate_Belarusian_v1.0.zip)

## Як усталяваць

1. **Спампуй архіў** па спасылцы вышэй (`LuckyTowerUltimate_Belarusian_v1.0.zip`, 1,2 МБ).
2. **Адкрый папку гульні.** У Steam: Бібліятэка → правая кнопка мышы на *Lucky Tower Ultimate* → **Кіраваньне → Прагледзець лякальныя файлы**.
3. **Распакуй архіў у гэтую папку.** Вылучы ўвесь зьмест архіва і перацягні яго ў адкрытую папку гульні. Калі Windows спытае пра замену файлаў, адкажы «Так».
4. **Запусьці гульню** праз Steam, як звычайна.
5. **Уключы беларускую мову:** **Settings → Other → Language → «Беларуская»**. Гульня запомніць выбар.

### Праверка: ці правільна распакавана

Новыя файлы мусяць ляжаць **побач з `Lucky Tower Ultimate.exe`**, а не ва ўкладзенай папцы:

```
Lucky Tower Ultimate\
├── Lucky Tower Ultimate.exe
├── Lucky Tower Ultimate_Data\
├── BepInEx\              ← з архіва
├── winhttp.dll           ← з архіва
├── doorstop_config.ini   ← з архіва
├── .doorstop_version     ← з архіва
├── changelog.txt         ← з архіва
├── README_BE.txt         ← з архіва
└── README_EN.txt         ← з архіва
```

## Калі нешта ня так

| Праблема | Што рабіць |
|---|---|
| Мовы «Беларуская» няма ў сьпісе | Архіў распакаваны ва ўкладзеную папку. Перанясі файлы на ўзровень вышэй, да `Lucky Tower Ultimate.exe`. |
| Гульня адкрылася ў акне | *Налады → Графіка* → поўнаэкранны рэжым. |
| На застаўцы «Failed connecting to Steam.» па-ангельску | Так і мусіць быць: мова ў гэты момант яшчэ не загружаная. |
| Пасьля абнаўленьня гульні частка тэксту па-ангельску | Гэта новыя радкі. Іх дадасьць наступная вэрсія перакладу. |
| Пасьля абнаўленьня гульня не запускаецца | Выдалі `winhttp.dll`. Гульня запусьціцца бяз перакладу. |
| У цябе ўжо ёсьць моды на BepInEx 5 | Скапіюй толькі папку `BepInEx\plugins\BelarusianLocalization\`. |

## Як выдаліць

Выдалі з папкі гульні `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt`, `README_BE.txt`, `README_EN.txt` і папку `BepInEx`.
Захаваньні гульні пры гэтым не чапаюцца.

---

<details>
<summary><b>Па-руску: как установить</b></summary>

1. Скачайте архив по ссылке **«Спампаваць пераклад»** выше.
2. В Steam: Библиотека → правой кнопкой по *Lucky Tower Ultimate* → **Управление → Просмотреть локальные файлы**.
3. Распакуйте всё содержимое архива в эту папку, чтобы `winhttp.dll` и папка `BepInEx` лежали рядом с `Lucky Tower Ultimate.exe`. На вопрос о замене файлов ответьте «Да».
4. Запустите игру и выберите **Settings → Other → Language → «Беларуская»**.

Удаление: удалите `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt`, `README_BE.txt`, `README_EN.txt` и папку `BepInEx`. Сохранения не затрагиваются.
</details>

<details>
<summary><b>In English: how to install</b></summary>

1. Download the archive via the **«Спампаваць пераклад»** link above.
2. In Steam: Library → right-click *Lucky Tower Ultimate* → **Manage → Browse local files**.
3. Extract everything from the archive into that folder, so that `winhttp.dll` and the `BepInEx` folder sit next to `Lucky Tower Ultimate.exe`.
4. Start the game and choose **Settings → Other → Language → «Беларуская»**.

No game files are modified: the translation is loaded at runtime by BepInEx 5 (included), so Steam updates don't remove it. To uninstall, delete `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt`, `README_BE.txt`, `README_EN.txt` and the `BepInEx` folder.
</details>

---

## Пра праект

- Пераклад зроблены толькі з ангельскай, па глясарыі з ≈590 тэрмінаў. Тэкст прайшоў шэсьць праходаў вычыткі: аўтаматычныя праверкі, русізмы і калькі, правапіс і граматыка, каламбуры, адзінства тэрміналёгіі і фінальная выбарка.
- Файлы гульні не мяняюцца. Пераклад падключаецца падчас запуску праз плагін для BepInEx 5 + HarmonyX, шрыфт Open Sans дадае літары і, ў, ґ і ’.
- У рэпазыторыі: `translation/be.json` — пераклад (ключ гульні → радок), `plugin/` — зыходны код плагіна (C#, net472). Арыгінальных рэсурсаў гульні тут няма.

<details>
<summary>Зборка плагіна</summary>

Патрэбныя .NET SDK, BepInEx 5.4.23.5 і ўсталяваная гульня:

```
dotnet build plugin/BelarusianLocalization.csproj -c Release -p:GameManaged="<папка гульні>\Lucky Tower Ultimate_Data\Managed" -p:BepInExCore="<BepInEx>\BepInEx\core"
```

`BelarusianLocalization.dll` і `be.json` кладуцца ў `BepInEx\plugins\BelarusianLocalization\`, шрыфты Open Sans — у `...\BelarusianLocalization\fonts\`.
</details>

## Ліцэнзіі і падзякі

- Пераклад — [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/), код плагіна — MIT ([LICENSE.md](LICENSE.md)).
- Тэрміналёгія абапіраецца на «[Дапаможнік для перакладчыкаў фэнтэзі](https://docs.google.com/document/d/1UXVF6uktrqd1EcK4CvzN9iXP7y7oxuxtvQg0EbRbygg/)» (вэрсія 0.4, CC BY-SA 4.0). Правапіс — «Беларускі клясычны правапіс. Збор правілаў» (2005), праверка — hunspell spell-be-tarask.
- У архіве: BepInEx, HarmonyX, MonoMod, Mono.Cecil (MIT), Unity Doorstop (LGPL-2.1), Open Sans (SIL OFL 1.1).
- Гульня, пэрсанажы і арыгінальны тэкст © Studio Seufz / AMC Games. Неафіцыйны аматарскі праект.
