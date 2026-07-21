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

从[发布页面](https://github.com/ModLaboratory/LanguageAdder/releases/latest)获取最新版本的模组，并下载你需要的文件。

通常情况下，你只需根据你的游戏版本，从发布页面中的两个压缩包文件中选择一个下载。Steam版本以及2025年之前的Epic版本的Among Us是32位的，但之后的Epic版本（2024年之后，至少从 `v16.1.0` 开始，也就是 `v2025.6.10`）是64位的，因此你必须相应地选择 `.zip` 文件进行下载，否则模组可能无法工作。然后，你只需将文件解压到游戏根目录即可。

如果你已经为你的Among Us安装了BepInEx，你可以直接下载 `dll` 文件并将其放置在 `[游戏根目录]\BepInEx\plugins\` 下。如果模组无法工作，请尝试更新BepInEx。

## 使用方法

1. 打开Among Us。
2. 当游戏显示主菜单时，按下 `F1`，然后打开Among Us游戏根目录。通常，你可以在游戏根目录中看到一个 `Language_Data` 文件夹。
3. 在此文件夹中，你可以找到一个示例语言文件夹（例如 `@English_Example`），它是根据你当前游戏语言自动生成的。你也可以随时按 `F1` 来生成一个示例语言文件。
4. 要创建自定义语言：
   - 在 `Language_Data` 文件夹内新建一个文件夹，名称使用你的自定义语言名称（例如 `My English`）。
   - 将示例 `TranslationData.json` 文件复制到此文件夹中。
   - 编辑 `TranslationData.json` 文件，填入你的翻译内容。
   - （可选）在同一文件夹中创建一个 `ReplacementConfigs.json` 文件，用于替换硬编码文本。
5. 按 `F2` 可在不重启游戏的情况下重新加载自定义语言配置。

### 文件夹结构示例

```
D:\Games\Among Us\
├── Among Us_Data\
├── BepInEx\
├── Among Us.exe
├── ...
│
└── Language_Data\
	├── @English_Example\             # 模组生成的示例文件夹
	│   └── TranslationData.json
	├── 简体中文优化版\                # 你的自定义语言文件夹
	│   ├── TranslationData.json      # 你的翻译
	│   └── ReplacementConfigs.json   # （可选）强制替换文本
	└── 简体中文鬼畜版\
		└── TranslationData.json
```

--------

### `TranslationData.json`

自定义语言的翻译文件。此文件应包含键值对，其中键为 `StringNames` 枚举值，值为翻译后的文本。

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

#### 说明

- 键对应游戏内部成员名称，因此请勿编辑它们，否则将无法被找到对应的翻译。
- 值为你所翻译的文本。
- 如果在你的翻译文件中找不到某个字符串，游戏将回退使用英文。

--------

### `CustomReplacementRule.json`（可选）

用于替换 `TranslationData.json` 未覆盖的硬编码文本的配置文件。

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

#### 说明

- 字段 `key`：要匹配的精确文本或正则表达式。
- 字段 `value`：替换后的文本。
- 字段 `isRegex`（可选）：若为 `true`，则 `key` 将被视为正则表达式。在上述示例中，它会匹配类似 `10 seconds remaining` 的字符串，并将其替换为 `剩余10秒`。
- 若 `isRegex` 为 `false` 或省略，则模组执行精确字符串匹配。
- 
--------


<details>
	<summary>旧版配置文件使用方法（已弃用，仅作为参考）</summary>

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
- 字段 `path`：你的翻译文件路径。在上述示例中，`.\\Language_Data\\lang1.lang` 和 `.\\Language_Data\\lang2.lang` 是文件的路径。
- 字段 `base`：你的自定义语言所基于的原始游戏语言。在上述示例中，`English` 是你语言所基于的语言名称。（可接受的值：`English, Latam, Brazilian, Portuguese, Korean, Russian, Dutch, Filipino, French, German, Italian, Japanese, Spanish, SChinese, TChinese, Irish`）
- 字段 `forceReplacementConfigPath`：可选。该值是目标配置文件的路径，该文件记录了替换硬编码游戏文本的规则。也就是说，你无法在 `path` 字段指向的翻译文件中找到你想要翻译的文本（例如，键盘绑定设置界面中的文本）。

### `[语言]_Example.lang`

自定义语言的翻译内容。在修改之前，你应该将其重命名，例如 `MyOwnLanguage.lang`，否则生成示例语言文件时会覆盖你的更改。

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

你可以自由修改翻译文本。~~任意游戏文本都可以替换，也就是说你甚至可以用此方法加入一些外国玩家常用的语录的翻译以实现对外国玩家的消息“自动翻译”。~~

### `my_force_text_replacement_rule.json`（`Languages.json` 中的 `forceReplacementConfigPath` 字段的值指向的目标文件）

用于替换游戏中硬编码文本的配置文件，这些文本你在 `[语言]_Example.lang` 中找不到对应的字符串。

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
- 字段 `key`：你要翻译的文本的精确匹配字符串。
- 字段 `value`：翻译后的文本。

</details>

## 其他功能

- 你可以按 `F1` 键生成当前游戏语言的示例语言文件。
- 你可以按 `F2` 键重新加载自定义语言的配置。
- 自定义语言设置会被保存，以便在下次安装了该模组的游戏会话中自动应用该自定义语言。
- 以 `@` 开头的语言文件夹会被模组忽略。
- 旧版自定义语言注册配置将自动迁移至新格式。如果你对 `Languages.json.old` 感兴趣，可参考`旧版配置文件使用方法`部分。
