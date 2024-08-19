# BingoSync

A Hollow Knight mod that automatically marks squares on bingosync.com, and allows modders to add new goals with additional mods, as well as generate custom boards with a hand-picked set of goals.

In the base mod, all goals for the `Normal` and `Item Randomizer` variants are implemented.

## Features

- Goals get automatically marked upon completion (with an option to unmark goals when their condition is no longer met)
- An in-game board display with 3 different opacity settings and a hotkey to cycle through them
- A built-in board generator, with seeded generation and the option to make the board lockout or non-lockout
- API for modders to add new custom goals and gamemodes
- Custom profiles, where players can pick and choose any goals from across gamemodes
- Hand-mode for hand-brain bingo (hides the card upon revealing, and confirms the top left goal as a chat message in the bingosync room, requires triple-pressing the show-board-button to actually show the board)

## Custom goals/gamemodes API

There are 2 groups of methods and 2 classes provided:
- `BingoSync.Variables.*` methods are used to interact with the internal variables tracking goal completion.
- `BingoSync.Goals.*` methods are used to parse and register custom goals, as well as register custom gamemodes.
- `BingoSync.CustomGoals.BingoGoal` represents a single goal. Excluded goals have to be added manually.
- `BingoSync.CustomGoals.GameMode` has a name and a list of goals. If a gamemode needs custom generation (custom weights, custom positions etc.), override the `GameMode.GenerateBoard` method in a child class.
See the source code or the tooltips from the documentation xml for more detailed comments about the individual methods.
Also see [BingoGoalPack1](https://github.com/TheMathGeek314/BingoGoalPack1/) as an example for how the API can be used.

## Dependencies

Requires version `1.8+` of `MagicUI`, `ItemChanger` and `Satchel`
