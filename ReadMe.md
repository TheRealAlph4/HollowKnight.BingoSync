# BingoSync

A Hollow Knight mod that automatically marks squares on bingosync.com, and allows modders to add new goals with additional mods, as well as generate custom boards with a hand-picked set of goals.

In the base mod, all goals for the `Normal` and `Item Randomizer` variants are implemented.

## Features
Note: All hotkeys can be customized in the mod settings.

### Automarking
The HUD on the right can be toggled on and off with the keybind `H`.  
Join a bingosync.com board by pasting the link and password into the appropriate fields, choosing a nickname and color, and pressing the `Join Room` button. On this board, goals will get automatically marked upon completion.  
There is a setting to have goals also automatically unmark when their condition is no longer met.  
Note: Automarking only works when the board is revealed in-game. Whether the board is visible or not does not matter. 

### Board display
The board can be revealed either by clicking the `Reveal Card` button in the top right, or by pressing the hotkey `R`.  
The board is displayed in the top right corner, it can be toggled on or off with the hotkey `B`.  
By default, the board display has 3 opacity presets, which can be cycled through with the hotkey `O`.  
There are settings to customize the exact opacity of each preset.  
The `Hand/Brain` button on the right of the `Join Room` button toggles Hand Mode for hand-brain bingo. Since hands are not supposed to see the board, revealing the card in hand-mode hides the board immediately. Players in hand-mode also need to triple-press the board-hotkey to show the board, to avoid accidentally showing it. To make sure that the hand is on the correct board, revealing in hand-mode also automatically sends a message to the room chat, confirming the top left goal.  
Note: BingoSync has the ability to highlight goals on the in-game board, but there is currently no way to use it without other mods.  

### Custom goals, gamemodes and profiles
Modders have the ability to add automarking for new goals. A more detailed guide for the API is planned, for now see [BingoGoalPack1](https://github.com/TheMathGeek314/BingoGoalPack1/) as an example for how this works.  
In these mods, modders can also add gamemodes, which are collections of goals, potentially with custom board generation rules. These gamemodes show up in the bottom right. When the player is connected to a room, the lower right menu can be used to generate boards locally, and send them to the bingosync room as custom boards.  
Players also have the ability to create profiles, which are like gamemodes, but the player can pick out any goals they like from all of the registered goals. Profiles also show up on the bottom right, and have a `*` next to their name, to make it easier to tell profiles and gamemodes apart.  
Generating boards also has the option to use a set seed, and to set the board to lockout or non-lockout.  
Custom profiles are stored as JSON files in `AppData/LocalLow/Team Cherry/Hollow Knight/BingoSync/CustomProfiles/`, and can be shared by simply sending the file and putting it in this folder.  
Note: Profiles are loaded on game start, and saved on exit. When sharing profiles, do it while the game is *not* running. 





