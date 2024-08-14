using MagicUI.Core;
using MagicUI.Elements;
using MagicUI.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BingoSync.GameUI
{
    internal static class BingoBoardUI
    {
        private static readonly List<Board> boards = [];

        private static readonly LayoutRoot commonRoot = new(true, "Persistent layout")
        {
            VisibilityCondition = () => false,
        };
        private static readonly Button revealCardButton = new(commonRoot, "revealCard")
        {
            Content = "Reveal Card",
            FontSize = 15,
            Margin = 20,
            BorderColor = Color.white,
            ContentColor = Color.white,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Padding(20),
            MinWidth = 200,
            Visibility = Visibility.Hidden,
        };
        private static readonly TextObject loadingText = new(commonRoot)
        {
            Text = "Loading...",
            FontSize = 15,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 200,
            Padding = new Padding(20),
            ContentColor = Color.white,
            Visibility = Visibility.Hidden,
        };
        
        private static Action<string> Log;
        private static readonly TextureLoader Loader = new(Assembly.GetExecutingAssembly(), "BingoSync.Resources.Images");

        public static void Setup(Action<string> log)
        {
            Log = log;

            commonRoot.VisibilityCondition = () => true;

            revealCardButton.Click += Controller.RevealButtonClicked;

            Loader.Preload();

            boards.Add(new Board(Loader.GetTexture("BingoSync Transparent Background.png").ToSprite()));
            boards.Add(new Board(Loader.GetTexture("BingoSync Opaque Background.png").ToSprite()));
            boards.Add(new Board(Loader.GetTexture("BingoSync Solid Background.png").ToSprite()));

            commonRoot.ListenForPlayerAction(Controller.GlobalSettings.Keybinds.ToggleBoard, Controller.ToggleBoardKeybindClicked);
            commonRoot.ListenForPlayerAction(Controller.GlobalSettings.Keybinds.RevealCard, Controller.RevealKeybindClicked);
            commonRoot.ListenForPlayerAction(Controller.GlobalSettings.Keybinds.CycleBoardOpacity, Controller.UpdateBoardOpacity);
        }

        public static void UpdateGrid()
        {
            loadingText.Visibility = (!Controller.BoardIsAvailable()) ? Visibility.Visible : Visibility.Hidden;
            revealCardButton.Visibility = (Controller.ClientIsConnected() && Controller.BoardIsAvailable() && !Controller.BoardIsRevealed) ? Visibility.Visible : Visibility.Hidden;
            boards.ForEach(board => board.gridLayout.Visibility = (Controller.BoardIsAvailable() && Controller.BoardIsRevealed) ? Visibility.Visible : Visibility.Hidden);

            if (!Controller.BoardIsAvailable())
            {
                return;
            }

            for (var position = 0; position < Controller.Board.Count; position++)
            {
                boards.ForEach(board => board.bingoLayout[position].Text.Text = Controller.Board[position].Name);
                var colors = Controller.Board[position].Colors.Split(' ').ToList();
                boards.ForEach(board => board.bingoLayout[position].BackgroundColors.Keys.ToList().ForEach(color =>
                {
                    board.bingoLayout[position].BackgroundColors[color].Height = 0;
                }));
                boards.ForEach(board => colors.ForEach(color =>
                {
                    board.bingoLayout[position].BackgroundColors[color].Height = 110 / colors.Count;
                }));
            }
        }

        public static int GetBoardCount()
        {
            return boards.Count;
        }
    }
}