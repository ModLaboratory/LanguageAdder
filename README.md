# LanguageAdder

[![latest_release](https://img.shields.io/badge/download-latest_release-blue?logo=github)](https://github.com/ModLaboratory/LanguageAdder/releases/latest)

[简体中文自述文件](README_SChinese.md)

A plugin for adding your custom languages in Among Us.

## Demonstrations

![option_menu](image\english\option_menu.png)

![lang_list](image\english\lang_list.png)

![example](images\english\example.png)

An example for replacing hard-coded texts:

![keyboard_binding](images\keyboard_binding.png)

## Installation

Get the latest version of the mod from [release page](https://github.com/ModLaboratory/LanguageAdder/releases/latest) and download the file you need.

Normally, you just have to download one of the 2 zip files in the release according to your game version. Steam versions and Epic versions before 2025 of Among Us are 32-bit, but later Epic versions of Among Us (after 2024, at least starting from `v16.1.0`, or also known as `v2025.6.10`) are 64-bit, so you must accordingly choose the `zip` file to download, otherwise the mod may not work. Then, you just gotta extract the files to the root directory of the game.

If you have already installed BepInEx for your Among Us, you can directly download the `dll` file and place it under `[GAME_ROOT_DIRECTORY]\BepInEx\plugins\`. If the mod does not work, please try updating the version of BepInEx.

## Usage

1. Open Among Us.
2. When the game shows you main menu, open your Among Us game root directory. Normally, you can see a `Language_Data` folder in your game root directory.
3. In this folder, there are two files: `[YOUR_CURRENT_GAME_LANGUAGE]_Example.lang` `Languages.json`. (Note: when you switched to other languages, an example language file will be automatically generated. You can also press `F1` to generate an example language file in the current game language.)
4. Edit those files according to your need. Below are the descriptions of the files.

### `Languages.json`

The main cofiguration of your custom languages.

#### Example

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

##### Description

- `Language1` `Language2` is the name of the language, which will be shown in the menu.
- Field `path`: The target file of your translation. In the example above, `.\\Language_DATA\\lang1.lang` `.\\Language_DATA\\lang2.lang` is the path of the file.
- Field `base`: The vanilla language that your custom language is based on. In the example above, `English` is the name of the language that your language is based on. (Acceptable values: `English, Latam, Brazilian, Portuguese, Korean, Russian, Dutch, Filipino, French, German, Italian, Japanese, Spanish, SChinese, TChinese, Irish`)
- Field `forceReplacementConfigPath`: Optional. The value is the path of the target configuration file that records the rule to replace the hard-coded game text. That is to say, you can't change the text by only editing the translation file that field `path` points to (e.g. Keyboard Binding Settings). 

### `[LANGUAGE]_Example.lang`

The translations of your custom language. You should rename it like `MyOwnLanguage.lang` before modifying it, or you might lose your changes because generating example language files will override it.

#### Example

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

##### Description

You can modify the translation texts freely.~~Any in-game text can be replaced, which means you can even use this method to add translations of phrases commonly used by foreign players, thereby achieving "automatic translation" of messages from foreign players.~~

### `my_force_text_replacement_rule.json` (The target file that the field `forceReplacementConfigPath` in `Languages.json` points to)

Configuration used to replace the texts that are hard-coded in the game, which you can't find the corresponding strings in `[LANGUAGE]_Example.lang`

#### Example

```json
[
	{
		"key": "Done", 
		"value": "完成"
	},
	{
		"key": "(\\d+)\\s*seconds?\\s+remaining",
		"value": "剩余$1秒"
        "isRegex": true
	}
]
```

##### Description

- Field `isRegex`: Optional. Indicates whether the mod should use Regular Expression to match string to replace. If the value is `true`, the mod will use the value of the field `key` to match every text in the game (even it is translated by the game) and use the value of the field `value` to replace the string matched. In the example above, the second translation, which uses Regular Expression, will match strings like `10 seconds remaining` and replace it with `剩余10秒`.
- Field `key`: The exact match of the text you want to translate.
- Field `value`: The translation.

## Other Features

- You can press `F1` to generate an example language file of the current game language.
- You can press `F2` to reload the configuration of custom languages.
- The custom language setting will be saved. If you restart the game with a custom language set, the mod will help you set the game language to your last custom language.