# LanguageAdder

A plugin to add your custom language.

## How To Add Your Custom Language
0. Get the newest mod release and install it.
1. Start your game.
2. After game shows you main menu, open your Among Us game root directory. In general, you can see a `Language_DATA` folder in your game root directory.
3. In this folder, there are two files: `(YOUR_CURRENT_GAME_LANGUAGE)_Example.lang` `language.dat`
4. Copy your example file and rename it.
5. Change the translations in the new file, then save it.
6. Open `language.dat` as a text file, then edit it:

 ```
 Test Language	C:\Path\To\Your\Game\Language_DATA\test.lang	0
 ```

 `Test Language` is the name of the language, that will be shown in the game.
 `C:\Path\To\Your\Game\Language_DATA\test.lang` is the path of the file.
 `0` is the id of the language that is based on.
 
 *Warning: `Tab` for separating each item!*

7. Open your game if you closed the game; If not, press `F2` to reload language.
8. Then you can switch your language in the game options!

## Known Bugs
[X] Hud buttons' text can not be changed. (Solved)
[ ] Main menu buttons' text can not be changed.

## Pull requests are welcome!

## Others
1. Use `#` to make the plugin ignore this line.
 ```
 # Hello World!
 #AMOGUS
 Test Language	C:\Path\To\Your\Game\Language_DATA\test.lang	0
 ```
 ```
 None	None
 BackButton	Back
 AvailableGamesLabel	Available Games
 # Not translated
 # CreateGameButton	
 ...
 ```