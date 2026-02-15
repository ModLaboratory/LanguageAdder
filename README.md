# LanguageAdder

A plugin to add your custom languages.

## How To Add Your Custom Languages
0. Get the latest mod release and install it.
1. Start your game.
2. After the game shows you main menu, open your Among Us game root directory. Normally, you can see a `Language_Data` folder in your game root directory.
3. In this folder, there are two files: `(YOUR_CURRENT_GAME_LANGUAGE)_Example.lang` `language.dat`. (Note: when you switched to other languages, an example language file will be automatically generated. You can also press `F1` to generate an example language file in the current game language.)
4. Copy the example file and rename it.
5. Modify the translations in the new file, then save it.
6. Edit `Languages.json`:

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
		"base": "English"
	}
}
 ```

- `Language1` `Language2` is the name of the language, which will be shown in the menu.
- `.\\Language_DATA\\lang1.lang` `.\\Language_DATA\\lang2.lang` is the path of the file.
- `English` is the name of the language that your language is based on. (Acceptable values: `English, Latam, Brazilian, Portuguese, Korean, Russian, Dutch, Filipino, French, German, Italian, Japanese, Spanish, SChinese, TChinese, Irish`)

7. Open your game if you quitted the game; Otherwise, press `F2` to reload language.
8. Then you can switch your language in settings!

## Pull requests are welcome!
