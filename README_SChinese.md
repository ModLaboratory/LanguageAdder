# LanguageAdder

[![latest_release](https://img.shields.io/badge/下载-最新版-blue?logo=github)](https://github.com/ModLaboratory/LanguageAdder/releases/latest)

[English README](README.md)

一个用于在Among Us中添加自定义语言的插件。

## 演示

![option_menu](/images/simplified_chinese/option_menu.png)

![lang_list](/images/simplified_chinese/lang_list.png)

![example](/images/simplified_chinese/example.png)

替换硬编码游戏字符串的一个示例：

![keyboard_binding](/images/keyboard_binding.png)

## 安装

从[发布页面](https://github.com/ModLaboratory/LanguageAdder/releases/latest)获取最新版本的模组，并下载您需要的文件。

通常情况下，您只需根据您的游戏版本，从发布页面中的两个压缩包文件中选择一个下载。Steam版本以及2025年之前的Epic版本的Among Us是32位的，但之后的Epic版本（2024年之后，至少从 `v16.1.0` 开始，也就是 `v2025.6.10`）是64位的，因此您必须相应地选择 `zip` 文件进行下载，否则模组可能无法工作。然后，您只需将文件解压到游戏根目录即可。

如果您已经为您的Among Us安装了BepInEx，您可以直接下载 `dll` 文件并将其放置在 `[游戏根目录]\BepInEx\plugins\` 下。如果模组无法工作，请尝试更新BepInEx。

## 使用方法

1. 打开Among Us。
2. 当游戏显示主菜单时，打开您的Among Us游戏根目录。通常，您可以在游戏根目录中看到一个 `Language_Data` 文件夹。
3. 在此文件夹中，有两个文件：`[当前游戏语言]_Example.lang` 和 `Languages.json`。（注意：当您切换到其他语言时，会自动生成一个示例语言文件。您也可以按 `F1` 键，以当前游戏语言生成一个示例语言文件。）
4. 根据您的需要编辑这些文件。以下是对这些文件的说明。

### `Languages.json`

自定义语言的主要配置文件。

#### 示例

```json
{
    "Language1":
    {
        "path": ".\\Language_Data\\lang1.lang",
        "base": "English"
    },
    "Language2":
    {
        "path": ".\\Language_Data\\lang2.lang",
        "base": "English",
        "forceReplacementConfigPath": ".\\Language_Data\\my_force_text_replacement_rule.json"
    }
}
```

##### 说明

- `Language1` 和 `Language2` 是语言的名称，将会在菜单中显示。
- 字段 `path`：您的翻译文件路径。在上述示例中，`.\\Language_Data\\lang1.lang` 和 `.\\Language_Data\\lang2.lang` 是文件的路径。
- 字段 `base`：您的自定义语言所基于的原始游戏语言。在上述示例中，`English` 是您语言所基于的语言名称。（可接受的值：`English, Latam, Brazilian, Portuguese, Korean, Russian, Dutch, Filipino, French, German, Italian, Japanese, Spanish, SChinese, TChinese, Irish`）
- 字段 `forceReplacementConfigPath`：可选。该值是目标配置文件的路径，该文件记录了替换硬编码游戏文本的规则。也就是说，您无法在 `path` 字段指向的翻译文件中找到您想要翻译的文本（例如，键盘绑定设置界面中的文本）。

### `[语言]_Example.lang`

自定义语言的翻译内容。在修改之前，您应该将其重命名，例如 `MyOwnLanguage.lang`，否则生成示例语言文件时会覆盖您的更改。

#### 示例

```json
{
  "None": "STRMISS",
  "BackButton": "Back",
  "AvailableGamesLabel": "Available Games",
  "CreateGameButton": "Create Game",
  "FindGameButton": "Find Game",
  "EnterCode": "Enter Code",
  "GhostIgnoreTasks": "You're dead. Enjoy the chaos.",
  "GhostDoTasks": "You're dead. Finish your tasks to win.",
  "GhostImpostor": "You're dead. You can still sabotage.",
  "ImpostorTask": "Sabotage and kill everyone.",
  "FakeTasks": "Fake Tasks:",
  "TaskComplete": "Task Completed!",
  ...
  "DetectiveNotesPostIt": "Interrogate crew!",
  "DetectiveNotesSuspectNumber": "{0}:"
}
```

##### 说明

您可以自由修改翻译文本。~~任意游戏文本都可以替换，也就是说你甚至可以用此方法加入一些外国玩家常用的语录的翻译以实现对外国玩家的消息“自动翻译”。~~

### `my_force_text_replacement_rule.json`（`Languages.json` 中的 `forceReplacementConfigPath` 字段的值指向的目标文件）

用于替换游戏中硬编码文本的配置文件，这些文本您在 `[语言]_Example.lang` 中找不到对应的字符串。

#### 示例

```json
[
    {
        "key": "Done",
        "value": "完成"
    },
    {
        "key": "(\\d+)\\s*seconds?\\s+remaining",
        "value": "剩余$1秒",
        "isRegex": true
    }
]
```

##### 说明

- 字段 `isRegex`：可选。指示模组是否应使用正则表达式来匹配要替换的文本。如果该值为 `true`，模组将使用 `key` 字段的值匹配游戏中的每个文本（即使它已被游戏翻译过），并使用 `value` 字段的值替换匹配到的字符串。在上述示例中，第二个使用正则表达式的翻译将匹配类似 `10 seconds remaining` 的字符串，并将其替换为 `剩余10秒`。
- 字段 `key`：您要翻译的文本的精确匹配字符串。
- 字段 `value`：翻译后的文本。

## 其他功能

- 您可以按 `F1` 键生成当前游戏语言的示例语言文件。
- 您可以按 `F2` 键重新加载自定义语言的配置。
- 自定义语言设置将会被保存。如果您在设置了自定义语言的情况下重启游戏，模组将帮助您将游戏语言设置为上次使用的自定义语言。